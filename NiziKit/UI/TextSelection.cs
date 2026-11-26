using System.Numerics;
using System.Text;
using DenOfIz;

namespace NiziKit.UI;

internal interface ISelectableText
{
    bool IsShown { get; }
    int BlockCount { get; }
    ReadOnlySpan<byte> Block(int index);
    uint TextId(int block);
    (int Block, int Offset) OffsetAt(Vector2 point);
}

// The last pressed selection takes the Cmd/Ctrl+A and C the focused widget leaves and ends at a press outside its widget, so selectable text
// takes no focus and no Tab stop.
internal sealed class TextSelection(ISelectableText owner)
{
    private static TextSelection? _active;

    private readonly ISelectableText _owner = owner;

    private int _anchorBlock;
    private int _anchor;
    private int _caretBlock;
    private int _caret;
    private bool _dragging;

    public bool HasSelection => _anchorBlock != _caretBlock || _anchor != _caret;

    public string Text
    {
        get
        {
            if (!HasSelection)
            {
                return "";
            }

            var (startBlock, start, endBlock, end) = Ordered();
            var count = _owner.BlockCount;
            var builder = new StringBuilder();
            for (var block = startBlock; block <= endBlock && block < count; block++)
            {
                var bytes = _owner.Block(block);
                var from = block == startBlock ? Math.Min(start, bytes.Length) : 0;
                var to = block == endBlock ? Math.Min(end, bytes.Length) : bytes.Length;
                if (block > startBlock)
                {
                    builder.Append('\n');
                }

                builder.Append(Encoding.UTF8.GetString(bytes[from..Math.Max(from, to)]));
            }

            return builder.Replace("\uFFFC", "").ToString();
        }
    }

    public void Update(in UIInteraction state)
    {
        if (state.PressedOutside && !state.PressStarted)
        {
            Clear();
            return;
        }

        if (state.PressStarted)
        {
            var (block, offset) = _owner.OffsetAt(Ui.PointerPosition);
            Press(block, offset, state.ClickCount);
            return;
        }

        if (!state.Pressed)
        {
            _dragging = false;
            return;
        }

        if (!_dragging)
        {
            return;
        }

        var (caretBlock, caret) = _owner.OffsetAt(Ui.PointerPosition);
        if (caretBlock != _caretBlock || caret != _caret)
        {
            _caretBlock = caretBlock;
            _caret = caret;
            Ui.Native.ScrollCaretIntoView(_owner.TextId(caretBlock), (uint)caret);
        }
    }

    public bool Range(int block, int length, out uint start, out uint end)
    {
        start = end = 0;
        var (startBlock, from, endBlock, to) = Ordered();
        if (!HasSelection || block < startBlock || block > endBlock)
        {
            return false;
        }

        start = (uint)Math.Min(block == startBlock ? from : 0, length);
        end = (uint)Math.Min(block == endBlock ? to : length, length);
        return start < end;
    }

    public void SelectAll()
    {
        var last = _owner.BlockCount - 1;
        _anchorBlock = _anchor = 0;
        _caretBlock = Math.Max(0, last);
        _caret = last >= 0 ? _owner.Block(last).Length : 0;
        _dragging = false;
        _active = this;
    }

    public void Clear()
    {
        _anchorBlock = _anchor = _caretBlock = _caret = 0;
        _dragging = false;
        if (_active == this)
        {
            _active = null;
        }
    }

    internal static bool HandleKey(in KeyboardEventData key)
    {
        if (_active is not { } active || !active._owner.IsShown || !Keys.Shortcut(key.Mod) || Keys.Shift(key.Mod) || Keys.Alt(key.Mod))
        {
            return false;
        }

        switch (key.KeyCode)
        {
            case KeyCode.A:
                active.SelectAll();
                return true;
            case KeyCode.C when active.HasSelection:
                using (var selected = new StringView.Pinned(active.Text))
                {
                    Clipboard.SetText(selected);
                }

                return true;
            default:
                return false;
        }
    }

    internal static void Reset()
    {
        _active = null;
    }

    internal static void Describe(UiWidgetInfo info, bool selectable, TextSelection? selection)
    {
        if (selectable)
        {
            info.Set("selectable", true);
        }

        if (selection is { HasSelection: true })
        {
            info.Set("selectedText", selection.Text);
        }
    }

    private void Press(int block, int offset, uint clicks)
    {
        var extend = _active == this && Keys.Shift(InputSystem.GetModState());
        _active = this;
        _dragging = false;
        if (clicks >= 3)
        {
            Select(block, 0, _owner.Block(block).Length);
        }
        else if (clicks == 2)
        {
            var (start, end) = TextEditor.WordAt(_owner.Block(block), offset);
            Select(block, start, end);
        }
        else
        {
            if (!extend)
            {
                _anchorBlock = block;
                _anchor = offset;
            }

            _caretBlock = block;
            _caret = offset;
            _dragging = true;
        }
    }

    private void Select(int block, int start, int end)
    {
        _anchorBlock = _caretBlock = block;
        _anchor = start;
        _caret = end;
    }

    private (int StartBlock, int Start, int EndBlock, int End) Ordered()
    {
        return _anchorBlock < _caretBlock || (_anchorBlock == _caretBlock && _anchor <= _caret)
            ? (_anchorBlock, _anchor, _caretBlock, _caret)
            : (_caretBlock, _caret, _anchorBlock, _anchor);
    }
}
