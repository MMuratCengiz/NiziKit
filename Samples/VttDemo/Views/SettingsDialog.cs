
namespace VttDemo.Views;

// Proportional text fonts loaded straight from the system font files.
public static class TextFonts
{
    public const ushort FontId = 2;

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

    public static void Apply(int index)
    {
        if (index < 0 || index >= Available.Length)
        {
            return;
        }

        var (name, path) = Available[index];
        if (path == null)
        {
            Ui.Theme.FontId = 0;
            Current = index;
            return;
        }

        try
        {
            Fonts.Load(path, FontId);
            Ui.Theme.FontId = FontId;
            Current = index;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"VttDemo: could not load {name} ({exception.Message}); keeping the current font.");
        }
    }

    public static int IndexOf(string name)
    {
        return Array.FindIndex(Available, c => c.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase));
    }
}

// Live settings in a modal dialog; every control is bound to the state it changes, so changes made elsewhere show up here.
public sealed class SettingsDialog : Dialog
{
    public SettingsDialog(MapView map)
    {
        Title = "Settings";
        Width = 480;
        Padding = 22;
        Children =
        [
            new Paragraph("Everything applies immediately.", TextStyle.Muted),
            Setting("Theme", new Dropdown(["Dark", "Light"]) { Width = 200, Selected = Bind(() => Light ? 1 : 0, index => SetTheme(index == 1)) }),
            Setting("Text font", new Dropdown(TextFonts.Available.Select(f => f.Name).ToArray()) { Width = 200, Selected = Bind(() => TextFonts.Current, TextFonts.Apply) }),
            Setting("UI scale", new Slider(0.75f, 1.75f) { Step = 0.05f, Format = "0.00x", ValueWidth = 50, Live = false, Value = Bind(() => Ui.Scale, v => Ui.Scale = v) }),
            Setting("Map", new Switch("Show the square grid (G)") { On = Bind(() => map.ShowGrid, on => map.ShowGrid = on) }),
            Setting("Debug", new Switch("Outline every element (F2)")
            {
                On = Bind(() => Ui.DebugFlags != UIDebugFlagBits.None, on => Ui.DebugFlags = on ? UIDebugFlagBits.Outlines : UIDebugFlagBits.None)
            }),
            new Row { Justify = UIJustify.End, Gap = 8, Children = [new Button("Done") { Tone = Tone.Primary, OnClick = Close }] }
        ];
    }

    public bool Light { get; private set; }

    public void SetTheme(bool light)
    {
        Light = light;
        var theme = light ? Theme.Light() : Theme.Dark();
        theme.FontId = Ui.Theme.FontId;
        Ui.Theme = theme;
    }

    private static Row Setting(string label, Widget control)
    {
        return new Row { Gap = 12, Children = [new Label(label, TextStyle.Muted) { Width = 96 }, control] };
    }
}
