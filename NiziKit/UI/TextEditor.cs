using System.Numerics;
using System.Text;
using DenOfIz;

namespace NiziKit.UI;

internal sealed class TextEditor
{
    private const float BlinkPeriod = 1.06f;

    private byte[] _bytes = new byte[64];
    private int _length;
    private string? _text;
    private string? _selectedText;
    private int _selectedStart;
    private int _selectedEnd;
    private bool _selecting;
    private float _touched;
    private float _preferredX = float.NaN;

    public int Caret;
    public int Anchor;
    public bool Multiline;
    public bool ReadOnly;
    public int MaxLength = int.MaxValue;
    public bool Reveal;

    public ReadOnlySpan<byte> Bytes => _bytes.AsSpan(0, _length);
    public int Length => _length;
    public bool HasSelection => Caret != Anchor;
    public int SelectionStart => Math.Min(Caret, Anchor);
    public int SelectionEnd => Math.Max(Caret, Anchor);
    public string Text => _text ??= Encoding.UTF8.GetString(Bytes);
    public string SelectedText
    {
        get
        {
            if (!HasSelection)
            {
                return "";
            }

            if (_selectedText == null || _selectedStart != SelectionStart || _selectedEnd != SelectionEnd)
            {
                _selectedStart = SelectionStart;
                _selectedEnd = SelectionEnd;
                _selectedText = Encoding.UTF8.GetString(_bytes, _selectedStart, _selectedEnd - _selectedStart);
            }

            return _selectedText;
        }
    }

    public bool CaretVisible => (Ui.Time - _touched) % BlinkPeriod < BlinkPeriod * 0.5f;

    public bool SetText(ReadOnlySpan<char> text)
    {
        var previous = _length;
        _length = 0;
        Caret = Anchor = 0;
        var maxBytes = Encoding.UTF8.GetMaxByteCount(text.Length);
        Span<byte> scratch = maxBytes <= 1024 ? stackalloc byte[maxBytes] : new byte[maxBytes];
        var count = Encoding.UTF8.GetBytes(text, scratch);
        Insert(scratch[..count], force: true);
        _text = null;
        _selectedText = null;
        return previous != 0 || _length != 0;
    }

    public bool Insert(ReadOnlySpan<byte> utf8, bool force = false)
    {
        if (ReadOnly && !force)
        {
            return false;
        }

        var deleted = DeleteSelection();
        var room = MaxLength == int.MaxValue ? int.MaxValue : MaxLength - CodepointCount(Bytes);
        var length = 0;
        var codepoints = 0;
        while (length < utf8.Length && codepoints < room)
        {
            length = NextCodepoint(utf8, length);
            codepoints++;
        }

        if (length == 0)
        {
            return deleted;
        }

        Reserve(_length + length);
        _bytes.AsSpan(Caret, _length - Caret).CopyTo(_bytes.AsSpan(Caret + length));
        var written = 0;
        for (var i = 0; i < length; i++)
        {
            var b = utf8[i];
            if (b == '\r')
            {
                if (!Multiline && (i + 1 >= length || utf8[i + 1] != '\n'))
                {
                    _bytes[Caret + written++] = (byte)' ';
                }

                continue;
            }

            _bytes[Caret + written++] = b == '\n' && !Multiline ? (byte)' ' : b;
        }

        if (written < length)
        {
            _bytes.AsSpan(Caret + length, _length - Caret).CopyTo(_bytes.AsSpan(Caret + written));
        }

        _length += written;
        _text = null;
        _selectedText = null;
        Move(Caret + written, false);
        return true;
    }

    public bool DeleteSelection()
    {
        if (!HasSelection || ReadOnly)
        {
            return false;
        }

        Delete(SelectionStart, SelectionEnd);
        return true;
    }

    public void Delete(int start, int end)
    {
        if (end <= start || ReadOnly)
        {
            return;
        }

        _bytes.AsSpan(end, _length - end).CopyTo(_bytes.AsSpan(start));
        _length -= end - start;
        _text = null;
        _selectedText = null;
        Move(start, false);
    }

