using System.Numerics;
using DenOfIz;

namespace NiziKit.Inputs;

public static class Input
{
    private static readonly HashSet<KeyCode> KeysPressed = [];
    private static readonly HashSet<KeyCode> KeysReleased = [];
    private static uint _mousePressed;
    private static uint _mouseReleased;

    public static Vector2 MouseDelta { get; private set; }

    public static Vector2 MouseScroll { get; private set; }

    public static bool WasKeyPressed(KeyCode key) => KeysPressed.Contains(key);

    public static bool WasKeyReleased(KeyCode key) => KeysReleased.Contains(key);

    public static bool WasMouseButtonPressed(MouseButton button) => (_mousePressed & Bit(button)) != 0;

    public static bool WasMouseButtonReleased(MouseButton button) => (_mouseReleased & Bit(button)) != 0;

    internal static void BeginFrame()
    {
        KeysPressed.Clear();
        KeysReleased.Clear();
        _mousePressed = 0;
        _mouseReleased = 0;
        MouseDelta = Vector2.Zero;
        MouseScroll = Vector2.Zero;
    }

    internal static void ProcessEvent(ref Event ev)
    {
        switch (ev.Type)
        {
            case EventType.KeyDown when ev.Key.Repeat == 0:
                KeysPressed.Add(ev.Key.KeyCode);
                break;
            case EventType.KeyUp:
                KeysReleased.Add(ev.Key.KeyCode);
                break;
            case EventType.MouseButtonDown:
                _mousePressed |= Bit(ev.MouseButton.Button);
                break;
            case EventType.MouseButtonUp:
                _mouseReleased |= Bit(ev.MouseButton.Button);
                break;
            case EventType.MouseMotion:
                MouseDelta += new Vector2(ev.MouseMotion.RelX, ev.MouseMotion.RelY);
                break;
            case EventType.MouseWheel:
                MouseScroll += new Vector2(ev.MouseWheel.X, ev.MouseWheel.Y);
                break;
        }
    }

    private static uint Bit(MouseButton button) => 1u << (int)button;
}
