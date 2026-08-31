using System.Numerics;
using System.Text;
using DenOfIz;

namespace NiziKit.UI.Widgets;

public class TextArea : Widget
{
    private struct Line
    {
        public int Start;
        public int End;
    }

    private const string LineHeightSample = "Ag";

    private readonly StringBuilder _buffer = new();
    private readonly TextBuffer _display = new();
    private readonly TextCache _placeholder = new();
    private readonly TextCache _sample = new();
    private readonly List<Line> _lines = new();
    private string? _text;
    private int _cursor;
    private int _anchor = -1;
    private bool _selecting;
    private float _lastEditTime;
    private float _scroll;
    private float _lineHeight;
    private float _wrapWidth;
    private bool _layoutDirty = true;
    private ushort _layoutFontId;
    private ushort _layoutFontSize;
    private bool _metricsDirty = true;
    private bool _scrollToCursor = true;
    private int _cursorLine;
    private float _cursorX;
    private float _preferredX = -1;
    private bool _preferLineEnd;

    public TextArea()
    {
        Focusable = true;
        Width = 300;
        Height = 120;
        Padding = new Thickness(10, 8);
        CornerRadius = 6;
        Background = Color.Rgb(28, 30, 38);
        BorderColor = Color.Rgb(70, 76, 94);
        Style = new Style
        {
            Normal = new StyleState { Text = Color.Rgb(235, 235, 240) },
            Focused = new StyleState { Border = Color.Rgb(88, 130, 240), BorderWidth = 1 },
            Disabled = new StyleState { Background = Color.Rgb(46, 50, 62), Text = Color.Rgb(160, 165, 180) }
        };
        ClipChildren = true;
    }

    public TextArea(string text) : this()
    {
        Text = text;
    }

    public string Text
    {
        get => _text ??= _buffer.ToString();
        set
        {
            _buffer.Clear();
            _buffer.Append(value);
            _text = value;
            _cursor = Math.Clamp(_cursor, 0, _buffer.Length);
            _anchor = -1;
            _layoutDirty = true;
            Touch();
        }
    }

    public string Placeholder { get; set; } = "";
    public bool ReadOnly { get; set; }
    public int MaxLength { get; set; } = int.MaxValue;
    public int FontSize { get; set; } = 14;
    public ushort FontId { get; set; } = Fonts.DefaultFontId;
    public Color PlaceholderColor { get; set; } = Color.Rgb(120, 125, 140);
    public Color CaretColor { get; set; } = Color.Rgb(235, 235, 240);
    public Color SelectionColor { get; set; } = Color.Rgba(88, 130, 240, 90);
    public float ScrollStep { get; set; } = 3;

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
    public int LineCount => _lines.Count;
    public int CursorLine => _cursorLine;
    public int CursorColumn => _lines.Count == 0 ? 0 : _cursor - _lines[Math.Clamp(_cursorLine, 0, _lines.Count - 1)].Start;
    public float ScrollOffset => _scroll;

    public event Action<TextArea>? TextChanged;
    public event Action<TextArea>? Submitted;

    private Color _resolvedText = Color.Rgb(235, 235, 240);

    protected override void OnStyleResolved(in StyleState state)
    {
        if (state.Text is { } text)
        {
            _resolvedText = text;
        }
    }

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

    public void ScrollToCursor()
    {
        _scrollToCursor = true;
    }

    private void Touch(bool keepPreferredX = false)
    {
        _metricsDirty = true;
        _scrollToCursor = true;
        _preferLineEnd = false;
        _lastEditTime = Ui.ElapsedSeconds;
        if (!keepPreferredX)
        {
            _preferredX = -1;
        }
    }

    private void NotifyChanged()
    {
        _text = null;
        _layoutDirty = true;
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

    private void MoveCursor(int index, bool extend, bool keepPreferredX = false)
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
        Touch(keepPreferredX);
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
        TextEditing.CopyToClipboard(HasSelection ? SelectedText : Text);
    }

