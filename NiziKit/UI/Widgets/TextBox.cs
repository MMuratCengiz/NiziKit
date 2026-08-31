using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using DenOfIz;

namespace NiziKit.UI.Widgets;

internal sealed class UiTextBuffer
{
    private byte[] _bytes = GC.AllocateUninitializedArray<byte>(64, pinned: true);
    private int[] _offsets = new int[16];
    private int _byteCount;

    public string Text { get; private set; } = "";
    public int Length => Text.Length;
    public int ByteCount => _byteCount;
    public StringView View => Slice(0, Text.Length);

    public bool Set(string text)
    {
        if (ReferenceEquals(text, Text) || string.Equals(text, Text, StringComparison.Ordinal))
        {
            Text = text;
            return false;
        }

        Text = text;
        var maxBytes = text.Length * 3 + 1;
        if (_bytes.Length < maxBytes)
        {
            _bytes = GC.AllocateUninitializedArray<byte>(Math.Max(maxBytes, _bytes.Length * 2), pinned: true);
        }

        if (_offsets.Length < text.Length + 1)
        {
            _offsets = new int[Math.Max(text.Length + 1, _offsets.Length * 2)];
        }

        var offset = 0;
        for (var i = 0; i < text.Length; i++)
        {
            _offsets[i] = offset;
            var c = text[i];
            if (c < 0x80)
            {
                _bytes[offset++] = (byte)c;
                continue;
            }

            var count = char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]) ? 2 : 1;
            offset += Encoding.UTF8.GetBytes(text.AsSpan(i, count), _bytes.AsSpan(offset));
            if (count == 2)
            {
                i++;
                _offsets[i] = offset;
            }
        }

        _offsets[text.Length] = offset;
        _byteCount = offset;
        return true;
    }

    public StringView Slice(int start, int end)
    {
        if (end <= start)
        {
            return default;
        }

        var first = _offsets[start];
        return new StringView
        {
            Chars = Marshal.UnsafeAddrOfPinnedArrayElement(_bytes, first),
            NumChars = (ulong)(_offsets[end] - first)
        };
    }

    public int ByteOffset(int charIndex)
    {
        return _offsets[Math.Clamp(charIndex, 0, Text.Length)];
    }

    public int CharIndexAt(int byteOffset)
    {
        var low = 0;
        var high = Text.Length;
        while (low < high)
        {
            var mid = (low + high) >> 1;
            if (_offsets[mid] < byteOffset)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }

        return low;
    }
}

internal static class TextEditing
{
    public const uint CtrlMask = (uint)(KeyMod.Lctrl | KeyMod.Rctrl | KeyMod.Lgui | KeyMod.Rgui);
    public const uint ShiftMask = (uint)(KeyMod.Lshift | KeyMod.Rshift);

    public static int CharClass(char c)
    {
        if (char.IsWhiteSpace(c))
        {
            return 0;
        }

        return char.IsLetterOrDigit(c) || c == '_' ? 1 : 2;
    }

    public static int PreviousWord(StringBuilder buffer, int index)
    {
        var i = Math.Clamp(index, 0, buffer.Length);
        while (i > 0 && char.IsWhiteSpace(buffer[i - 1]))
        {
            i--;
        }

        if (i > 0)
        {
            var cls = CharClass(buffer[i - 1]);
            while (i > 0 && CharClass(buffer[i - 1]) == cls)
            {
                i--;
            }
        }

        return i;
    }

    public static int NextWord(StringBuilder buffer, int index)
    {
        var i = Math.Clamp(index, 0, buffer.Length);
        if (i < buffer.Length)
        {
            var cls = CharClass(buffer[i]);
            while (i < buffer.Length && CharClass(buffer[i]) == cls)
            {
                i++;
            }
        }

        while (i < buffer.Length && char.IsWhiteSpace(buffer[i]) && buffer[i] != '\n')
        {
            i++;
        }

        return i;
    }