    public void Move(int offset, bool extend, bool keepPreferredX = false)
    {
        Caret = Math.Clamp(offset, 0, _length);
        if (!extend)
        {
            Anchor = Caret;
        }

        Anchor = Math.Clamp(Anchor, 0, _length);
        if (!keepPreferredX)
        {
            _preferredX = float.NaN;
        }

        Touch();
    }

    public void Select(int start, int end)
    {
        Anchor = Math.Clamp(start, 0, _length);
        Move(end, true);
    }

    public void SelectAll()
    {
        Select(0, _length);
    }

    public void Touch()
    {
        _touched = Ui.Time;
        Reveal = true;
    }

    public void HandlePointer(in UIInteraction interaction, int offset)
    {
        if (!interaction.PressStarted)
        {
            if (_selecting && offset != Caret)
            {
                Move(offset, true);
            }

            return;
        }

        _selecting = interaction.ClickCount <= 1;
        if (interaction.ClickCount >= 3)
        {
            SelectAll();
        }
        else if (interaction.ClickCount == 2)
        {
            var start = offset;
            var end = offset;
            while (start > 0 && IsWordByte(_bytes[start - 1]))
            {
                start--;
            }

            while (end < _length && IsWordByte(_bytes[end]))
            {
                end++;
            }

            Select(start, end);
        }
        else
        {
            Move(offset, Keys.Shift(InputSystem.GetModState()));
        }
    }

    public void EndPointer()
    {
        _selecting = false;
    }

    public bool? HandleKey(in KeyboardEventData key, uint textId)
    {
        var extend = Keys.Shift(key.Mod);
        var word = Keys.Word(key.Mod);
        var line = Keys.Line(key.Mod);
        var shortcut = Keys.Shortcut(key.Mod);
        switch (key.KeyCode)
        {
            case KeyCode.Backspace:
                if (DeleteSelection())
                {
                    return true;
                }

                if (Caret == 0 || ReadOnly)
                {
                    return false;
                }

                Delete(line ? LineStart(textId) : word ? PrevWord(Caret) : PrevCodepoint(Bytes, Caret), Caret);
                return true;
            case KeyCode.Delete:
                if (DeleteSelection())
                {
                    return true;
                }

                if (Caret >= _length || ReadOnly)
                {
                    return false;
                }

                Delete(Caret, word ? NextWord(Caret) : NextCodepoint(Bytes, Caret));
                return true;
            case KeyCode.Left:
                Move(!extend && !word && !line && HasSelection ? SelectionStart : line ? LineStart(textId) : word ? PrevWord(Caret) : PrevCodepoint(Bytes, Caret), extend);
                return false;
            case KeyCode.Right:
                Move(!extend && !word && !line && HasSelection ? SelectionEnd : line ? LineEnd(textId) : word ? NextWord(Caret) : NextCodepoint(Bytes, Caret), extend);
                return false;
            case KeyCode.Home:
                Move(shortcut || !Multiline ? 0 : LineStart(textId), extend);
                return false;
            case KeyCode.End:
                Move(shortcut || !Multiline ? _length : LineEnd(textId), extend);
                return false;
            case KeyCode.Up:
            case KeyCode.Down:
                var up = key.KeyCode == KeyCode.Up;
                if (Multiline && !line)
                {
                    MoveVertical(textId, up ? -1 : 1, 1, extend);
                }
                else
                {
                    Move(up ? 0 : _length, extend);
                }

                return false;
            case KeyCode.Pageup:
            case KeyCode.Pagedown:
                if (Multiline)
                {
                    MoveVertical(textId, key.KeyCode == KeyCode.Pageup ? -1 : 1, 10, extend);
                }

                return false;
            case KeyCode.Return:
            case KeyCode.KeypadEnter:
                if (!Multiline || shortcut)
                {
                    return null;
                }

                return Insert("\n"u8);
            case KeyCode.A when shortcut:
                SelectAll();
                return false;
            case KeyCode.C when shortcut:
                Copy();
                return false;
            case KeyCode.X when shortcut:
                Copy();
                return DeleteSelection();
            case KeyCode.V when shortcut:
                return Paste();
            default:
                return null;
        }
    }