    private static string Normalize(string text)
    {
        return text.IndexOf('\r') < 0 ? text : text.Replace("\r\n", "\n").Replace('\r', '\n');
    }

    private void EnsureDisplay()
    {
        if (_display.Set(Text))
        {
            _layoutDirty = true;
        }
    }

    private float AvailableWidth()
    {
        if (Width.IsFixed)
        {
            return Ui.Clay.PointsToPixels(Width.Value - Padding.Horizontal);
        }

        var width = Bounds.Width;
        return width <= 0 ? 0 : width - Ui.Clay.PointsToPixels(Padding.Horizontal);
    }

    private float ViewHeight()
    {
        if (Height.IsFixed)
        {
            return Ui.Clay.PointsToPixels(Height.Value - Padding.Vertical);
        }

        var height = Bounds.Height;
        return height <= 0 ? 0 : height - Ui.Clay.PointsToPixels(Padding.Vertical);
    }

    private void EnsureLayout()
    {
        EnsureDisplay();
        var fontId = ResolvedFontId;
        var fontSize = ResolvedFontSize;
        var wrapWidth = MathF.Max(0, AvailableWidth());
        if (!_layoutDirty && fontId == _layoutFontId && fontSize == _layoutFontSize && MathF.Abs(wrapWidth - _wrapWidth) < 0.5f)
        {
            return;
        }

        _layoutDirty = false;
        _metricsDirty = true;
        _layoutFontId = fontId;
        _layoutFontSize = fontSize;
        _wrapWidth = wrapWidth;

        var sample = _sample.Measure(LineHeightSample, fontId, fontSize);
        _lineHeight = Ui.Clay.PointsToPixels(sample.Y > 0 ? sample.Y : fontSize * 1.2f);

        _lines.Clear();
        var text = _display.Text;
        var index = 0;
        while (true)
        {
            var paragraphEnd = text.IndexOf('\n', index);
            if (paragraphEnd < 0)
            {
                paragraphEnd = text.Length;
            }

            WrapParagraph(text, index, paragraphEnd, fontId, fontSize);
            if (paragraphEnd >= text.Length)
            {
                break;
            }

            index = paragraphEnd + 1;
        }
    }

    private void WrapParagraph(string text, int start, int end, ushort fontId, ushort fontSize)
    {
        if (_wrapWidth <= 0 || start == end)
        {
            _lines.Add(new Line { Start = start, End = end });
            return;
        }

        var lineStart = start;
        var lineWidth = 0f;
        var position = start;
        while (position < end)
        {
            var tokenStart = position;
            var isSpace = text[position] == ' ';
            while (position < end && (text[position] == ' ') == isSpace)
            {
                position++;
            }

            var width = Ui.Clay.MeasureText(_display.Slice(tokenStart, position), fontId, fontSize).Width;
            if (isSpace)
            {
                lineWidth += width;
                continue;
            }

            if (lineWidth + width > _wrapWidth && lineStart < tokenStart)
            {
                _lines.Add(new Line { Start = lineStart, End = tokenStart });
                lineStart = tokenStart;
                lineWidth = 0;
            }

            while (width > _wrapWidth && lineStart == tokenStart && position - tokenStart >= 2)
            {
                var relative = (int)Ui.Clay.GetCharIndexAtOffset(_display.Slice(tokenStart, position), _wrapWidth, fontId, fontSize);
                var fit = Math.Clamp(_display.CharIndexAt(_display.ByteOffset(tokenStart) + relative), tokenStart + 1, position - 1);

                _lines.Add(new Line { Start = lineStart, End = fit });
                lineStart = fit;
                tokenStart = fit;
                width = Ui.Clay.MeasureText(_display.Slice(tokenStart, position), fontId, fontSize).Width;
            }

            lineWidth += width;
        }

        _lines.Add(new Line { Start = lineStart, End = end });
    }

