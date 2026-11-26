using System.Globalization;
using System.Net;
using System.Text;
using DenOfIz;

namespace NiziKit.UI;

// The HTML subset RichText draws: a forgiving tag parser, inline style attributes, and a builder that turns the tree into blocks of styled runs.
// Unknown tags render their content, unknown properties are ignored.
internal sealed class MarkupNode
{
    public string Tag = "";
    public string? Text;
    public Dictionary<string, string>? Attributes;
    public List<MarkupNode>? Children;

    public string? Attribute(string name)
    {
        return Attributes != null && Attributes.TryGetValue(name, out var value) ? value : null;
    }
}

internal struct InlineStyle
{
    public float Size;
    public bool Bold;
    public bool Italic;
    public bool Mono;
    public Color Color;
    public Color Background;
    public UITextDecorationFlagBits Decoration;
    public Color DecorationColor;
    public float Shift;
    public int Link;
    public TextCase Case;

    public readonly bool SameRun(in InlineStyle other)
    {
        return Size == other.Size && Bold == other.Bold && Italic == other.Italic && Mono == other.Mono && Color.Equals(other.Color) &&
               Background.Equals(other.Background) && Decoration == other.Decoration && DecorationColor.Equals(other.DecorationColor) &&
               Shift == other.Shift && Link == other.Link;
    }
}

internal enum TextCase : byte
{
    None,
    Upper,
    Lower,
    Capitalize
}

internal enum ImageAlign : byte
{
    Baseline,
    Middle,
    Top
}

internal sealed class MarkupImage
{
    public required string Source;
    public string? Alt;
    public float Width;
    public float Height;
    public float Radius;
    public ImageAlign Align;
}

internal sealed class MarkupRun
{
    public int ByteEnd;
    public InlineStyle Style;
    public MarkupImage? Image;
}

internal struct BlockBox
{
    public float MarginTop;
    public float MarginBottom;
    public Edges Padding;
    public Color Background;
    public Color BorderColor;
    public UIEdges BorderWidth;
    public float Radius;
    public UIValue Width;
    public UIValue MaxWidth;
    public bool Row;
    public float Flex;
}

internal abstract class MarkupBlock
{
    public BlockBox Box;
    public int Index;
}

internal sealed class MarkupParagraph : MarkupBlock
{
    public byte[] Utf8 = [];
    public List<MarkupRun> Runs = new();
    public float Size;
    public Color Color;
    public UITextAlign Align;
    public float LineHeight;
    public UITextWrap Wrap;
    public bool HasLinks;
    public UITextRun[] Native = [];
}

internal sealed class MarkupContainer : MarkupBlock
{
    public readonly List<MarkupBlock> Children = new();
    public string? Marker;
    public InlineStyle MarkerStyle;
    public Color MarkerColor;
}

internal sealed class MarkupImageBlock : MarkupBlock
{
    public required MarkupImage Image;
}

internal sealed class MarkupRule : MarkupBlock
{
    public Color Color;
}

internal sealed record MarkupLink(string Href, string? Title);

internal sealed class MarkupDocument
{
    public readonly MarkupContainer Root = new();
    public readonly List<MarkupLink> Links = new();
    public readonly List<MarkupParagraph> Paragraphs = new();
}

internal static class MarkupParser
{
    private static readonly HashSet<string> VoidTags = ["br", "img", "hr", "meta", "link", "input", "wbr", "col", "source"];

    internal static readonly HashSet<string> BlockTags =
    [
        "p", "div", "h1", "h2", "h3", "h4", "h5", "h6", "ul", "ol", "li", "blockquote", "pre", "hr", "section", "article", "header", "footer", "nav",
        "main", "aside", "figure", "figcaption", "address", "center", "table", "thead", "tbody", "tfoot", "tr", "td", "th", "dl", "dt", "dd", "details",
        "summary"
    ];

    public static MarkupNode Parse(string markup)
    {
        var root = new MarkupNode { Tag = "#root", Children = new List<MarkupNode>() };
        var stack = new List<MarkupNode> { root };
        var i = 0;
        while (i < markup.Length)
        {
            var lt = markup.IndexOf('<', i);
            if (lt < 0)
            {
                AddText(stack[^1], markup[i..]);
                break;
            }

            if (lt > i)
            {
                AddText(stack[^1], markup[i..lt]);
            }

            if (markup.AsSpan(lt).StartsWith("<!--"))
            {
                var end = markup.IndexOf("-->", lt + 4, StringComparison.Ordinal);
                i = end < 0 ? markup.Length : end + 3;
                continue;
            }

            if (lt + 1 < markup.Length && (markup[lt + 1] == '!' || markup[lt + 1] == '?'))
            {
                var end = markup.IndexOf('>', lt);
                i = end < 0 ? markup.Length : end + 1;
                continue;
            }

            var closing = lt + 1 < markup.Length && markup[lt + 1] == '/';
            var nameStart = lt + (closing ? 2 : 1);
            var nameEnd = nameStart;
            while (nameEnd < markup.Length && (char.IsLetterOrDigit(markup[nameEnd]) || markup[nameEnd] == '-'))
            {
                nameEnd++;
            }

            if (nameEnd == nameStart)
            {
                AddText(stack[^1], "<");
                i = lt + 1;
                continue;
            }

            var tag = markup[nameStart..nameEnd].ToLowerInvariant();
            var (attributes, selfClosing, after) = ParseAttributes(markup, nameEnd);
            i = after;
            if (closing)
            {
                Close(stack, tag);
                continue;
            }

            if (tag is "script" or "style" or "head" or "title")
            {
                var end = markup.IndexOf("</" + tag, i, StringComparison.OrdinalIgnoreCase);
                var close = end < 0 ? -1 : markup.IndexOf('>', end);
                i = close < 0 ? markup.Length : close + 1;
                continue;
            }

            ImplicitClose(stack, tag);
            var node = new MarkupNode { Tag = tag, Attributes = attributes };
            var parent = stack[^1];
            (parent.Children ??= new List<MarkupNode>()).Add(node);
            if (!selfClosing && !VoidTags.Contains(tag))
            {
                stack.Add(node);
            }
        }

        return root;
    }

