using System.Numerics;
using DenOfIz;
using NiziKit.Graphics;
using DzUI = DenOfIz.UI;
using Semaphore = DenOfIz.Semaphore;

namespace NiziKit.UI;

public static partial class Ui
{
    private const float TooltipOffset = 6;
    private const float NotificationLifetime = 3.5f;

    private struct LayoutFrame
    {
        public bool Row;
        public bool Scrolls;
        public bool Fills;
    }

    private sealed class Notification
    {
        public uint Id;
        public required string Text;
        public Color Color;
        public float Age;
    }

    private static DzUI? _ui;
    private static uint _activateId;
    private static uint _tooltipId;
    private static uint _notificationsId;
    private static uint _tooltipTarget;
    private static uint _tooltipOffer;
    private static float _tooltipTime;
    private static string _tooltipText = "";
    private static int _focusables;
    private static int _frameFocusables;
    private static Widget? _focusedWidget;
    private static Widget? _frameFocusedWidget;
    private static SystemCursor _cursor;
    private static bool _textInputActive;
    private static float _scale = 1;
    private static UIDebugFlagBits _debugFlags;
    private static List<Widget> _built = new();
    private static List<Widget> _previous = new();
    private static readonly List<LayoutFrame> Layout = new();
    private static readonly List<bool> EnabledStack = new();
    private static readonly List<Notification> Notifications = new();
    private static uint _nextNotification;

    internal static readonly List<Popup> OpenPopups = new();
    internal static Popup? BuildingPopup;

    internal static DzUI Native => _ui ?? throw new InvalidOperationException("Ui is not initialized; it is created with the GraphicsContext of a window.");

    internal static bool IsInitialized => _ui != null;

    internal static bool Headless { get; set; }

    internal static string? ShownTooltip => _tooltipTarget != 0 && _tooltipTime >= TooltipDelay ? _tooltipText : null;

    internal static IEnumerable<string> NotificationTexts => Notifications.Select(n => n.Text);

    internal static List<Widget> Built => _built;

    internal static int FrameNumber { get; private set; }

    public static Widget? Root { get; set; }

    public static Theme Theme { get; set; } = Theme.Dark();

    public static float Time { get; private set; }

    public static float DeltaTime { get; private set; }

    public static bool FocusVisible { get; private set; }

    public static float TooltipDelay { get; set; } = 0.5f;

    public static float Scale
    {
        get => _scale;
        set
        {
            _scale = value > 0 ? value : 1;
            _ui?.SetScale(_scale);
        }
    }

    public static float PixelsPerPoint => Native.PixelsPerPoint;

    public static Vector2 ViewportSize => Native.ViewportSize;

    public static UIDebugFlagBits DebugFlags
    {
        get => _debugFlags;
        set
        {
            _debugFlags = value;
            Native.SetDebugFlags(value);
        }
    }

    public static bool IsPointerOverUi => _ui != null && _ui.IsPointerOverUI;

    public static Vector2 PointerPosition => Native.PointerPosition;

    public static Widget? FocusedWidget => _focusedWidget;

    public static bool WantsTextInput => _textInputActive;

    internal static bool IsAnimating => _ui is { IsAnimating: true };

    internal static float NextTimer
    {
        get
        {
            var next = _focusedWidget is TextInput { IsFocused: true, ReadOnly: false } input ? input.Editor.NextBlink : float.PositiveInfinity;
            if (_tooltipTarget != 0 && _tooltipTime < TooltipDelay)
            {
                next = MathF.Min(next, TooltipDelay - _tooltipTime);
            }

            foreach (var notification in Notifications)
            {
                next = MathF.Min(next, NotificationLifetime - notification.Age);
            }

            return next;
        }
    }

    internal static uint Focus
    {
        get => _ui?.Focus ?? 0;
        set => Native.Focus = value;
    }

    internal static bool ParentEnabled => EnabledStack.Count == 0 || EnabledStack[^1];

