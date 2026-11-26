using System.Numerics;
using System.Text;
using DenOfIz;

namespace NiziKit.UI;

public class Label : Widget, ISelectableText
{
    private uint _textId;
    private TextSelection? _selection;

    public Label(string text, TextStyle style = default)
    {
        Text = text;
        Style = style;
    }

    public Label(TextSource text, TextStyle style = default)
    {
        Text = new TextValue(text);
        Style = style;
    }

    public TextValue Text { get; set; }
    public TextStyle Style { get; set; }
    public ColorProp Color { get; set; }
    public float Size { get; set; }
    public UITextAlign Align { get; set; }
    public int MaxLines { get; set; }

    /// <summary>
    /// Lets the user select the text: drag, double click for a word, triple click for all, Shift+click to extend. Cmd+C (Ctrl+C) copies and
    /// Cmd+A (Ctrl+A) selects all after a press in the text, unless the focused widget takes the key. The text takes no focus or Tab stop, and
    /// a press anywhere else clears the selection.
    /// </summary>
    public bool Selectable { get; set; }

    /// <summary>
    /// The selected text, empty without a selection. Inside a <see cref="SelectionScope"/>, the part of the scope's selection in this widget.
    /// </summary>
    public string SelectedText => _selection?.TextOf(this) ?? "";

    private protected virtual UITextWrap WrapMode => UITextWrap.None;

    internal override bool Interactive => base.Interactive || Selectable;

    private bool NeedsBox => Width.Type != UIValueType.Auto || Height.Type != UIValueType.Auto || MinWidth.Type != UIValueType.Auto ||
                             MaxWidth.Type != UIValueType.Auto || Flex > 0 || Float != null || AlignSelf != UIAlignSelf.Auto ||
                             Margin is not { Left: 0, Top: 0, Right: 0, Bottom: 0 } || Clickable || Selectable;

    Widget ISelectableText.Widget => this;

    int ISelectableText.BlockCount => 1;

    ReadOnlySpan<byte> ISelectableText.Block(int index)
    {
        return Encoding.UTF8.GetBytes(Text.ToString());
    }

    uint ISelectableText.TextId(int block)
    {
        return _textId;
    }

    (int Block, int Offset) ISelectableText.OffsetAt(Vector2 point)
    {
        return (0, (int)Ui.Native.GetTextOffsetAtPoint(_textId, point));
    }

    protected internal override void Describe(UiWidgetInfo info)
    {
        info.Text = Text.ToString();
        if (MaxLines > 0)
        {
            info.Set("maxLines", MaxLines);
        }

        TextSelection.Describe(info, Selectable, SelectedText);
    }

    internal override void Dispatch()
    {
        base.Dispatch();
        if (Selectable && EnabledNow)
        {
            _selection?.Update(this, in State);
        }
        else if (_selection is { IsScoped: false } selection)
        {
            selection.Clear();
        }
    }

    internal override void Render()
    {
        if (Selectable && EnabledNow)
        {
            _selection = TextSelection.Attach(this, _selection);
        }

        var theme = Ui.Theme;
        var desc = Style.Resolve(theme, EnabledNow);
        if (Size > 0)
        {
            desc.FontSize = Size;
        }

        if (Color.IsSet && EnabledNow)
        {
            desc.Color = Color.Value.Resolve(theme);
        }

        var wrap = WrapMode;
        desc.Wrap = wrap;
        desc.Overflow = wrap == UITextWrap.None || MaxLines > 0 ? UITextOverflow.Ellipsis : UITextOverflow.Clip;
        desc.MaxLines = (uint)MaxLines;
        desc.Align = Align;
        var text = Text.Read();
        if (!NeedsBox)
        {
            desc.Id = Id;
            Ui.Text(text, in desc);
            OfferTooltip();
            return;
        }

        var box = Desc();
        box.Layout.Direction = UIDirection.Column;
        box.Layout.Justify = UIJustify.Center;
        if (Selectable && !Clickable)
        {
            box.Cursor = SystemCursor.Text;
        }

        if (_selection is { HasSelection: true } selection && selection.Range(this, 0, Encoding.UTF8.GetByteCount(text), out var start, out var end))
        {
            desc.Selection = new UITextSelection { Start = start, End = end, Color = theme.Selection };
        }

        Ui.Open(in box);
        OfferTooltip();
        if (_textId == 0)
        {
            _textId = Ui.Id(Id, 1);
        }

        desc.Id = _textId;
        Ui.Text(text, in desc);
        Ui.Close();
    }
}

