using DenOfIz;

namespace NiziKit.UI;

public sealed class RichText : Widget
{
    private MarkupDocument? _document;
    private string? _source;
    private float _sourceSize;
    private int _hoveredLink = -1;
    private MarkupParagraph? _hoveredParagraph;

    public RichText(string markup, TextStyle style = default)
    {
        Markup = markup;
        Style = style;
    }

    public RichText(Func<string> markup, TextStyle style = default)
    {
        Markup = Binding.Bind(markup);
        Style = style;
    }

    public Prop<string> Markup { get; set; }
    public TextStyle Style { get; set; }
    public Action<string>? OnLink { get; set; }

    internal override void Dispatch()
    {
        base.Dispatch();
        _hoveredLink = -1;
        _hoveredParagraph = null;
        if (_document == null)
        {
            return;
        }

        foreach (var paragraph in _document.Paragraphs)
        {
            if (!paragraph.HasLinks)
            {
                continue;
            }

            var id = TextId(paragraph);
            var state = Ui.Interaction(id);
            if (!state.Hovered && !state.Clicked)
            {
                continue;
            }

            var run = Ui.Native.GetTextRunAtPoint(id, Ui.PointerPosition);
            if (run == 0 || run > _document.Links.Count)
            {
                continue;
            }

            var link = (int)run - 1;
            if (state.Hovered)
            {
                _hoveredLink = link;
                _hoveredParagraph = paragraph;
            }

            if (state.Clicked && EnabledNow && !Consumed)
            {
                ConsumeAncestors();
                OnLink?.Invoke(_document.Links[link].Href);
            }
        }
    }

    internal override void Render()
    {
        var theme = Ui.Theme;
        var text = Style.Resolve(theme, EnabledNow);
        var markup = Markup.Value ?? "";
        if (_document == null || text.FontSize != _sourceSize || !string.Equals(markup, _source, StringComparison.Ordinal))
        {
            _document = MarkupBuilder.Build(markup, text.FontSize);
            _source = markup;
            _sourceSize = text.FontSize;
            _hoveredLink = -1;
            _hoveredParagraph = null;
        }

        var desc = Desc();
        desc.Layout.Direction = UIDirection.Column;
        if (_hoveredLink >= 0)
        {
            desc.Cursor = SystemCursor.Pointer;
        }

        Ui.Open(in desc);
        OfferTooltip();
        Children(_document.Root, in text, theme);
        Ui.Close();
    }

    private uint TextId(MarkupParagraph paragraph)
    {
        return Ui.Id(Id, (uint)paragraph.Index * 2 + 2);
    }

    // Sibling margins collapse to the larger of the two, and the first and last child keep none so the box hugs its content.
    private void Children(MarkupContainer container, in UITextDesc text, Theme theme)
    {
        var children = container.Children;
        for (var i = 0; i < children.Count; i++)
        {
            var top = i == 0 ? 0 : Math.Max(children[i - 1].Box.MarginBottom, children[i].Box.MarginTop);
            Block(children[i], top, in text, theme);
        }
    }

    private void Block(MarkupBlock block, float top, in UITextDesc text, Theme theme)
    {
        var box = block.Box;
        var desc = new UIElementDesc { Id = Ui.Id(Id, (uint)block.Index * 2 + 1) };
        ref var layout = ref desc.Layout;
        layout.Direction = box.Row ? UIDirection.Row : UIDirection.Column;
        layout.Margin = new UILengthEdges(0, top, 0, 0);
        layout.Padding = box.Padding.ToLength();
        layout.Width = box.Width;
        layout.MaxWidth = box.MaxWidth;
        layout.Shrink = 1;
        if (box.Flex > 0)
        {
            layout.Grow = box.Flex;
            layout.Basis = UIValue.Points(0);
        }

        desc.Style.BackgroundColor = box.Background.IsSet ? box.Background.Resolve(theme) : default;
        desc.Style.BorderColor = box.BorderColor.IsSet ? box.BorderColor.Resolve(theme) : default;
        desc.Style.BorderWidth = box.BorderColor.IsSet ? box.BorderWidth : default;
        desc.Style.CornerRadius = UICorners.All(box.Radius);
        if (box.Radius > 0)
        {
            desc.Scroll.OverflowX = UIOverflow.Clip;
            desc.Scroll.OverflowY = UIOverflow.Clip;
        }

        switch (block)
        {
            case MarkupParagraph paragraph:
                Ui.Open(in desc);
                Paragraph(paragraph, in text, theme);
                Ui.Close();
                break;
            case MarkupContainer { Marker: { } marker } item:
                ListItem(item, marker, desc, in text, theme);
                break;
            case MarkupContainer container:
                Ui.Open(in desc);
                Children(container, in text, theme);
                Ui.Close();
                break;
            case MarkupImageBlock image:
                ImageBlock(image.Image, desc, theme);
                break;
            case MarkupRule rule:
                layout.Height = UIValue.Points(1);
                desc.Style.BackgroundColor = rule.Color.Resolve(theme);
                Ui.Open(in desc);
                Ui.Close();
                break;
        }
    }

