using System.Numerics;
using DenOfIz;

namespace NiziKit.UI.Widgets;

public abstract class Widget
{
    private const uint LeftBit = 1u << (int)UiMouseButton.Left;

    private uint _pressStarted;
    private bool _dragCandidate;
    private bool _dragging;
    private Vector2 _pressPosition;
    private Vector2 _lastDragPosition;
    private int _lastBuiltFrame = -1;
    private bool _exiting;
    private float _exitEnd;
    private Vector2 _exitOffset;
    private UiColor? _exitOverlay;
    private Action? _exitCompleted;

    public uint Id { get; } = Ui.AllocateWidgetId();
    public string? Name { get; set; }
    public object? Tag { get; set; }
    public Widget? Parent { get; internal set; }

    public bool Visible { get; set; } = true;
    public Func<bool>? VisibleWhen { get; set; }
    public bool Enabled { get; init; } = true;
    public Func<bool>? EnabledWhen { get; init; }
    public bool Focusable { get; init; }
    public bool Draggable { get; set; }
    public bool AllowDrop { get; set; }
    public string? Tooltip { get; set; }
    public Action<Widget>? OnUpdate { get; set; }

    public UiSize Width { get; set; } = UiSize.Fit;
    public UiSize Height { get; set; } = UiSize.Fit;
    public UiThickness Padding { get; set; }
    public UiThickness Margin { get; set; }
    public UiColor? Background { get; set; }
    public Texture? BackgroundTexture { get; set; }
    public float CornerRadius { get; init; }
    public UiColor? BorderColor { get; set; }
    public float BorderWidth { get; set; } = 1;
    public float BorderBetweenChildren { get; set; }
    public bool ClipChildren { get; set; }
    public float AspectRatio { get; init; }
    public ClayTransitionDesc? Transition { get; set; }
    public UiFloating? Floating { get; set; }
    public UiColor? Overlay { get; set; }

    public bool IsVisible { get; private set; } = true;
    public bool IsEnabled { get; private set; } = true;
    public bool IsHovered { get; private set; }
    public bool IsPressed { get; private set; }
    public bool IsDragging => _dragging;
    public bool IsExiting => _exiting;
    public bool BuiltThisFrame => _lastBuiltFrame == Ui.FrameCount;
    public bool BuiltLastFrame => _lastBuiltFrame >= Ui.FrameCount - 1;
    public bool IsDragOver { get; private set; }
    public bool IsFocused => Ui.Focused == this;

    public event Action<Widget>? Clicked;
    public event Action<Widget>? RightClicked;
    public event Action<Widget>? MiddleClicked;
    public event Action<Widget>? DoubleClicked;
    public event Action<Widget, UiPointerEvent>? Pressed;
    public event Action<Widget, UiPointerEvent>? Released;
    public event Action<Widget, float>? Scrolled;
    public event Action<Widget>? PointerEntered;
    public event Action<Widget>? PointerExited;
    public event Action<Widget, UiDragEvent>? DragStarted;
    public event Action<Widget, UiDragEvent>? Dragging;
    public event Action<Widget, UiDragEvent>? DragEnded;
    public event Action<Widget, UiDropEvent>? DragEntered;
    public event Action<Widget>? DragLeft;
    public event Action<Widget, UiDropEvent>? Dropped;
    public event Action<Widget>? FocusGained;
    public event Action<Widget>? FocusLost;

    public ClayBoundingBox Bounds => Ui.Clay.GetElementBoundingBox(Id);

    protected float AncestorInnerWidthPixels()
    {
        var result = 0f;
        for (var ancestor = Parent; ancestor != null; ancestor = ancestor.Parent)
        {
            var inner = ancestor.Bounds.Width - Ui.Clay.PointsToPixels(ancestor.Padding.Horizontal);
            if (inner > 0)
            {
                result = result <= 0 ? inner : MathF.Min(result, inner);
            }

            if (ancestor.Width.IsFixed || ancestor.Width.IsPercent || ancestor.Floating != null)
            {
                break;
            }
        }

        return result;
    }

