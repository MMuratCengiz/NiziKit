using DenOfIz;

namespace NiziKit.UI;

internal static class Keys
{
    private const uint ShiftMask = (uint)(KeyMod.Shift | KeyMod.Lshift | KeyMod.Rshift);
    private const uint CtrlMask = (uint)(KeyMod.Ctrl | KeyMod.Lctrl | KeyMod.Rctrl);
    private const uint AltMask = (uint)(KeyMod.Alt | KeyMod.Lalt | KeyMod.Ralt);
    private const uint GuiMask = (uint)(KeyMod.Gui | KeyMod.Lgui | KeyMod.Rgui);

    public static bool Shift(uint mod) => (mod & ShiftMask) != 0;

    public static bool Word(uint mod) => (mod & (AltMask | CtrlMask)) != 0;

    public static bool Line(uint mod) => (mod & GuiMask) != 0;

    public static bool Shortcut(uint mod) => (mod & (GuiMask | CtrlMask)) != 0;
}
