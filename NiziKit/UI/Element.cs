using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DenOfIz;

namespace NiziKit.UI;

internal struct Element
{
    public static Element Box(uint id = 0)
    {
        return new Element(id);
    }

    public static Element Row(uint id = 0)
    {
        return new Element(id);
    }

    public static Element Column(uint id = 0)
    {
        var element = new Element(id);
        element._desc.Layout.Direction = UIDirection.Column;
        return element;
    }

    private UIElementDesc _desc;

    public Element(uint id)
    {
        _desc.Id = id;
    }

    public Element(in UIElementDesc desc)
    {
        _desc = desc;
    }

    [UnscopedRef]
    public ref UIElementDesc Desc => ref _desc;

    [UnscopedRef]
    public ref Element Id(uint id)
    {
        _desc.Id = id;
        return ref this;
    }

    [UnscopedRef]
    public ref Element Direction(UIDirection direction)
    {
        _desc.Layout.Direction = direction;
        return ref this;
    }

    [UnscopedRef]
    public ref Element Width(UIValue width)
    {
        _desc.Layout.Width = width;
        return ref this;
    }

    [UnscopedRef]
    public ref Element Height(UIValue height)
    {
        _desc.Layout.Height = height;
        return ref this;
    }

    [UnscopedRef]
    public ref Element Size(UIValue width, UIValue height)
    {
        _desc.Layout.Width = width;
        _desc.Layout.Height = height;
        return ref this;
    }

    [UnscopedRef]
    public ref Element MinWidth(UIValue width)
    {
        _desc.Layout.MinWidth = width;
        return ref this;
    }

    [UnscopedRef]
    public ref Element MinHeight(UIValue height)
    {
        _desc.Layout.MinHeight = height;
        return ref this;
    }

    [UnscopedRef]
    public ref Element MaxWidth(UIValue width)
    {
        _desc.Layout.MaxWidth = width;
        return ref this;
    }

    [UnscopedRef]
    public ref Element MaxHeight(UIValue height)
    {
        _desc.Layout.MaxHeight = height;
        return ref this;
    }

    [UnscopedRef]
    public ref Element AspectRatio(float widthOverHeight)
    {
        _desc.Layout.AspectRatio = widthOverHeight;
        return ref this;
    }

    [UnscopedRef]
    public ref Element Grow(float grow = 1)
    {
        _desc.Layout.Grow = grow;
        return ref this;
    }

    [UnscopedRef]
    public ref Element Shrink(float shrink = 1)
    {
        _desc.Layout.Shrink = shrink;
        return ref this;
    }

    [UnscopedRef]
    public ref Element Basis(UIValue basis)
    {
        _desc.Layout.Basis = basis;
        return ref this;
    }

    [UnscopedRef]
    public ref Element Fill()
    {
        _desc.Layout.Grow = 1;
        _desc.Layout.Shrink = 1;
        _desc.Layout.Basis = UIValue.Points(0);
        return ref this;
    }

    [UnscopedRef]
    public ref Element Justify(UIJustify justify)
    {
        _desc.Layout.Justify = justify;
        return ref this;
    }

    [UnscopedRef]
    public ref Element AlignItems(UIAlign align)
    {
        _desc.Layout.AlignItems = align;
        return ref this;
    }

    [UnscopedRef]
    public ref Element AlignSelf(UIAlignSelf align)
    {
        _desc.Layout.AlignSelf = align;
        return ref this;
    }

    [UnscopedRef]
    public ref Element AlignContent(UIAlignContent align)
    {
        _desc.Layout.AlignContent = align;
        return ref this;
    }

    [UnscopedRef]
    public ref Element Center()
    {
        _desc.Layout.Justify = UIJustify.Center;
        _desc.Layout.AlignItems = UIAlign.Center;
        return ref this;
    }

    [UnscopedRef]
    public ref Element Wrap(UIFlexWrap wrap = UIFlexWrap.Wrap)
    {
        _desc.Layout.Wrap = wrap;
        return ref this;
    }

    [UnscopedRef]
    public ref Element Gap(UILength gap)
    {
        _desc.Layout.Gap = gap;
        return ref this;
    }

    [UnscopedRef]
    public ref Element LineGap(UILength gap)
    {
        _desc.Layout.LineGap = gap;
        return ref this;
    }

    [UnscopedRef]
    public ref Element Padding(UILength all)
    {
        _desc.Layout.Padding = UILengthEdges.All(all);
        return ref this;
    }

    [UnscopedRef]
    public ref Element Padding(UILength horizontal, UILength vertical)
    {
        _desc.Layout.Padding = UILengthEdges.Symmetric(horizontal, vertical);
        return ref this;
    }

    [UnscopedRef]
    public ref Element Padding(UILength left, UILength top, UILength right, UILength bottom)
    {
        _desc.Layout.Padding = new UILengthEdges(left, top, right, bottom);
        return ref this;
    }