    private void ListItem(MarkupContainer item, string marker, UIElementDesc desc, in UITextDesc text, Theme theme)
    {
        var id = desc.Id;
        desc.Layout.Direction = UIDirection.Row;
        desc.Layout.AlignItems = UIAlign.Baseline;
        Ui.Open(in desc);

        var size = item.MarkerStyle.Size;
        var markerBox = new UIElementDesc { Id = Ui.Id(id, 1) };
        markerBox.Layout.Direction = UIDirection.Row;
        markerBox.Layout.Justify = UIJustify.End;
        markerBox.Layout.Width = UIValue.Points(size * 1.6f);
        markerBox.Layout.Padding = new UILengthEdges(0, 0, size * 0.45f, 0);
        Ui.Open(in markerBox);
        var markerText = text;
        markerText.Id = Ui.Id(id, 3);
        markerText.FontSize = size;
        markerText.Color = item.MarkerColor.Resolve(theme);
        markerText.FontId = Fonts.Styled(text.FontId, item.MarkerStyle.Bold, false);
        markerText.Wrap = UITextWrap.None;
        Ui.Text(marker, in markerText);
        Ui.Close();

        var content = new UIElementDesc { Id = Ui.Id(id, 2) };
        content.Layout.Direction = UIDirection.Column;
        content.Layout.Grow = 1;
        content.Layout.Shrink = 1;
        content.Layout.Basis = UIValue.Points(0);
        Ui.Open(in content);
        Children(item, in text, theme);
        Ui.Close();

        Ui.Close();
    }

    private static void ImageBlock(MarkupImage image, UIElementDesc desc, Theme theme)
    {
        var entry = Images.Get(image.Source);
        ref var layout = ref desc.Layout;
        layout.AlignSelf = UIAlignSelf.Start;
        layout.MaxWidth = UIValue.Percent(1);
        if (image.Width > 0)
        {
            layout.Width = UIValue.Points(image.Width);
        }

        if (image.Height > 0)
        {
            layout.Height = UIValue.Points(image.Height);
        }

        if (entry.Texture == null && image.Width > 0 && image.Height <= 0)
        {
            layout.Height = UIValue.Points(image.Width * 0.5625f);
        }

        desc.Image.Texture = entry.Texture;
        desc.Image.Fit = UIImageFit.Contain;
        desc.Style.CornerRadius = UICorners.All(image.Radius);
        desc.Style.BackgroundColor = entry.Texture == null && !entry.Failed ? Color.SurfaceAlt.Resolve(theme) : default;
        Ui.Open(in desc);
        Ui.Close();
    }

    private void Paragraph(MarkupParagraph paragraph, in UITextDesc text, Theme theme)
    {
        var desc = text;
        desc.Id = TextId(paragraph);
        desc.FontSize = paragraph.Size;
        desc.Color = paragraph.Color.IsSet && EnabledNow ? paragraph.Color.Resolve(theme) : text.Color;
        desc.Wrap = paragraph.Wrap;
        desc.Align = paragraph.Align;
        desc.LineHeight = paragraph.LineHeight;

        var runs = paragraph.Runs;
        var native = paragraph.Native;
        for (var i = 0; i < runs.Count; i++)
        {
            var run = runs[i];
            var style = run.Style;
            var hovered = style.Link >= 0 && style.Link == _hoveredLink && paragraph == _hoveredParagraph;
            var color = style.Color.IsSet && EnabledNow ? style.Color.Resolve(theme) : default;
            if (hovered)
            {
                color = style.Color.Equals(Color.Accent) ? theme.AccentHover : color;
            }

            native[i] = new UITextRun
            {
                ByteEnd = (uint)run.ByteEnd,
                Id = style.Link >= 0 ? (uint)style.Link + 1 : 0,
                FontId = Fonts.Styled(style.Mono ? Fonts.DefaultFontId : text.FontId, style.Bold, style.Italic),
                FontSize = style.Size,
                Color = color,
                BackgroundColor = style.Background.IsSet ? style.Background.Resolve(theme) : default,
                Decoration = (uint)style.Decoration,
                DecorationColor = style.DecorationColor.IsSet ? style.DecorationColor.Resolve(theme) : default,
                BaselineShift = style.Shift
            };
            if (run.Image != null)
            {
                InlineImage(ref native[i], run.Image, style.Size, style.Shift);
            }
        }

        Ui.Native.RichText(paragraph.Utf8, native, in desc);
        if (paragraph == _hoveredParagraph && _hoveredLink >= 0 && _document!.Links[_hoveredLink].Title is { Length: > 0 } title)
        {
            Ui.OfferTooltip(desc.Id, title);
        }
    }

    private static void InlineImage(ref UITextRun run, MarkupImage image, float size, float shift)
    {
        var entry = Images.Get(image.Source);
        var width = image.Width;
        var height = image.Height;
        if (entry.Texture != null)
        {
            if (width <= 0 && height <= 0)
            {
                width = entry.Width;
                height = entry.Height;
            }
            else if (width <= 0)
            {
                width = height * entry.Width / entry.Height;
            }
            else if (height <= 0)
            {
                height = width * entry.Height / entry.Width;
            }
        }
        else
        {
            width = width > 0 ? width : height > 0 ? height : size;
            height = height > 0 ? height : width;
        }

        run.ObjectSize = new System.Numerics.Vector2(Math.Max(width, 1), Math.Max(height, 1));
        run.Image = entry.Texture is { } texture ? (ulong)texture : 0;
        run.BaselineShift = image.Align switch
        {
            ImageAlign.Middle => shift + size * 0.32f - height * 0.5f,
            ImageAlign.Top => shift + size * 0.75f - height,
            _ => shift
        };
    }
}
