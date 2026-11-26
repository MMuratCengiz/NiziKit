using System.Numerics;
using DenOfIz;

namespace NiziKit.UI;

public abstract class Canvas : Widget
{
    private readonly Painter _painter;

    protected Canvas()
    {
        _painter = new Painter(this);
    }

    public ColorProp Background { get; set; }
    public SystemCursor Cursor { get; set; }

    public Hit Input => new(State);

    internal override bool Interactive => true;

    protected abstract void Paint(Painter p);

    internal override void Render()
    {
        var desc = Desc();
        ref var layout = ref desc.Layout;
        if (Flex <= 0 && Width.Type == UIValueType.Auto && Height.Type == UIValueType.Auto)
        {
            layout.Grow = 1;
            layout.Shrink = 1;
            layout.Basis = UIValue.Points(0);
            layout.AlignSelf = UIAlignSelf.Stretch;
        }

        desc.Scroll.OverflowX = UIOverflow.Clip;
        desc.Scroll.OverflowY = UIOverflow.Clip;
        desc.Style.BackgroundColor = Background.IsSet ? Background.Value.Resolve() : default;
        desc.Pointer = UIPointerMode.Block;
        desc.Cursor = Cursor;
        Ui.Open(in desc);
        OfferTooltip();
        Paint(_painter);
        Ui.Close();
    }

    public Hit HitOf(long key)
    {
        return new Hit(Ui.Interaction(KeyId(key)));
    }

    public void ShowTooltip(long key, string text)
    {
        Ui.OfferTooltip(KeyId(key), text);
    }

    internal uint KeyId(long key)
    {
        return Ui.Id(Ui.Id(Id, (uint)key), (uint)(key >> 32));
    }
}

public struct Paint
{
    public Color Background;
    public Color Border;
    public float BorderWidth;
    public Corners Radius;
    public Color Overlay;
    public SystemCursor Cursor;
    public UITransitionPropertyFlagBits Animate;
    public float Duration;
    public UIEasing Easing;
    public UITransitionState? Enter;
    public UITransitionState? Exit;
    public string? Text;
    public TextStyle TextStyle;
}

public readonly struct Hit
{
    private readonly UIInteraction _state;

    internal Hit(in UIInteraction state)
    {
        _state = state;
    }

    public bool Hovered => _state.Hovered;
    public bool Pressed => _state.Pressed;
    public bool PressStarted => _state.PressStarted;
    public bool Clicked => _state.Clicked;
    public bool DoubleClicked => _state.DoubleClicked;
    public bool RightClicked => _state.RightClicked;
    public bool Dragging => _state.Dragging;
    public bool DragStarted => _state.DragStarted;
    public bool DragEnded => _state.DragEnded;
    public Vector2 DragDelta => _state.DragDelta;
    public Vector2 PointerPosition => _state.PointerPosition;
    public Rectangle Rect => Rectangle.From(_state.Rect);
}

public sealed class Painter
{
    private readonly Canvas _canvas;

    internal Painter(Canvas canvas)
    {
        _canvas = canvas;
    }

    public Rectangle Bounds => _canvas.Bounds;

    public Hit Box(in Rectangle rect, in Paint paint, long key = 0)
    {
        var id = key != 0 ? _canvas.KeyId(key) : 0;
        var theme = Ui.Theme;
        var element = Element.Box(id)
            .Absolute(left: rect.X, top: rect.Y)
            .Size(rect.Width, rect.Height)
            .Background(paint.Background.Resolve(theme))
            .CornerRadius(paint.Radius.ToUI())
            .Overlay(paint.Overlay.Resolve(theme));
        if (paint.Border.IsSet)
        {
            element.Border(paint.Border.Resolve(theme), paint.BorderWidth > 0 ? paint.BorderWidth : 1);
        }

        if (key == 0)
        {
            element.Passthrough();
        }
        else
        {
            element.Pointer(UIPointerMode.Block).Cursor(paint.Cursor);
        }

        if (paint.Animate != UITransitionPropertyFlagBits.None || paint.Enter != null || paint.Exit != null)
        {
            element.Animate(paint.Animate, paint.Duration, paint.Easing);
            if (paint.Enter is { } enter)
            {
                element.Enter(enter);
            }

            if (paint.Exit is { } exit)
            {
                element.Exit(exit);
            }
        }

        if (paint.Text == null)
        {
            element.Leaf();
        }
        else
        {
            using (element.Center().Open())
            {
                var desc = paint.TextStyle.Resolve(theme);
                desc.Wrap = UITextWrap.None;
                Ui.Text(paint.Text, in desc);
            }
        }

        return key != 0 ? new Hit(Ui.Interaction(id)) : default;
    }

    public void Text(in Rectangle rect, ReadOnlySpan<char> text, TextStyle style = default, UITextAlign align = UITextAlign.Center)
    {
        var desc = style.Resolve(Ui.Theme);
        desc.Wrap = UITextWrap.None;
        desc.Align = align;
        using (Element.Column()
                   .Absolute(left: rect.X, top: rect.Y)
                   .Size(rect.Width, rect.Height)
                   .Justify(UIJustify.Center)
                   .Passthrough()
                   .Open())
        {
            Ui.Text(text, in desc);
        }
    }

    public void Glyph(in Rectangle rect, string glyph, float size, Color color)
    {
        using (Element.Row()
                   .Absolute(left: rect.X, top: rect.Y)
                   .Size(rect.Width, rect.Height)
                   .Center()
                   .Passthrough()
                   .Open())
        {
            Ui.Glyph(glyph, size, color.Resolve());
        }
    }
}
