using DenOfIz;

namespace NiziKit.UI;

public static partial class Ui
{
    internal static uint Open(in UIElementDesc desc)
    {
        if ((desc.Flags & (uint)UIElementFlagBits.Focusable) != 0 && desc.Layout.Display != UIDisplay.None)
        {
            _frameFocusables++;
        }

        return Native.OpenElement(in desc);
    }

    internal static void Close()
    {
        Native.CloseElement();
    }
}
