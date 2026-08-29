using DenOfIz;

namespace NiziKit.UI;

public readonly struct UiColor(byte r, byte g, byte b, byte a = 255)
{
    public readonly byte R = r;
    public readonly byte G = g;
    public readonly byte B = b;
    public readonly byte A = a;

    public static UiColor Rgb(byte r, byte g, byte b)
    {
        return new UiColor(r, g, b);
    }

    public static UiColor Rgba(byte r, byte g, byte b, byte a)
    {
        return new UiColor(r, g, b, a);
    }

    public UiColor WithAlpha(byte a)
    {
        return new UiColor(R, G, B, a);
    }

    public ClayColor ToClay()
    {
        return ClayColor.Create(R, G, B, A);
    }
}
