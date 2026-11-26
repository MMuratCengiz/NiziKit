namespace UIGallery;

internal sealed partial class Gallery
{
    private const string Sample =
        "Text is measured by the font and wrapped by the layout, resize the window to see it reflow. " +
        "A word longer than the line, like Supercalifragilisticexpialidocious, overflows instead of breaking.";

    private const string Notes =
        "Multi-line editing: the caret keeps its column on Up and Down, Option or Ctrl jump words, Command jumps to line ends.\n\n" +
        "Double click selects a word, triple click everything. Copy, cut and paste use the system clipboard. " +
        "The area scrolls to keep the caret in view; ünïcödé and emoji like 🎮 are UTF-8 byte offsets underneath.";

    private const string ArialPath = "/System/Library/Fonts/Supplemental/Arial.ttf";
    private const string ArialBoldPath = "/System/Library/Fonts/Supplemental/Arial Bold.ttf";
    private const ushort ArialId = 2;
    private const ushort ArialBoldId = 3;

    private static readonly string[] BodyFonts = ["Built-in JetBrains Mono", "Arial, with Arial Bold as its bold face"];

    private static readonly string[] LogLines =
    [
        "12:01:07  build started",
        "12:01:09  warning CS8618: non-nullable field '_name' must contain a non-null value when exiting the constructor",
        "12:01:12  tests passed: 214, skipped: 3",
        "12:01:12  done in 5.2 s"
    ];

    private static readonly string[] Glyphs =
    [
        FontAwesome.Gamepad, FontAwesome.Cube, FontAwesome.Heart, FontAwesome.Star, FontAwesome.Bell, FontAwesome.Gear,
        FontAwesome.Music, FontAwesome.Camera, FontAwesome.Globe, FontAwesome.Sun, FontAwesome.Lightbulb, FontAwesome.Palette
    ];

    private readonly List<Label> _selectables = new();
    private string _submitted = "nothing yet";
    private int _bodyFont;
    private string? _logLine;

    public TextField NameBox { get; private set; } = null!;
    public TextArea NotesArea { get; private set; } = null!;

    private ushort BodyFontId => _bodyFont == 1 ? ArialId : (ushort)0;

    private Widget TextPage()
    {
        NameBox = new TextField { Placeholder = "Your name", Width = 260, OnSubmit = text => _submitted = text };
        NotesArea = new TextArea { Text = Notes, Height = 160 };

        return Page(
            Card("Text fields", "Click to place the caret, drag or Shift+arrows to select, Enter submits.", 0,
                Line(12, NameBox, new TextField { Text = "hunter2", IsPassword = true, Width = 260 }),
                Line(12, new TextField { Text = "Read only: select and copy me", ReadOnly = true, Width = 260 },
                    new TextField { Placeholder = "At most 12 characters", MaxLength = 12, Width = 260 }),
                new Paragraph(() => $"caret {NameBox.Caret}, selection {NameBox.SelectionStart}..{NameBox.SelectionEnd}, {NameBox.ByteLength} bytes, submitted \"{_submitted}\"",
                    TextStyle.Small),
                new Column
                {
                    Gap = 8,
                    Align = UIAlign.Start,
                    Children =
                    [
                        Caption("In a column that does not stretch its children: the default 200 wide, then Stretch = true"),
                        new TextField { Name = "text.width.default", Placeholder = "Width 200" },
                        new TextField { Name = "text.stretch.column", Placeholder = "Stretch = true fills the column", Stretch = true }
                    ]
                },
                new Row
                {
                    Gap = 8,
                    Children =
                    [
                        Caption("In a row"),
                        new TextField { Name = "text.stretch.row", Placeholder = "Stretch = true grows along the row", Stretch = true },
                        new Button("Send")
                    ]
                }),
            SelectableCard(),
            Card("Text area", "A wrapped, scrolling text element with a caret, a selection and vertical caret movement.", 0,
                NotesArea,
                new Paragraph(() => $"caret {NotesArea.Caret}, {NotesArea.LineCount} lines, selection \"{NotesArea.SelectedText}\"", TextStyle.Small)),
            Card("Wrapping", "Each column takes an equal share of the row (Flex = 1) and wraps its text to that width.", 0,
                new Row
                {
                    Gap = 16,
                    Align = UIAlign.Start,
                    Children =
                    [
                        Captioned("Words", new Paragraph(Sample)),
                        Captioned("Two lines, ellipsis", new Paragraph(Sample) { MaxLines = 2 }),
                        Captioned("Centered", new Paragraph(Sample) { Align = UITextAlign.Center })
                    ]
                },
                new Row
                {
                    Gap = 16,
                    Align = UIAlign.Start,
                    Children =
                    [
                        Narrow("Label: one line, ellipsis", new Label("A single line that is far too long for its box")),
                        Narrow("Paragraph, break anywhere", new Paragraph("https://example.com/a/very/long/path/without/any/spaces") { BreakAnywhere = true }),
                        Narrow("Line height and spacing", new Paragraph("Airy text with a taller line height", TextStyle.Sized(13) with { LineHeight = 22, LetterSpacing = 1.5f }))
                    ]
                }),
            Card("Icons and fonts", "Font 1 is FontAwesome, loaded from an embedded asset; font 0 is the built-in font.", 0,
                new Row
                {
                    Gap = 14,
                    Wrap = true,
                    Children =
                    [
                        .. Glyphs.Select(glyph => new Row
                        {
                            Width = 40,
                            Height = 40,
                            Justify = UIJustify.Center,
                            Background = Color.SurfaceAlt,
                            Radius = 8,
                            Children = [new Icon(glyph) { Size = 18, Color = Color.Accent }]
                        }),
                        new Label("Mixed fonts: built-in "),
                        new Icon(FontAwesome.ArrowRight) { Color = Color.TextMuted },
                        new Label(" FontAwesome")
                    ]
                }),
            Card("Weights and mono",
                "TextStyle.Weight draws the face registered with Fonts.SetVariant, else a synthesized one. TextStyle.Mono draws Theme.MonoFontId, " +
                "the built-in JetBrains Mono, also when Theme.FontId is another font. Switch the body font to see both.", 0,
                new Row
                {
                    Gap = 12,
                    Children =
                    [
                        new Label("Body font"),
                        new Dropdown(File.Exists(ArialPath) && File.Exists(ArialBoldPath) ? BodyFonts : BodyFonts[..1])
                        {
                            Name = "text.bodyfont", Width = 320, Selected = Bind(() => _bodyFont, SetBodyFont)
                        }
                    ]
                },
                Line(20,
                    new Label("Regular") { Name = "text.weight.regular" },
                    new Label("Medium", new TextStyle { Weight = FontWeight.Medium }) { Name = "text.weight.medium" },
                    new Label("Bold", new TextStyle { Weight = FontWeight.Bold }) { Name = "text.weight.bold" },
                    new Label("Mono 0O1lI", new TextStyle { Mono = true }) { Name = "text.mono" },
                    new Label("Mono bold", new TextStyle { Mono = true, Weight = FontWeight.Bold }) { Name = "text.mono.bold" }),
                new RichText("<p>RichText follows both: <b>bold</b> takes the registered bold face, <code>code</code> the mono font.</p>")));
    }

