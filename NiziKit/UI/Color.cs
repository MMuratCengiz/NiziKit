using DenOfIz;

namespace NiziKit.UI;

public readonly struct Color(byte r, byte g, byte b, byte a = 255)
{
    public readonly byte R = r;
    public readonly byte G = g;
    public readonly byte B = b;
    public readonly byte A = a;

    public static Color Rgb(byte r, byte g, byte b)
    {
        return new Color(r, g, b);
    }

    public static Color Rgba(byte r, byte g, byte b, byte a)
    {
        return new Color(r, g, b, a);
    }

    public Color WithAlpha(byte a)
    {
        return new Color(R, G, B, a);
    }

    public ClayColor ToClay()
    {
        return ClayColor.Create(R, G, B, A);
    }
}
