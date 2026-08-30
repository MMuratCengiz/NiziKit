using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using DenOfIz;

namespace NiziKit.UI.Widgets;

public sealed class UiTextCache : IDisposable
{
    private string? _text;
    private byte[]? _bytes;
    private StringView _view;

    public string? Text => _text;
    public int ByteCount => (int)_view.NumChars;
    public bool IsEmpty => _view.NumChars == 0;

    public StringView Get(string text)
    {
        if (ReferenceEquals(text, _text))
        {
            return _view;
        }

        if (_text != null && _text.Length == text.Length && string.Equals(_text, text, StringComparison.Ordinal))
        {
            _text = text;
            return _view;
        }

        Encode(text);
        return _view;
    }

    public void Draw(string text, in ClayTextDesc desc)
    {
        var view = Get(text);
        if (view.NumChars == 0)
        {
            return;
        }

        Ui.Clay.Text(view, in desc);
    }

    public Vector2 Measure(string text, ushort fontId, ushort fontSize)
    {
        var view = Get(text);
        if (view.NumChars == 0)
        {
            return Vector2.Zero;
        }

        var dimensions = Ui.Clay.MeasureText(view, fontId, fontSize);
        return new Vector2(Ui.Clay.PixelsToPoints(dimensions.Width), Ui.Clay.PixelsToPoints(dimensions.Height));
    }

    public void Invalidate()
    {
        _text = null;
        _view = default;
    }

    public void Dispose()
    {
        _text = null;
        _bytes = null;
        _view = default;
    }

    private void Encode(string text)
    {
        _text = text;
        if (text.Length == 0)
        {
            _view = default;
            return;
        }

        var byteCount = Encoding.UTF8.GetByteCount(text);
        if (_bytes == null || _bytes.Length < byteCount)
        {
            var capacity = Math.Max(byteCount, _bytes == null ? 16 : _bytes.Length * 2);
            _bytes = GC.AllocateUninitializedArray<byte>(capacity, pinned: true);
        }

        Encoding.UTF8.GetBytes(text, 0, text.Length, _bytes, 0);
        _view = new StringView
        {
            Chars = Marshal.UnsafeAddrOfPinnedArrayElement(_bytes, 0),
            NumChars = (ulong)byteCount
        };
    }
}

public static class UiText
{
    private const int SlotCount = 256;
    private static readonly UiTextCache?[] Slots = new UiTextCache?[SlotCount];

    public static UiTextCache Slot(string text)
    {
        var index = (text.GetHashCode() & int.MaxValue) % SlotCount;
        return Slots[index] ??= new UiTextCache();
    }

    public static StringView View(string text)
    {
        if (text.Length == 0)
        {
            return default;
        }

        return Slot(text).Get(text);
    }

    public static void Draw(string text, in ClayTextDesc desc)
    {
        if (text.Length == 0)
        {
            return;
        }

        Slot(text).Draw(text, in desc);
    }

    public static Vector2 Measure(string text, ushort fontId, ushort fontSize)
    {
        if (text.Length == 0)
        {
            return Vector2.Zero;
        }

        return Slot(text).Measure(text, fontId, fontSize);
    }

    public static Vector2 Measure(string text)
    {
        return Measure(text, UiTheme.FontId, (ushort)UiTheme.FontSize);
    }

    public static ClayTextDesc Desc(UiColor color, int fontSize = 0, ushort? fontId = null, bool wrap = true, UiAlign align = UiAlign.Start)
    {
        var desc = ClayTextDesc.Default();
        desc.TextColor = color.ToClay();
        desc.FontId = fontId ?? UiTheme.FontId;
        desc.FontSize = (ushort)(fontSize > 0 ? fontSize : UiTheme.FontSize);
        desc.WrapMode = wrap ? ClayTextWrapMode.Words : ClayTextWrapMode.None;
        desc.TextAlignment = align.ToClayText();
        return desc;
    }

    public static void Clear()
    {
        for (var i = 0; i < Slots.Length; i++)
        {
            Slots[i]?.Dispose();
            Slots[i] = null;
        }
    }
}
