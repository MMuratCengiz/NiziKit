using System.Numerics;
using DenOfIz;
using NiziKit.UI.Widgets;

namespace NiziKit.UI;

/// <summary>
/// Fluent builder for a Clay element. Obtain via <see cref="Ui.Element"/>, <see cref="Ui.Row"/>
/// or <see cref="Ui.Column"/>, configure, then call <see cref="Open"/> (with a using scope)
/// or <see cref="OpenClose"/> for childless elements.
/// </summary>
public ref struct Element
{
    private ClayElementDeclaration _decl;

    internal Element(uint id, ClayLayoutDirection direction)
    {
        _decl = ClayElementDeclaration.Default();
        _decl.Id = id;
        _decl.Layout.LayoutDirection = direction;
    }

    internal Element WithId(uint id)
    {
        _decl.Id = id;
        return this;
    }

    public Element Row()
    {
        _decl.Layout.LayoutDirection = ClayLayoutDirection.LeftToRight;
        return this;
    }

    public Element Column()
    {
        _decl.Layout.LayoutDirection = ClayLayoutDirection.TopToBottom;
        return this;
    }

    public Element Grow()
    {
        _decl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
        _decl.Layout.Sizing.Height = ClaySizingAxis.Grow(0, float.MaxValue);
        return this;
    }

    public Element GrowWidth()
    {
        _decl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
        return this;
    }

    public Element GrowHeight()
    {
        _decl.Layout.Sizing.Height = ClaySizingAxis.Grow(0, float.MaxValue);
        return this;
    }

    public Element Fit()
    {
        _decl.Layout.Sizing.Width = ClaySizingAxis.Fit(0, float.MaxValue);
        _decl.Layout.Sizing.Height = ClaySizingAxis.Fit(0, float.MaxValue);
        return this;
    }

    public Element FitWidth()
    {
        _decl.Layout.Sizing.Width = ClaySizingAxis.Fit(0, float.MaxValue);
        return this;
    }

    public Element FitHeight()
    {
        _decl.Layout.Sizing.Height = ClaySizingAxis.Fit(0, float.MaxValue);
        return this;
    }

    public Element Fixed(float width, float height)
    {
        _decl.Layout.Sizing.Width = ClaySizingAxis.Fixed(width);
        _decl.Layout.Sizing.Height = ClaySizingAxis.Fixed(height);
        return this;
    }

    public Element Width(float width)
    {
        _decl.Layout.Sizing.Width = ClaySizingAxis.Fixed(width);
        return this;
    }

    public Element Height(float height)
    {
        _decl.Layout.Sizing.Height = ClaySizingAxis.Fixed(height);
        return this;
    }

    public Element WidthPercent(float percent)
    {
        _decl.Layout.Sizing.Width = ClaySizingAxis.Percent(percent);
        return this;
    }

    public Element HeightPercent(float percent)
    {
        _decl.Layout.Sizing.Height = ClaySizingAxis.Percent(percent);
        return this;
    }

    public Element Padding(float all)
    {
        _decl.Layout.Padding = ClayPadding.CreateUniform(all);
        return this;
    }

    public Element Padding(float x, float y)
    {
        _decl.Layout.Padding = ClayPadding.Create(x, x, y, y);
        return this;
    }

    public Element Gap(int gap)
    {
        _decl.Layout.ChildGap = (ushort)gap;
        return this;
    }

    public Element Background(Color color)
    {
        _decl.BackgroundColor = color.ToClay();
        return this;
    }

    public Element CornerRadius(CornerRadius radius)
    {
        _decl.BorderRadius = radius.ToClay();
        return this;
    }

    public Element Border(Color color, float width)
    {
        _decl.Border.Color = color.ToClay();
        _decl.Border.Width = ClayBorderWidth.CreateUniform(width);
        return this;
    }

    public Element Border(Color color, float width, float betweenChildren)
    {
        _decl.Border.Color = color.ToClay();
        _decl.Border.Width = ClayBorderWidth.Create(width, width, width, width, betweenChildren);
        return this;
    }

    public Element ScrollVertical()
    {
        _decl.Scroll.Vertical = true;
        return this;
    }

    public Element ScrollHorizontal()
    {
        _decl.Scroll.Horizontal = true;
        return this;
    }

    public Element Clip()
    {
        _decl.Clip.Horizontal = true;
        _decl.Clip.Vertical = true;
        return this;
    }

    public Element Clip(bool horizontal, bool vertical, Vector2 childOffset = default)
    {
        _decl.Clip = ClayClipDesc.Create(horizontal, vertical, childOffset);
        return this;
    }

    public Element Overlay(Color color)
    {
        _decl.OverlayColor = color.ToClay();
        return this;
    }

    public Element AspectRatio(float widthOverHeight)
    {
        _decl.AspectRatio = widthOverHeight;
        return this;
    }

    public Element Transition(ClayTransitionDesc transition)
    {
        _decl.Transition = transition;
        return this;
    }

    public Element Transition(float duration,
        ClayTransitionPropertyFlagBits properties = ClayTransitionPropertyFlagBits.BoundingBox | ClayTransitionPropertyFlagBits.BackgroundColor |
                                                    ClayTransitionPropertyFlagBits.OverlayColor | ClayTransitionPropertyFlagBits.Border,
        ClayTransitionEasing easing = ClayTransitionEasing.EaseOut)
    {
        _decl.Transition = ClayTransitionDesc.Create(duration, properties, easing);
        return this;
    }

    public Element EnterFrom(ClayTransitionStateDesc state, ClayTransitionEnterTrigger trigger = ClayTransitionEnterTrigger.SkipOnFirstParentFrame)
    {
        EnsureTransition();
        _decl.Transition = _decl.Transition.WithEnter(state, trigger);
        return this;
    }

    public Element ExitTo(ClayTransitionStateDesc state, ClayTransitionExitTrigger trigger = ClayTransitionExitTrigger.SkipWhenParentExits,
        ClayExitTransitionSiblingOrdering siblingOrdering = ClayExitTransitionSiblingOrdering.UnderneathSiblings)
    {
        EnsureTransition();
        _decl.Transition = _decl.Transition.WithExit(state, trigger, siblingOrdering);
        return this;
    }

    public Element FadeIn(Color from)
    {
        return EnterFrom(ClayTransitionStateDesc.FromOverlay(from.ToClay()));
    }

    public Element FadeOut(Color to)
    {
        return ExitTo(ClayTransitionStateDesc.FromOverlay(to.ToClay()));
    }

    public Element SlideIn(Vector2 fromOffset)
    {
        return EnterFrom(ClayTransitionStateDesc.FromOffset(fromOffset));
    }

    public Element SlideOut(Vector2 toOffset)
    {
        return ExitTo(ClayTransitionStateDesc.FromOffset(toOffset));
    }

    private void EnsureTransition()
    {
        if (!_decl.Transition.Enabled)
        {
            _decl.Transition = ClayTransitionDesc.Default();
        }
    }

    public Element CenterChildren()
    {
        _decl.Layout.ChildAlignment.X = ClayAlignmentX.Center;
        _decl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
        return this;
    }

    public Element AlignChildren(ClayAlignmentX x, ClayAlignmentY y)
    {
        _decl.Layout.ChildAlignment.X = x;
        _decl.Layout.ChildAlignment.Y = y;
        return this;
    }
    public Scope Open()
    {
        Ui.Clay.OpenElement(in _decl);
        return default;
    }

    public void OpenClose()
    {
        Open();
        Ui.Clay.CloseElement();
    }
}

/// <summary>Scope returned by <see cref="Element.Open"/>; disposing closes the element.</summary>
public readonly ref struct Scope : IDisposable
{
    public void Dispose()
    {
        Ui.Clay.CloseElement();
    }
}
