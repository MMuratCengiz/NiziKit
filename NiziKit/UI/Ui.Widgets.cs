using System.Numerics;
using DenOfIz;
using NiziKit.UI.Widgets;
using Semaphore = DenOfIz.Semaphore;

namespace NiziKit.UI;

public sealed class OverlayLayer : Container
{
    public OverlayLayer()
    {
        Width = 0;
        Height = 0;
    }
}

public sealed class Shortcut(KeyCode key, bool ctrl, bool shift, bool alt, Action action)
{
    public KeyCode Key { get; } = key;
    public bool Ctrl { get; } = ctrl;
    public bool Shift { get; } = shift;
    public bool Alt { get; } = alt;
    public Action Action { get; } = action;
}

public static partial class Ui
{
    private const uint CtrlMask = (uint)(KeyMod.Lctrl | KeyMod.Rctrl | KeyMod.Lgui | KeyMod.Rgui);
    private const uint ShiftMask = (uint)(KeyMod.Lshift | KeyMod.Rshift);
    private const uint AltMask = (uint)(KeyMod.Lalt | KeyMod.Ralt);

    private static uint _nextWidgetId = 1;
    private static readonly List<Widget> FrameWidgets = new();
    private static readonly List<Shortcut> Shortcuts = new();
    private static Widget? _focusCandidate;
    private static Widget? _dropCandidate;
    private static Widget? _hoverCandidate;
    private static float _hoverStart;
    private static uint _buttonsDown;
    private static uint _pressedThisFrame;
    private static uint _releasedThisFrame;
    private static Vector2 _lastPointerPosition;
    private static float _nextZ = 1;

    public static Widget? Root { get; set; }
    public static OverlayLayer Overlays { get; } = new();
    public static Widget? Focused { get; private set; }
    public static Widget? HoveredWidget { get; internal set; }
    public static Widget? DragSource { get; private set; }
    public static object? DragPayload { get; private set; }
    public static Vector2 PointerPosition { get; private set; }
    public static Vector2 PointerDelta { get; private set; }
    public static float WheelDelta { get; private set; }
    public static float ElapsedSeconds { get; private set; }
    public static float DeltaTime { get; private set; }
    public static float DragThreshold { get; set; } = 4;
    public static float FloatingZIndex { get; internal set; }
    public static int WarmupFrames { get; set; } = 2;
    public static Color WarmupColor { get; set; } = Color.Rgb(24, 26, 32);
    public static int FrameCount => _frameCount;
    internal static Color? WarmupOverlay { get; private set; }
    private static int _frameCount;
    public static int PressClicks { get; private set; }
    public static int ReleaseClicks { get; private set; }

    public static bool PointerDown => IsButtonDown(MouseButton.Left);
    internal static bool PointerPressedThisFrame => WasPressed(MouseButton.Left);
    internal static bool PointerReleasedThisFrame => WasReleased(MouseButton.Left);

    public static event Action<KeyboardEventData>? UnhandledKeyDown;

    /// <summary>
    /// Bit for a button within the tracked-button masks. DenOfIz numbers buttons from 1
    /// (SDL convention), so bit 0 is unused.
    /// </summary>
    internal static uint Bit(MouseButton button) => 1u << (int)button;

    internal static bool IsTracked(MouseButton button) => button >= MouseButton.Left && button <= MouseButton.X2;

    public static bool IsButtonDown(MouseButton button)
    {
        return (_buttonsDown & Bit(button)) != 0;
    }

    public static bool WasPressed(MouseButton button)
    {
        return (_pressedThisFrame & Bit(button)) != 0;
    }

    public static bool WasReleased(MouseButton button)
    {
        return (_releasedThisFrame & Bit(button)) != 0;
    }

    internal static uint AllocateWidgetId()
    {
        while (true)
        {
            var hash = _nextWidgetId++;
            hash ^= hash >> 16;
            hash *= 0x85EBCA6B;
            hash ^= hash >> 13;
            hash *= 0xC2B2AE35;
            hash ^= hash >> 16;
            if (hash != 0)
            {
                return hash;
            }
        }
    }

    public static float NextZIndex()
    {
        return _nextZ++;
    }

    /// <summary>
    /// The widget under the pointer once it has been hovered continuously for <paramref name="delay"/>
    /// seconds, otherwise null. Holding or clicking a button restarts the wait. Call it from Update
    /// (it reads the hover published by the previous frame) to drive hover-triggered overlays:
    /// <c>if (Ui.HoverDelay(0.6f) == target) tip.ShowAt(Ui.PointerPosition);</c>
    /// Walk <see cref="Widget.Parent"/> from the result if the trigger sits on an ancestor.
    /// </summary>
    public static Widget? HoverDelay(float delay)
    {
        var hovered = HoveredWidget;
        if (hovered != _hoverCandidate || _pressedThisFrame != 0)
        {
            _hoverCandidate = hovered;
            _hoverStart = ElapsedSeconds;
        }

        if (_hoverCandidate == null || PointerDown || ElapsedSeconds - _hoverStart < delay)
        {
            return null;
        }

        return _hoverCandidate;
    }

    internal static void RequestFocus(Widget widget)
    {
        _focusCandidate = widget;
    }

    internal static void SetDropCandidate(Widget widget)
    {
        _dropCandidate = widget;
    }

    internal static void BeginDrag(Widget source)
    {
        DragSource = source;
        DragPayload = null;
    }

    public static void BeginDragDrop(object? payload)
    {
        DragPayload = payload;
    }

    public static void SetFocus(Widget? widget)
    {
        if (Focused == widget)
        {
            return;
        }

        var previous = Focused;
        Focused = widget;
        previous?.OnFocusChanged(false);
        widget?.OnFocusChanged(true);
    }