    private static void AddText(MarkupNode parent, string text)
    {
        (parent.Children ??= new List<MarkupNode>()).Add(new MarkupNode { Tag = "#text", Text = WebUtility.HtmlDecode(text) });
    }

    private static (Dictionary<string, string>? Attributes, bool SelfClosing, int After) ParseAttributes(string markup, int i)
    {
        Dictionary<string, string>? attributes = null;
        var selfClosing = false;
        while (i < markup.Length)
        {
            var c = markup[i];
            if (c == '>')
            {
                return (attributes, selfClosing, i + 1);
            }

            if (c == '/')
            {
                selfClosing = true;
                i++;
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            selfClosing = false;
            var nameStart = i;
            while (i < markup.Length && !char.IsWhiteSpace(markup[i]) && markup[i] is not ('=' or '>' or '/'))
            {
                i++;
            }

            var name = markup[nameStart..i].ToLowerInvariant();
            while (i < markup.Length && char.IsWhiteSpace(markup[i]))
            {
                i++;
            }

            var value = "";
            if (i < markup.Length && markup[i] == '=')
            {
                i++;
                while (i < markup.Length && char.IsWhiteSpace(markup[i]))
                {
                    i++;
                }

                if (i < markup.Length && markup[i] is '"' or '\'')
                {
                    var quote = markup[i];
                    var end = markup.IndexOf(quote, i + 1);
                    end = end < 0 ? markup.Length : end;
                    value = markup[(i + 1)..end];
                    i = Math.Min(end + 1, markup.Length);
                }
                else
                {
                    var start = i;
                    while (i < markup.Length && !char.IsWhiteSpace(markup[i]) && markup[i] != '>')
                    {
                        i++;
                    }

                    value = markup[start..i];
                }
            }

            if (name.Length > 0)
            {
                (attributes ??= new Dictionary<string, string>())[name] = WebUtility.HtmlDecode(value);
            }
        }

        return (attributes, selfClosing, markup.Length);
    }

    private static void Close(List<MarkupNode> stack, string tag)
    {
        for (var i = stack.Count - 1; i > 0; i--)
        {
            if (stack[i].Tag == tag)
            {
                stack.RemoveRange(i, stack.Count - i);
                return;
            }
        }
    }

    // A block start ends an open paragraph, a list item ends the previous item of its list, a cell or row the previous one.
    private static void ImplicitClose(List<MarkupNode> stack, string tag)
    {
        if (BlockTags.Contains(tag))
        {
            for (var i = stack.Count - 1; i > 0; i--)
            {
                if (stack[i].Tag == "p")
                {
                    stack.RemoveRange(i, stack.Count - i);
                    break;
                }

                if (BlockTags.Contains(stack[i].Tag))
                {
                    break;
                }
            }
        }

        var (sibling, scope) = tag switch
        {
            "li" => ("li", new[] { "ul", "ol" }),
            "dt" or "dd" => (tag, new[] { "dl" }),
            "td" or "th" => (tag, new[] { "tr", "table" }),
            "tr" => ("tr", new[] { "table", "thead", "tbody", "tfoot" }),
            _ => ((string?)null, Array.Empty<string>())
        };
        if (sibling == null)
        {
            return;
        }

        for (var i = stack.Count - 1; i > 0; i--)
        {
            if (Array.IndexOf(scope, stack[i].Tag) >= 0)
            {
                return;
            }

            if (stack[i].Tag == sibling || (sibling is "td" or "th" && stack[i].Tag is "td" or "th") || (sibling is "dt" or "dd" && stack[i].Tag is "dt" or "dd"))
            {
                stack.RemoveRange(i, stack.Count - i);
                return;
            }
        }
    }
}

internal sealed class MarkupBuilder
{
    private readonly MarkupDocument _document = new();
    private readonly float _base;
    private readonly StringBuilder _text = new();
    private readonly List<(int CharEnd, InlineStyle Style, MarkupImage? Image)> _runs = new();
    private bool _lastSpace = true;
    private bool _wordStart = true;
    private int _nextIndex;

    private MarkupBuilder(float baseSize)
    {
        _base = baseSize;
    }

