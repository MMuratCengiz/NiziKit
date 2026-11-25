using System.Numerics;
using DenOfIz;
using NiziKit.Graphics;
using NiziKit.UI.Widgets;
using DzUI = DenOfIz.UI;
using Semaphore = DenOfIz.Semaphore;

namespace NiziKit.UI;

public static partial class Ui
{
    private const float TooltipOffset = 6;

    private static DzUI? _ui;
    private static uint _nextWidgetId;
    private static uint _activateId;
    private static uint _tooltipId;
    private static uint _tooltipTarget;
    private static uint _tooltipOffer;
    private static float _tooltipTime;
    private static char[] _tooltipText = new char[128];
    private static int _tooltipLength;
    private static int _focusables;
    private static int _frameFocusables;
    private static Widget? _focusedWidget;
    private static Widget? _frameFocusedWidget;
    private static SystemCursor _cursor;
    private static bool _textInputActive;
    private static float _scale = 1;

    internal static readonly List<Popup> OpenPopups = new();
    internal static Popup? BuildingPopup;

    public static DzUI Native => _ui ?? throw new InvalidOperationException("Ui is not initialized; it is created with the GraphicsContext of a window.");

    internal static bool IsInitialized => _ui != null;

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
        set => Native.SetDebugFlags(value);
    }

    public static bool IsPointerOverUi => _ui != null && _ui.IsPointerOverUI;

    public static uint HoveredId => _ui?.HoveredId ?? 0;

    public static Vector2 PointerPosition => Native.PointerPosition;

    public static uint Focus
    {
        get => _ui?.Focus ?? 0;
        set => Native.Focus = value;
    }

    public static Widget? FocusedWidget => _focusedWidget;

    public static bool WantsTextInput => _textInputActive;

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
            InputSystem.StopTextInput();
            _textInputActive = false;
        }

        OpenPopups.Clear();
        _focusedWidget = null;
        _frameFocusedWidget = null;
        _ui.Dispose();
        _ui = null;
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

    public static void BeginFrame(float deltaTime)
    {
        Native.BeginFrame(deltaTime);
        Fonts.ReleaseRemoved();
        DeltaTime = deltaTime;
        Time += deltaTime;
        _frameFocusedWidget = null;
        _frameFocusables = 0;
        _tooltipOffer = 0;
    }

    public static void EndFrame()
    {
        var ui = Native;
        DeclarePopups(null);
        DeclareTooltip();
        ui.EndFrame();

        _activateId = 0;
        _focusedWidget = _frameFocusedWidget;
        _focusables = _frameFocusables;

        var wantsText = _focusedWidget is { AcceptsTextInput: true };
        if (wantsText != _textInputActive)
        {
            _textInputActive = wantsText;
            if (wantsText)
            {
                InputSystem.StartTextInput();
            }
            else
            {
                InputSystem.StopTextInput();
            }
        }

        if (ui.Cursor != _cursor)
        {
            _cursor = ui.Cursor;
            InputSystem.SetCursor(_cursor);
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

    public static void Draw(Widget widget)
    {
        widget.Build();
    }

    public static uint Id(ReadOnlySpan<char> name, uint seed = 0)
    {
        return DzUI.HashId(name, seed);
    }

    public static uint Id(uint parent, uint value)
    {
        return DzUI.CombineId(parent, value);
    }

    internal static uint NewWidgetId()
    {
        return DzUI.CombineId(0x4E5A4B57u, ++_nextWidgetId);
    }

    public static UIInteraction Interaction(uint id)
    {
        return Native.GetInteraction(id);
    }

    public static bool Activated(uint id, in UIInteraction interaction)
    {
        return interaction.Clicked || (_activateId != 0 && _activateId == id);
    }

    public static bool TryGetRect(uint id, out UIRect rect)
    {
        return Native.TryGetElementRect(id, out rect);
    }

    public static void FocusNext(bool reverse = false)
    {
        Native.FocusNext(reverse);
    }

    public static bool TryGetScroll(uint id, out UIScrollState state)
    {
        return Native.TryGetScrollState(id, out state);
    }

    public static void ScrollTo(uint id, Vector2 offset)
    {
        Native.SetScrollOffset(id, offset);
    }

    public static void ScrollToEnd(uint id)
    {
        var x = TryGetScroll(id, out var state) ? state.Offset.X : 0;
        Native.SetScrollOffset(id, new Vector2(x, float.MaxValue));
    }

    public static void ScrollIntoView(uint id)
    {
        Native.ScrollIntoView(id);
    }

    public static void Tooltip(uint targetId, ReadOnlySpan<char> text)
    {
        if (targetId == 0 || text.IsEmpty)
        {
            return;
        }

        if (_tooltipText.Length < text.Length)
        {
            _tooltipText = new char[Math.Max(text.Length, _tooltipText.Length * 2)];
        }

        text.CopyTo(_tooltipText);
        _tooltipLength = text.Length;
        _tooltipOffer = targetId;
    }

    internal static void TrackFocused(Widget widget)
    {
        _frameFocusedWidget = widget;
    }

    internal static void CountFocusable()
    {
        _frameFocusables++;
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
            popup.Build();
            if (i >= OpenPopups.Count || !ReferenceEquals(OpenPopups[i], popup))
            {
                i--;
            }
        }
    }

    internal static Popup? TopmostPopup => OpenPopups.Count > 0 ? OpenPopups[^1] : null;

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
        using (Row(_tooltipId)
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
            Text(_tooltipText.AsSpan(0, _tooltipLength), theme.SmallFontSize, theme.TooltipText);
        }
    }

    private static bool HandleKeyDown(in KeyboardEventData key)
    {
        var popup = TopmostPopup;
        if (popup != null && popup.OnKeyDown(in key))
        {
            return true;
        }

        if (_focusedWidget is { Enabled: true } focused && focused.OnKeyDown(in key))
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
        if (_focusedWidget is not { AcceptsTextInput: true, Enabled: true } focused || text.Chars == IntPtr.Zero || text.NumChars == 0)
        {
            return false;
        }

        focused.OnTextInput(new ReadOnlySpan<byte>((void*)text.Chars, (int)text.NumChars));
        return true;
    }
}
