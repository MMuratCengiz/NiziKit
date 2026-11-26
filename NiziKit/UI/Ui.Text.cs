using System.Numerics;
using System.Runtime.CompilerServices;
using DenOfIz;

namespace NiziKit.UI;

public static partial class Ui
{
    internal static UITextDesc TextStyleOf(float size = 0, UIColor color = default, ushort fontId = 0)
    {
        var theme = Theme;
        return new UITextDesc
        {
            FontSize = size > 0 ? size : theme.FontSize,
            Color = color.A != 0 ? color : theme.Text,
            FontId = fontId != 0 ? fontId : theme.FontId
        };
    }

    internal static uint Text(ReadOnlySpan<char> text, float size, UIColor color, ushort fontId = 0)
    {
        var desc = TextStyleOf(size, color, fontId);
        return Native.Text(text, in desc);
    }

    internal static uint Text(ReadOnlySpan<char> text, in UITextDesc desc)
    {
        return Native.Text(text, in desc);
    }

    internal static uint Text(ReadOnlySpan<byte> utf8, in UITextDesc desc)
    {
        return Native.Text(utf8, in desc);
    }

    internal static uint Line(ReadOnlySpan<char> text, float size, UIColor color, ushort fontId = 0)
    {
        var desc = TextStyleOf(size, color, fontId);
        desc.Wrap = UITextWrap.None;
        desc.Overflow = UITextOverflow.Ellipsis;
        return Native.Text(text, in desc);
    }

    internal static void Glyph(string glyph, float size, UIColor color)
    {
        if (FontAwesome.IsInitialized)
        {
            var desc = TextStyleOf(size, color, FontAwesome.FontId);
            desc.Wrap = UITextWrap.None;
            Native.Text(glyph, in desc);
        }
    }

    public static Vector2 MeasureText(ReadOnlySpan<char> text, TextStyle style = default, float maxWidth = 0)
    {
        var desc = style.Resolve(Theme);
        return Native.MeasureText(text, in desc, maxWidth);
    }
}

[InterpolatedStringHandler]
public ref struct FormattedText
{
    private static char[] _buffer = new char[256];
    private int _length;

    public FormattedText(int literalLength, int formattedCount)
    {
        _length = 0;
        Reserve(literalLength + formattedCount * 8);
    }

    public readonly ReadOnlySpan<char> Text => _buffer.AsSpan(0, _length);

    public static implicit operator FormattedText(string? value)
    {
        var text = new FormattedText(0, 0);
        text.AppendFormatted(value.AsSpan());
        return text;
    }

    public void AppendLiteral(string value)
    {
        AppendFormatted(value.AsSpan());
    }

    public void AppendFormatted(ReadOnlySpan<char> value)
    {
        Reserve(_length + value.Length);
        value.CopyTo(_buffer.AsSpan(_length));
        _length += value.Length;
    }

    public void AppendFormatted(string? value)
    {
        AppendFormatted(value.AsSpan());
    }

    public void AppendFormatted(int value, string? format = null) => AppendSpanFormattable(value, format);
    public void AppendFormatted(uint value, string? format = null) => AppendSpanFormattable(value, format);
    public void AppendFormatted(long value, string? format = null) => AppendSpanFormattable(value, format);
    public void AppendFormatted(ulong value, string? format = null) => AppendSpanFormattable(value, format);
    public void AppendFormatted(float value, string? format = null) => AppendSpanFormattable(value, format);
    public void AppendFormatted(double value, string? format = null) => AppendSpanFormattable(value, format);

    public void AppendFormatted(bool value)
    {
        AppendFormatted(value ? "True" : "False");
    }

    public void AppendFormatted(char value)
    {
        Reserve(_length + 1);
        _buffer[_length++] = value;
    }

    public void AppendFormatted<T>(T value, string? format = null)
    {
        if (value is ISpanFormattable)
        {
            int written;
            while (!((ISpanFormattable)value).TryFormat(_buffer.AsSpan(_length), out written, format, null))
            {
                Reserve(_buffer.Length * 2);
            }

            _length += written;
            return;
        }

        AppendFormatted(value?.ToString());
    }

    private void AppendSpanFormattable<T>(T value, string? format) where T : ISpanFormattable
    {
        int written;
        while (!value.TryFormat(_buffer.AsSpan(_length), out written, format, null))
        {
            Reserve(_buffer.Length * 2);
        }

        _length += written;
    }

    private readonly void Reserve(int capacity)
    {
        if (capacity <= _buffer.Length)
        {
            return;
        }

        var grown = new char[Math.Max(capacity, _buffer.Length * 2)];
        _buffer.AsSpan(0, _length).CopyTo(grown);
        _buffer = grown;
    }
}