    private struct BlockContext
    {
        public InlineStyle Inline;
        public UITextAlign Align;
        public float LineHeight;
        public bool Pre;
        public bool NoWrap;
    }

    public static MarkupDocument Build(string markup, float baseSize)
    {
        var builder = new MarkupBuilder(baseSize);
        var root = MarkupParser.Parse(markup);
        var context = new BlockContext { Inline = new InlineStyle { Size = baseSize, Link = -1 }, Align = UITextAlign.Start };
        builder.BuildChildren(root, builder._document.Root, context);
        return builder._document;
    }

    private int NextIndex()
    {
        return ++_nextIndex;
    }

    private void BuildChildren(MarkupNode node, MarkupContainer container, BlockContext context)
    {
        if (node.Children == null)
        {
            return;
        }

        var paragraphContext = context;
        var inParagraph = false;
        var listCounter = 0;
        foreach (var child in node.Children)
        {
            if (child.Tag == "#text")
            {
                if (!inParagraph)
                {
                    paragraphContext = context;
                    inParagraph = true;
                    BeginParagraph();
                }

                AppendText(child.Text!, context.Inline, context.Pre);
                continue;
            }

            var style = ParseStyle(child.Attribute("style"));
            if (style != null && style.TryGetValue("display", out var display) && display == "none")
            {
                continue;
            }

            if (IsBlock(child, style))
            {
                if (inParagraph)
                {
                    FlushParagraph(container, paragraphContext);
                    inParagraph = false;
                }

                if (child.Tag == "li")
                {
                    listCounter++;
                }

                BuildBlock(child, style, container, context, node.Tag, listCounter);
                continue;
            }

            if (!inParagraph)
            {
                paragraphContext = context;
                inParagraph = true;
                BeginParagraph();
            }

            BuildInline(child, style, context.Inline, context.Pre);
        }

        if (inParagraph)
        {
            FlushParagraph(container, paragraphContext);
        }
    }

    private static bool IsBlock(MarkupNode node, Dictionary<string, string>? style)
    {
        if (style != null && style.TryGetValue("display", out var display))
        {
            if (display is "block" or "flex" or "list-item")
            {
                return true;
            }

            if (display is "inline" or "inline-block")
            {
                return false;
            }
        }

        return MarkupParser.BlockTags.Contains(node.Tag);
    }

    private void BuildBlock(MarkupNode node, Dictionary<string, string>? style, MarkupContainer parent, BlockContext context, string parentTag, int listIndex)
    {
        var inline = context.Inline;
        inline.Background = default;
        var box = new BlockBox { Width = UIValue.Auto, MaxWidth = UIValue.Auto };
        var tag = node.Tag;
        var em = inline.Size;
        switch (tag)
        {
            case "p":
            case "dl":
                box.MarginTop = box.MarginBottom = em * 0.75f;
                break;
            case "h1" or "h2" or "h3" or "h4" or "h5" or "h6":
                var scale = tag[1] switch { '1' => 2f, '2' => 1.55f, '3' => 1.3f, '4' => 1.12f, '5' => 1f, _ => 0.9f };
                inline.Size = _base * scale;
                inline.Bold = true;
                if (tag == "h6")
                {
                    inline.Color = Color.TextMuted;
                }

                box.MarginTop = inline.Size * 0.8f;
                box.MarginBottom = inline.Size * 0.35f;
                break;
            case "ul" or "ol":
                box.MarginTop = box.MarginBottom = parentTag is "li" ? 0 : em * 0.6f;
                break;
            case "li":
                box.MarginTop = box.MarginBottom = em * 0.2f;
                box.Row = true;
                break;
            case "dd":
                box.Padding = new Edges(em * 1.6f, 0, 0, 0);
                break;
            case "dt":
                inline.Bold = true;
                break;
            case "blockquote":
                box.MarginTop = box.MarginBottom = em * 0.75f;
                box.Padding = new Edges(em * 0.9f, em * 0.15f, 0, em * 0.15f);
                box.BorderColor = Color.BorderStrong;
                box.BorderWidth = new UIEdges(3, 0, 0, 0);
                inline.Color = Color.TextMuted;
                break;
            case "pre":
                box.MarginTop = box.MarginBottom = em * 0.75f;
                box.Padding = new Edges(em * 0.9f, em * 0.65f);
                box.Background = Color.SurfaceAlt;
                box.BorderColor = Color.Border;
                box.BorderWidth = UIEdges.All(1);
                box.Radius = 6;
                inline.Mono = true;
                inline.Size = em * 0.92f;
                context.Pre = true;
                break;
            case "hr":
                box.MarginTop = box.MarginBottom = em * 0.9f;
                break;
            case "center":
                context.Align = UITextAlign.Center;
                break;
            case "table":
                box.MarginTop = box.MarginBottom = em * 0.75f;
                box.BorderColor = Color.Border;
                box.BorderWidth = UIEdges.All(1);
                box.Radius = 6;
                break;
            case "tr":
                box.Row = true;
                break;
            case "th" or "td":
                box.Flex = 1;
                box.Padding = new Edges(em * 0.6f, em * 0.4f);
                box.BorderColor = Color.Border;
                box.BorderWidth = new UIEdges(0, 0, 0, 1);
                if (tag == "th")
                {
                    inline.Bold = true;
                    box.Background = Color.SurfaceAlt;
                }

                break;
        }

        context.Inline = inline;
        ApplyBlockStyle(style, ref box, ref context);
        ApplyAttributes(node, ref context);

        if (tag == "hr")
        {
            var rule = new MarkupRule { Box = box, Index = NextIndex(), Color = box.BorderColor.IsSet ? box.BorderColor : Color.Border };
            parent.Children.Add(rule);
            return;
        }

        if (tag == "img")
        {
            if (ImageOf(node, style, context.Inline) is { } image)
            {
                parent.Children.Add(new MarkupImageBlock { Box = box, Index = NextIndex(), Image = image });
            }

            return;
        }

        var container = new MarkupContainer { Box = box, Index = NextIndex() };
        if (tag == "li")
        {
            container.Marker = parentTag == "ol" ? listIndex.ToString(CultureInfo.InvariantCulture) + "." : "•";
            container.MarkerStyle = context.Inline with { Link = -1, Decoration = UITextDecorationFlagBits.None, Background = default };
            container.MarkerColor = Color.TextMuted;
        }

        parent.Children.Add(container);
        BuildChildren(node, container, context);
    }