    private int LineOf(int index)
    {
        var low = 0;
        var high = _lines.Count - 1;
        while (low < high)
        {
            var mid = (low + high + 1) >> 1;
            if (_lines[mid].Start <= index)
            {
                low = mid;
            }
            else
            {
                high = mid - 1;
            }
        }

        if (_preferLineEnd && low > 0 && _lines[low].Start == index && _lines[low - 1].End == index)
        {
            low--;
        }

        return low;
    }

    private bool IsSoftLineEnd(int line, int index)
    {
        return line + 1 < _lines.Count && _lines[line].End == index && _lines[line + 1].Start == index;
    }

    private float OffsetInLine(int line, int index)
    {
        var info = _lines[line];
        if (index <= info.Start || info.End <= info.Start)
        {
            return 0;
        }

        var relative = _display.ByteOffset(Math.Min(index, info.End)) - _display.ByteOffset(info.Start);
        return Ui.Clay.GetCursorOffsetAtIndex(_display.Slice(info.Start, info.End), (uint)relative, _layoutFontId, _layoutFontSize);
    }

    private int IndexAtLineX(int line, float x, out bool atLineEnd)
    {
        var info = _lines[line];
        if (info.End <= info.Start || x <= 0)
        {
            atLineEnd = IsSoftLineEnd(line, info.Start);
            return info.Start;
        }

        var relative = (int)Ui.Clay.GetCharIndexAtOffset(_display.Slice(info.Start, info.End), x, _layoutFontId, _layoutFontSize);
        var index = Math.Clamp(_display.CharIndexAt(_display.ByteOffset(info.Start) + relative), info.Start, info.End);
        atLineEnd = IsSoftLineEnd(line, index);
        return index;
    }

    private int IndexAtPointer(out bool atLineEnd)
    {
        EnsureLayout();
        if (_lines.Count == 0)
        {
            atLineEnd = false;
            return 0;
        }

        var bounds = Bounds;
        var localY = Ui.PointerPosition.Y - bounds.Y - Ui.Clay.PointsToPixels(Padding.Top) + _scroll;
        var line = _lineHeight <= 0 ? 0 : Math.Clamp((int)MathF.Floor(localY / _lineHeight), 0, _lines.Count - 1);
        var localX = Ui.PointerPosition.X - bounds.X - Ui.Clay.PointsToPixels(Padding.Left);
        return IndexAtLineX(line, localX, out atLineEnd);
    }

    private void UpdateMetrics()
    {
        EnsureLayout();
        if (_metricsDirty)
        {
            _metricsDirty = false;
            _cursorLine = _lines.Count == 0 ? 0 : LineOf(_cursor);
            _cursorX = _lines.Count == 0 ? 0 : OffsetInLine(_cursorLine, _cursor);
        }

        var view = ViewHeight();
        if (view <= 0)
        {
            return;
        }

        if (_scrollToCursor)
        {
            _scrollToCursor = false;
            var top = _cursorLine * _lineHeight;
            if (top < _scroll)
            {
                _scroll = top;
            }
            else if (top + _lineHeight > _scroll + view)
            {
                _scroll = top + _lineHeight - view;
            }
        }

        _scroll = Math.Clamp(_scroll, 0, MathF.Max(0, _lines.Count * _lineHeight - view));
    }

    private void MoveVertical(int lines, bool extend)
    {
        EnsureLayout();
        if (_lines.Count == 0)
        {
            return;
        }

        if (_metricsDirty)
        {
            _cursorLine = LineOf(_cursor);
            _cursorX = OffsetInLine(_cursorLine, _cursor);
            _metricsDirty = false;
        }

        if (_preferredX < 0)
        {
            _preferredX = _cursorX;
        }

        var target = Math.Clamp(_cursorLine + lines, 0, _lines.Count - 1);
        if (target == _cursorLine)
        {
            MoveCursor(lines < 0 ? 0 : _buffer.Length, extend, true);
            return;
        }

        var index = IndexAtLineX(target, _preferredX, out var atLineEnd);
        MoveCursor(index, extend, true);
        _preferLineEnd = atLineEnd;
    }

