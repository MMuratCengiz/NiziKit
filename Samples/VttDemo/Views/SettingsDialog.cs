using System.Diagnostics;
using NiziKit.UI.Widgets;
using VttDemo.UiKit;
using Switch = NiziKit.UI.Widgets.Switch;

namespace VttDemo.Views;

// Proportional text fonts imported from the system at runtime and cached next to the executable.
public static class TextFonts
{
    public const ushort FontId = 2;

    private static readonly UnicodeRange[] Ranges =
    [
        new() { Start = 0x20, End = 0x7E },
        new() { Start = 0xA0, End = 0x17F },
        new() { Start = 0x2010, End = 0x205E },
        new() { Start = 0x2190, End = 0x21FF },
        new() { Start = 0x2200, End = 0x22FF },
        new() { Start = 0x25A0, End = 0x25FF }
    ];

    private static readonly (string Name, string? Path)[] Candidates =
    [
        ("Built-in (JetBrains Mono)", null),
        ("Verdana", "/System/Library/Fonts/Supplemental/Verdana.ttf"),
        ("Arial", "/System/Library/Fonts/Supplemental/Arial.ttf"),
        ("Georgia", "/System/Library/Fonts/Supplemental/Georgia.ttf"),
        ("Trebuchet MS", "/System/Library/Fonts/Supplemental/Trebuchet MS.ttf"),
        ("Segoe UI", @"C:\Windows\Fonts\segoeui.ttf"),
        ("DejaVu Sans", "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf")
    ];

    public static readonly (string Name, string? Path)[] Available = Candidates.Where(c => c.Path == null || File.Exists(c.Path)).ToArray();

    public static int Current { get; private set; }

    // Returns the milliseconds spent importing, or -1 when the font could not be loaded.
    public static double Apply(int index)
    {
        if (index < 0 || index >= Available.Length)
        {
            return -1;
        }

        var (name, path) = Available[index];
        var timer = Stopwatch.StartNew();
        if (path == null)
        {
            Ui.Theme.FontId = 0;
            Current = index;
            return 0;
        }

        try
        {
            var cache = Path.Combine(AppContext.BaseDirectory, "FontCache", name.Replace(' ', '_') + ".dzfont");
            if (File.Exists(cache))
            {
                Fonts.Load(cache, FontId);
            }
            else
            {
                var asset = Fonts.ImportTtfToAsset(path, Ranges);
                Directory.CreateDirectory(Path.GetDirectoryName(cache)!);
                File.WriteAllBytes(cache, asset);
                Fonts.LoadAsset(asset, FontId);
            }

            Ui.Theme.FontId = FontId;
            Current = index;
            return timer.Elapsed.TotalMilliseconds;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"VttDemo: could not load {name} ({exception.Message}); keeping the current font.");
            return -1;
        }
    }

    public static int IndexOf(string name)
    {
        return Array.FindIndex(Available, c => c.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase));
    }
}

// Live settings in a modal popup built only from retained widgets.
public sealed class SettingsDialog : Popup
{
    private readonly Dropdown _theme = new(["Dark", "Light"]) { Width = 200 };
    private readonly Dropdown _font = new(TextFonts.Available.Select(f => f.Name)) { Width = 200 };
    private readonly Slider _scale = new(0.75f, 1.75f, 1) { Step = 0.05f, ShowValue = true, ValueFormat = "0.00x", ValueWidth = 50 };
    private readonly Switch _grid;
    private readonly Switch _outlines = new("Outline every element (F2)");

    public SettingsDialog(MapView map, Toasts toasts)
    {
        Modal = true;
        Layout.Width = 480;
        Layout.Padding = UILengthEdges.All(22);
        Layout.Gap = 14;

        _theme.SelectionChanged += d =>
        {
            var theme = d.SelectedIndex == 1 ? Theme.Light() : Theme.Dark();
            theme.FontId = Ui.Theme.FontId;
            Ui.Theme = theme;
        };
        _font.SelectedIndex = TextFonts.Current;
        _font.SelectionChanged += d =>
        {
            var ms = TextFonts.Apply(d.SelectedIndex);
            if (ms > 50)
            {
                toasts.Show($"Imported {d.SelectedItem} in {ms:0} ms (cached for next time)", Ui.Theme.Accent);
            }
        };
        _grid = new Switch("Show the square grid (G)", map.ShowGrid);
        _grid.Changed += s => map.ShowGrid = s.On;
        _outlines.Changed += s => Ui.DebugFlags = s.On ? UIDebugFlagBits.Outlines : UIDebugFlagBits.None;

        var done = new Button("Done") { Kind = ButtonKind.Primary };
        done.Clicked += _ => Close();
        var buttons = new HStack { Justify = UIJustify.End, Gap = 8 };
        buttons.Add(done);

        Add(
            new Label("Settings") { FontSize = 18 },
            new Label("Everything applies immediately. Fonts are imported from the system the first time you pick them.") { Muted = true },
            Row("Theme", _theme),
            Row("Text font", _font),
            Row("UI scale", _scale),
            Row("Map", _grid),
            Row("Debug", _outlines),
            buttons);

        Sync(map);
    }

    public void ToggleTheme()
    {
        _theme.SelectedIndex = _theme.SelectedIndex == 1 ? 0 : 1;
    }

    public void ToggleOutlines()
    {
        _outlines.On = !_outlines.On;
    }

    public void SetScale(float scale)
    {
        _scale.Value = scale;
        Ui.Scale = _scale.Value;
    }

    // Applies the scale once the slider is released (rescaling under the pointer while dragging fights the drag) and mirrors state changed elsewhere.
    public void Sync(MapView map)
    {
        if (!_scale.IsPressed && MathF.Abs(Ui.Scale - _scale.Value) > 0.001f)
        {
            Ui.Scale = _scale.Value;
        }

        _grid.On = map.ShowGrid;
        _font.SelectedIndex = TextFonts.Current;
    }

    private static HStack Row(string label, Widget widget)
    {
        var row = new HStack { Gap = 12 };
        row.Add(new Label(label) { Width = 96, Muted = true }, widget);
        return row;
    }
}