    public static void FocusNext(bool reverse = false)
    {
        var count = FrameWidgets.Count;
        if (count == 0)
        {
            return;
        }

        var start = Focused != null ? FrameWidgets.IndexOf(Focused) : -1;
        for (var step = 1; step <= count; step++)
        {
            var index = reverse ? (start - step + count * 2) % count : (start + step) % count;
            var candidate = FrameWidgets[index];
            if (candidate.Focusable && candidate.IsEnabled && candidate.IsVisible)
            {
                SetFocus(candidate);
                return;
            }
        }
    }

    public static Shortcut AddShortcut(KeyCode key, Action action, bool ctrl = false, bool shift = false, bool alt = false)
    {
        var shortcut = new Shortcut(key, ctrl, shift, alt, action);
        Shortcuts.Add(shortcut);
        return shortcut;
    }

    public static void RemoveShortcut(Shortcut shortcut)
    {
        Shortcuts.Remove(shortcut);
    }

    public static void Render(float dt, CommandList commandList)
    {
        BeginFrame(dt);
        DrawTree(Root, Overlays);
        EndFrame(dt, commandList);
    }

    public static (Texture Texture, Semaphore Semaphore)? RenderToTexture(float dt)
    {
        BeginFrame(dt);
        DrawTree(Root, Overlays);
        return EndFrame(dt);
    }

    public static void Draw(Widget widget)
    {
        DrawTree(widget, null);
    }

    private static void DrawTree(Widget? first, Widget? second)
    {
        FrameWidgets.Clear();
        first?.CollectFrame(FrameWidgets);
        second?.CollectFrame(FrameWidgets);

        _focusCandidate = null;
        _dropCandidate = null;
        HoveredWidget = null;
        FloatingZIndex = 0;
        WarmupOverlay = _frameCount < WarmupFrames ? WarmupColor : null;
        _frameCount++;

        for (var i = 0; i < FrameWidgets.Count; i++)
        {
            FrameWidgets[i].Poll();
        }

        if (PointerPressedThisFrame)
        {
            if (_focusCandidate != null)
            {
                SetFocus(_focusCandidate);
            }
            else if (Focused != null && FrameWidgets.Contains(Focused))
            {
                SetFocus(null);
            }
        }

        if (DragSource != null && !IsButtonDown(MouseButton.Left))
        {
            var source = DragSource;
            var target = _dropCandidate;
            var payload = DragPayload;
            DragSource = null;
            DragPayload = null;
            target?.ReceiveDrop(new DropEvent(source, payload, PointerPosition));
            source.FinishDrag();
        }

        first?.Build();
        second?.Build();
    }

    private static void BeginInput(float dt)
    {
        ElapsedSeconds += dt;
        DeltaTime = dt;
        IsPointerOverUi = false;
        PointerDelta = PointerPosition - _lastPointerPosition;
        _lastPointerPosition = PointerPosition;
    }

    private static void EndInput()
    {
        _pressedThisFrame = 0;
        _releasedThisFrame = 0;
        WheelDelta = 0;
    }

    private static void HandleWidgetEvent(ref Event ev)
    {
        switch (ev.Type)
        {
            case EventType.MouseMotion:
                PointerPosition = new Vector2(ev.MouseMotion.X, ev.MouseMotion.Y);
                break;
            case EventType.MouseButtonDown when IsTracked(ev.MouseButton.Button):
                PointerPosition = new Vector2(ev.MouseButton.X, ev.MouseButton.Y);
                _buttonsDown |= Bit(ev.MouseButton.Button);
                _pressedThisFrame |= Bit(ev.MouseButton.Button);
                PressClicks = (int)ev.MouseButton.Clicks;
                break;
            case EventType.MouseButtonUp when IsTracked(ev.MouseButton.Button):
                PointerPosition = new Vector2(ev.MouseButton.X, ev.MouseButton.Y);
                _buttonsDown &= ~Bit(ev.MouseButton.Button);
                _releasedThisFrame |= Bit(ev.MouseButton.Button);
                ReleaseClicks = (int)ev.MouseButton.Clicks;
                break;
            case EventType.MouseWheel:
                WheelDelta += ev.MouseWheel.Y;
                break;
            case EventType.KeyDown:
                HandleKeyDown(in ev.Key);
                break;
            case EventType.TextInput:
                Focused?.OnTextInput(ev.Text.Text.ToString());
                break;
        }
    }

    private static void HandleKeyDown(in KeyboardEventData key)
    {
        if (Focused != null && Focused.OnKeyDown(in key))
        {
            return;
        }

        var ctrl = (key.Mod & CtrlMask) != 0;
        var shift = (key.Mod & ShiftMask) != 0;
        var alt = (key.Mod & AltMask) != 0;

        switch (key.KeyCode)
        {
            case KeyCode.Tab:
                FocusNext(shift);
                return;
            case KeyCode.Return:
            case KeyCode.Space:
                if (Focused is { IsKeyActivatable: true })
                {
                    Focused.Activate();
                    return;
                }

                break;
            case KeyCode.Escape:
                if (Focused != null)
                {
                    SetFocus(null);
                }

                break;
        }

        for (var i = 0; i < Shortcuts.Count; i++)
        {
            var shortcut = Shortcuts[i];
            if (shortcut.Key == key.KeyCode && shortcut.Ctrl == ctrl && shortcut.Shift == shift && shortcut.Alt == alt)
            {
                shortcut.Action();
                return;
            }
        }

        UnhandledKeyDown?.Invoke(key);
    }
}
