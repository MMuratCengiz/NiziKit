using System.Diagnostics;
using System.Numerics;
using NiziKit.UI.Widgets;
using VttDemo.Model;
using VttDemo.UiKit;
using VttDemo.Views;

namespace VttDemo;

// A virtual tabletop shell: a menu bar, the battle map with floating windows over it, and a status bar.
// Environment switches: VTTDEMO_FONT=<name|mono>, VTTDEMO_THEME=light, VTTDEMO_SPAM=<n>, VTTDEMO_OPEN=all,
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
    private const float MenuBarHeight = 44;
    private const float StatusBarHeight = 26;

    private readonly bool _printStats = Environment.GetEnvironmentVariable("VTTDEMO_STATS") == "1";
    private readonly bool _script = Environment.GetEnvironmentVariable("VTTDEMO_SCRIPT") == "1";
    private readonly Stopwatch _uiTimer = new();
    private readonly Dictionary<Character, CharacterSheet> _sheets = new();
    private readonly uint _rootId = Ui.Id("Vtt.Root");
    private readonly uint _areaId = Ui.Id("Vtt.Area");

    private RenderFrame _frame = null!;
    private CycledTexture _color = null!;
    private Campaign _campaign = null!;
    private Desktop _desktop = null!;
    private MapView _map = null!;
    private ChatView _chat = null!;
    private InitiativeView _initiative = null!;
    private PartyView _party = null!;
    private JournalView _journal = null!;
    private Toasts _toasts = null!;
    private ConfirmDialog _confirm = null!;
    private SettingsDialog _settings = null!;
    private Menu _gameMenu = null!;
    private Menu _windowsMenu = null!;
    private Menu _viewMenu = null!;
    private Button _gameButton = null!;
    private Button _windowsButton = null!;
    private Button _viewButton = null!;
    private Button _settingsButton = null!;
    private Button _zoomOut = null!;
    private Button _zoomIn = null!;
    private Button _zoomReset = null!;
    private Button _grid = null!;

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

        _toasts = new Toasts();
        _campaign = new Campaign();
        _campaign.Toast += (text, color) => _toasts.Show(text, color);
        _desktop = new Desktop();
        _confirm = new ConfirmDialog();
        _map = new MapView(_campaign);
        _map.OpenSheet += OpenSheet;

        _chat = new ChatView(_campaign, _confirm);
        _initiative = new InitiativeView(_campaign, _map, OpenSheet);
        _party = new PartyView(_campaign, _map, OpenSheet);
        _journal = new JournalView(_campaign);
        _desktop.Add(_party.Window);
        _desktop.Add(_initiative.Window);
        _desktop.Add(_journal.Window);
        _desktop.Add(_chat.Window);
        _journal.Window.IsOpen = false;

        BuildMenus();
        BuildMapToolbar();

        var font = Environment.GetEnvironmentVariable("VTTDEMO_FONT");
        var fontIndex = font == "mono" ? 0 : TextFonts.IndexOf(font ?? "Verdana");
        TextFonts.Apply(fontIndex >= 0 ? fontIndex : Math.Min(1, TextFonts.Available.Length - 1));
        _settings = new SettingsDialog(_map, _toasts);
        if (Environment.GetEnvironmentVariable("VTTDEMO_THEME") == "light")
        {
            _settings.ToggleTheme();
        }

        OpenSheet(_campaign.Characters[0]);
        Arrange(Ui.ViewportSize - new Vector2(0, MenuBarHeight + StatusBarHeight));
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
        Ui.BeginFrame(dt);
        _settings.Sync(_map);
        DrawShell(dt);
        Ui.EndFrame();
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
                    _settings.SetScale(Ui.Scale + 0.1f);
                    break;
                case KeyCode.Minus or KeyCode.KeypadMinus:
                    _settings.SetScale(Ui.Scale - 0.1f);
                    break;
                case KeyCode.Num0:
                    _settings.SetScale(1);
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
                _desktop.Show(_chat.Window);
                _chat.FocusInput();
                break;
            case KeyCode.N:
                _campaign.NextTurn();
                break;
            case KeyCode.G:
                _map.ShowGrid = !_map.ShowGrid;
                break;
            case KeyCode.F2:
                _settings.ToggleOutlines();
                break;
            case KeyCode.Escape:
                _map.Selected = null;
                break;
        }
    }

    private void OpenSheet(Character character)
    {
        if (!_sheets.TryGetValue(character, out var sheet))
        {
            sheet = new CharacterSheet(_campaign, character);
            var step = _sheets.Count % 6 * 30;
            sheet.Window.Position = new Vector2(316 + step, 52 + step);
            if (_desktop.AreaSize.Y > 0)
            {
                sheet.Window.Size = new Vector2(580, MathF.Min(680, _desktop.AreaSize.Y - 64 - step));
            }

            _sheets[character] = sheet;
            _desktop.Add(sheet.Window);
        }

        _desktop.Show(sheet.Window);
    }

    private void OpenEverything()
    {
        foreach (var character in _campaign.Party)
        {
            OpenSheet(character);
        }

        _desktop.Show(_journal.Window);
        _desktop.Show(_chat.Window);
    }

    private void Arrange(Vector2 area)
    {
        _party.Window.Position = new Vector2(12, 12);
        _party.Window.Size = new Vector2(290, 300);
        _initiative.Window.Position = new Vector2(12, 324);
        _initiative.Window.Size = new Vector2(290, MathF.Max(200, area.Y - 324 - 64));
        _chat.Window.Size = new Vector2(410, MathF.Max(300, area.Y - 24));
        _chat.Window.Position = new Vector2(MathF.Max(320, area.X - 422), 12);
        _journal.Window.Size = new Vector2(560, MathF.Min(660, area.Y - 100));
        _journal.Window.Position = new Vector2(MathF.Max(330, area.X * 0.5f - 280), 70);
        foreach (var (i, sheet) in _sheets.Values.Index())
        {
            var step = i % 6 * 30;
            sheet.Window.Position = new Vector2(316 + step, 52 + step);
            sheet.Window.Size = new Vector2(580, MathF.Min(680, area.Y - 64 - step));
        }
    }

    private void BuildMenus()
    {
        _gameMenu = new Menu();
        _gameMenu.Add("Roll initiative", () => _campaign.RollInitiative(), FontAwesome.Cube, "/init");
        _gameMenu.Add("Next turn", () => _campaign.NextTurn(), FontAwesome.AngleRight, "N");
        _gameMenu.Add("Previous turn", () => _campaign.PreviousTurn(), FontAwesome.AngleLeft);
        _gameMenu.AddSeparator();
        _gameMenu.Add("Stress test: post 500 messages", () => _chat.Spam(500), FontAwesome.ChartBar, "/spam");
        _gameMenu.Add("Settings…", () => _settings.ShowCentered(), FontAwesome.Gear);
        _gameMenu.AddSeparator();
        _gameMenu.Add("Quit", Quit, FontAwesome.Power, "Ctrl+Q");

        _windowsMenu = new Menu();
        _windowsMenu.Opened += _ => FillWindowsMenu();

        _viewMenu = new Menu();
        _viewMenu.Add("Zoom map in", () => _map.ZoomBy(1), FontAwesome.Plus, "Wheel");
        _viewMenu.Add("Zoom map out", () => _map.ZoomBy(-1), FontAwesome.Minus, "Wheel");
        _viewMenu.Add("Reset map view", () => _map.ResetView(), FontAwesome.Compress);
        _viewMenu.Add("Toggle grid", () => _map.ShowGrid = !_map.ShowGrid, FontAwesome.BorderAll, "G");
        _viewMenu.AddSeparator();
        _viewMenu.Add("Larger interface", () => _settings.SetScale(Ui.Scale + 0.1f), FontAwesome.MagnifyingGlass, "Ctrl +");
        _viewMenu.Add("Smaller interface", () => _settings.SetScale(Ui.Scale - 0.1f), null, "Ctrl -");
        _viewMenu.Add("Actual size", () => _settings.SetScale(1), null, "Ctrl 0");
        _viewMenu.AddSeparator();
        _viewMenu.Add("Toggle light / dark theme", () => _settings.ToggleTheme(), FontAwesome.Sun);
        _viewMenu.Add("Element outlines", () => _settings.ToggleOutlines(), FontAwesome.VectorSquare, "F2");

        _gameButton = MenuButton("Game", _gameMenu);
        _windowsButton = MenuButton("Windows", _windowsMenu);
        _viewButton = MenuButton("View", _viewMenu);
        _settingsButton = new Button { Icon = FontAwesome.Gear, Kind = ButtonKind.Ghost, Tooltip = "Settings", Padding = UILengthEdges.Symmetric(9, 6) };
        _settingsButton.Clicked += _ => _settings.ShowCentered();
    }

    private static Button MenuButton(string text, Menu menu)
    {
        var button = new Button(text) { Kind = ButtonKind.Ghost, Padding = UILengthEdges.Symmetric(10, 6) };
        button.Clicked += b => menu.Toggle(b);
        return button;
    }

    private void FillWindowsMenu()
    {
        var menu = _windowsMenu;
        menu.ClearItems();
        Toggle(_chat.Window, "Enter");
        Toggle(_initiative.Window, null);
        Toggle(_party.Window, null);
        Toggle(_journal.Window, null);
        menu.AddSeparator();
        foreach (var character in _campaign.Party)
        {
            var open = _sheets.TryGetValue(character, out var sheet) && sheet.Window.IsOpen;
            menu.Add(character.Name, () => OpenSheet(character), open ? FontAwesome.Check : null);
        }

        menu.AddSeparator();
        menu.Add("Open everything", OpenEverything, FontAwesome.LayerGroup);
        menu.Add("Default layout", () =>
        {
            foreach (var window in _desktop.Windows)
            {
                window.Collapsed = false;
            }

            Arrange(_desktop.AreaSize);
        }, FontAwesome.TableCells);
        menu.Add("Cascade", () => _desktop.Cascade(), FontAwesome.Bars);
        menu.Add("Close all sheets", () =>
        {
            foreach (var sheet in _sheets.Values)
            {
                sheet.Window.Close();
            }
        }, FontAwesome.WindowClose);
        return;

        void Toggle(FloatingWindow window, string? shortcut)
        {
            menu.Add(window.Title, () => _desktop.Toggle(window), window.IsOpen ? FontAwesome.Check : null, shortcut);
        }
    }

    private void BuildMapToolbar()
    {
        _zoomOut = ToolButton(FontAwesome.Minus, "Zoom out", () => _map.ZoomBy(-1));
        _zoomIn = ToolButton(FontAwesome.Plus, "Zoom in", () => _map.ZoomBy(1));
        _zoomReset = ToolButton(FontAwesome.Compress, "Reset view", () => _map.ResetView());
        _grid = ToolButton(FontAwesome.BorderAll, "Toggle grid (G)", () => _map.ShowGrid = !_map.ShowGrid);
    }

    private static Button ToolButton(string icon, string tooltip, Action action)
    {
        var button = new Button { Icon = icon, Kind = ButtonKind.Ghost, Tooltip = tooltip, Padding = UILengthEdges.Symmetric(9, 6) };
        button.Clicked += _ => action();
        return button;
    }

    private void DrawShell(float dt)
    {
        var theme = Ui.Theme;
        using (Ui.Column(_rootId).Grow(1).Background(theme.Background).Open())
        {
            DrawMenuBar(theme);
            using (Ui.Element(_areaId).Fill().Clip().Open())
            {
                _map.Draw(dt);
                DrawSceneBanner(theme);
                DrawMapToolbar(theme);
                if (Ui.TryGetRect(_areaId, out var area))
                {
                    _desktop.AreaSize = new Vector2(area.Width, area.Height);
                }

                _desktop.Draw();
            }

            DrawStatusBar(theme);
        }

        _toasts.Draw(dt);
    }

    private void DrawMenuBar(Theme theme)
    {
        using (Ui.Row()
                   .Height(MenuBarHeight)
                   .Shrink(0)
                   .Padding(10, 0)
                   .Gap(4)
                   .AlignItems(UIAlign.Center)
                   .Background(theme.Surface)
                   .Border(theme.Border, new UIEdges(0, 0, 0, 1))
                   .Open())
        {
            using (Ui.Row().Size(28, 28).Shrink(0).Center().Background(theme.Accent).CornerRadius(7).Open())
            {
                Im.Icon(FontAwesome.Cube, 14, theme.OnAccent);
            }

            using (Ui.Column().Margin(4, 0, 12, 0).Shrink(0).Open())
            {
                Im.Line("Lanterns & Lore", 14, theme.Text);
                Im.Line("NiziKit VTT proof of concept", 10, theme.TextMuted);
            }

            Ui.Draw(_gameButton);
            Ui.Draw(_windowsButton);
            Ui.Draw(_viewButton);
            Im.Spacer();

            Im.Line("At the table:", 12, theme.TextMuted);
            using (Ui.Row().Gap(4).Margin(4, 0, 8, 0).Shrink(0).Open())
            {
                foreach (var pc in _campaign.Party)
                {
                    var id = Ui.Id("Vtt.Presence", pc.Id);
                    Im.Portrait(pc, 26, id);
                    if (Ui.Interaction(id).Hovered)
                    {
                        Ui.Tooltip(id, pc.Name);
                    }
                }
            }

            Ui.Draw(_settingsButton);
        }
    }

    private void DrawSceneBanner(Theme theme)
    {
        var current = _campaign.Current;
        using (Ui.Row().Absolute(left: 0, right: 0, top: 12).Justify(UIJustify.Center).Passthrough().Open())
        {
            using (Ui.Row().Padding(14, 6).Gap(10).AlignItems(UIAlign.Center).Background(UIColor.Hex(0x0B0C10E0)).Border(UIColor.Hex(0xFFFFFF1A), 1).CornerRadius(16).Open())
            {
                Im.Icon(FontAwesome.LocationCrosshairs, 11, Campaign.GmColor);
                Im.Line(_campaign.SceneName, 13, Im.White);
                Im.Line("·", 13, UIColor.Hex(0xFFFFFF60));
                Ui.Text($"Round {_campaign.Round}", 13, UIColor.Hex(0xFFFFFFB0));
                if (current != null)
                {
                    Im.Line("·", 13, UIColor.Hex(0xFFFFFF60));
                    Ui.Element().Size(8, 8).CornerRadius(4).Background(current.Color).Leaf();
                    Ui.Text($"{current.Name}'s turn", 13, UIColor.Hex(0xFFFFFFB0));
                }
            }
        }
    }

    private void DrawMapToolbar(Theme theme)
    {
        using (Ui.Row()
                   .Absolute(left: 12, bottom: 12)
                   .Padding(4)
                   .Gap(2)
                   .AlignItems(UIAlign.Center)
                   .Background(theme.Surface)
                   .Border(theme.Border, 1)
                   .CornerRadius(8)
                   .Open())
        {
            Ui.Draw(_zoomOut);
            using (Ui.Row().Width(46).Justify(UIJustify.Center).Open())
            {
                Ui.Text($"{_map.Zoom * 100:0}%", 12, theme.TextMuted);
            }

            Ui.Draw(_zoomIn);
            Ui.Draw(_zoomReset);
            Ui.Element().Size(1, 18).Margin(4, 0).Background(theme.Border).Leaf();
            _grid.Checked = _map.ShowGrid;
            Ui.Draw(_grid);
        }
    }

    private void DrawStatusBar(Theme theme)
    {
        using (Ui.Row()
                   .Height(StatusBarHeight)
                   .Shrink(0)
                   .Padding(12, 0)
                   .Gap(16)
                   .AlignItems(UIAlign.Center)
                   .Background(theme.Surface)
                   .Border(theme.Border, new UIEdges(0, 1, 0, 0))
                   .Open())
        {
            using (Ui.Row().Gap(6).AlignItems(UIAlign.Center).Shrink(0).Open())
            {
                Ui.Element().Size(7, 7).CornerRadius(4).Background(Im.Good).Leaf();
                Im.Line("Connected (simulated)", 11, theme.TextMuted);
            }

            Im.Line("Enter: chat · N: next turn · G: grid · Ctrl +/-: UI scale · F2: outlines · right-click tokens and messages", 11, theme.TextDisabled);
            Im.Spacer();
            Ui.Text($"{_fps:0} fps · UI {_uiMs:0.00} ms · {_campaign.Chat.Messages.Count} messages · {_desktop.OpenCount} windows · {Ui.Scale * 100:0}%", 11, theme.TextMuted);
        }
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
            case 600:
                _settings.SetScale(1.25f);
                _campaign.RollInitiative();
                break;
            case 720:
                _settings.ToggleTheme();
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
