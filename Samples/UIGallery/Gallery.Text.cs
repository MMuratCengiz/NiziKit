using NiziKit.UI.Widgets;

namespace UIGallery;

internal sealed partial class Gallery
{
    private const string Paragraph =
        "Text is measured by the font and wrapped by the layout, resize the window to see it reflow. " +
        "A word longer than the line, like Supercalifragilisticexpialidocious, overflows instead of breaking.";

    private const string Notes =
        "Multi-line editing: the caret keeps its column on Up and Down, Option or Ctrl jump words, Command jumps to line ends.\n\n" +
        "Double click selects a word, triple click everything. Copy, cut and paste use the system clipboard. " +
        "The area scrolls to keep the caret in view; ünïcödé and emoji like 🎮 are UTF-8 byte offsets underneath.";

    private string _submitted = "nothing yet";

    public TextBox NameBox { get; private set; } = null!;
    public TextArea NotesArea { get; private set; } = null!;

    private Widget TextPage()
    {
        NameBox = new TextBox { Placeholder = "Your name", Width = 260 };
        NameBox.Submitted += t => _submitted = t.Text;
        var password = new TextBox("hunter2") { IsPassword = true, Width = 260 };
        var readOnly = new TextBox("Read only: select and copy me") { ReadOnly = true, Width = 260 };
        var limited = new TextBox { Placeholder = "At most 12 characters", MaxLength = 12, Width = 260 };
        NotesArea = new TextArea(Notes) { Height = 160 };

        var words = new Label(Paragraph) { FontSize = 14 };
        var clamped = new Label(Paragraph) { MaxLines = 2, Ellipsis = true };
        var centered = new Label(Paragraph) { Align = UITextAlign.Center };

        return Page(
            new Card("Text boxes", "Click to place the caret, drag or Shift+arrows to select, Enter submits.")
            {
                Row(12, NameBox, password),
                Row(12, readOnly, limited),
                new Immediate(() => Ui.Text(
                    $"caret {NameBox.Caret}, selection {NameBox.SelectionStart}..{NameBox.SelectionEnd}, {NameBox.Utf8.Length} bytes, submitted \"{_submitted}\"",
                    12, Ui.Theme.TextMuted))
            },
            new Card("Text area", "A wrapped, scrolling text element with a caret, a selection and vertical caret movement.")
            {
                NotesArea,
                new Immediate(() => Ui.Text($"caret {NotesArea.Caret}, {NotesArea.LineCount} lines, selection \"{NotesArea.SelectedText}\"", 12,
                    Ui.Theme.TextMuted))
            },
            new Card("Wrapping", "Each column takes an equal share of the row (Grow 1, Shrink 1, Basis 0) and wraps its text to that width.")
            {
                Columns(
                    Column("Words", words),
                    Column("Two lines, ellipsis", clamped),
                    Column("Centered", centered)),
                new Immediate(DrawNarrowTexts)
            },
            new Card("Icons and fonts", "Font 1 is FontAwesome, loaded from an embedded asset; font 0 is the built-in font.")
            {
                new Immediate(DrawIcons)
            });
    }

    private static HStack Columns(params ReadOnlySpan<Widget> columns)
    {
        var row = new HStack { Gap = 16, AlignItems = UIAlign.Start };
        row.Add(columns);
        return row;
    }

    private static VStack Column(string caption, Widget content)
    {
        var column = new VStack { Gap = 6, Fill = true };
        column.Add(Caption(caption));
        column.Add(content);
        return column;
    }

    private static void DrawNarrowTexts()
    {
        var theme = Ui.Theme;
        using (Ui.Row().Gap(16).AlignItems(UIAlign.Start).Open())
        {
            using (Ui.Column().Width(180).Padding(8).Gap(4).Background(theme.SurfaceAlt).CornerRadius(6).Open())
            {
                Ui.Text("Wrap NONE, ellipsis", theme.SmallFontSize, theme.TextMuted);
                var single = Ui.TextStyle();
                single.Wrap = UITextWrap.None;
                single.Overflow = UITextOverflow.Ellipsis;
                Ui.Text("A single line that is far too long for its box", in single);
            }

            using (Ui.Column().Width(180).Padding(8).Gap(4).Background(theme.SurfaceAlt).CornerRadius(6).Open())
            {
                Ui.Text("Wrap ANYWHERE", theme.SmallFontSize, theme.TextMuted);
                var anywhere = Ui.TextStyle();
                anywhere.Wrap = UITextWrap.Anywhere;
                Ui.Text("https://example.com/a/very/long/path/without/any/spaces", in anywhere);
            }

            using (Ui.Column().Width(180).Padding(8).Gap(4).Background(theme.SurfaceAlt).CornerRadius(6).Open())
            {
                Ui.Text("Line height and spacing", theme.SmallFontSize, theme.TextMuted);
                var spaced = Ui.TextStyle(13);
                spaced.LineHeight = 22;
                spaced.LetterSpacing = 1.5f;
                Ui.Text("Airy text with a taller line height", in spaced);
            }
        }
    }

    private static void DrawIcons()
    {
        var theme = Ui.Theme;
        ReadOnlySpan<string> icons =
        [
            FontAwesome.Gamepad, FontAwesome.Cube, FontAwesome.Heart, FontAwesome.Star, FontAwesome.Bell, FontAwesome.Gear,
            FontAwesome.Music, FontAwesome.Camera, FontAwesome.Globe, FontAwesome.Sun, FontAwesome.Lightbulb, FontAwesome.Palette
        ];
        using (Ui.Row().Gap(14).Wrap().AlignItems(UIAlign.Center).Open())
        {
            foreach (var icon in icons)
            {
                using (Ui.Row().Size(40, 40).Center().Background(theme.SurfaceAlt).CornerRadius(8).Open())
                {
                    Ui.Text(icon, 18, theme.Accent, FontAwesome.FontId);
                }
            }

            Ui.Text("Mixed fonts: built-in ", 14, theme.Text);
            Ui.Text(FontAwesome.ArrowRight, 14, theme.TextMuted, FontAwesome.FontId);
            Ui.Text(" FontAwesome", 14, theme.Text);
        }
    }
}