    protected virtual bool TracksPointer =>
        Clicked != null || RightClicked != null || MiddleClicked != null || DoubleClicked != null ||
        Pressed != null || Released != null || Scrolled != null ||
        PointerEntered != null || PointerExited != null ||
        Focusable || Draggable || AllowDrop || Tooltip != null;

    protected internal virtual bool IsKeyActivatable => false;

    public virtual T? Find<T>(string name) where T : Widget
    {
        return Name == name ? this as T : null;
    }

    public Widget Named(string name)
    {
        Name = name;
        return this;
    }

    public void Focus()
    {
        Ui.SetFocus(this);
    }

    public void Activate()
    {
        if (!IsEnabled)
        {
            return;
        }

        OnClick(UiMouseButton.Left);
        Clicked?.Invoke(this);
    }

    public void BeginExit(float duration, Vector2 offset, UiColor? overlay, Action? completed)
    {
        if (Transition is not { Enabled: true })
        {
            Animate(duration);
        }

        _exiting = true;
        _exitEnd = Ui.ElapsedSeconds + duration;
        _exitOffset = offset;
        _exitOverlay = overlay;
        _exitCompleted = completed;
    }

    public void BeginExit(float duration, Action? completed)
    {
        BeginExit(duration, Vector2.Zero, UiTheme.Background, completed);
    }

    public void CancelExit()
    {
        _exiting = false;
        _exitCompleted = null;
    }

    public void Raise()
    {
        Floating ??= new UiFloating { AttachTo = UiAttachTo.Root };
        Floating.ZIndex = Ui.NextZIndex();
    }

    public void Animate(float duration, ClayTransitionEasing easing = ClayTransitionEasing.EaseOut)
    {
        Animate(duration,
            ClayTransitionPropertyFlagBits.BoundingBox | ClayTransitionPropertyFlagBits.BackgroundColor |
            ClayTransitionPropertyFlagBits.OverlayColor | ClayTransitionPropertyFlagBits.Border, easing);
    }

    public void Animate(float duration, ClayTransitionPropertyFlagBits properties, ClayTransitionEasing easing = ClayTransitionEasing.EaseOut)
    {
        var enter = Transition?.Enter ?? default;
        var exit = Transition?.Exit ?? default;
        var desc = ClayTransitionDesc.Create(duration, properties, easing);
        desc.InteractionHandling = ClayTransitionInteractionHandling.AllowWhileMoving;
        desc.Enter = enter;
        desc.Exit = exit;
        Transition = desc;
    }

    public void EnterFrom(ClayTransitionStateDesc state, ClayTransitionEnterTrigger trigger = ClayTransitionEnterTrigger.SkipOnFirstParentFrame)
    {
        Transition = EnsureTransition().WithEnter(state, trigger);
    }

    public void ExitTo(ClayTransitionStateDesc state, ClayTransitionExitTrigger trigger = ClayTransitionExitTrigger.SkipWhenParentExits)
    {
        Transition = EnsureTransition().WithExit(state, trigger);
    }

    private ClayTransitionStateDesc EnterState()
    {
        return Transition is { Enter.Enabled: true } transition ? transition.Enter.State : ClayTransitionStateDesc.FromScale(1);
    }

    private ClayTransitionStateDesc ExitState()
    {
        return Transition is { Exit.Enabled: true } transition ? transition.Exit.State : ClayTransitionStateDesc.FromScale(1);
    }

    public void FadeIn(UiColor from, float duration = 0.2f)
    {
        Animate(duration);
        var state = EnterState();
        state.OverlayColor = from.ToClay();
        EnterFrom(state);
    }

    public void FadeOut(UiColor to, float duration = 0.2f)
    {
        Animate(duration);
        var state = ExitState();
        state.OverlayColor = to.ToClay();
        ExitTo(state);
    }

    public void SlideIn(Vector2 fromOffset, float duration = 0.2f)
    {
        Animate(duration);
        var state = EnterState();
        state.PositionOffset = fromOffset;
        EnterFrom(state);
    }