    internal static bool ParentIsRow => Layout.Count > 0 && Layout[^1].Row;

    public static void Initialize()
    {
        if (_ui != null)
        {
            return;
        }

        _ui = new DzUI(new UIDesc
        {
            LogicalDevice = GraphicsContext.Device,
            ResourceTracking = GraphicsContext.ResourceTracking,
            Window = GraphicsContext.Window,
            RenderTargetFormat = GraphicsContext.BackBufferFormat,
            NumFrames = GraphicsContext.NumFrames,
            Width = GraphicsContext.Width,
            Height = GraphicsContext.Height
        });
        _ui.SetScale(_scale);
        _tooltipId = Id("NiziKit.Tooltip");
        _notificationsId = Id("NiziKit.Notifications");
        FontAwesome.TryInitialize();
    }

    public static void Shutdown()
    {
        if (_ui == null)
        {
            return;
        }

        if (_textInputActive)
        {
            if (!Headless)
            {
                InputSystem.StopTextInput();
            }

            _textInputActive = false;
        }

        OpenPopups.Clear();
        _built.Clear();
        _previous.Clear();
        _focusedWidget = null;
        _frameFocusedWidget = null;
        _ui.Dispose();
        _ui = null;
        Images.Shutdown();
        FontAwesome.Shutdown();
        Fonts.Shutdown();
    }

    public static bool HandleEvent(in Event ev)
    {
        if (_ui == null)
        {
            return false;
        }

        _ui.HandleEvent(in ev);
        switch (ev.Type)
        {
            case EventType.MouseButtonDown:
                FocusVisible = false;
                return false;
            case EventType.KeyDown:
                return HandleKeyDown(in ev.Key);
            case EventType.TextInput:
                return HandleTextInput(ev.Text.Text);
            default:
                return false;
        }
    }

    public static void Update(float deltaTime)
    {
        var ui = Native;
        Images.Upload();
        UiDebug.DispatchQueued();
        ui.BeginFrame(deltaTime);
        Fonts.ReleaseRemoved();
        DeltaTime = deltaTime;
        Time += deltaTime;
        FrameNumber++;
        _frameFocusedWidget = null;
        _frameFocusables = 0;
        _tooltipOffer = 0;

        (_previous, _built) = (_built, _previous);
        _built.Clear();
        Dispatch(_previous);

        Layout.Clear();
        EnabledStack.Clear();
        Root?.BuildFrame();
        DeclarePopups(null);
        DeclareNotifications();
        DeclareTooltip();
        ui.EndFrame();

        _activateId = 0;
        _focusedWidget = _frameFocusedWidget;
        _focusables = _frameFocusables;

        var wantsText = _focusedWidget is { AcceptsTextInput: true };
        if (wantsText != _textInputActive)
        {
            _textInputActive = wantsText;
            if (!Headless && wantsText)
            {
                InputSystem.StartTextInput();
            }
            else if (!Headless)
            {
                InputSystem.StopTextInput();
            }
        }

        if (ui.Cursor != _cursor)
        {
            _cursor = ui.Cursor;
            if (!Headless)
            {
                InputSystem.SetCursor(_cursor);
            }
        }
    }

    public static void Render(CommandList commandList)
    {
        Native.Render((uint)GraphicsContext.FrameIndex, commandList);
    }

    public static (Texture Texture, Semaphore Semaphore) RenderToTexture(ReadOnlySpan<Semaphore> waitSemaphores = default)
    {
        return Native.RenderToTexture((uint)GraphicsContext.FrameIndex, waitSemaphores);
    }

    internal static void AddIdleTime(float seconds)
    {
        Time += seconds;
        if (_tooltipTarget != 0)
        {
            _tooltipTime += seconds;
        }

        foreach (var notification in Notifications)
        {
            notification.Age += seconds;
        }
    }