    [UnscopedRef]
    public ref Element Margin(UILength all)
    {
        _desc.Layout.Margin = UILengthEdges.All(all);
        return ref this;
    }

    [UnscopedRef]
    public ref Element Margin(UILength horizontal, UILength vertical)
    {
        _desc.Layout.Margin = UILengthEdges.Symmetric(horizontal, vertical);
        return ref this;
    }

    [UnscopedRef]
    public ref Element Margin(UILength left = default, UILength top = default, UILength right = default, UILength bottom = default)
    {
        _desc.Layout.Margin = new UILengthEdges(left, top, right, bottom);
        return ref this;
    }

    [UnscopedRef]
    public ref Element Position(UIPosition position)
    {
        _desc.Layout.Position = position;
        return ref this;
    }

    [UnscopedRef]
    public ref Element Absolute(UIValue left = default, UIValue top = default, UIValue right = default, UIValue bottom = default)
    {
        _desc.Layout.Position = UIPosition.Absolute;
        _desc.Layout.Insets = new UIInsets(left, top, right, bottom);
        return ref this;
    }

    [UnscopedRef]
    public ref Element Insets(UIValue left = default, UIValue top = default, UIValue right = default, UIValue bottom = default)
    {
        _desc.Layout.Insets = new UIInsets(left, top, right, bottom);
        return ref this;
    }

    [UnscopedRef]
    public ref Element Display(UIDisplay display)
    {
        _desc.Layout.Display = display;
        return ref this;
    }

    [UnscopedRef]
    public ref Element Hidden(bool hidden = true)
    {
        if (hidden)
        {
            _desc.Layout.Display = UIDisplay.None;
        }

        return ref this;
    }

    [UnscopedRef]
    public ref Element BoxSizing(UIBoxSizing boxSizing)
    {
        _desc.Layout.BoxSizing = boxSizing;
        return ref this;
    }

    [UnscopedRef]
    public ref Element Background(UIColor color)
    {
        _desc.Style.BackgroundColor = color;
        return ref this;
    }

    [UnscopedRef]
    public ref Element Border(UIColor color, float width = 1)
    {
        _desc.Style.BorderColor = color;
        _desc.Style.BorderWidth = UIEdges.All(width);
        return ref this;
    }

    [UnscopedRef]
    public ref Element Border(UIColor color, in UIEdges width)
    {
        _desc.Style.BorderColor = color;
        _desc.Style.BorderWidth = width;
        return ref this;
    }

    [UnscopedRef]
    public ref Element CornerRadius(float radius)
    {
        _desc.Style.CornerRadius = UICorners.All(radius);
        return ref this;
    }

    [UnscopedRef]
    public ref Element CornerRadius(in UICorners radius)
    {
        _desc.Style.CornerRadius = radius;
        return ref this;
    }

    [UnscopedRef]
    public ref Element Overlay(UIColor color)
    {
        _desc.Style.OverlayColor = color;
        return ref this;
    }

    [UnscopedRef]
    public ref Element Dividers(UIColor color, float width = 1)
    {
        _desc.Style.DividerColor = color;
        _desc.Style.DividerWidth = width;
        return ref this;
    }

    [UnscopedRef]
    public ref Element Clip()
    {
        _desc.Scroll.OverflowX = UIOverflow.Clip;
        _desc.Scroll.OverflowY = UIOverflow.Clip;
        return ref this;
    }

    [UnscopedRef]
    public ref Element ScrollY()
    {
        _desc.Scroll.OverflowX = UIOverflow.Clip;
        _desc.Scroll.OverflowY = UIOverflow.Scroll;
        return ref this;
    }

    [UnscopedRef]
    public ref Element ScrollX()
    {
        _desc.Scroll.OverflowX = UIOverflow.Scroll;
        _desc.Scroll.OverflowY = UIOverflow.Clip;
        return ref this;
    }

    [UnscopedRef]
    public ref Element Overflow(UIOverflow x, UIOverflow y)
    {
        _desc.Scroll.OverflowX = x;
        _desc.Scroll.OverflowY = y;
        return ref this;
    }

    [UnscopedRef]
    public ref Element HideScrollbar()
    {
        _desc.Scroll.Scrollbar = UIScrollbarMode.Hidden;
        return ref this;
    }

    [UnscopedRef]
    public ref Element Scrollbar(UIColor color, float width = 0)
    {
        _desc.Scroll.ScrollbarColor = color;
        _desc.Scroll.ScrollbarWidth = width;
        return ref this;
    }

    [UnscopedRef]
    public ref Element Image(Texture? texture, UIImageFit fit = UIImageFit.Fill)
    {
        _desc.Image.Texture = texture;
        _desc.Image.Fit = fit;
        return ref this;
    }

    [UnscopedRef]
    public ref Element Tint(UIColor color)
    {
        _desc.Image.Tint = color;
        return ref this;
    }

    [UnscopedRef]
    public ref Element Uv(Vector4 uv)
    {
        _desc.Image.UV = uv;
        return ref this;
    }