    public void SlideOut(Vector2 toOffset, float duration = 0.2f)
    {
        Animate(duration);
        var state = ExitState();
        state.PositionOffset = toOffset;
        ExitTo(state);
    }

    public void ScaleIn(float fromScale, float duration = 0.2f)
    {
        Animate(duration);
        var state = EnterState();
        state.Scale = fromScale;
        EnterFrom(state);
    }

    public void ScaleOut(float toScale, float duration = 0.2f)
    {
        Animate(duration);
        var state = ExitState();
        state.Scale = toScale;
        ExitTo(state);
    }

    private ClayTransitionDesc EnsureTransition()
    {
        if (Transition is { Enabled: true } existing)
        {
            return existing;
        }

        return ClayTransitionDesc.Create(0.2f,
            ClayTransitionPropertyFlagBits.BoundingBox | ClayTransitionPropertyFlagBits.BackgroundColor |
            ClayTransitionPropertyFlagBits.OverlayColor | ClayTransitionPropertyFlagBits.Border);
    }

    internal void CollectFrame(List<Widget> frame)
    {
        if (_exiting && Ui.ElapsedSeconds >= _exitEnd)
        {
            var completed = _exitCompleted;
            _exiting = false;
            _exitCompleted = null;
            IsVisible = false;
            completed?.Invoke();
            return;
        }

        OnUpdate?.Invoke(this);
        IsVisible = Visible && (VisibleWhen?.Invoke() ?? true);
        if (!IsVisible)
        {
            return;
        }

        frame.Add(this);
        CollectChildren(frame);
    }

    protected virtual void CollectChildren(List<Widget> frame)
    {
    }

    internal void Poll()
    {
        IsEnabled = Enabled && !_exiting && (EnabledWhen?.Invoke() ?? true);
        var hovered = (TracksPointer || Background.HasValue) && HitTest();
        if (hovered)
        {
            Ui.IsPointerOverUi = true;
            Ui.HoveredWidget = this;
        }

        if (!IsEnabled)
        {
            hovered = false;
            _pressStarted = 0;
            _dragCandidate = false;
        }

        if (hovered != IsHovered)
        {
            IsHovered = hovered;
            if (hovered)
            {
                PointerEntered?.Invoke(this);
            }
            else
            {
                PointerExited?.Invoke(this);
            }
        }

        var dragOver = hovered && AllowDrop && Ui.DragSource != null && Ui.DragSource != this;
        if (dragOver != IsDragOver)
        {
            IsDragOver = dragOver;
            if (dragOver)
            {
                DragEntered?.Invoke(this, new UiDropEvent(Ui.DragSource!, Ui.DragPayload, Ui.PointerPosition));
            }
            else
            {
                DragLeft?.Invoke(this);
            }
        }

        if (dragOver)
        {
            Ui.SetDropCandidate(this);
        }

        var captured = Ui.DragSource != null && Ui.DragSource != this;
        for (var b = 0; b < 3; b++)
        {
            var button = (UiMouseButton)b;
            var bit = 1u << b;
            if (Ui.WasPressed(button) && hovered && !captured)
            {
                _pressStarted |= bit;
                Pressed?.Invoke(this, new UiPointerEvent(button, Ui.PointerPosition, Ui.PressClicks));
                if (button == UiMouseButton.Left)
                {
                    _pressPosition = Ui.PointerPosition;
                    _dragCandidate = Draggable;
                    if (Focusable)
                    {
                        Ui.RequestFocus(this);
                    }
                }
            }

            if (Ui.WasReleased(button))
            {
                if ((_pressStarted & bit) != 0 && hovered)
                {
                    Released?.Invoke(this, new UiPointerEvent(button, Ui.PointerPosition, Ui.ReleaseClicks));
                    if (!_dragging)
                    {
                        OnClick(button);
                        switch (button)
                        {
                            case UiMouseButton.Left:
                                Clicked?.Invoke(this);
                                if (Ui.ReleaseClicks >= 2)
                                {
                                    DoubleClicked?.Invoke(this);
                                }

                                break;
                            case UiMouseButton.Right:
                                RightClicked?.Invoke(this);
                                break;
                            case UiMouseButton.Middle:
                                MiddleClicked?.Invoke(this);
                                break;
                        }
                    }
                }

                _pressStarted &= ~bit;
            }
        }

        if (_dragCandidate && !_dragging && (_pressStarted & LeftBit) != 0 && Ui.IsButtonDown(UiMouseButton.Left))
        {
            if (Vector2.Distance(Ui.PointerPosition, _pressPosition) >= Ui.DragThreshold)
            {
                _dragging = true;
                _lastDragPosition = _pressPosition;
                Ui.BeginDrag(this);
                DragStarted?.Invoke(this, new UiDragEvent(UiMouseButton.Left, _pressPosition, Ui.PointerPosition, Vector2.Zero));
            }
        }

        if (_dragging && Ui.PointerPosition != _lastDragPosition)
        {
            Dragging?.Invoke(this, new UiDragEvent(UiMouseButton.Left, _pressPosition, Ui.PointerPosition, Ui.PointerPosition - _lastDragPosition));
            _lastDragPosition = Ui.PointerPosition;
        }

        if (!Ui.IsButtonDown(UiMouseButton.Left))
        {
            _dragCandidate = false;
        }

        if (hovered && Ui.WheelDelta != 0)
        {
            Scrolled?.Invoke(this, Ui.WheelDelta);
        }

        IsPressed = (_pressStarted & LeftBit) != 0 && hovered;
        OnPoll();
    }