    private void BuildInline(MarkupNode node, Dictionary<string, string>? style, InlineStyle inline, bool pre)
    {
        var em = inline.Size;
        inline.Background = default;
        switch (node.Tag)
        {
            case "b" or "strong":
                inline.Bold = true;
                break;
            case "i" or "em" or "cite" or "var" or "dfn":
                inline.Italic = true;
                break;
            case "u" or "ins":
                inline.Decoration |= UITextDecorationFlagBits.Underline;
                break;
            case "s" or "del" or "strike":
                inline.Decoration |= UITextDecorationFlagBits.LineThrough;
                break;
            case "code" or "kbd" or "samp" or "tt":
                inline.Mono = true;
                inline.Size = em * 0.92f;
                inline.Background = Color.Hover;
                break;
            case "small":
                inline.Size = em * 0.85f;
                break;
            case "big":
                inline.Size = em * 1.2f;
                break;
            case "mark":
                inline.Background = Color.Hex(0xF5C51870);
                break;
            case "sub":
                inline.Size = em * 0.75f;
                inline.Shift -= em * 0.22f;
                break;
            case "sup":
                inline.Size = em * 0.75f;
                inline.Shift += em * 0.38f;
                break;
            case "a" when node.Attribute("href") is { } href:
                inline.Link = _document.Links.Count;
                _document.Links.Add(new MarkupLink(href, node.Attribute("title")));
                inline.Color = Color.Accent;
                inline.Decoration |= UITextDecorationFlagBits.Underline;
                break;
            case "br":
                AppendBreak(inline);
                return;
            case "img":
                if (ImageOf(node, style, inline) is { } image)
                {
                    AppendImage(image, inline);
                }

                return;
        }

        ApplyInlineStyle(style, ref inline);
        if (node.Children == null)
        {
            return;
        }

        foreach (var child in node.Children)
        {
            if (child.Tag == "#text")
            {
                AppendText(child.Text!, inline, pre);
                continue;
            }

            var childStyle = ParseStyle(child.Attribute("style"));
            if (childStyle != null && childStyle.TryGetValue("display", out var display) && display == "none")
            {
                continue;
            }

            BuildInline(child, childStyle, inline, pre);
        }
    }

    private MarkupImage? ImageOf(MarkupNode node, Dictionary<string, string>? style, in InlineStyle inline)
    {
        var source = node.Attribute("src");
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        var image = new MarkupImage { Source = source.Trim(), Alt = node.Attribute("alt") };
        image.Width = Length(node.Attribute("width"), inline.Size);
        image.Height = Length(node.Attribute("height"), inline.Size);
        if (style != null)
        {
            if (style.TryGetValue("width", out var width))
            {
                image.Width = Length(width, inline.Size);
            }

            if (style.TryGetValue("height", out var height))
            {
                image.Height = Length(height, inline.Size);
            }

            if (style.TryGetValue("border-radius", out var radius))
            {
                image.Radius = Length(radius, inline.Size);
            }

            if (style.TryGetValue("vertical-align", out var align))
            {
                image.Align = align switch { "middle" => ImageAlign.Middle, "top" or "text-top" => ImageAlign.Top, _ => ImageAlign.Baseline };
            }
        }

        return image;
    }

    private void BeginParagraph()
    {
        _text.Clear();
        _runs.Clear();
        _lastSpace = true;
        _wordStart = true;
    }

    private void AppendText(string text, in InlineStyle style, bool pre)
    {
        var start = _text.Length;
        foreach (var c in text)
        {
            var space = c is ' ' or '\t' or '\n' or '\r' or '\f';
            if (pre)
            {
                if (c == '\r')
                {
                    continue;
                }

                if (c == '\n' && _text.Length == 0)
                {
                    continue;
                }

                _text.Append(c);
                _lastSpace = space;
                continue;
            }

            if (space)
            {
                if (!_lastSpace)
                {
                    _text.Append(' ');
                    _lastSpace = true;
                }

                _wordStart = true;
                continue;
            }

            var ch = style.Case switch
            {
                TextCase.Upper => char.ToUpperInvariant(c),
                TextCase.Lower => char.ToLowerInvariant(c),
                TextCase.Capitalize when _wordStart => char.ToUpperInvariant(c),
                _ => c
            };
            _text.Append(ch);
            _lastSpace = false;
            _wordStart = false;
        }

        if (_text.Length > start)
        {
            PushRun(style, null);
        }
    }

