using System.Numerics;
using DenOfIz;

namespace NiziKit.UI;

public class Panel : Widget
{
    private WidgetList _children = new();
    private Style _own;
    private Style _style;

    public Panel(UIDirection direction)
    {
        Direction = direction;
        _children.AttachTo(this);
    }

    public UIDirection Direction { get; set; }

    public WidgetList Children
    {
        get => _children;
        set
        {
            _children.Detach();
            _children = value;
            value.AttachTo(this);
        }
    }

    public Style Style
    {
        get => _style;
        set => _style = value;
    }

    public float Gap
    {
        get => _own.Gap ?? _style.Gap ?? 0;
        set => _own = _own with { Gap = value };
    }

    public float LineGap
    {
        get => _own.LineGap ?? _style.LineGap ?? 0;
        set => _own = _own with { LineGap = value };
    }

    public Edges Padding
    {
        get => _own.Padding ?? _style.Padding ?? default;
        set => _own = _own with { Padding = value };
    }

    public UIAlign Align
    {
        get => _own.Align ?? _style.Align ?? (Direction is UIDirection.Row or UIDirection.RowReverse ? UIAlign.Center : UIAlign.Stretch);
        set => _own = _own with { Align = value };
    }

    public UIJustify Justify
    {
        get => _own.Justify ?? _style.Justify ?? UIJustify.Start;
        set => _own = _own with { Justify = value };
    }

    public bool Wrap
    {
        get => _own.Wrap ?? _style.Wrap ?? false;
        set => _own = _own with { Wrap = value };
    }

    public ColorProp Background { get; set; }

    public Color HoverBackground
    {
        get => _own.HoverBackground ?? _style.HoverBackground ?? default;
        set => _own = _own with { HoverBackground = value };
    }

    public Color PressedBackground
    {
        get => _own.PressedBackground ?? _style.PressedBackground ?? default;
        set => _own = _own with { PressedBackground = value };
    }

    public Border Border
    {
        get => _own.Border ?? _style.Border ?? default;
        set => _own = _own with { Border = value };
    }

    public ColorProp BorderColor { get; set; }

    public Color HoverBorder
    {
        get => _own.HoverBorder ?? _style.HoverBorder ?? default;
        set => _own = _own with { HoverBorder = value };
    }

    public Corners Radius
    {
        get => _own.Radius ?? _style.Radius ?? default;
        set => _own = _own with { Radius = value };
    }

    public bool Clip
    {
        get => _own.Clip ?? _style.Clip ?? false;
        set => _own = _own with { Clip = value };
    }

    public ColorProp Overlay { get; set; }
    public Color Dividers { get; set; }
    public SystemCursor Cursor { get; set; }
    public bool PassThrough { get; set; }
    public UITransitionPropertyFlagBits Animate { get; set; }
    public float AnimateDuration { get; set; }
    public UIEasing Easing { get; set; }
    public UITransitionState? Enter { get; set; }
    public UITransitionState? Exit { get; set; }

    internal override bool Interactive => base.Interactive || HoverBackground.IsSet || PressedBackground.IsSet || HoverBorder.IsSet;

    private protected virtual UIOverflow OverflowX => Clip ? UIOverflow.Clip : UIOverflow.Visible;
    private protected virtual UIOverflow OverflowY => Clip ? UIOverflow.Clip : UIOverflow.Visible;
    private protected virtual float DefaultFlex => 0;
    private protected virtual bool FillsChildren => false;

    internal override void Render()
    {
        var desc = PanelDesc();
        Ui.Open(in desc);
        OfferTooltip();
        Ui.PushLayout(Direction is UIDirection.Row or UIDirection.RowReverse, Scrolls(desc), FillsChildren);
        BeforeChildren();
        BuildChildren();
        AfterChildren();
        Ui.PopLayout();
        Ui.Close();
    }

    private protected virtual void BuildChildren()
    {
        _children.BuildAll();
    }