public class Paragraph : Label
{
    public Paragraph(string text, TextStyle style = default) : base(text, style)
    {
    }

    public Paragraph(TextSource text, TextStyle style = default) : base(text, style)
    {
    }

    public bool BreakAnywhere { get; set; }

    private protected override UITextWrap WrapMode => BreakAnywhere ? UITextWrap.Anywhere : UITextWrap.Words;
}

public sealed class Icon : Widget
{
    private readonly string? _glyph;
    private readonly Func<string>? _source;

    public Icon(string glyph)
    {
        _glyph = glyph;
    }

    public Icon(Func<string> glyph)
    {
        _source = glyph;
    }

    public float Size { get; set; }
    public ColorProp Color { get; set; }

    protected internal override void Describe(UiWidgetInfo info)
    {
        info.Set("glyph", UiWidgetInfo.Glyph(_source?.Invoke() ?? _glyph));
    }

    internal override void Render()
    {
        if (!FontAwesome.IsInitialized)
        {
            return;
        }

        var theme = Ui.Theme;
        var desc = Ui.TextStyleOf(Size, !EnabledNow ? theme.TextDisabled : Color.IsSet ? Color.Value.Resolve(theme) : theme.Text, FontAwesome.FontId);
        desc.Wrap = UITextWrap.None;
        var glyph = _source != null ? _source() : _glyph;
        if (!NeedsBox)
        {
            desc.Id = Id;
            Ui.Text(glyph, in desc);
            OfferTooltip();
            return;
        }

        var box = Desc();
        box.Layout.Justify = UIJustify.Center;
        box.Layout.AlignItems = UIAlign.Center;
        Ui.Open(in box);
        OfferTooltip();
        desc.Id = Ui.Id(Id, 1);
        Ui.Text(glyph, in desc);
        Ui.Close();
    }

    private bool NeedsBox => Width.Type != UIValueType.Auto || Height.Type != UIValueType.Auto || Float != null || AlignSelf != UIAlignSelf.Auto ||
                             Margin is not { Left: 0, Top: 0, Right: 0, Bottom: 0 } || Clickable;
}

public sealed class Image : Widget
{
    public Image(Texture? texture = null)
    {
        Source = texture;
    }

    public Prop<Texture?> Source { get; set; }
    public UIImageFit Fit { get; set; }
    public Color Tint { get; set; }
    public Vector4 Uv { get; set; }
    public ColorProp Background { get; set; }
    public Corners Radius { get; set; }
    public ColorProp Overlay { get; set; }
    public Color HoverOverlay { get; set; }

    internal override bool Interactive => base.Interactive || HoverOverlay.IsSet;

    protected internal override void Describe(UiWidgetInfo info)
    {
        info.Set("loaded", Source.Value != null);
        info.Set("fit", Fit);
    }

    internal override void Render()
    {
        var theme = Ui.Theme;
        var desc = Desc();
        desc.Image.Texture = Source.Value;
        desc.Image.Fit = Fit;
        desc.Image.Tint = Tint.IsSet ? Tint.Resolve(theme) : default;
        desc.Image.UV = Uv;
        desc.Style.BackgroundColor = Background.IsSet ? Background.Value.Resolve(theme) : default;
        desc.Style.CornerRadius = Radius.ToUI();
        var overlay = HoverOverlay.IsSet && State.Hovered ? HoverOverlay : Overlay.IsSet ? Overlay.Value : default;
        desc.Style.OverlayColor = overlay.Resolve(theme);
        if (HoverOverlay.IsSet)
        {
            desc.Transition.Duration = 0.2f;
            desc.Transition.Properties = (uint)UITransitionPropertyFlagBits.OverlayColor;
        }

        Ui.Open(in desc);
        OfferTooltip();
        Ui.Close();
    }
}