    private void AppendBreak(in InlineStyle style)
    {
        _text.Append('\n');
        _lastSpace = true;
        _wordStart = true;
        PushRun(style with { Background = default, Decoration = UITextDecorationFlagBits.None, Link = -1 }, null);
    }

    private void AppendImage(MarkupImage image, in InlineStyle style)
    {
        _text.Append('￼');
        _lastSpace = false;
        _wordStart = false;
        _runs.Add((_text.Length, style with { Background = default, Decoration = UITextDecorationFlagBits.None }, image));
    }

    private void PushRun(in InlineStyle style, MarkupImage? image)
    {
        if (_runs.Count > 0 && _runs[^1].Image == null && image == null && _runs[^1].Style.SameRun(style))
        {
            _runs[^1] = (_text.Length, _runs[^1].Style, null);
            return;
        }

        _runs.Add((_text.Length, style, image));
    }

    private void FlushParagraph(MarkupContainer container, in BlockContext context)
    {
        if (!context.Pre)
        {
            while (_text.Length > 0 && _text[^1] == ' ')
            {
                _text.Length--;
            }
        }
        else
        {
            while (_text.Length > 0 && _text[^1] == '\n')
            {
                _text.Length--;
            }
        }

        if (_text.Length == 0)
        {
            return;
        }

        // An image alone on its line scales down with its container like a block image, which an inline box cannot.
        if (_text.Length == 1 && _text[0] == '\uFFFC' && _runs[0].Image is { } lone)
        {
            container.Children.Add(new MarkupImageBlock { Index = NextIndex(), Image = lone, Box = new BlockBox { Width = UIValue.Auto, MaxWidth = UIValue.Auto } });
            BeginParagraph();
            return;
        }

        var text = _text.ToString();
        var paragraph = new MarkupParagraph
        {
            Index = NextIndex(),
            Utf8 = Encoding.UTF8.GetBytes(text),
            Size = context.Inline.Size,
            Color = context.Inline.Color,
            Align = context.Align,
            LineHeight = context.LineHeight,
            Wrap = context.NoWrap ? UITextWrap.None : context.Pre ? UITextWrap.Newlines : UITextWrap.Words
        };
        var charStart = 0;
        var byteEnd = 0;
        foreach (var (charEnd, style, image) in _runs)
        {
            var end = Math.Min(charEnd, text.Length);
            if (end <= charStart)
            {
                continue;
            }

            byteEnd += Encoding.UTF8.GetByteCount(text.AsSpan(charStart, end - charStart));
            paragraph.Runs.Add(new MarkupRun { ByteEnd = byteEnd, Style = style, Image = image });
            paragraph.HasLinks |= style.Link >= 0;
            charStart = end;
        }

        paragraph.Native = new UITextRun[paragraph.Runs.Count];
        container.Children.Add(paragraph);
        _document.Paragraphs.Add(paragraph);
        BeginParagraph();
    }

    private static void ApplyAttributes(MarkupNode node, ref BlockContext context)
    {
        if (node.Attribute("align") is { } align)
        {
            context.Align = TextAlign(align, context.Align);
        }
    }

    private void ApplyBlockStyle(Dictionary<string, string>? style, ref BlockBox box, ref BlockContext context)
    {
        if (style == null)
        {
            return;
        }

        var inline = context.Inline;
        ApplyInlineStyle(style, ref inline);
        inline.Background = default;
        context.Inline = inline;
        var em = inline.Size;
        foreach (var (property, value) in style)
        {
            switch (property)
            {
                case "background" or "background-color":
                    if (Colors.TryParse(value, out var background))
                    {
                        box.Background = background;
                    }

                    break;
                case "margin":
                {
                    var edges = EdgesOf(value, em);
                    box.MarginTop = edges.Top;
                    box.MarginBottom = edges.Bottom;
                    break;
                }
                case "margin-top":
                    box.MarginTop = Length(value, em);
                    break;
                case "margin-bottom":
                    box.MarginBottom = Length(value, em);
                    break;
                case "padding":
                    box.Padding = EdgesOf(value, em);
                    break;
                case "padding-left":
                    box.Padding = new Edges(Length(value, em), box.Padding.Top, box.Padding.Right, box.Padding.Bottom);
                    break;
                case "padding-top":
                    box.Padding = new Edges(box.Padding.Left, Length(value, em), box.Padding.Right, box.Padding.Bottom);
                    break;
                case "padding-right":
                    box.Padding = new Edges(box.Padding.Left, box.Padding.Top, Length(value, em), box.Padding.Bottom);
                    break;
                case "padding-bottom":
                    box.Padding = new Edges(box.Padding.Left, box.Padding.Top, box.Padding.Right, Length(value, em));
                    break;
                case "border":
                    Border(value, em, ref box, 0b1111);
                    break;
                case "border-left":
                    Border(value, em, ref box, 0b0001);
                    break;
                case "border-top":
                    Border(value, em, ref box, 0b0010);
                    break;
                case "border-right":
                    Border(value, em, ref box, 0b0100);
                    break;
                case "border-bottom":
                    Border(value, em, ref box, 0b1000);
                    break;
                case "border-color":
                    if (Colors.TryParse(value, out var borderColor))
                    {
                        box.BorderColor = borderColor;
                    }

                    break;
                case "border-width":
                {
                    var edges = EdgesOf(value, em);
                    box.BorderWidth = new UIEdges(edges.Left, edges.Top, edges.Right, edges.Bottom);
                    break;
                }
                case "border-radius":
                    box.Radius = Length(value, em);
                    break;
                case "width":
                    box.Width = Value(value, em);
                    break;
                case "max-width":
                    box.MaxWidth = Value(value, em);
                    break;
                case "text-align":
                    context.Align = TextAlign(value, context.Align);
                    break;
                case "line-height":
                    context.LineHeight = value == "normal" ? 0 :
                        float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var factor) ? factor * em : Length(value, em);
                    break;
                case "white-space":
                    context.Pre = value is "pre" or "pre-wrap" or "pre-line" or "break-spaces";
                    context.NoWrap = value is "nowrap" or "pre";
                    break;
            }
        }
    }

