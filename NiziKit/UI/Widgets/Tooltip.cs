using System.Numerics;
using DenOfIz;
using TooltipSettings = NiziKit.UI.Widgets.Tooltip;

namespace NiziKit.UI.Widgets;

public static class Tooltip
{
    private static readonly Dictionary<Widget, Func<Widget>> Factories = new();
    private static TooltipLayer? _layer;

    public static float Delay { get; set; } = 0.6f;
    public static Func<VStack>? FrameFactory { get; set; }
    public static Vector2 PointerOffset { get; set; } = new(12, 16);
    public static int FontSize { get; set; }
    public static bool IsEnabled => _layer != null;
    public static bool IsShowing => _layer is { IsShowing: true };

    public static void Enable()
    {
        if (_layer != null)
        {
            return;
        }

        _layer = new TooltipLayer();
        Ui.Overlays.Add(_layer);
    }

    public static void SetContent(Widget target, Func<Widget> factory)
    {
        target.Tooltip ??= "";
        Factories[target] = factory;
    }

    public static void ClearContent(Widget target)
    {
        Factories.Remove(target);
    }

    public static void Hide()
    {
        _layer?.Hide();
    }

    internal static bool HasContent(Widget widget)
    {
        return Factories.ContainsKey(widget) || !string.IsNullOrEmpty(widget.Tooltip);
    }

    internal static Func<Widget>? GetFactory(Widget widget)
    {
        return Factories.GetValueOrDefault(widget);
    }
}

public sealed class TooltipLayer : Container
{
    private readonly UiFloating _placement = new() { AttachTo = UiAttachTo.Root, CapturePointer = false };
    private VStack _frame;
    private readonly Label _label;
    private Widget? _candidate;
    private Widget? _target;
    private float _hoverStart;

    public TooltipLayer()
    {
        Width = 0;
        Height = 0;
        _label = new Label { Wrap = false };
        _frame = CreateFrame();
        Children.Add(_frame);
    }

    private VStack CreateFrame()
    {
        var frame = TooltipSettings.FrameFactory?.Invoke() ?? new VStack
        {
            Background = UiTheme.SurfaceRaised,
            BorderColor = UiTheme.Border,
            CornerRadius = 4,
            Padding = new UiThickness(8, 5)
        };
        frame.Floating = _placement;
        frame.Animate(0.15f, ClayTransitionPropertyFlagBits.OverlayColor);
        frame.EnterFrom(ClayTransitionStateDesc.FromOverlay(UiTheme.Background.ToClay()), ClayTransitionEnterTrigger.OnFirstParentFrame);
        return frame;
    }

    public bool IsShowing => _target != null;

    public void Hide()
    {
        _target = null;
        _candidate = null;
        _frame.Children.Clear();
    }

    private static Widget? FindTooltipOwner(Widget? widget)
    {
        while (widget != null)
        {
            if (TooltipSettings.HasContent(widget))
            {
                return widget;
            }

            widget = widget.Parent;
        }

        return null;
    }

    private void Show(Widget target)
    {
        _target = target;
        Children.Remove(_frame);
        _frame = CreateFrame();
        Children.Add(_frame);
        var factory = TooltipSettings.GetFactory(target);
        if (factory != null)
        {
            _frame.Children.Add(factory());
        }
        else
        {
            _label.Text = target.Tooltip ?? "";
            _label.FontSize = TooltipSettings.FontSize;
            _frame.Children.Add(_label);
        }

        _placement.ZIndex = Ui.NextZIndex();
        _placement.Offset = new Vector2(Ui.Clay.PixelsToPoints(Ui.PointerPosition.X), Ui.Clay.PixelsToPoints(Ui.PointerPosition.Y)) + TooltipSettings.PointerOffset;
    }

    private void ClampToViewport()
    {
        var bounds = _frame.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var viewport = Ui.Clay.GetViewportSize();
        var offset = _placement.Offset;
        var overflowX = bounds.X + bounds.Width - viewport.Width;
        if (overflowX > 0)
        {
            offset.X -= Ui.Clay.PixelsToPoints(overflowX);
        }

        var overflowY = bounds.Y + bounds.Height - viewport.Height;
        if (overflowY > 0)
        {
            offset.Y -= Ui.Clay.PixelsToPoints(overflowY);
        }

        _placement.Offset = new Vector2(MathF.Max(0, offset.X), MathF.Max(0, offset.Y));
    }

    private void Update()
    {
        var hovered = FindTooltipOwner(Ui.HoveredWidget);
        var pressed = Ui.WasPressed(UiMouseButton.Left) || Ui.WasPressed(UiMouseButton.Right) || Ui.WasPressed(UiMouseButton.Middle);

        if (hovered != _candidate)
        {
            _candidate = hovered;
            _hoverStart = Ui.ElapsedSeconds;
        }

        if (_target != null && (_target != hovered || pressed || !_target.IsVisible || !_target.IsEnabled))
        {
            Hide();
            _candidate = hovered;
            _hoverStart = Ui.ElapsedSeconds;
            if (pressed)
            {
                _hoverStart += TooltipSettings.Delay;
            }

            return;
        }

        if (_target == null && _candidate != null && !pressed && !Ui.PointerDown && Ui.ElapsedSeconds - _hoverStart >= TooltipSettings.Delay)
        {
            Show(_candidate);
            return;
        }

        if (_target != null)
        {
            ClampToViewport();
        }
    }

    protected override void CollectChildren(List<Widget> frame)
    {
        Update();
        if (_target != null)
        {
            _frame.CollectFrame(frame);
        }
    }

    protected override void BuildContent()
    {
        if (_target != null)
        {
            _frame.Build();
        }
    }
}