    public unsafe void Copy()
    {
        if (!HasSelection)
        {
            return;
        }

        fixed (byte* bytes = &_bytes[SelectionStart])
        {
            Clipboard.SetText(new StringView { Chars = (IntPtr)bytes, NumChars = (ulong)(SelectionEnd - SelectionStart) });
        }
    }

    public unsafe bool Paste()
    {
        if (ReadOnly || !Clipboard.HasText())
        {
            return false;
        }

        var text = Clipboard.GetText();
        return text.Chars != IntPtr.Zero && Insert(new ReadOnlySpan<byte>((void*)text.Chars, (int)text.NumChars));
    }

    public static int PrevCodepoint(ReadOnlySpan<byte> text, int offset)
    {
        if (offset <= 0)
        {
            return 0;
        }

        offset--;
        while (offset > 0 && (text[offset] & 0xC0) == 0x80)
        {
            offset--;
        }

        return offset;
    }

    public static int NextCodepoint(ReadOnlySpan<byte> text, int offset)
    {
        if (offset >= text.Length)
        {
            return text.Length;
        }

        offset++;
        while (offset < text.Length && (text[offset] & 0xC0) == 0x80)
        {
            offset++;
        }

        return offset;
    }

    public static int CodepointCount(ReadOnlySpan<byte> text)
    {
        var count = 0;
        foreach (var b in text)
        {
            if ((b & 0xC0) != 0x80)
            {
                count++;
            }
        }

        return count;
    }

    private int PrevWord(int offset)
    {
        while (offset > 0 && !IsWordByte(_bytes[offset - 1]))
        {
            offset--;
        }

        while (offset > 0 && IsWordByte(_bytes[offset - 1]))
        {
            offset--;
        }

        return offset;
    }

    private int NextWord(int offset)
    {
        while (offset < _length && !IsWordByte(_bytes[offset]))
        {
            offset++;
        }

        while (offset < _length && IsWordByte(_bytes[offset]))
        {
            offset++;
        }

        return offset;
    }

    private int LineStart(uint textId)
    {
        return Multiline && FindLine(textId, out var line) ? (int)line.StartOffset : 0;
    }

    private int LineEnd(uint textId)
    {
        return Multiline && FindLine(textId, out var line) ? (int)line.EndOffset : _length;
    }

    private bool FindLine(uint textId, out UITextLine found)
    {
        var ui = Ui.Native;
        var count = ui.GetTextLineCount(textId);
        for (var i = 0u; i < count; i++)
        {
            if (!ui.TryGetTextLine(textId, i, out var line) || Caret > line.EndOffset)
            {
                continue;
            }

            if (Caret == line.EndOffset && i + 1 < count && ui.TryGetTextLine(textId, i + 1, out var next) && next.StartOffset == Caret)
            {
                continue;
            }

            found = line;
            return true;
        }

        found = default;
        return false;
    }

    private void MoveVertical(uint textId, int direction, int lines, bool extend)
    {
        var ui = Ui.Native;
        if (!ui.TryGetTextCaretRect(textId, (uint)Caret, out var caret))
        {
            return;
        }

        if (float.IsNaN(_preferredX))
        {
            _preferredX = caret.X;
        }

        var y = direction < 0 ? caret.Y - caret.Height * (lines - 0.5f) : caret.Y + caret.Height * (lines + 0.5f);
        var offset = (int)ui.GetTextOffsetAtPoint(textId, new Vector2(_preferredX, y));
        if (offset == Caret)
        {
            offset = direction < 0 ? 0 : _length;
        }

        Move(offset, extend, true);
    }

    private void Reserve(int capacity)
    {
        if (capacity <= _bytes.Length)
        {
            return;
        }

        Array.Resize(ref _bytes, Math.Max(capacity, _bytes.Length * 2));
    }

    private static bool IsWordByte(byte b)
    {
        return b >= 0x80 || b == '_' || (b >= '0' && b <= '9') || (b >= 'a' && b <= 'z') || (b >= 'A' && b <= 'Z');
    }
}
