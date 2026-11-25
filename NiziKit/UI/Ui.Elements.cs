using DenOfIz;

namespace NiziKit.UI;

public static partial class Ui
{
    public static Element Element(uint id = 0)
    {
        return new Element(id);
    }

    public static Element Row(uint id = 0)
    {
        return new Element(id);
    }

    public static Element Column(uint id = 0)
    {
        var element = new Element(id);
        element.Desc.Layout.Direction = UIDirection.Column;
        return element;
    }

    public static uint Open(in UIElementDesc desc)
    {
        if ((desc.Flags & (uint)UIElementFlagBits.Focusable) != 0 && desc.Layout.Display != UIDisplay.None)
        {
            _frameFocusables++;
        }

        return Native.OpenElement(in desc);
    }

    public static void Close()
    {
        Native.CloseElement();
    }
}
