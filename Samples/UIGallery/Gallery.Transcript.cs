using System.Text;

namespace UIGallery;

internal sealed record ChatLine(int Id, string Time, string Author, string Text, bool Rich);

internal sealed partial class Gallery
{
    private static readonly string[] ChatTexts =
    [
        "Reading the build log to see why the shader cache misses on a cold start.",
        "<p>The key comes from <code>ShaderCache.Key</code>, which hashes the <b>absolute</b> path of each source.</p>",
        "So every checkout gets its own entries. Hashing the path relative to the repository root fixes it.",
        "<p>Changed <code>Graphics/Cache.cs</code>, three lines, and added a test for two checkouts of one tree.</p>",
        "Ran the full suite twice, no flakes. A cold start drops from 4.1 s to 1.3 s."
    ];

    private readonly ChatLine[] _chat = Enumerable.Range(0, 200).Select(Chat).ToArray();

    public SelectionScope ChatScope { get; private set; } = null!;
    public TypingPad TypingPad { get; private set; } = null!;
    public TextField SteadyField { get; private set; } = null!;

    private Widget ScopeCard()
    {
        var list = new ListView<ChatLine>(() => _chat, ChatRow)
        {
            Name = "text.scope.list", Height = 240, Key = line => line.Id, Padding = (0, 4), Background = Color.SurfaceAlt, Radius = 6
        };
        ChatScope = new SelectionScope(list) { Name = "text.scope" };
        return Card("Selection across rows",
            "A SelectionScope around a ListView of 200 rows, one selectable Paragraph or RichText each: drag from one row over the others, past the " +
            "list's edge to scroll it, Shift+click to extend, Cmd+A (Ctrl+A) after a click in the list and Cmd+C (Ctrl+C) to copy one line per row.", 0,
            ChatScope,
            new Row
            {
                Gap = 8,
                Children =
                [
                    new Button("Select all") { Name = "text.scope.all", OnClick = ChatScope.SelectAll },
                    new Button("Clear") { Tone = Tone.Ghost, OnClick = ChatScope.ClearSelection },
                    new Paragraph(() => ScopeReadout(), TextStyle.Small) { Name = "text.scope.readout", Flex = 1 }
                ]
            });
    }

    private Widget TypingCard()
    {
        TypingPad = new TypingPad();
        SteadyField = new TextField { Name = "text.steady", Placeholder = "CaretBlink = false", CaretBlink = false, Width = 260 };
        return Card("Text input without a TextField",
            "A Canvas that overrides Focusable, AcceptsTextInput and OnTextInput takes typed and IME-composed text while it has focus and draws a " +
            "steady cursor, so while it has focus a render-on-demand app wakes only for input. CaretBlink = false does the same for a TextField.", 0,
            TypingPad,
            new Paragraph(() => $"{TypingPad.Text.Length} characters typed, OS text input {(Ui.WantsTextInput ? "on" : "off")}", TextStyle.Small),
            SteadyField);
    }

    private string ScopeReadout()
    {
        var text = ChatScope.SelectedText;
        if (text.Length == 0)
        {
            return "nothing selected";
        }

        var lines = text.Count(c => c == '\n') + 1;
        var first = text.AsSpan(0, text.IndexOf('\n') is var end and >= 0 ? end : text.Length);
        return $"{lines} lines, {text.Length} characters, starting \"{first[..Math.Min(first.Length, 40)]}\"";
    }

    private static ChatLine Chat(int i)
    {
        var text = ChatTexts[i % ChatTexts.Length];
        var rich = text.StartsWith("<p>", StringComparison.Ordinal);
        return new ChatLine(i, $"12:{i / 60:00}:{i % 60:00}", i % 2 == 0 ? "agent" : "you", rich ? $"<p>{i}. {text[3..]}" : $"{i}. {text}", rich);
    }

    private static Widget ChatRow(ChatLine line)
    {
        Widget message = line.Rich
            ? new RichText(line.Text) { Selectable = true, Flex = 1 }
            : new Paragraph(line.Text) { Selectable = true, Flex = 1 };
        return new Row
        {
            Name = $"text.scope.row.{line.Id}",
            Gap = 10,
            Align = UIAlign.Start,
            Padding = (10, 3),
            Children =
            [
                new Label(line.Time, TextStyle.Small with { Mono = true }) { Width = 64 },
                new Label(line.Author, TextStyle.Small with { Color = line.Author == "you" ? Color.Accent : Color.Success }) { Width = 40 },
                message
            ]
        };
    }
}

internal sealed class TypingPad : Canvas
{
    private const float Pad = 12;

    private static readonly TextStyle Style = TextStyle.Sized(14) with { Mono = true };

    private readonly StringBuilder _typed = new();
    private string _text = "";
    private Rectangle _cursor;

    public TypingPad()
    {
        Name = "text.typing";
        Height = 48;
        Cursor = SystemCursor.Text;
    }

    public string Text => _text;

    protected override bool Focusable => true;
    protected override bool AcceptsTextInput => true;
    protected override Rectangle? TextInputRect => _cursor;

    protected override void OnTextInput(string text)
    {
        _typed.Append(text);
        _text = _typed.ToString();
    }

    protected override bool OnKeyDown(in KeyboardEventData key)
    {
        if (key.KeyCode != KeyCode.Backspace || _typed.Length == 0)
        {
            return false;
        }

        _typed.Length -= _typed.Length > 1 && char.IsLowSurrogate(_typed[^1]) ? 2 : 1;
        _text = _typed.ToString();
        return true;
    }

    protected override void Describe(UiWidgetInfo info)
    {
        info.Text = _text;
    }

    protected override void Paint(Painter p)
    {
        var bounds = p.Bounds;
        p.Box(new Rectangle(0, 0, bounds.Width, bounds.Height), new Paint
        {
            Background = Color.SurfaceAlt, Border = IsFocused ? Color.Accent : Color.Border, Radius = 6
        });
        var shown = _text.Length > 0 || IsFocused ? _text.AsSpan(Math.Max(0, _text.Length - 90)) : "Click, then type";
        var width = _text.Length > 0 ? Ui.MeasureText(shown, Style).X : 0;
        var height = Ui.MeasureText("M", Style).Y;
        p.Text(new Rectangle(Pad, 0, bounds.Width - 2 * Pad, bounds.Height), shown, _text.Length > 0 ? Style : Style with { Color = Color.TextMuted },
            UITextAlign.Start);
        var cursor = new Rectangle(Pad + width + 1, (bounds.Height - height) / 2, 8, height);
        _cursor = cursor with { X = bounds.X + cursor.X, Y = bounds.Y + cursor.Y };
        if (IsFocused)
        {
            p.Box(cursor, new Paint { Background = Color.Accent });
        }
    }
}