    internal void FinishDrag()
    {
        if (!_dragging)
        {
            return;
        }

        _dragging = false;
        _dragCandidate = false;
        DragEnded?.Invoke(this, new UiDragEvent(UiMouseButton.Left, _pressPosition, Ui.PointerPosition, Vector2.Zero));
    }

    internal void ReceiveDrop(in UiDropEvent drop)
    {
        IsDragOver = false;
        Dropped?.Invoke(this, drop);
    }

    protected virtual bool HitTest()
    {
        return Ui.Clay.PointerOver(Id);
    }

    internal void Build()
    {
        if (!IsVisible)
        {
            return;
        }

        var hasMargin = Floating == null && (Margin.Horizontal > 0 || Margin.Vertical > 0);
        if (hasMargin)
        {
            var outer = ClayElementDeclaration.Default();
            outer.Layout.Sizing.Width = OuterSizing(Width, Margin.Horizontal);
            outer.Layout.Sizing.Height = OuterSizing(Height, Margin.Vertical);
            outer.Layout.Padding = Margin.ToClay();
            Ui.Clay.OpenElement(in outer);
        }

        var decl = ClayElementDeclaration.Default();
        ApplyDeclaration(ref decl);
        _lastBuiltFrame = Ui.FrameCount;

        if (hasMargin)
        {
            decl.Layout.Sizing.Width = InnerSizing(Width);
            decl.Layout.Sizing.Height = InnerSizing(Height);
        }

        var previousZ = Ui.FloatingZIndex;
        if (Floating != null)
        {
            Ui.FloatingZIndex = decl.Floating.ZIndex;
        }

        Ui.Clay.OpenElement(in decl);
        if (!_exiting || Floating != null)
        {
            BuildContent();
        }

        Ui.Clay.CloseElement();

        Ui.FloatingZIndex = previousZ;

        if (hasMargin)
        {
            Ui.Clay.CloseElement();
        }
    }

    private static ClaySizingAxis OuterSizing(UiSize size, float margin)
    {
        return size.Kind switch
        {
            UiSizeKind.Fixed => ClaySizingAxis.Fixed(size.Value + margin),
            UiSizeKind.Fit => ClaySizingAxis.Fit(size.Min + margin, size.Max == float.MaxValue ? float.MaxValue : size.Max + margin),
            UiSizeKind.Grow => ClaySizingAxis.Grow(size.Min + margin, size.Max == float.MaxValue ? float.MaxValue : size.Max + margin),
            _ => size.ToClay()
        };
    }