    private void Border(string value, float em, ref BlockBox box, int sides)
    {
        var width = 1f;
        var none = false;
        foreach (var part in SplitValues(value))
        {
            if (part is "none" or "hidden")
            {
                none = true;
            }
            else if (Colors.TryParse(part, out var color))
            {
                box.BorderColor = color;
            }
            else if (part is "thin")
            {
                width = 1;
            }
            else if (part is "medium")
            {
                width = 3;
            }
            else if (part is "thick")
            {
                width = 5;
            }
            else if (part.Length > 0 && (char.IsDigit(part[0]) || part[0] == '.'))
            {
                width = Length(part, em);
            }
        }

        width = none ? 0 : width;
        var w = box.BorderWidth;
        box.BorderWidth = new UIEdges((sides & 1) != 0 ? width : w.Left, (sides & 2) != 0 ? width : w.Top, (sides & 4) != 0 ? width : w.Right,
            (sides & 8) != 0 ? width : w.Bottom);
        if (!box.BorderColor.IsSet)
        {
            box.BorderColor = Color.Border;
        }
    }

    private void ApplyInlineStyle(Dictionary<string, string>? style, ref InlineStyle inline)
    {
        if (style == null)
        {
            return;
        }

        foreach (var (property, value) in style)
        {
            switch (property)
            {
                case "color":
                    if (Colors.TryParse(value, out var color))
                    {
                        inline.Color = color;
                    }

                    break;
                case "background" or "background-color":
                    if (Colors.TryParse(value, out var background))
                    {
                        inline.Background = background;
                    }

                    break;
                case "font-size":
                    inline.Size = FontSize(value, inline.Size);
                    break;
                case "font-weight":
                    inline.Bold = value is "bold" or "bolder" ||
                                  (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var weight) && weight >= 600);
                    break;
                case "font-style":
                    inline.Italic = value is "italic" or "oblique";
                    break;
                case "font-family":
                    inline.Mono = value.Contains("mono", StringComparison.OrdinalIgnoreCase) || value.Contains("courier", StringComparison.OrdinalIgnoreCase) ||
                                  value.Contains("consolas", StringComparison.OrdinalIgnoreCase);
                    break;
                case "font":
                    foreach (var part in SplitValues(value))
                    {
                        if (part is "bold" or "bolder")
                        {
                            inline.Bold = true;
                        }
                        else if (part is "italic" or "oblique")
                        {
                            inline.Italic = true;
                        }
                        else if (part.Length > 0 && char.IsDigit(part[0]) && !part.All(char.IsDigit))
                        {
                            inline.Size = FontSize(part.Split('/')[0], inline.Size);
                        }
                    }

                    break;
                case "text-decoration" or "text-decoration-line":
                    var decoration = UITextDecorationFlagBits.None;
                    foreach (var part in SplitValues(value))
                    {
                        decoration |= part switch
                        {
                            "underline" => UITextDecorationFlagBits.Underline,
                            "overline" => UITextDecorationFlagBits.Overline,
                            "line-through" => UITextDecorationFlagBits.LineThrough,
                            _ => UITextDecorationFlagBits.None
                        };
                        if (Colors.TryParse(part, out var decorationColor))
                        {
                            inline.DecorationColor = decorationColor;
                        }
                    }

                    inline.Decoration = decoration;
                    break;
                case "text-decoration-color":
                    if (Colors.TryParse(value, out var lineColor))
                    {
                        inline.DecorationColor = lineColor;
                    }

                    break;
                case "vertical-align":
                    inline.Shift = value switch
                    {
                        "super" => inline.Size * 0.38f,
                        "sub" => -inline.Size * 0.22f,
                        "baseline" => 0,
                        _ => Length(value, inline.Size)
                    };
                    break;
                case "text-transform":
                    inline.Case = value switch
                    {
                        "uppercase" => TextCase.Upper,
                        "lowercase" => TextCase.Lower,
                        "capitalize" => TextCase.Capitalize,
                        _ => TextCase.None
                    };
                    break;
            }
        }
    }

    private float FontSize(string value, float current)
    {
        return value switch
        {
            "xx-small" => _base * 0.6f,
            "x-small" => _base * 0.75f,
            "small" => _base * 0.89f,
            "medium" => _base,
            "large" => _base * 1.2f,
            "x-large" => _base * 1.5f,
            "xx-large" => _base * 2f,
            "smaller" => current * 0.83f,
            "larger" => current * 1.2f,
            _ => Length(value, current) is var size and > 0 ? size : current
        };
    }

    private static UITextAlign TextAlign(string value, UITextAlign current)
    {
        return value.ToLowerInvariant() switch
        {
            "left" => UITextAlign.Left,
            "right" => UITextAlign.Right,
            "center" or "middle" => UITextAlign.Center,
            "start" or "justify" => UITextAlign.Start,
            "end" => UITextAlign.End,
            _ => current
        };
    }

    private Edges EdgesOf(string value, float em)
    {
        var parts = SplitValues(value).Select(part => Length(part, em)).ToArray();
        return parts.Length switch
        {
            1 => new Edges(parts[0]),
            2 => new Edges(parts[1], parts[0], parts[1], parts[0]),
            3 => new Edges(parts[1], parts[0], parts[1], parts[2]),
            >= 4 => new Edges(parts[3], parts[0], parts[1], parts[2]),
            _ => default
        };
    }

    private UIValue Value(string value, float em)
    {
        value = value.Trim();
        if (value.EndsWith('%') && float.TryParse(value[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var percent))
        {
            return UIValue.Percent(percent / 100f);
        }

        var length = Length(value, em);
        return length > 0 ? UIValue.Points(length) : UIValue.Auto;
    }

    private float Length(string? value, float em)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        value = value.Trim().ToLowerInvariant();
        var unitStart = value.Length;
        while (unitStart > 0 && char.IsLetter(value[unitStart - 1]) || unitStart > 0 && value[unitStart - 1] == '%')
        {
            unitStart--;
        }

        if (!float.TryParse(value.AsSpan(0, unitStart), NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            return 0;
        }

        return value[unitStart..] switch
        {
            "" or "px" => number,
            "em" => number * em,
            "rem" => number * _base,
            "pt" => number * 4f / 3f,
            "%" => number / 100f * em,
            _ => 0
        };
    }

    private static IEnumerable<string> SplitValues(string value)
    {
        var depth = 0;
        var start = 0;
        for (var i = 0; i <= value.Length; i++)
        {
            if (i < value.Length && value[i] == '(')
            {
                depth++;
            }
            else if (i < value.Length && value[i] == ')')
            {
                depth--;
            }
            else if (i == value.Length || (depth == 0 && char.IsWhiteSpace(value[i])))
            {
                if (i > start)
                {
                    yield return value[start..i].Trim();
                }

                start = i + 1;
            }
        }
    }

    internal static Dictionary<string, string>? ParseStyle(string? style)
    {
        if (string.IsNullOrWhiteSpace(style))
        {
            return null;
        }

        var result = new Dictionary<string, string>();
        foreach (var declaration in style.Split(';'))
        {
            var colon = declaration.IndexOf(':');
            if (colon <= 0)
            {
                continue;
            }

            var name = declaration[..colon].Trim().ToLowerInvariant();
            var value = declaration[(colon + 1)..].Trim();
            if (value.EndsWith("!important", StringComparison.OrdinalIgnoreCase))
            {
                value = value[..^10].Trim();
            }

            result[name] = value;
        }

        return result;
    }
}