    private protected virtual void BeforeChildren()
    {
    }

    private protected virtual void AfterChildren()
    {
    }

    private protected UIElementDesc PanelDesc()
    {
        var theme = Ui.Theme;
        var desc = Desc();
        ref var layout = ref desc.Layout;
        var row = Direction is UIDirection.Row or UIDirection.RowReverse;
        var styleSized = false;
        if (Width.Type == UIValueType.Auto && _style.Width is { } width)
        {
            layout.Width = width;
            styleSized = true;
        }

        if (Height.Type == UIValueType.Auto && _style.Height is { } height)
        {
            layout.Height = height;
            styleSized = true;
        }

        if (MinWidth.Type == UIValueType.Auto && _style.MinWidth is { } minWidth)
        {
            layout.MinWidth = minWidth;
        }

        if (MinHeight.Type == UIValueType.Auto && _style.MinHeight is { } minHeight)
        {
            layout.MinHeight = minHeight;
        }

        if (MaxWidth.Type == UIValueType.Auto && _style.MaxWidth is { } maxWidth)
        {
            layout.MaxWidth = maxWidth;
        }

        if (MaxHeight.Type == UIValueType.Auto && _style.MaxHeight is { } maxHeight)
        {
            layout.MaxHeight = maxHeight;
        }

        if (_style.Margin is { } margin && Margin is { Left: 0, Top: 0, Right: 0, Bottom: 0 })
        {
            layout.Margin = margin.ToLength();
        }

        var flex = Flex > 0 ? Flex : _style.Flex ?? 0;
        if (flex <= 0 && DefaultFlex > 0 && (Ui.ParentIsRow ? layout.Width.Type : layout.Height.Type) == UIValueType.Auto)
        {
            flex = DefaultFlex;
        }

        if (flex > 0)
        {
            layout.Grow = flex;
            layout.Shrink = 1;
            layout.Basis = Basis.Type == UIValueType.Auto ? UIValue.Points(0) : Basis;
        }
        else if (styleSized)
        {
            layout.Shrink = Ui.ShrinkFor(KeepsSize, layout.Width, layout.Height);
        }

        layout.Direction = Direction;
        layout.Gap = Gap;
        layout.LineGap = LineGap;
        layout.Padding = Padding.ToLength();
        layout.AlignItems = Align;
        layout.Justify = Justify;
        layout.Wrap = Wrap ? UIFlexWrap.Wrap : UIFlexWrap.NoWrap;

        var hovered = State.Hovered && EnabledNow;
        var pressed = State.Pressed && EnabledNow;
        var background = Background.IsSet ? Background.Value : _style.Background ?? default;
        if (pressed && PressedBackground.IsSet)
        {
            background = PressedBackground;
        }
        else if (hovered && HoverBackground.IsSet)
        {
            background = HoverBackground;
        }

        var border = Border;
        var borderColor = BorderColor.IsSet ? BorderColor.Value : border.Color;
        if (hovered && HoverBorder.IsSet)
        {
            borderColor = HoverBorder;
        }

        var borderWidth = border.Width;
        if (IsFocused && Ui.FocusVisible && Focusable)
        {
            borderColor = Color.FocusRing;
            if (!border.IsSet)
            {
                borderWidth = 1;
            }
        }
        else if (!border.IsSet && !BorderColor.IsSet)
        {
            borderWidth = 0;
        }
        else if (!border.IsSet)
        {
            borderWidth = 1;
        }

        ref var style = ref desc.Style;
        style.BackgroundColor = background.Resolve(theme);
        style.BorderColor = borderColor.Resolve(theme);
        style.BorderWidth = borderWidth.ToEdges();
        style.CornerRadius = Radius.ToUI();
        style.OverlayColor = Overlay.IsSet ? Overlay.Value.Resolve(theme) : default;
        if (Dividers.IsSet)
        {
            style.DividerColor = Dividers.Resolve(theme);
            style.DividerWidth = 1;
        }

        desc.Scroll.OverflowX = OverflowX;
        desc.Scroll.OverflowY = OverflowY;
        if (Cursor != SystemCursor.Default)
        {
            desc.Cursor = Cursor;
        }

        if (PassThrough)
        {
            desc.Pointer = UIPointerMode.Passthrough;
        }

        var animate = Animate;
        if (animate == UITransitionPropertyFlagBits.None && (HoverBackground.IsSet || PressedBackground.IsSet || HoverBorder.IsSet || Clickable))
        {
            animate = UITransitionPropertyFlagBits.Colors;
        }

        if (animate != UITransitionPropertyFlagBits.None || Enter != null || Exit != null)
        {
            desc.Transition.Duration = AnimateDuration > 0 ? AnimateDuration : theme.TransitionDuration;
            desc.Transition.Properties = (uint)animate;
            desc.Transition.Easing = Easing;
            if (Enter is { } enter)
            {
                desc.Transition.Enter = enter;
            }

            if (Exit is { } exit)
            {
                desc.Transition.Exit = exit;
            }
        }

        return desc;
    }