    private static ClaySizingAxis InnerSizing(UiSize size)
    {
        return size.Kind is UiSizeKind.Grow or UiSizeKind.Percent ? ClaySizingAxis.Grow(0, float.MaxValue) : size.ToClay();
    }

    protected virtual void ApplyDeclaration(ref ClayElementDeclaration decl)
    {
        decl.Id = Id;
        decl.Layout.Sizing.Width = Width.ToClay();
        decl.Layout.Sizing.Height = Height.ToClay();
        decl.Layout.Padding = Padding.ToClay();

        if (Background is { } background)
        {
            decl.BackgroundColor = background.ToClay();
        }

        if (BackgroundTexture != null)
        {
            decl.Custom.CustomData = (IntPtr)(ulong)BackgroundTexture;
        }

        if (Overlay is { } overlay)
        {
            decl.OverlayColor = overlay.ToClay();
        }

        if (Parent == null && Ui.WarmupOverlay is { } warmup)
        {
            decl.OverlayColor = warmup.ToClay();
        }

        if (CornerRadius > 0)
        {
            decl.BorderRadius = ClayBorderRadius.CreateUniform(CornerRadius);
        }

        if (BorderColor is { } borderColor)
        {
            decl.Border.Color = borderColor.ToClay();
            decl.Border.Width = ClayBorderWidth.Create(BorderWidth, BorderWidth, BorderWidth, BorderWidth, BorderBetweenChildren);
        }

        if (ClipChildren)
        {
            decl.Clip = ClayClipDesc.Create(false, true);
        }

        if (AspectRatio > 0)
        {
            decl.AspectRatio = AspectRatio;
        }

        if (Transition is { } transition)
        {
            decl.Transition = transition;
            if (_dragging)
            {
                decl.Transition.Properties &= ~(uint)ClayTransitionPropertyFlagBits.BoundingBox;
            }
        }

        if (Floating != null)
        {
            Floating.Apply(ref decl);
            if (Floating.AttachTo == UiAttachTo.Element && (Floating.Anchor == null || !Floating.Anchor.BuiltThisFrame))
            {
                var bounds = Bounds;
                decl.Floating.AttachTo = ClayFloatingAttachTo.Root;
                decl.Floating.ParentId = 0;
                decl.Floating.ElementAttachPoint = ClayFloatingAttachPoint.LeftTop;
                decl.Floating.ParentAttachPoint = ClayFloatingAttachPoint.LeftTop;
                decl.Floating.Offset = new Vector2(Ui.Clay.PixelsToPoints(bounds.X), Ui.Clay.PixelsToPoints(bounds.Y));
            }

            if (Floating.ZIndex == 0)
            {
                decl.Floating.ZIndex = Ui.FloatingZIndex;
            }

            if (_dragging || _exiting)
            {
                decl.Floating.PointerCaptureMode = ClayPointerCaptureMode.Passthrough;
            }

            if (_exiting)
            {
                decl.Floating.Offset += _exitOffset;
            }
        }

        if (_exiting)
        {
            if (_exitOverlay is { } exitOverlay)
            {
                decl.OverlayColor = exitOverlay.ToClay();
            }

            if (Floating == null)
            {
                decl.Layout.Sizing.Height = ClaySizingAxis.Fixed(0);
                decl.Layout.Padding = ClayPadding.CreateUniform(0);
                decl.Clip = default;
                decl.Scroll = default;
            }
        }
    }

    protected virtual void BuildContent()
    {
    }

    protected virtual void OnPoll()
    {
    }

    protected virtual void OnClick(UiMouseButton button)
    {
    }

    protected internal virtual bool OnKeyDown(in KeyboardEventData key)
    {
        return false;
    }

    protected internal virtual void OnTextInput(string text)
    {
    }

    protected internal virtual void OnFocusChanged(bool focused)
    {
        if (focused)
        {
            FocusGained?.Invoke(this);
        }
        else
        {
            FocusLost?.Invoke(this);
        }
    }
}