    public static void Notify(string text, Color color = default)
    {
        Notifications.Add(new Notification { Id = ++_nextNotification, Text = text, Color = color.IsSet ? color : Color.Accent });
        if (Notifications.Count > 4)
        {
            Notifications.RemoveAt(0);
        }
    }

    public static void FocusNext(bool reverse = false)
    {
        Native.FocusNext(reverse);
    }

    internal static uint Id(ReadOnlySpan<char> name, uint seed = 0)
    {
        return DzUI.HashId(name, seed);
    }

    internal static uint Id(uint parent, uint value)
    {
        return DzUI.CombineId(parent, value);
    }

    internal static UIInteraction Interaction(uint id)
    {
        return Native.GetInteraction(id);
    }

    internal static bool Activated(uint id, in UIInteraction interaction)
    {
        return interaction.Clicked || (_activateId != 0 && _activateId == id);
    }

    internal static bool TryGetRect(uint id, out UIRect rect)
    {
        return Native.TryGetElementRect(id, out rect);
    }

    internal static bool TryGetScroll(uint id, out UIScrollState state)
    {
        return Native.TryGetScrollState(id, out state);
    }

    internal static void ScrollTo(uint id, Vector2 offset)
    {
        Native.SetScrollOffset(id, offset);
    }

    internal static void ScrollIntoView(uint id)
    {
        Native.ScrollIntoView(id);
    }

    internal static void OfferTooltip(uint targetId, string text)
    {
        _tooltipText = text;
        _tooltipOffer = targetId;
    }

    internal static void TrackFocused(Widget widget)
    {
        _frameFocusedWidget = widget;
    }

    internal static void PushLayout(bool row, bool scrolls, bool fills = false)
    {
        Layout.Add(new LayoutFrame { Row = row, Scrolls = scrolls, Fills = fills });
    }

    internal static bool ParentFills => Layout.Count > 0 && Layout[^1].Fills;

    internal static void PopLayout()
    {
        Layout.RemoveAt(Layout.Count - 1);
    }

    internal static void PushEnabled(bool enabled)
    {
        EnabledStack.Add(enabled);
    }

    internal static void PopEnabled()
    {
        EnabledStack.RemoveAt(EnabledStack.Count - 1);
    }

    internal static float ShrinkFor(bool keepsSize, UIValue width, UIValue height)
    {
        if (keepsSize || Layout.Count == 0)
        {
            return 0;
        }

        var parent = Layout[^1];
        if (!parent.Row || parent.Scrolls)
        {
            return 0;
        }

        return width.Type == UIValueType.Points ? 0 : 1;
    }

    internal static void DeclarePopups(Popup? layer)
    {
        for (var i = 0; i < OpenPopups.Count; i++)
        {
            var popup = OpenPopups[i];
            if (popup.ParentPopup != layer)
            {
                continue;
            }

            popup.ZIndex = 100 + i;
            popup.BuildFrame();
            if (i >= OpenPopups.Count || !ReferenceEquals(OpenPopups[i], popup))
            {
                i--;
            }
        }
    }

    internal static Popup? TopmostPopup => OpenPopups.Count > 0 ? OpenPopups[^1] : null;

    private static void Dispatch(List<Widget> previous)
    {
        for (var i = 0; i < previous.Count; i++)
        {
            var widget = previous[i];
            widget.State = widget.Interactive ? Native.GetInteraction(widget.Id) : default;
            if (widget.State.Focused)
            {
                TrackFocused(widget);
            }
        }

        for (var i = 0; i < previous.Count; i++)
        {
            previous[i].OnUpdate?.Invoke();
        }

        for (var i = previous.Count - 1; i >= 0; i--)
        {
            previous[i].Dispatch();
        }
    }