    public static void WordAt(StringBuilder buffer, int index, out int start, out int end)
    {
        if (buffer.Length == 0)
        {
            start = 0;
            end = 0;
            return;
        }

        var i = Math.Clamp(index, 0, buffer.Length - 1);
        var cls = CharClass(buffer[i]);
        start = i;
        while (start > 0 && CharClass(buffer[start - 1]) == cls)
        {
            start--;
        }

        end = i + 1;
        while (end < buffer.Length && CharClass(buffer[end]) == cls)
        {
            end++;
        }
    }

    public static void CopyToClipboard(string text)
    {
        if (text.Length == 0)
        {
            return;
        }

        using var pinned = new StringView.Pinned(text);
        Clipboard.SetText(pinned);
    }

    public static string? ReadClipboard()
    {
        return Clipboard.HasText() ? Clipboard.GetText().ToString() : null;
    }
}

public class TextBox : Widget
{
    private readonly StringBuilder _buffer = new();
    private readonly UiTextBuffer _display = new();
    private readonly UiTextCache _placeholder = new();
    private string? _text;
    private string? _displayText;
    private bool _displayPassword;
    private char _displayPasswordChar;
    private int _cursor;
    private int _anchor = -1;
    private bool _selecting;
    private float _lastEditTime;
    private float _scroll;
    private float _textWidth;
    private float _cursorX;
    private bool _metricsDirty = true;
    private ushort _metricsFontId;
    private ushort _metricsFontSize;

    public TextBox()
    {
        Focusable = true;
        Width = 200;
        Height = 32;
        Padding = new UiThickness(10, 0);
        CornerRadius = 6;
        Background = UiColor.Rgb(28, 30, 38);
        BorderColor = UiColor.Rgb(70, 76, 94);
        ClipChildren = true;
    }

    public TextBox(string text) : this()
    {
        Text = text;
    }

    public string Text
    {
        get => _text ??= _buffer.ToString();
        init
        {
            _buffer.Clear();
            _buffer.Append(value);
            _text = value;
            _cursor = Math.Clamp(_cursor, 0, _buffer.Length);
            _anchor = -1;
            Touch();
        }
    }

    public string Placeholder { get; init; } = "";
    public int MaxLength { get; set; } = int.MaxValue;
    public int FontSize { get; set; } = 14;
    public ushort FontId { get; set; } = UiFonts.DefaultFontId;
    public UiColor TextColor { get; set; } = UiColor.Rgb(235, 235, 240);
    public UiColor DisabledTextColor { get; set; } = UiColor.Rgb(160, 165, 180);
    public UiColor PlaceholderColor { get; set; } = UiColor.Rgb(120, 125, 140);
    public UiColor FocusBorderColor { get; set; } = UiColor.Rgb(88, 130, 240);
    public UiColor SelectionColor { get; set; } = UiColor.Rgba(88, 130, 240, 90);
    public bool IsPassword { get; init; }
    public char PasswordChar { get; set; } = '•';
    public bool ReadOnly { get; init; }

    public int Cursor
    {
        get => _cursor;
        set
        {
            _cursor = Math.Clamp(value, 0, _buffer.Length);
            _anchor = -1;
            Touch();
        }
    }

    public bool HasSelection => _anchor >= 0 && _anchor != _cursor;
    public int SelectionStart => HasSelection ? Math.Min(_anchor, _cursor) : _cursor;
    public int SelectionEnd => HasSelection ? Math.Max(_anchor, _cursor) : _cursor;
    public int SelectionLength => SelectionEnd - SelectionStart;
    public string SelectedText => HasSelection ? Text.Substring(SelectionStart, SelectionLength) : "";
    public int Length => _buffer.Length;

    public event Action<TextBox>? TextChanged;
    public event Action<TextBox>? Submitted;

    private ushort ResolvedFontSize => (ushort)FontSize;
    private ushort ResolvedFontId => FontId;

    public void Clear()
    {
        if (_buffer.Length == 0)
        {
            return;
        }

        _buffer.Clear();
        _cursor = 0;
        _anchor = -1;
        NotifyChanged();
    }

    public void SelectAll()
    {
        if (_buffer.Length == 0)
        {
            return;
        }

        _anchor = 0;
        _cursor = _buffer.Length;
        Touch();
    }