internal static class Colors
{
    private static readonly Dictionary<string, uint> Named = new(StringComparer.OrdinalIgnoreCase)
    {
        ["black"] = 0x000000FF, ["white"] = 0xFFFFFFFF, ["red"] = 0xFF0000FF, ["green"] = 0x008000FF, ["blue"] = 0x0000FFFF,
        ["yellow"] = 0xFFFF00FF, ["orange"] = 0xFFA500FF, ["purple"] = 0x800080FF, ["gray"] = 0x808080FF, ["grey"] = 0x808080FF,
        ["silver"] = 0xC0C0C0FF, ["maroon"] = 0x800000FF, ["navy"] = 0x000080FF, ["teal"] = 0x008080FF, ["olive"] = 0x808000FF,
        ["lime"] = 0x00FF00FF, ["aqua"] = 0x00FFFFFF, ["cyan"] = 0x00FFFFFF, ["fuchsia"] = 0xFF00FFFF, ["magenta"] = 0xFF00FFFF,
        ["pink"] = 0xFFC0CBFF, ["brown"] = 0xA52A2AFF, ["gold"] = 0xFFD700FF, ["coral"] = 0xFF7F50FF, ["crimson"] = 0xDC143CFF,
        ["indigo"] = 0x4B0082FF, ["violet"] = 0xEE82EEFF, ["tomato"] = 0xFF6347FF, ["orchid"] = 0xDA70D6FF, ["salmon"] = 0xFA8072FF,
        ["khaki"] = 0xF0E68CFF, ["tan"] = 0xD2B48CFF, ["skyblue"] = 0x87CEEBFF, ["steelblue"] = 0x4682B4FF, ["seagreen"] = 0x2E8B57FF,
        ["slategray"] = 0x708090FF, ["darkgray"] = 0xA9A9A9FF, ["lightgray"] = 0xD3D3D3FF, ["dodgerblue"] = 0x1E90FFFF,
        ["firebrick"] = 0xB22222FF, ["forestgreen"] = 0x228B22FF, ["goldenrod"] = 0xDAA520FF, ["hotpink"] = 0xFF69B4FF,
        ["limegreen"] = 0x32CD32FF, ["orangered"] = 0xFF4500FF, ["royalblue"] = 0x4169E1FF, ["turquoise"] = 0x40E0D0FF,
        ["transparent"] = 0x00000000
    };