    private static void DeclareNotifications()
    {
        for (var i = Notifications.Count - 1; i >= 0; i--)
        {
            if ((Notifications[i].Age += DeltaTime) > NotificationLifetime)
            {
                Notifications.RemoveAt(i);
            }
        }

        var theme = Theme;
        using (Element.Column(_notificationsId)
                   .FloatOnViewport(UIAnchor.BottomCenter, UIAnchor.BottomCenter, new Vector2(0, -44))
                   .ZIndex(5000, absolute: true)
                   .Gap(8)
                   .AlignItems(UIAlign.Center)
                   .Passthrough()
                   .Open())
        {
            foreach (var notification in Notifications)
            {
                using (Element.Row(Id(_notificationsId, notification.Id))
                           .AlignItems(UIAlign.Center)
                           .Gap(10)
                           .Padding(16, 10)
                           .Background(theme.TooltipBackground)
                           .Border(notification.Color.Resolve(theme), 1)
                           .CornerRadius(10)
                           .Animate(UITransitionPropertyFlagBits.Position, 0.25f, UIEasing.EaseOutBack)
                           .Enter(UITransitionState.FromOpacity(0).WithOffset(new Vector2(0, 16)).WithScale(0.94f))
                           .Exit(UITransitionState.FromOpacity(0).WithScale(0.96f))
                           .Open())
                {
                    Element.Box().Size(8, 8).CornerRadius(4).Background(notification.Color.Resolve(theme)).Leaf();
                    Line(notification.Text, theme.FontSize, theme.TooltipText);
                }
            }
        }
    }

    private static void DeclareTooltip()
    {
        if (_tooltipOffer != _tooltipTarget)
        {
            _tooltipTarget = _tooltipOffer;
            _tooltipTime = 0;
        }
        else if (_tooltipTarget != 0)
        {
            _tooltipTime += DeltaTime;
        }

        if (_tooltipTarget == 0 || _tooltipTime < TooltipDelay)
        {
            return;
        }

        var theme = Theme;
        using (Element.Row(_tooltipId)
                   .Padding(8, 5)
                   .MaxWidth(320)
                   .Background(theme.TooltipBackground)
                   .CornerRadius(5)
                   .Passthrough()
                   .FloatOn(_tooltipTarget, UIAnchor.BottomCenter, UIAnchor.TopCenter, new Vector2(0, TooltipOffset))
                   .KeepInViewport()
                   .ZIndex(10000, absolute: true)
                   .Enter(UITransitionState.FromOpacity(0).WithOffset(new Vector2(0, -4)))
                   .Exit(UITransitionState.FromOpacity(0))
                   .Open())
        {
            Text(_tooltipText, theme.SmallFontSize, theme.TooltipText);
        }
    }

    private static bool HandleKeyDown(in KeyboardEventData key)
    {
        var popup = TopmostPopup;
        if (popup != null && popup.OnKeyDown(in key))
        {
            return true;
        }

        if (_focusedWidget is { EnabledNow: true } focused && focused.OnKeyDown(in key))
        {
            return true;
        }

        switch (key.KeyCode)
        {
            case KeyCode.Tab when _focusables > 0 || Focus != 0:
                FocusNext(Keys.Shift(key.Mod));
                FocusVisible = true;
                return true;
            case KeyCode.Escape:
                if (popup is { CloseOnEscape: true })
                {
                    popup.Close();
                    return true;
                }

                if (Focus != 0)
                {
                    Focus = 0;
                    return true;
                }

                return false;
            case KeyCode.Return:
            case KeyCode.KeypadEnter:
            case KeyCode.Space:
                var focus = Focus;
                if (focus == 0)
                {
                    return false;
                }

                _activateId = focus;
                return true;
            default:
                return false;
        }
    }

    private static unsafe bool HandleTextInput(StringView text)
    {
        if (_focusedWidget is not { AcceptsTextInput: true, EnabledNow: true } focused || text.Chars == IntPtr.Zero || text.NumChars == 0)
        {
            return false;
        }

        focused.OnTextInput(new ReadOnlySpan<byte>((void*)text.Chars, (int)text.NumChars));
        return true;
    }
}