    public void Select(int start, int length)
    {
        start = Math.Clamp(start, 0, _buffer.Length);
        var end = Math.Clamp(start + length, start, _buffer.Length);
        _anchor = end > start ? start : -1;
        _cursor = end;
        Touch();
    }

    public void ClearSelection()
    {
        _anchor = -1;
        Touch();
    }

    private void Touch()
    {
        _metricsDirty = true;
        _lastEditTime = Ui.ElapsedSeconds;
    }

    private void NotifyChanged()
    {
        _text = null;
        Touch();
        TextChanged?.Invoke(this);
    }

    private bool DeleteSelection()
    {
        if (!HasSelection)
        {
            return false;
        }

        var start = SelectionStart;
        _buffer.Remove(start, SelectionLength);
        _cursor = start;
        _anchor = -1;
        NotifyChanged();
        return true;
    }

    private void Remove(int start, int count)
    {
        if (count <= 0)
        {
            return;
        }

        _buffer.Remove(start, count);
        _cursor = start;
        _anchor = -1;
        NotifyChanged();
    }

    private void Insert(string text)
    {
        if (ReadOnly)
        {
            return;
        }

        var hadSelection = HasSelection;
        if (hadSelection)
        {
            var start = SelectionStart;
            _buffer.Remove(start, SelectionLength);
            _cursor = start;
            _anchor = -1;
        }

        var room = MaxLength - _buffer.Length;
        if (room > 0)
        {
            if (text.Length > room)
            {
                text = text[..room];
            }

            _buffer.Insert(_cursor, text);
            _cursor += text.Length;
        }
        else if (!hadSelection)
        {
            return;
        }

        NotifyChanged();
    }

    private void MoveCursor(int index, bool extend)
    {
        if (extend)
        {
            if (_anchor < 0)
            {
                _anchor = _cursor;
            }
        }
        else
        {
            _anchor = -1;
        }

        _cursor = Math.Clamp(index, 0, _buffer.Length);
        Touch();
    }

    private void SelectWordAt(int index)
    {
        TextEditing.WordAt(_buffer, index, out var start, out var end);
        _anchor = end > start ? start : -1;
        _cursor = end;
        Touch();
    }

    private void Copy()
    {
        if (IsPassword)
        {
            return;
        }

        TextEditing.CopyToClipboard(HasSelection ? SelectedText : Text);
    }

    private static string SingleLine(string text)
    {
        return text.IndexOf('\n') < 0 && text.IndexOf('\r') < 0 ? text : text.Replace("\r", "").Replace('\n', ' ');
    }

    private void EnsureDisplay()
    {
        var text = Text;
        if (ReferenceEquals(text, _displayText) && _displayPassword == IsPassword && _displayPasswordChar == PasswordChar)
        {
            return;
        }

        _displayText = text;
        _displayPassword = IsPassword;
        _displayPasswordChar = PasswordChar;
        if (_display.Set(IsPassword && text.Length > 0 ? new string(PasswordChar, text.Length) : text))
        {
            _metricsDirty = true;
        }
    }

    private float OffsetAt(int index, ushort fontId, ushort fontSize)
    {
        if (index <= 0 || _display.Length == 0)
        {
            return 0;
        }

        return Ui.Clay.GetCursorOffsetAtIndex(_display.View, (uint)_display.ByteOffset(index), fontId, fontSize);
    }

    private int IndexAtPointer()
    {
        EnsureDisplay();
        if (_display.Length == 0)
        {
            return 0;
        }

        var bounds = Bounds;
        var localX = Ui.PointerPosition.X - bounds.X - Ui.Clay.PointsToPixels(Padding.Left) + _scroll;
        var byteIndex = (int)Ui.Clay.GetCharIndexAtOffset(_display.View, localX, ResolvedFontId, ResolvedFontSize);
        return Math.Clamp(_display.CharIndexAt(byteIndex), 0, _buffer.Length);
    }