    private static bool Scrolls(in UIElementDesc desc)
    {
        return desc.Layout.Direction is UIDirection.Row or UIDirection.RowReverse
            ? desc.Scroll.OverflowX == UIOverflow.Scroll
            : desc.Scroll.OverflowY == UIOverflow.Scroll;
    }
}

public class Row : Panel
{
    public Row() : base(UIDirection.Row)
    {
    }
}

public class Column : Panel
{
    public Column() : base(UIDirection.Column)
    {
    }
}

public class ScrollView : Panel
{
    private const float EndSlack = 24;

    private Vector2 _saved;
    private bool _stick = true;
    private bool _follow = true;
    private int _pending;
    private float _lastContent;
    private float _lastViewport;
    private bool _atEnd = true;
    private bool _toEnd;
    private Vector2? _scrollTo;
    private Widget? _reveal;

    public ScrollView() : base(UIDirection.Column)
    {
    }

    public bool Horizontal { get; set; }
    public bool HideScrollbar { get; set; }
    public bool FollowEnd { get; set; }
    public Action? OnReachEnd { get; set; }

    public bool IsAtEnd => FollowEnd ? _stick : _atEnd;

    public Vector2 ScrollOffset => Ui.TryGetScroll(Id, out var state) ? state.Offset : _saved;

    public void ScrollToEnd()
    {
        _stick = true;
        _follow = true;
        _toEnd = true;
    }

    public void ScrollToTop()
    {
        _stick = false;
        _scrollTo = Vector2.Zero;
    }

    public void ScrollTo(Vector2 offset)
    {
        _stick = false;
        _scrollTo = offset;
    }

    public void ScrollIntoView(Widget widget)
    {
        _reveal = widget;
    }

    private protected override float DefaultFlex => Horizontal == Ui.ParentIsRow ? 1 : 0;
    private protected override UIOverflow OverflowX => Horizontal ? UIOverflow.Scroll : UIOverflow.Clip;
    private protected override UIOverflow OverflowY => Horizontal ? UIOverflow.Clip : UIOverflow.Scroll;

    internal override bool Interactive => true;