    [UnscopedRef]
    public ref Element FloatOnParent(UIAnchor targetAnchor = UIAnchor.TopLeft, UIAnchor anchor = UIAnchor.TopLeft, Vector2 offset = default)
    {
        return ref Float(UIFloatingTarget.Parent, 0, targetAnchor, anchor, offset);
    }

    [UnscopedRef]
    public ref Element FloatOn(uint targetId, UIAnchor targetAnchor, UIAnchor anchor, Vector2 offset = default)
    {
        return ref Float(UIFloatingTarget.Element, targetId, targetAnchor, anchor, offset);
    }

    [UnscopedRef]
    public ref Element FloatOnViewport(UIAnchor targetAnchor = UIAnchor.TopLeft, UIAnchor anchor = UIAnchor.TopLeft, Vector2 offset = default)
    {
        return ref Float(UIFloatingTarget.Viewport, 0, targetAnchor, anchor, offset);
    }

    [UnscopedRef]
    public ref Element ZIndex(int zIndex, bool absolute = false)
    {
        _desc.Floating.ZIndex = zIndex;
        SetFloatingFlag(UIFloatingFlagBits.AbsoluteZ, absolute);
        return ref this;
    }

    [UnscopedRef]
    public ref Element KeepInViewport(bool keep = true)
    {
        SetFloatingFlag(UIFloatingFlagBits.KeepInViewport, keep);
        return ref this;
    }

    [UnscopedRef]
    public ref Element ClipToTarget(bool clip = true)
    {
        SetFloatingFlag(UIFloatingFlagBits.ClipToTarget, clip);
        return ref this;
    }

    [UnscopedRef]
    public ref Element Focusable(bool focusable = true)
    {
        SetFlag(UIElementFlagBits.Focusable, focusable);
        return ref this;
    }

    [UnscopedRef]
    public ref Element Disabled(bool disabled = true)
    {
        SetFlag(UIElementFlagBits.Disabled, disabled);
        return ref this;
    }

    [UnscopedRef]
    public ref Element Pointer(UIPointerMode mode)
    {
        _desc.Pointer = mode;
        return ref this;
    }

    [UnscopedRef]
    public ref Element Passthrough()
    {
        _desc.Pointer = UIPointerMode.Passthrough;
        return ref this;
    }

    [UnscopedRef]
    public ref Element Cursor(SystemCursor cursor)
    {
        _desc.Cursor = cursor;
        return ref this;
    }

    [UnscopedRef]
    public ref Element Animate(UITransitionPropertyFlagBits properties = UITransitionPropertyFlagBits.Colors, float duration = 0,
        UIEasing easing = UIEasing.EaseOut)
    {
        _desc.Transition.Duration = duration > 0 ? duration : Ui.Theme.TransitionDuration;
        _desc.Transition.Properties = (uint)properties;
        _desc.Transition.Easing = easing;
        return ref this;
    }

    [UnscopedRef]
    public ref Element Enter(in UITransitionState state)
    {
        EnsureTransition();
        _desc.Transition.Enter = state;
        return ref this;
    }

    [UnscopedRef]
    public ref Element Exit(in UITransitionState state)
    {
        EnsureTransition();
        _desc.Transition.Exit = state;
        return ref this;
    }

    [UnscopedRef]
    public ref Element TransitionFlags(UITransitionFlagBits flags)
    {
        _desc.Transition.Flags = (uint)flags;
        return ref this;
    }

    public readonly Scope Open()
    {
        return new Scope(Ui.Open(in _desc));
    }

    public readonly uint Leaf()
    {
        var id = Ui.Open(in _desc);
        Ui.Close();
        return id;
    }

    private void EnsureTransition()
    {
        if (_desc.Transition.Duration <= 0)
        {
            _desc.Transition.Duration = Ui.Theme.TransitionDuration;
        }
    }

    [UnscopedRef]
    private ref Element Float(UIFloatingTarget target, uint targetId, UIAnchor targetAnchor, UIAnchor anchor, Vector2 offset)
    {
        _desc.Floating.Target = target;
        _desc.Floating.TargetId = targetId;
        _desc.Floating.TargetAnchor = targetAnchor;
        _desc.Floating.Anchor = anchor;
        _desc.Floating.Offset = offset;
        return ref this;
    }

    private void SetFlag(UIElementFlagBits flag, bool set)
    {
        _desc.Flags = set ? _desc.Flags | (uint)flag : _desc.Flags & ~(uint)flag;
    }

    private void SetFloatingFlag(UIFloatingFlagBits flag, bool set)
    {
        _desc.Floating.Flags = set ? _desc.Floating.Flags | (uint)flag : _desc.Floating.Flags & ~(uint)flag;
    }
}

internal readonly ref struct Scope
{
    internal Scope(uint id)
    {
        Id = id;
    }

    public uint Id { get; }

    public void Dispose()
    {
        Ui.Close();
    }
}