    private void UpdateMetrics()
    {
        EnsureDisplay();
        var fontId = ResolvedFontId;
        var fontSize = ResolvedFontSize;
        if (_metricsDirty || fontId != _metricsFontId || fontSize != _metricsFontSize)
        {
            _metricsDirty = false;
            _metricsFontId = fontId;
            _metricsFontSize = fontSize;
            _textWidth = _display.Length == 0 ? 0 : Ui.Clay.MeasureText(_display.View, fontId, fontSize).Width;
            _cursorX = OffsetAt(_cursor, fontId, fontSize);
        }

        var visible = Bounds.Width - Ui.Clay.PointsToPixels(Padding.Horizontal);
        if (visible <= 0)
        {
            _scroll = 0;
            return;
        }

        const float margin = 2;
        if (_cursorX - _scroll > visible - margin)
        {
            _scroll = _cursorX - visible + margin;
        }

        if (_cursorX - _scroll < 0)
        {
            _scroll = _cursorX;
        }

        _scroll = Math.Clamp(_scroll, 0, MathF.Max(0, _textWidth + margin - visible));
    }

    protected override void OnPoll()
    {
        if (Ui.PointerPressedThisFrame && IsHovered && IsEnabled)
        {
            var index = IndexAtPointer();
            if (Ui.PressClicks >= 2)
            {
                SelectWordAt(index);
                _selecting = false;
            }
            else
            {
                _cursor = index;
                _anchor = index;
                _selecting = true;
                Touch();
            }

            return;
        }

        if (!_selecting)
        {
            return;
        }

        if (!Ui.IsButtonDown(UiMouseButton.Left))
        {
            _selecting = false;
            if (_anchor == _cursor)
            {
                _anchor = -1;
            }

            return;
        }

        var dragIndex = IndexAtPointer();
        if (dragIndex != _cursor)
        {
            _cursor = dragIndex;
            Touch();
        }
    }

    protected internal override void OnFocusChanged(bool focused)
    {
        base.OnFocusChanged(focused);
        if (focused)
        {
            InputSystem.StartTextInput();
            _lastEditTime = Ui.ElapsedSeconds;
        }
        else
        {
            InputSystem.StopTextInput();
            _selecting = false;
        }
    }

    protected internal override void OnTextInput(string text)
    {
        if (IsEnabled && !ReadOnly)
        {
            Insert(SingleLine(text));
        }
    }

    protected internal override bool OnKeyDown(in KeyboardEventData key)
    {
        if (!IsEnabled)
        {
            return false;
        }

        var ctrl = (key.Mod & TextEditing.CtrlMask) != 0;
        var shift = (key.Mod & TextEditing.ShiftMask) != 0;
        switch (key.KeyCode)
        {
            case KeyCode.Backspace:
                if (ReadOnly || DeleteSelection())
                {
                    break;
                }

                if (_cursor > 0)
                {
                    var start = ctrl ? TextEditing.PreviousWord(_buffer, _cursor) : _cursor - 1;
                    Remove(start, _cursor - start);
                }

                break;
            case KeyCode.Delete:
                if (ReadOnly || DeleteSelection())
                {
                    break;
                }

                if (_cursor < _buffer.Length)
                {
                    var end = ctrl ? TextEditing.NextWord(_buffer, _cursor) : _cursor + 1;
                    Remove(_cursor, end - _cursor);
                }

                break;
            case KeyCode.Left:
                if (!shift && !ctrl && HasSelection)
                {
                    MoveCursor(SelectionStart, false);
                }
                else
                {
                    MoveCursor(ctrl ? TextEditing.PreviousWord(_buffer, _cursor) : _cursor - 1, shift);
                }

                break;
            case KeyCode.Right:
                if (!shift && !ctrl && HasSelection)
                {
                    MoveCursor(SelectionEnd, false);
                }
                else
                {
                    MoveCursor(ctrl ? TextEditing.NextWord(_buffer, _cursor) : _cursor + 1, shift);
                }

                break;
            case KeyCode.Home:
                MoveCursor(0, shift);
                break;
            case KeyCode.End:
                MoveCursor(_buffer.Length, shift);
                break;
            case KeyCode.Return:
                Submitted?.Invoke(this);
                break;
            case KeyCode.Escape:
                Ui.SetFocus(null);
                break;
            case KeyCode.A when ctrl:
                SelectAll();
                break;
            case KeyCode.C when ctrl:
                Copy();
                break;
            case KeyCode.X when ctrl:
                Copy();
                if (!ReadOnly && !IsPassword)
                {
                    if (!DeleteSelection())
                    {
                        Clear();
                    }
                }

                break;
            case KeyCode.V when ctrl:
                if (!ReadOnly && TextEditing.ReadClipboard() is { } pasted)
                {
                    Insert(SingleLine(pasted));
                }

                break;
            default:
                return false;
        }

        return true;
    }