    private static readonly Dictionary<string, Color> Theme = new(StringComparer.OrdinalIgnoreCase)
    {
        ["--text"] = Color.Text, ["--muted"] = Color.TextMuted, ["--accent"] = Color.Accent, ["--danger"] = Color.Danger,
        ["--surface"] = Color.Surface, ["--surface-alt"] = Color.SurfaceAlt, ["--border"] = Color.Border, ["--background"] = Color.Background,
        ["--accent-soft"] = Color.AccentSoft, ["--selection"] = Color.Selection
    };

    public static bool TryParse(string value, out Color color)
    {
        color = default;
        value = value.Trim();
        if (value.Length == 0)
        {
            return false;
        }

        if (value.StartsWith("var(", StringComparison.OrdinalIgnoreCase) && value.EndsWith(')'))
        {
            var name = value[4..^1].Split(',')[0].Trim();
            return Theme.TryGetValue(name, out color);
        }

        if (value[0] == '#')
        {
            return TryHex(value.AsSpan(1), out color);
        }

        if (Named.TryGetValue(value, out var rgba))
        {
            color = Color.Hex(rgba);
            return true;
        }

        var open = value.IndexOf('(');
        if (open < 0 || !value.EndsWith(')'))
        {
            return false;
        }

        var function = value[..open].Trim().ToLowerInvariant();
        var args = value[(open + 1)..^1].Replace(',', ' ').Replace('/', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (args.Length < 3)
        {
            return false;
        }

        var alpha = args.Length > 3 ? Channel(args[3], 1) : 1;
        if (function is "rgb" or "rgba")
        {
            color = Color.Rgba(Byte(Channel(args[0], 255)), Byte(Channel(args[1], 255)), Byte(Channel(args[2], 255)), Byte(alpha * 255));
            return true;
        }

        if (function is "hsl" or "hsla")
        {
            var h = float.TryParse(args[0].TrimEnd('d', 'e', 'g'), NumberStyles.Float, CultureInfo.InvariantCulture, out var hue) ? hue : 0;
            var s = Channel(args[1], 1);
            var l = Channel(args[2], 1);
            var (r, g, b) = Hsl(h, s, l);
            color = Color.Rgba(Byte(r * 255), Byte(g * 255), Byte(b * 255), Byte(alpha * 255));
            return true;
        }

        return false;
    }

    private static bool TryHex(ReadOnlySpan<char> hex, out Color color)
    {
        color = default;
        if (!uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
        {
            return false;
        }

        uint Expand(uint nibble) => nibble << 4 | nibble;
        switch (hex.Length)
        {
            case 3:
                color = Color.Hex(Expand(value >> 8 & 0xF) << 24 | Expand(value >> 4 & 0xF) << 16 | Expand(value & 0xF) << 8 | 0xFF);
                return true;
            case 4:
                color = Color.Hex(Expand(value >> 12 & 0xF) << 24 | Expand(value >> 8 & 0xF) << 16 | Expand(value >> 4 & 0xF) << 8 | Expand(value & 0xF));
                return true;
            case 6:
                color = Color.Hex(value << 8 | 0xFF);
                return true;
            case 8:
                color = Color.Hex(value);
                return true;
            default:
                return false;
        }
    }

    // A number scaled by max, or a percentage of it.
    private static float Channel(string value, float max)
    {
        if (value.EndsWith('%'))
        {
            return float.TryParse(value[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var percent) ? percent / 100f * max : 0;
        }

        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : 0;
    }

    private static byte Byte(float value)
    {
        return (byte)Math.Clamp(MathF.Round(value), 0, 255);
    }

    private static (float R, float G, float B) Hsl(float h, float s, float l)
    {
        h = (h % 360 + 360) % 360 / 360f;
        if (s <= 0)
        {
            return (l, l, l);
        }

        var q = l < 0.5f ? l * (1 + s) : l + s - l * s;
        var p = 2 * l - q;
        float Hue(float t)
        {
            t = t < 0 ? t + 1 : t > 1 ? t - 1 : t;
            return t < 1f / 6 ? p + (q - p) * 6 * t : t < 0.5f ? q : t < 2f / 3 ? p + (q - p) * (2f / 3 - t) * 6 : p;
        }

        return (Hue(h + 1f / 3), Hue(h), Hue(h - 1f / 3));
    }
}
