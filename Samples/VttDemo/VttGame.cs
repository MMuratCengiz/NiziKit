using System.Diagnostics;
using System.Numerics;
using VttDemo.Model;
using VttDemo.UiKit;
using VttDemo.Views;

namespace VttDemo;

// A virtual tabletop shell: a menu bar, the battle map with floating windows over it, and a status bar.
// Environment switches: VTTDEMO_FONT=<name|mono>, VTTDEMO_THEME=light, VTTDEMO_SCALE=1.5, VTTDEMO_SPAM=<n>, VTTDEMO_OPEN=all,
// VTTDEMO_STATS=1 (print frame stats), VTTDEMO_SCRIPT=1 (drive a scripted tour, print stats, quit).
internal sealed class VttGame() : Game(new GameDesc
{
    Title = "Lanterns & Lore - NiziKit VTT proof of concept",
    Width = 1600,
    Height = 960,
    Resizable = true,
    Graphics = new GraphicsDesc { PresentMode = PresentMode.Fifo }
})
{
    private readonly bool _printStats = Environment.GetEnvironmentVariable("VTTDEMO_STATS") == "1";
    private readonly bool _script = Environment.GetEnvironmentVariable("VTTDEMO_SCRIPT") == "1";
    private readonly Stopwatch _uiTimer = new();
    private readonly Dictionary<Character, CharacterSheet> _sheets = new();

    private RenderFrame _frame = null!;
    private CycledTexture _color = null!;
    private Campaign _campaign = null!;
    private Desktop _desktop = null!;
    private MapView _map = null!;
    private ChatView _chat = null!;
    private InitiativeView _initiative = null!;
    private PartyView _party = null!;
    private JournalView _journal = null!;
    private SettingsDialog _settings = null!;

    private float _uiMs;
    private float _fps = 60;
    private int _frameCount;
    private int _statsFrames;
    private double _statsUi;
    private double _statsUiMax;
    private long _statsAllocated;

    protected override void Load()
    {
        _frame = new RenderFrame();
        _color = CycledTexture.ColorAttachment("VttColor");

        _campaign = new Campaign();
        _campaign.Toast += (text, color) => Ui.Notify(text, color);
        _map = new MapView(_campaign);
        _map.OpenSheet += OpenSheet;
        _chat = new ChatView(_campaign);
        _initiative = new InitiativeView(_campaign, _map, OpenSheet);
        _party = new PartyView(_campaign, _map, OpenSheet);
        _journal = new JournalView(_campaign);
        _journal.Window.IsOpen = false;
        _settings = new SettingsDialog(_map);

        _desktop = new Desktop
        {
            Flex = 1,
            Children = [_map, SceneBanner(), MapToolbar()],
            Windows = [_party.Window, _initiative.Window, _journal.Window, _chat.Window]
        };
        Ui.Root = new Column
        {
            Flex = 1,
            Background = Color.Background,
            Children = [MenuBar(), _desktop, StatusBar()]
        };

        var font = Environment.GetEnvironmentVariable("VTTDEMO_FONT");
        var fontIndex = font == "mono" ? 0 : TextFonts.IndexOf(font ?? "Verdana");
        TextFonts.Apply(fontIndex >= 0 ? fontIndex : Math.Min(1, TextFonts.Available.Length - 1));
        if (Environment.GetEnvironmentVariable("VTTDEMO_THEME") == "light")
        {
            _settings.SetTheme(true);
        }

        if (float.TryParse(Environment.GetEnvironmentVariable("VTTDEMO_SCALE"), System.Globalization.CultureInfo.InvariantCulture, out var scale))
        {
            Ui.Scale = scale;
        }

        OpenSheet(_campaign.Characters[0]);
        Arrange(Ui.ViewportSize - new Vector2(0, 44 + 26));
        if (Environment.GetEnvironmentVariable("VTTDEMO_OPEN") == "all")
        {
            OpenEverything();
        }

        if (int.TryParse(Environment.GetEnvironmentVariable("VTTDEMO_SPAM"), out var spam))
        {
            _chat.Spam(spam);
        }

        _statsAllocated = GC.GetAllocatedBytesForCurrentThread();
    }

    protected override void Render(float dt)
    {
        _frameCount++;
        RunScript();

        _frame.BeginFrame();
        _uiTimer.Restart();
        Ui.Update(dt);
        var uiMs = _uiTimer.Elapsed.TotalMilliseconds;

        var pass = _frame.BeginGraphicsPass();
        pass.SetRenderTarget(0, _color, LoadOp.Clear);
        pass.Begin();
        pass.BeginRendering();
        Ui.Render(pass.CommandList);
        pass.End();
        _frame.Submit();
        _frame.Present(_color);

        UpdateStats(dt, uiMs);
    }

    protected override void OnShutdown()
    {
        GraphicsContext.WaitIdle();
        _color.Dispose();
        _frame.Dispose();
    }

    protected override void OnEvent(ref Event ev)
    {
        switch (ev.Type)
        {
            case EventType.KeyDown:
                OnKey(ev.Key);
                break;
            case EventType.MouseWheel when _map.IsHovered:
                _map.ZoomAt(Ui.PointerPosition, ev.MouseWheel.Y);
                break;
        }
    }

    private void OnKey(in KeyboardEventData key)
    {
        const uint command = (uint)(KeyMod.Ctrl | KeyMod.Lctrl | KeyMod.Rctrl | KeyMod.Gui | KeyMod.Lgui | KeyMod.Rgui);
        if ((key.Mod & command) != 0)
        {
            switch (key.KeyCode)
            {
                case KeyCode.Equals or KeyCode.Plus or KeyCode.KeypadPlus:
                    SetScale(Ui.Scale + 0.1f);
                    break;
                case KeyCode.Minus or KeyCode.KeypadMinus:
                    SetScale(Ui.Scale - 0.1f);
                    break;
                case KeyCode.Num0:
                    SetScale(1);
                    break;
                case KeyCode.Q:
                    Quit();
                    break;
            }

            return;
        }

        switch (key.KeyCode)
        {
            case KeyCode.Return or KeyCode.KeypadEnter:
                _chat.Window.Show();
                _chat.FocusInput();
                break;
            case KeyCode.N:
                _campaign.NextTurn();
                break;
            case KeyCode.G:
                _map.ShowGrid = !_map.ShowGrid;
                break;
            case KeyCode.F2:
                ToggleOutlines();
                break;
            case KeyCode.Escape:
                _map.Selected = null;
                break;
        }
    }

    private static void SetScale(float scale)
    {
        Ui.Scale = Math.Clamp(scale, 0.75f, 1.75f);
    }

    private static void ToggleOutlines()
    {
        Ui.DebugFlags = Ui.DebugFlags == UIDebugFlagBits.None ? UIDebugFlagBits.Outlines : UIDebugFlagBits.None;
    }

    private void OpenSheet(Character character)
    {
        if (!_sheets.TryGetValue(character, out var sheet))
        {
            sheet = new CharacterSheet(_campaign, character);
            var step = _sheets.Count % 6 * 30;
            var height = _desktop.Area.Y > 0 ? MathF.Min(680, _desktop.Area.Y - 64 - step) : 660;
            sheet.Window.Rect = new Rectangle(316 + step, 52 + step, 620, height);
            _sheets[character] = sheet;
            _desktop.Add(sheet.Window);
        }

        sheet.Window.Show();
    }

    private bool IsSheetOpen(Character character)
    {
        return _sheets.TryGetValue(character, out var sheet) && sheet.Window.IsOpen;
    }

    private void OpenEverything()
    {
        foreach (var character in _campaign.Party)
        {
            OpenSheet(character);
        }

        _journal.Window.Show();
        _chat.Window.Show();
    }

    private void Arrange(Vector2 area)
    {
        foreach (var window in _desktop.Windows)
        {
            window.Collapsed = false;
        }

        _party.Window.Rect = new Rectangle(12, 12, 290, 300);
        _initiative.Window.Rect = new Rectangle(12, 324, 290, MathF.Max(200, area.Y - 324 - 64));
        _chat.Window.Rect = new Rectangle(MathF.Max(320, area.X - 422), 12, 410, MathF.Max(300, area.Y - 24));
        _journal.Window.Rect = new Rectangle(MathF.Max(330, area.X * 0.5f - 280), 70, 560, MathF.Min(660, area.Y - 100));
        foreach (var (i, sheet) in _sheets.Values.Index())
        {
            var step = i % 6 * 30;
            sheet.Window.Rect = new Rectangle(316 + step, 52 + step, 620, MathF.Min(680, area.Y - 64 - step));
        }
    }

    private Widget MenuBar()
    {
        return new Row
        {
            Height = 44,
            Padding = (10, 0),
            Gap = 4,
            Background = Color.Surface,
            Border = Border.Bottom(Color.Border),
            Children =
            [
                new Row
                {
                    Width = 28,
                    Height = 28,
                    Justify = UIJustify.Center,
                    Background = Color.Accent,
                    Radius = 7,
                    Children = [new Icon(FontAwesome.Cube) { Size = 14, Color = Color.OnAccent }]
                },
                new Column
                {
                    Margin = (4, 0, 12, 0),
                    Children = [new Label("Lanterns & Lore"), new Label("NiziKit VTT proof of concept", TextStyle.Caption)]
                },
                new MenuButton("Game", GameMenu()),
                new MenuButton("Windows", WindowsMenu()),
                new MenuButton("View", ViewMenu()),
                new Spacer(),
                new Label("At the table:", TextStyle.Small),
                new Row
                {
                    Gap = 4,
                    Margin = (4, 0, 8, 0),
                    Children = [.. _campaign.Party.Select(Presence)]
                },
                Kit.IconButton(FontAwesome.Gear, "Settings", _settings.Show)
            ]
        };
    }

    private static Widget Presence(Character character)
    {
        var portrait = Kit.Portrait(character, 26);
        portrait.Tooltip = character.Name;
        return portrait;
    }

    private Menu GameMenu()
    {
        return new Menu
        {
            Items =
            [
                new MenuItem("Roll initiative", FontAwesome.Cube, "/init") { OnClick = _campaign.RollInitiative },
                new MenuItem("Next turn", FontAwesome.AngleRight, "N") { OnClick = _campaign.NextTurn },
                new MenuItem("Previous turn", FontAwesome.AngleLeft) { OnClick = _campaign.PreviousTurn },
                new MenuSeparator(),
                new MenuItem("Stress test: post 500 messages", FontAwesome.ChartBar, "/spam") { OnClick = () => _chat.Spam(500) },
                new MenuItem("Settings…", FontAwesome.Gear) { OnClick = _settings.Show },
                new MenuSeparator(),
                new MenuItem("Quit", FontAwesome.Power, "Ctrl+Q") { OnClick = Quit }
            ]
        };
    }

    private Menu WindowsMenu()
    {
        return new Menu
        {
            Items =
            [
                WindowItem(_chat.Window, "Enter"),
                WindowItem(_initiative.Window),
                WindowItem(_party.Window),
                WindowItem(_journal.Window),
                new MenuSeparator(),
                new ForEach<Character>(() => _campaign.Party, pc => new MenuItem(pc.Name)
                {
                    Checked = Bind(() => IsSheetOpen(pc)),
                    OnClick = () => OpenSheet(pc)
                }),
                new MenuSeparator(),
                new MenuItem("Open everything", FontAwesome.LayerGroup) { OnClick = OpenEverything },
                new MenuItem("Default layout", FontAwesome.TableCells) { OnClick = () => Arrange(_desktop.Area) },
                new MenuItem("Cascade", FontAwesome.Bars) { OnClick = _desktop.Cascade },
                new MenuItem("Close all sheets", FontAwesome.WindowClose)
                {
                    OnClick = () =>
                    {
                        foreach (var sheet in _sheets.Values)
                        {
                            sheet.Window.Close();
                        }
                    }
                }
            ]
        };
    }

    private static MenuItem WindowItem(FloatingWindow window, string? shortcut = null)
    {
        return new MenuItem(window.Title, shortcut: shortcut) { Checked = Bind(() => window.IsOpen), OnClick = window.Toggle };
    }

    private Menu ViewMenu()
    {
        return new Menu
        {
            Items =
            [
                new MenuItem("Zoom map in", FontAwesome.Plus, "Wheel") { OnClick = () => _map.ZoomBy(1) },
                new MenuItem("Zoom map out", FontAwesome.Minus, "Wheel") { OnClick = () => _map.ZoomBy(-1) },
                new MenuItem("Reset map view", FontAwesome.Compress) { OnClick = _map.ResetView },
                new MenuItem("Show grid", FontAwesome.BorderAll, "G") { Checked = Bind(() => _map.ShowGrid), OnClick = () => _map.ShowGrid = !_map.ShowGrid },
                new MenuSeparator(),
                new MenuItem("Larger interface", FontAwesome.MagnifyingGlass, "Ctrl +") { OnClick = () => SetScale(Ui.Scale + 0.1f) },
                new MenuItem("Smaller interface", shortcut: "Ctrl -") { OnClick = () => SetScale(Ui.Scale - 0.1f) },
                new MenuItem("Actual size", shortcut: "Ctrl 0") { OnClick = () => SetScale(1) },
                new MenuSeparator(),
                new MenuItem("Light theme", FontAwesome.Sun) { Checked = Bind(() => _settings.Light), OnClick = () => _settings.SetTheme(!_settings.Light) },
                new MenuItem("Element outlines", FontAwesome.VectorSquare, "F2") { Checked = Bind(() => Ui.DebugFlags != UIDebugFlagBits.None), OnClick = ToggleOutlines }
            ]
        };
    }

    private Widget SceneBanner()
    {
        var dim = Color.Hex(0xFFFFFF60);
        var soft = Color.Hex(0xFFFFFFB0);
        return new Row
        {
            Float = UIAnchor.TopCenter,
            Offset = new Vector2(0, 12),
            PassThrough = true,
            Padding = (14, 6),
            Gap = 10,
            Background = Color.Hex(0x0B0C10E0),
            Border = Color.Hex(0xFFFFFF1A),
            Radius = 16,
            Children =
            [
                new Icon(FontAwesome.LocationCrosshairs) { Size = 11, Color = Campaign.GmColor },
                new Label(_campaign.SceneName, TextStyle.Sized(13, Palette.White)),
                new Label("·", TextStyle.Sized(13, dim)),
                new Label(() => $"Round {_campaign.Round}", TextStyle.Sized(13, soft)),
                new Row
                {
                    Gap = 10,
                    Visible = Bind(() => _campaign.Current != null),
                    Children =
                    [
                        new Label("·", TextStyle.Sized(13, dim)),
                        new Row { Width = 8, Height = 8, Radius = 4, Background = Bind(() => _campaign.Current?.Color ?? default) },
                        new Label(() => $"{_campaign.Current?.Name}'s turn", TextStyle.Sized(13, soft))
                    ]
                }
            ]
        };
    }

    private Widget MapToolbar()
    {
        return new Row
        {
            Float = UIAnchor.BottomLeft,
            Offset = new Vector2(12, -12),
            Padding = 4,
            Gap = 2,
            Background = Color.Surface,
            Border = Color.Border,
            Radius = 8,
            Children =
            [
                Kit.IconButton(FontAwesome.Minus, "Zoom out", () => _map.ZoomBy(-1)),
                new Label(() => $"{_map.Zoom * 100:0}%", TextStyle.Small) { Width = 46, Align = UITextAlign.Center },
                Kit.IconButton(FontAwesome.Plus, "Zoom in", () => _map.ZoomBy(1)),
                Kit.IconButton(FontAwesome.Compress, "Reset view", _map.ResetView),
                new Divider(),
                new Button
                {
                    Icon = FontAwesome.BorderAll,
                    Tone = Tone.Ghost,
                    Tooltip = "Toggle grid (G)",
                    Checked = Bind(() => _map.ShowGrid, on => _map.ShowGrid = on)
                }
            ]
        };
    }

    private Widget StatusBar()
    {
        return new Row
        {
            Height = 26,
            Padding = (12, 0),
            Gap = 16,
            Background = Color.Surface,
            Border = Border.Top(Color.Border),
            Children =
            [
                new Row
                {
                    Gap = 6,
                    Children =
                    [
                        new Row { Width = 7, Height = 7, Radius = 4, Background = Palette.Good },
                        new Label("Connected (simulated)", TextStyle.Sized(11, Color.TextMuted))
                    ]
                },
                new Label("Enter: chat · N: next turn · G: grid · Ctrl +/-: UI scale · F2: outlines · right-click tokens and messages",
                    TextStyle.Sized(11, Color.TextDisabled)) { Flex = 1 },
                new Label(() => $"{_fps:0} fps · UI {_uiMs:0.00} ms · {_campaign.Chat.Messages.Count} messages · {_desktop.OpenCount} windows · {Ui.Scale * 100:0}%",
                    TextStyle.Sized(11, Color.TextMuted))
            ]
        };
    }

    private void UpdateStats(float dt, double uiMs)
    {
        if (dt > 0)
        {
            _fps += (1 / dt - _fps) * 0.05f;
        }

        _uiMs += ((float)uiMs - _uiMs) * 0.1f;
        _statsFrames++;
        _statsUi += uiMs;
        _statsUiMax = Math.Max(_statsUiMax, uiMs);
        if (_statsFrames < 120)
        {
            return;
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread();
        if (_printStats || _script)
        {
            Console.WriteLine(
                $"frame {_frameCount}: ui {_statsUi / _statsFrames:0.000} ms avg, {_statsUiMax:0.000} ms max, {(allocated - _statsAllocated) / _statsFrames} B/frame, {_campaign.Chat.Messages.Count} messages, {_desktop.OpenCount} windows, scale {Ui.Scale:0.00}");
        }

        _statsAllocated = allocated;
        _statsFrames = 0;
        _statsUi = 0;
        _statsUiMax = 0;
    }

    // Prints whether every open window's shown rectangle lies inside the desktop area.
    private void CheckLayout(string label)
    {
        var area = _desktop.Area;
        var outside = new List<string>();
        foreach (var window in _desktop.Windows)
        {
            var r = window.ShownRect;
            if (window.IsOpen && (r.X < -1 || r.Y < -1 || r.Right > area.X + 1 || r.Bottom > area.Y + 1))
            {
                outside.Add($"{window.Title} {r.X:0},{r.Y:0} {r.Width:0}x{r.Height:0}");
            }
        }

        var chat = _chat.Window.ShownRect;
        Console.WriteLine(
            $"layout {label}: area {area.X:0}x{area.Y:0}, chat at {chat.X:0},{chat.Y:0} {chat.Width:0}x{chat.Height:0} (asked {_chat.Window.Rect.X:0},{_chat.Window.Rect.Y:0}), " +
            (outside.Count == 0 ? $"all {_desktop.OpenCount} windows inside" : $"outside: {string.Join("; ", outside)}"));
    }

    // A scripted tour for unattended runs: opens everything, flips through tabs, floods the chat, rescales, re-themes and quits.
    private void RunScript()
    {
        if (!_script)
        {
            return;
        }

        switch (_frameCount)
        {
            case 120:
                OpenEverything();
                break;
            case 360:
                _chat.Spam(1000);
                break;
            case 590:
                CheckLayout("start at 1x");
                break;
            case 600:
                SetScale(1.5f);
                _campaign.RollInitiative();
                break;
            case 660:
                CheckLayout("zoomed to 1.5x");
                SetScale(1);
                break;
            case 720:
                CheckLayout("back to 1x");
                _settings.SetTheme(!_settings.Light);
                break;
            case 840:
                Quit();
                break;
        }

        if (_frameCount > 120 && _frameCount % 40 == 0)
        {
            foreach (var sheet in _sheets.Values)
            {
                sheet.SelectedTab = (sheet.SelectedTab + 1) % sheet.TabCount;
            }
        }
    }
}