    protected override void ApplyDeclaration(ref ClayElementDeclaration decl)
    {
        base.ApplyDeclaration(ref decl);
        UpdateMetrics();
        decl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
        UiColor? border = IsFocused ? FocusBorderColor : BorderColor;
        if (border is { } borderColor)
        {
            decl.Border.Color = borderColor.ToClay();
            decl.Border.Width = ClayBorderWidth.CreateUniform(BorderWidth);
        }

        decl.Clip = ClayClipDesc.Create(true, false, new Vector2(-Ui.Clay.PixelsToPoints(_scroll), 0));
    }

    protected override void BuildContent()
    {
        var fontSize = ResolvedFontSize;
        var fontId = ResolvedFontId;

        var desc = ClayTextDesc.Default();
        desc.FontId = fontId;
        desc.FontSize = fontSize;
        desc.WrapMode = ClayTextWrapMode.None;

        var length = _display.Length;
        if (length > 0)
        {
            desc.TextColor = (IsEnabled ? TextColor : DisabledTextColor).ToClay();
            if (HasSelection)
            {
                var start = SelectionStart;
                var end = SelectionEnd;
                if (start > 0)
                {
                    Ui.Clay.Text(_display.Slice(0, start), in desc);
                }

                var highlight = ClayElementDeclaration.Default();
                highlight.BackgroundColor = SelectionColor.ToClay();
                highlight.BorderRadius = ClayBorderRadius.CreateUniform(2);
                highlight.Layout.Sizing.Width = ClaySizingAxis.Fit(0, float.MaxValue);
                highlight.Layout.Sizing.Height = ClaySizingAxis.Fit(0, float.MaxValue);
                highlight.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
                Ui.Clay.OpenElement(in highlight);
                Ui.Clay.Text(_display.Slice(start, end), in desc);
                Ui.Clay.CloseElement();

                if (end < length)
                {
                    Ui.Clay.Text(_display.Slice(end, length), in desc);
                }
            }
            else
            {
                Ui.Clay.Text(_display.View, in desc);
            }
        }
        else if (Placeholder.Length > 0)
        {
            desc.TextColor = PlaceholderColor.ToClay();
            _placeholder.Draw(Placeholder, in desc);
        }

        if (!IsFocused || (Ui.ElapsedSeconds - _lastEditTime) % 1.0f > 0.5f)
        {
            return;
        }

        var cursor = ClayElementDeclaration.Default();
        cursor.Layout.Sizing.Width = ClaySizingAxis.Fixed(1.5f);
        cursor.Layout.Sizing.Height = ClaySizingAxis.Fixed(fontSize + 4);
        cursor.BackgroundColor = TextColor.ToClay();
        cursor.Floating.AttachTo = ClayFloatingAttachTo.Parent;
        cursor.Floating.ZIndex = Ui.FloatingZIndex;
        cursor.Floating.ElementAttachPoint = ClayFloatingAttachPoint.LeftCenter;
        cursor.Floating.ParentAttachPoint = ClayFloatingAttachPoint.LeftCenter;
        cursor.Floating.Offset = new Vector2(Padding.Left + Ui.Clay.PixelsToPoints(_cursorX - _scroll), 0);
        cursor.Floating.PointerCaptureMode = ClayPointerCaptureMode.Passthrough;
        Ui.Clay.OpenElement(in cursor);
        Ui.Clay.CloseElement();
    }
}
