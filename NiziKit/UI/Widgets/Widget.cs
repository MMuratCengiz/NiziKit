using System.Numerics;
using DenOfIz;

namespace NiziKit.UI.Widgets;

public abstract class Widget
{
    private const uint LeftBit = 1u << (int)MouseButton.Left;

    private uint _pressStarted;
    private bool _dragCandidate;
    private bool _dragging;
    private Vector2 _pressPosition;
    private Vector2 _lastDragPosition;
    private int _lastBuiltFrame = -1;
    private bool _exiting;
    private float _exitEnd;
    private Vector2 _exitOffset;
    private Color? _exitOverlay;
    private Action? _exitCompleted;
    private Style? _style;

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
    public Action<Widget>? OnUpdate { get; set; }

    public Sizing Width { get; set; } = Sizing.Fit;
    public Sizing Height { get; set; } = Sizing.Fit;
    public Thickness Padding { get; set; }
    public Thickness Margin { get; set; }
    public Color? Background { get; set; }
    public CornerRadius CornerRadius { get; init; }
    public Color? BorderColor { get; set; }
    public BorderThickness BorderWidth { get; set; } = 1;
    public float BorderBetweenChildren { get; set; }
    public bool ClipChildren { get; set; }
    public float AspectRatio { get; init; }
    public ClayTransitionDesc? Transition { get; set; }
    public Floating? Floating { get; set; }
    public Color? Overlay { get; set; }

    public Style Style
    {
        get => _style ??= new Style();
        set => _style = value;
    }

    public bool HasStyle => _style != null;

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
    public event Action<Widget, PointerEvent>? Pressed;
    public event Action<Widget, PointerEvent>? Released;
    public event Action<Widget, float>? Scrolled;
    public event Action<Widget>? PointerEntered;
    public event Action<Widget>? PointerExited;
    public event Action<Widget, DragEvent>? DragStarted;
    public event Action<Widget, DragEvent>? Dragging;
    public event Action<Widget, DragEvent>? DragEnded;
    public event Action<Widget, DropEvent>? DragEntered;
    public event Action<Widget>? DragLeft;
    public event Action<Widget, DropEvent>? Dropped;
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

    protected virtual Widget StyleSource => this;

    protected virtual bool IsChecked => false;

    protected virtual void OnStyleResolved(in StyleState state)
    {
    }

    protected virtual bool TracksPointer =>
        Clicked != null || RightClicked != null || MiddleClicked != null || DoubleClicked != null ||
        Pressed != null || Released != null || Scrolled != null ||
        PointerEntered != null || PointerExited != null ||
        Focusable || Draggable || AllowDrop;

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

        OnClick(MouseButton.Left);
        Clicked?.Invoke(this);
    }

    public void BeginExit(float duration, Vector2 offset, Color? overlay, Action? completed)
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
        BeginExit(duration, Vector2.Zero, Background, completed);
    }

    public void CancelExit()
    {
        _exiting = false;
        _exitCompleted = null;
    }

    public void Raise()
    {
        Floating ??= new Floating { AttachTo = AttachTo.Root };
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

    public void FadeIn(Color from, float duration = 0.2f)
    {
        Animate(duration);
        var state = EnterState();
        state.OverlayColor = from.ToClay();
        EnterFrom(state);
    }

    public void FadeOut(Color to, float duration = 0.2f)
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
                DragEntered?.Invoke(this, new DropEvent(Ui.DragSource!, Ui.DragPayload, Ui.PointerPosition));
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
        for (var b = (int)MouseButton.Left; b <= (int)MouseButton.X2; b++)
        {
            var button = (MouseButton)b;
            var bit = Ui.Bit(button);
            if (Ui.WasPressed(button) && hovered && !captured)
            {
                _pressStarted |= bit;
                Pressed?.Invoke(this, new PointerEvent(button, Ui.PointerPosition, Ui.PressClicks));
                if (button == MouseButton.Left)
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
                    Released?.Invoke(this, new PointerEvent(button, Ui.PointerPosition, Ui.ReleaseClicks));
                    if (!_dragging)
                    {
                        OnClick(button);
                        switch (button)
                        {
                            case MouseButton.Left:
                                Clicked?.Invoke(this);
                                if (Ui.ReleaseClicks >= 2)
                                {
                                    DoubleClicked?.Invoke(this);
                                }

                                break;
                            case MouseButton.Right:
                                RightClicked?.Invoke(this);
                                break;
                            case MouseButton.Middle:
                                MiddleClicked?.Invoke(this);
                                break;
                        }
                    }
                }

                _pressStarted &= ~bit;
            }
        }

        if (_dragCandidate && !_dragging && (_pressStarted & LeftBit) != 0 && Ui.IsButtonDown(MouseButton.Left))
        {
            if (Vector2.Distance(Ui.PointerPosition, _pressPosition) >= Ui.DragThreshold)
            {
                _dragging = true;
                _lastDragPosition = _pressPosition;
                Ui.BeginDrag(this);
                DragStarted?.Invoke(this, new DragEvent(MouseButton.Left, _pressPosition, Ui.PointerPosition, Vector2.Zero));
            }
        }

        if (_dragging && Ui.PointerPosition != _lastDragPosition)
        {
            Dragging?.Invoke(this, new DragEvent(MouseButton.Left, _pressPosition, Ui.PointerPosition, Ui.PointerPosition - _lastDragPosition));
            _lastDragPosition = Ui.PointerPosition;
        }

        if (!Ui.IsButtonDown(MouseButton.Left))
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
        DragEnded?.Invoke(this, new DragEvent(MouseButton.Left, _pressPosition, Ui.PointerPosition, Vector2.Zero));
    }

    internal void ReceiveDrop(in DropEvent drop)
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

    private static ClaySizingAxis OuterSizing(Sizing size, float margin)
    {
        return size.Kind switch
        {
            SizingKind.Fixed => ClaySizingAxis.Fixed(size.Value + margin),
            SizingKind.Fit => ClaySizingAxis.Fit(size.Min + margin, size.Max == float.MaxValue ? float.MaxValue : size.Max + margin),
            SizingKind.Grow => ClaySizingAxis.Grow(size.Min + margin, size.Max == float.MaxValue ? float.MaxValue : size.Max + margin),
            _ => size.ToClay()
        };
    }

    private static ClaySizingAxis InnerSizing(Sizing size)
    {
        return size.Kind is SizingKind.Grow or SizingKind.Percent ? ClaySizingAxis.Grow(0, float.MaxValue) : size.ToClay();
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

        if (Overlay is { } overlay)
        {
            decl.OverlayColor = overlay.ToClay();
        }

        if (Parent == null && Ui.WarmupOverlay is { } warmup)
        {
            decl.OverlayColor = warmup.ToClay();
        }

        if (!CornerRadius.IsZero)
        {
            decl.BorderRadius = CornerRadius.ToClay();
        }

        if (BorderColor is { } borderColor)
        {
            decl.Border.Color = borderColor.ToClay();
            decl.Border.Width = BorderWidth.ToClay(BorderBetweenChildren);
        }

        if (_style != null)
        {
            var state = _style.Resolve(StyleSource, IsChecked);
            state.Apply(ref decl, BorderBetweenChildren);
            OnStyleResolved(in state);
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
            if (Floating.AttachTo == AttachTo.Element && (Floating.Anchor == null || !Floating.Anchor.BuiltThisFrame))
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

    protected virtual void OnClick(MouseButton button)
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