    protected override void OnPoll()
    {
        if (IsHovered && Ui.WheelDelta != 0 && _lineHeight > 0)
        {
            _scroll -= Ui.WheelDelta * _lineHeight * ScrollStep;
            _scrollToCursor = false;
        }

        if (Ui.PointerPressedThisFrame && IsHovered && IsEnabled)
        {
            var index = IndexAtPointer(out var atLineEnd);
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
                _preferLineEnd = atLineEnd;
            }

            return;
        }

        if (!_selecting)
        {
            return;
        }

        if (!Ui.IsButtonDown(MouseButton.Left))
        {
            _selecting = false;
            if (_anchor == _cursor)
            {
                _anchor = -1;
            }

            return;
        }

        var dragIndex = IndexAtPointer(out var dragAtLineEnd);
        if (dragIndex != _cursor)
        {
            _cursor = dragIndex;
            Touch();
            _preferLineEnd = dragAtLineEnd;
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
            Insert(Normalize(text));
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
            case KeyCode.Up:
                MoveVertical(-1, shift);
                break;
            case KeyCode.Down:
                MoveVertical(1, shift);
                break;
            case KeyCode.Home:
                EnsureLayout();
                MoveCursor(ctrl || _lines.Count == 0 ? 0 : _lines[LineOf(_cursor)].Start, shift);
                break;
            case KeyCode.End:
                EnsureLayout();
                if (ctrl || _lines.Count == 0)
                {
                    MoveCursor(_buffer.Length, shift);
                }
                else
                {
                    var line = LineOf(_cursor);
                    MoveCursor(_lines[line].End, shift);
                    _preferLineEnd = IsSoftLineEnd(line, _cursor);
                }

                break;
            case KeyCode.Return:
                if (ctrl)
                {
                    Submitted?.Invoke(this);
                }
                else if (!ReadOnly)
                {
                    Insert("\n");
                }

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
                if (!ReadOnly && !DeleteSelection())
                {
                    Clear();
                }

                break;
            case KeyCode.V when ctrl:
                if (!ReadOnly && TextEditing.ReadClipboard() is { } pasted)
                {
                    Insert(Normalize(pasted));
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
        decl.Layout.LayoutDirection = ClayLayoutDirection.TopToBottom;
        decl.Clip = ClayClipDesc.Create(true, true, new Vector2(0, -Ui.Clay.PixelsToPoints(_scroll)));
    }

    protected override void BuildContent()
    {
        var fontSize = ResolvedFontSize;
        var fontId = ResolvedFontId;
        var lineHeightPoints = Ui.Clay.PixelsToPoints(_lineHeight);

        var desc = ClayTextDesc.Default();
        desc.FontId = fontId;
        desc.FontSize = fontSize;
        desc.WrapMode = ClayTextWrapMode.None;

        if (_display.Length == 0)
        {
            if (Placeholder.Length > 0)
            {
                desc.TextColor = PlaceholderColor.ToClay();
                desc.WrapMode = ClayTextWrapMode.Words;
                _placeholder.Draw(Placeholder, in desc);
            }

            BuildCursor(lineHeightPoints);
            return;
        }

        desc.TextColor = _resolvedText.ToClay();

        var view = ViewHeight();
        var first = 0;
        var last = _lines.Count - 1;
        if (_lineHeight > 0 && view > 0)
        {
            first = Math.Clamp((int)(_scroll / _lineHeight), 0, last);
            last = Math.Clamp((int)((_scroll + view) / _lineHeight), first, last);
        }
        else
        {
            last = Math.Min(last, first + 64);
        }

        if (first > 0)
        {
            var spacer = ClayElementDeclaration.Default();
            spacer.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
            spacer.Layout.Sizing.Height = ClaySizingAxis.Fixed(first * lineHeightPoints);
            Ui.Clay.OpenElement(in spacer);
            Ui.Clay.CloseElement();
        }

        var hasSelection = HasSelection;
        var selectionStart = SelectionStart;
        var selectionEnd = SelectionEnd;
        var highlightColor = SelectionColor.ToClay();

        for (var i = first; i <= last; i++)
        {
            var line = _lines[i];
            var row = ClayElementDeclaration.Default();
            row.Layout.LayoutDirection = ClayLayoutDirection.LeftToRight;
            row.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
            row.Layout.Sizing.Height = ClaySizingAxis.Fixed(lineHeightPoints);
            row.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
            Ui.Clay.OpenElement(in row);

            var start = Math.Max(line.Start, selectionStart);
            var end = Math.Min(line.End, selectionEnd);
            var continues = hasSelection && selectionStart <= line.End && selectionEnd > line.End;
            if (hasSelection && (start < end || continues))
            {
                if (start > line.Start)
                {
                    Ui.Clay.Text(_display.Slice(line.Start, start), in desc);
                }

                var highlight = ClayElementDeclaration.Default();
                highlight.BackgroundColor = highlightColor;
                highlight.BorderRadius = ClayBorderRadius.CreateUniform(2);
                highlight.Layout.Sizing.Width = ClaySizingAxis.Fit(0, float.MaxValue);
                highlight.Layout.Sizing.Height = ClaySizingAxis.Fixed(lineHeightPoints);
                highlight.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
                if (continues)
                {
                    highlight.Layout.Padding = ClayPadding.Create(0, 4, 0, 0);
                }

                Ui.Clay.OpenElement(in highlight);
                if (start < end)
                {
                    Ui.Clay.Text(_display.Slice(start, end), in desc);
                }

                Ui.Clay.CloseElement();

                if (end < line.End)
                {
                    Ui.Clay.Text(_display.Slice(end, line.End), in desc);
                }
            }
            else if (line.End > line.Start)
            {
                Ui.Clay.Text(_display.Slice(line.Start, line.End), in desc);
            }

            Ui.Clay.CloseElement();
        }

        BuildCursor(lineHeightPoints);
    }

    private void BuildCursor(float lineHeightPoints)
    {
        if (!IsFocused || (Ui.ElapsedSeconds - _lastEditTime) % 1.0f > 0.5f)
        {
            return;
        }

        var top = _cursorLine * _lineHeight - _scroll;
        var view = ViewHeight();
        if (view > 0 && (top + _lineHeight < 0 || top > view))
        {
            return;
        }

        var cursor = ClayElementDeclaration.Default();
        cursor.Layout.Sizing.Width = ClaySizingAxis.Fixed(1.5f);
        cursor.Layout.Sizing.Height = ClaySizingAxis.Fixed(lineHeightPoints > 0 ? lineHeightPoints : ResolvedFontSize + 4);
        cursor.BackgroundColor = CaretColor.ToClay();
        cursor.Floating.AttachTo = ClayFloatingAttachTo.Parent;
        cursor.Floating.ZIndex = Ui.FloatingZIndex;
        cursor.Floating.ElementAttachPoint = ClayFloatingAttachPoint.LeftTop;
        cursor.Floating.ParentAttachPoint = ClayFloatingAttachPoint.LeftTop;
        cursor.Floating.Offset = new Vector2(Padding.Left + Ui.Clay.PixelsToPoints(_cursorX), Padding.Top + Ui.Clay.PixelsToPoints(top));
        cursor.Floating.PointerCaptureMode = ClayPointerCaptureMode.Passthrough;
        Ui.Clay.OpenElement(in cursor);
        Ui.Clay.CloseElement();
    }
}
