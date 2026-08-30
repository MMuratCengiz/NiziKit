using System.Numerics;
using DenOfIz;

namespace NiziKit.UI;

/// <summary>
/// Fluent builder for a Clay element. Obtain via <see cref="Ui.Element"/>, <see cref="Ui.Row"/>
/// or <see cref="Ui.Column"/>, configure, then call <see cref="Open"/> (with a using scope)
/// or <see cref="OpenClose"/> for childless elements.
/// </summary>
public ref struct UiElement
{
    private ClayElementDeclaration _decl;
    private bool _capturePointer;

    internal UiElement(uint id, ClayLayoutDirection direction)
    {
        _decl = ClayElementDeclaration.Default();
        _decl.Id = id;
        _decl.Layout.LayoutDirection = direction;
        _capturePointer = false;
    }

    internal UiElement WithId(uint id)
    {
        _decl.Id = id;
        return this;
    }

    public UiElement Row()
    {
        _decl.Layout.LayoutDirection = ClayLayoutDirection.LeftToRight;
        return this;
    }

    public UiElement Column()
    {
        _decl.Layout.LayoutDirection = ClayLayoutDirection.TopToBottom;
        return this;
    }

    public UiElement Grow()
    {
        _decl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
        _decl.Layout.Sizing.Height = ClaySizingAxis.Grow(0, float.MaxValue);
        return this;
    }

    public UiElement GrowWidth()
    {
        _decl.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
        return this;
    }

    public UiElement GrowHeight()
    {
        _decl.Layout.Sizing.Height = ClaySizingAxis.Grow(0, float.MaxValue);
        return this;
    }

    public UiElement Fit()
    {
        _decl.Layout.Sizing.Width = ClaySizingAxis.Fit(0, float.MaxValue);
        _decl.Layout.Sizing.Height = ClaySizingAxis.Fit(0, float.MaxValue);
        return this;
    }

    public UiElement FitWidth()
    {
        _decl.Layout.Sizing.Width = ClaySizingAxis.Fit(0, float.MaxValue);
        return this;
    }

    public UiElement FitHeight()
    {
        _decl.Layout.Sizing.Height = ClaySizingAxis.Fit(0, float.MaxValue);
        return this;
    }

    public UiElement Fixed(float width, float height)
    {
        _decl.Layout.Sizing.Width = ClaySizingAxis.Fixed(width);
        _decl.Layout.Sizing.Height = ClaySizingAxis.Fixed(height);
        return this;
    }

    public UiElement Width(float width)
    {
        _decl.Layout.Sizing.Width = ClaySizingAxis.Fixed(width);
        return this;
    }

    public UiElement Height(float height)
    {
        _decl.Layout.Sizing.Height = ClaySizingAxis.Fixed(height);
        return this;
    }

    public UiElement WidthPercent(float percent)
    {
        _decl.Layout.Sizing.Width = ClaySizingAxis.Percent(percent);
        return this;
    }

    public UiElement HeightPercent(float percent)
    {
        _decl.Layout.Sizing.Height = ClaySizingAxis.Percent(percent);
        return this;
    }

    public UiElement Padding(float all)
    {
        _decl.Layout.Padding = ClayPadding.CreateUniform(all);
        return this;
    }

    public UiElement Padding(float x, float y)
    {
        _decl.Layout.Padding = ClayPadding.Create(x, x, y, y);
        return this;
    }

    public UiElement Gap(int gap)
    {
        _decl.Layout.ChildGap = (ushort)gap;
        return this;
    }

    public UiElement Background(UiColor color)
    {
        _decl.BackgroundColor = color.ToClay();
        return this;
    }

    public UiElement CornerRadius(float radius)
    {
        _decl.BorderRadius = ClayBorderRadius.CreateUniform(radius);
        return this;
    }

    public UiElement Border(UiColor color, float width)
    {
        _decl.Border.Color = color.ToClay();
        _decl.Border.Width = ClayBorderWidth.CreateUniform(width);
        return this;
    }

    public UiElement Border(UiColor color, float width, float betweenChildren)
    {
        _decl.Border.Color = color.ToClay();
        _decl.Border.Width = ClayBorderWidth.Create(width, width, width, width, betweenChildren);
        return this;
    }

    public UiElement ScrollVertical()
    {
        _decl.Scroll.Vertical = true;
        return this;
    }

    public UiElement ScrollHorizontal()
    {
        _decl.Scroll.Horizontal = true;
        return this;
    }

    public UiElement Clip()
    {
        _decl.Clip.Horizontal = true;
        _decl.Clip.Vertical = true;
        return this;
    }

    public UiElement Clip(bool horizontal, bool vertical, Vector2 childOffset = default)
    {
        _decl.Clip = ClayClipDesc.Create(horizontal, vertical, childOffset);
        return this;
    }

    public UiElement Overlay(UiColor color)
    {
        _decl.OverlayColor = color.ToClay();
        return this;
    }

    public UiElement AspectRatio(float widthOverHeight)
    {
        _decl.AspectRatio = widthOverHeight;
        return this;
    }

    public UiElement Transition(ClayTransitionDesc transition)
    {
        _decl.Transition = transition;
        return this;
    }

    public UiElement Transition(float duration,
        ClayTransitionPropertyFlagBits properties = ClayTransitionPropertyFlagBits.BoundingBox | ClayTransitionPropertyFlagBits.BackgroundColor |
                                                    ClayTransitionPropertyFlagBits.OverlayColor | ClayTransitionPropertyFlagBits.Border,
        ClayTransitionEasing easing = ClayTransitionEasing.EaseOut)
    {
        _decl.Transition = ClayTransitionDesc.Create(duration, properties, easing);
        return this;
    }

    public UiElement EnterFrom(ClayTransitionStateDesc state, ClayTransitionEnterTrigger trigger = ClayTransitionEnterTrigger.SkipOnFirstParentFrame)
    {
        EnsureTransition();
        _decl.Transition = _decl.Transition.WithEnter(state, trigger);
        return this;
    }

    public UiElement ExitTo(ClayTransitionStateDesc state, ClayTransitionExitTrigger trigger = ClayTransitionExitTrigger.SkipWhenParentExits,
        ClayExitTransitionSiblingOrdering siblingOrdering = ClayExitTransitionSiblingOrdering.UnderneathSiblings)
    {
        EnsureTransition();
        _decl.Transition = _decl.Transition.WithExit(state, trigger, siblingOrdering);
        return this;
    }

    public UiElement FadeIn(UiColor from)
    {
        return EnterFrom(ClayTransitionStateDesc.FromOverlay(from.ToClay()));
    }

    public UiElement FadeOut(UiColor to)
    {
        return ExitTo(ClayTransitionStateDesc.FromOverlay(to.ToClay()));
    }

    public UiElement SlideIn(Vector2 fromOffset)
    {
        return EnterFrom(ClayTransitionStateDesc.FromOffset(fromOffset));
    }

    public UiElement SlideOut(Vector2 toOffset)
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

    public UiElement CenterChildren()
    {
        _decl.Layout.ChildAlignment.X = ClayAlignmentX.Center;
        _decl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
        return this;
    }

    public UiElement AlignChildren(ClayAlignmentX x, ClayAlignmentY y)
    {
        _decl.Layout.ChildAlignment.X = x;
        _decl.Layout.ChildAlignment.Y = y;
        return this;
    }
    
    public UiElement CapturePointer()
    {
        _capturePointer = true;
        return this;
    }

    public UiScope Open()
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

/// <summary>Scope returned by <see cref="UiElement.Open"/>; disposing closes the element.</summary>
public readonly ref struct UiScope : IDisposable
{
    public void Dispose()
    {
        Ui.Clay.CloseElement();
    }
}
