using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using DenOfIz;

namespace NiziKit.UI.Widgets;

public sealed class TextCache : IDisposable
{
    private string? _text;
    private byte[]? _bytes;
    private StringView _view;

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

public static class Text
{
    private const int SlotCount = 256;
    private static readonly TextCache?[] Slots = new TextCache?[SlotCount];

    private static TextCache Slot(string text)
    {
        var index = (text.GetHashCode() & int.MaxValue) % SlotCount;
        return Slots[index] ??= new TextCache();
    }

    public static void Draw(string text, in ClayTextDesc desc)
    {
        if (text.Length == 0)
        {
            return;
        }

        Slot(text).Draw(text, in desc);
    }

    /// <summary>Releases the pinned encode buffers. Called from <see cref="Ui.Shutdown"/>.</summary>
    public static void Clear()
    {
        for (var i = 0; i < Slots.Length; i++)
        {
            Slots[i]?.Dispose();
            Slots[i] = null;
        }
    }
}