    private Widget SelectableCard()
    {
        var label = Selectable(new Label("A selectable Label: drag across me or double click a word") { Name = "text.select.label" });
        var paragraph = Selectable(new Paragraph(Sample) { Name = "text.select.paragraph" });
        var scrolled = Selectable(new Paragraph(Notes + "\n\n" + Sample) { Name = "text.select.scrolled" });
        return Card("Selectable text",
            "Selectable = true on a Label, Paragraph or RichText: drag, double click a word, triple click all, Shift+click extends. " +
            "Cmd+C (Ctrl+C) copies and Cmd+A (Ctrl+A) selects all after a click in the text; a click elsewhere clears it.", 0,
            label,
            paragraph,
            new ScrollView { Height = 90, Padding = 8, Background = Color.SurfaceAlt, Radius = 6, Children = [scrolled] },
            new ListView<string>(() => LogLines, line => Selectable(new Label(line, TextStyle.Small with { Mono = true })))
            {
                Name = "text.select.log", Height = 110, Selected = Bind(() => _logLine, line => _logLine = line)
            },
            new Paragraph(() => $"selected \"{SelectedText()}\", log row {_logLine ?? "none"}", TextStyle.Small) { Name = "text.select.readout" });
    }

    private Label Selectable(Label label)
    {
        label.Selectable = true;
        _selectables.Add(label);
        return label;
    }

    private string SelectedText()
    {
        foreach (var label in _selectables)
        {
            if (label.SelectedText is { Length: > 0 } text)
            {
                return text;
            }
        }

        return "";
    }

    private void SetBodyFont(int index)
    {
        _bodyFont = index;
        if (index == 1 && !Fonts.IsRegistered(ArialId))
        {
            Fonts.Load(ArialPath, ArialId);
            Fonts.Load(ArialBoldPath, ArialBoldId);
            Fonts.SetVariant(ArialId, FontWeight.Bold, ArialBoldId);
        }

        Ui.Theme.FontId = BodyFontId;
    }

    private static Column Captioned(string caption, Widget content)
    {
        return new Column { Gap = 6, Flex = 1, Children = [Caption(caption), content] };
    }

    private static Column Narrow(string caption, Widget content)
    {
        return new Column
        {
            Width = 180,
            Padding = 8,
            Gap = 4,
            Background = Color.SurfaceAlt,
            Radius = 6,
            Children = [Caption(caption), content]
        };
    }
}