    internal override void Dispatch()
    {
        base.Dispatch();
        if (!Ui.TryGetScroll(Id, out var scroll))
        {
            return;
        }

        _saved = scroll.Offset;
        var axis = Horizontal ? 0 : 1;
        var offset = axis == 0 ? scroll.Offset.X : scroll.Offset.Y;
        var max = axis == 0 ? scroll.MaxOffset.X : scroll.MaxOffset.Y;
        var content = axis == 0 ? scroll.ContentSize.X : scroll.ContentSize.Y;
        var viewport = axis == 0 ? scroll.ViewportSize.X : scroll.ViewportSize.Y;
        var atEnd = offset >= max - EndSlack;
        var resized = MathF.Abs(content - _lastContent) > 0.5f || MathF.Abs(viewport - _lastViewport) > 0.5f;
        _lastContent = content;
        _lastViewport = viewport;

        var wasAtEnd = IsAtEnd;
        _atEnd = atEnd;
        if (FollowEnd)
        {
            if (_pending > 0)
            {
                _pending = atEnd ? 0 : _pending - 1;
            }
            else if (_stick && resized && !atEnd)
            {
                _follow = true;
            }
            else
            {
                _stick = atEnd;
            }
        }

        if (!wasAtEnd && IsAtEnd)
        {
            OnReachEnd?.Invoke();
        }
    }

    private protected override void BeforeChildren()
    {
        if (!BuiltLastFrame && _saved != Vector2.Zero)
        {
            Ui.ScrollTo(Id, _saved);
        }
    }

    private protected override void AfterChildren()
    {
        if (_scrollTo is { } offset)
        {
            Ui.ScrollTo(Id, offset);
            _scrollTo = null;
        }

        if ((FollowEnd && _follow && _stick) || _toEnd)
        {
            var end = Horizontal ? new Vector2(float.MaxValue, _saved.Y) : new Vector2(_saved.X, float.MaxValue);
            Ui.ScrollTo(Id, end);
            _pending = 3;
            _toEnd = false;
        }

        _follow = false;
        if (_reveal != null)
        {
            Ui.ScrollIntoView(_reveal.Id);
            _reveal = null;
        }
    }

    internal override void Render()
    {
        var desc = PanelDesc();
        if (HideScrollbar)
        {
            desc.Scroll.Scrollbar = UIScrollbarMode.Hidden;
        }

        Ui.Open(in desc);
        OfferTooltip();
        Ui.PushLayout(Direction is UIDirection.Row or UIDirection.RowReverse, true);
        BeforeChildren();
        BuildChildren();
        AfterChildren();
        Ui.PopLayout();
        Ui.Close();
        WarnIfUnbounded();
    }

    private bool _warned;

    private void WarnIfUnbounded()
    {
        if (_warned || !BuiltLastFrame || !Ui.TryGetRect(Id, out var rect) || Children.Count == 0)
        {
            return;
        }

        if ((Horizontal ? rect.Width : rect.Height) < 1)
        {
            _warned = true;
            Console.Error.WriteLine($"NiziKit.UI: {this} has no {(Horizontal ? "width" : "height")} because nothing above it limits its size. " +
                                    $"Give it a {(Horizontal ? "Width" : "Height")}, or give its parent a fixed size or Flex = 1.");
        }
    }
}

public sealed class Spacer : Widget
{
    internal override void Render()
    {
        var desc = Desc();
        desc.Layout.Grow = Flex > 0 ? Flex : 1;
        desc.Layout.Shrink = 1;
        desc.Layout.Basis = UIValue.Points(0);
        Ui.Open(in desc);
        Ui.Close();
    }
}

public sealed class Divider : Widget
{
    public Color Color { get; set; }
    public float Thickness { get; set; } = 1;

    internal override void Render()
    {
        var vertical = Ui.ParentIsRow;
        var desc = Desc();
        desc.Layout.AlignSelf = UIAlignSelf.Stretch;
        desc.Layout.Shrink = 0;
        if (vertical)
        {
            desc.Layout.Width = UIValue.Points(Thickness);
        }
        else
        {
            desc.Layout.Height = UIValue.Points(Thickness);
        }

        if (Margin is { Left: 0, Top: 0, Right: 0, Bottom: 0 })
        {
            desc.Layout.Margin = (vertical ? new Edges(4, 2) : new Edges(0, 4)).ToLength();
        }

        desc.Style.BackgroundColor = (Color.IsSet ? Color : NiziKit.UI.Color.Border).Resolve();
        Ui.Open(in desc);
        Ui.Close();
    }
}
