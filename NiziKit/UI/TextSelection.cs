using System.Numerics;
using System.Text;
using DenOfIz;

namespace NiziKit.UI;

internal interface ISelectableText
{
    Widget Widget { get; }
    int BlockCount { get; }
    ReadOnlySpan<byte> Block(int index);
    uint TextId(int block);
    (int Block, int Offset) OffsetAt(Vector2 point);
}

// The last pressed selection takes the Cmd/Ctrl+A and C the focused widget leaves and ends at a press outside its widget, so selectable text
// takes no focus and no Tab stop. A scoped selection follows the scope's own element, which reports the press and drag anywhere under it, so a
// drag goes on after the pressed text has scrolled out of the built rows.
internal sealed class TextSelection
{
    private const float AutoScrollRate = 12;

    private static TextSelection? _active;

    private readonly ISelectableText? _owner;
    private readonly SelectionScope? _scope;

    private Mark _anchor;
    private Mark _caret;
    private bool _dragging;
    private int _pressFrame = -1;
    private ScrollView? _scroller;
    private bool _autoScrolling;

    public TextSelection(ISelectableText owner)
    {
        _owner = owner;
        _anchor = _caret = new Mark(owner, 0, 0);
    }

    public TextSelection(SelectionScope scope)
    {
        _scope = scope;
    }

    public bool IsScoped => _scope != null;

    public bool HasSelection => _anchor.Owner != null && _anchor != _caret;

    public string Text => TextOf(null);

    internal static bool AutoScrolling => _active is { _autoScrolling: true };

    private Widget Host => _scope ?? _owner!.Widget;

    public static TextSelection Attach(ISelectableText owner, TextSelection? current)
    {
        return SelectionScope.Join(owner) ?? (current is { IsScoped: false } ? current : new TextSelection(owner));
    }

    public string TextOf(ISelectableText? only)
    {
        if (!HasSelection)
        {
            return "";
        }

        var (start, end) = Ordered();
        var from = Order(start.Owner);
        var to = Order(end.Owner);
        if (from < 0 || to < 0)
        {
            return "";
        }

        var builder = new StringBuilder();
        var first = true;
        for (var i = from; i <= to; i++)
        {
            var owner = _scope?.Member(i) ?? _owner!;
            if (only != null && owner != only)
            {
                continue;
            }

            var count = owner.BlockCount;
            var lastBlock = i == to ? end.Block : count - 1;
            for (var block = i == from ? start.Block : 0; block <= lastBlock && block < count; block++)
            {
                var bytes = owner.Block(block);
                var a = i == from && block == start.Block ? Math.Min(start.Offset, bytes.Length) : 0;
                var b = i == to && block == end.Block ? Math.Min(end.Offset, bytes.Length) : bytes.Length;
                if (!first)
                {
                    builder.Append('\n');
                }

                first = false;
                builder.Append(Encoding.UTF8.GetString(bytes[a..Math.Max(a, b)]));
            }
        }

        return builder.Replace("\uFFFC", "").ToString();
    }

    public void Update(ISelectableText owner, in UIInteraction state)
    {
        if (_scope != null)
        {
            if (state.PressStarted)
            {
                var (pressedBlock, pressed) = owner.OffsetAt(Ui.PointerPosition);
                Press(owner, pressedBlock, pressed, state.ClickCount);
                _pressFrame = Ui.FrameNumber;
            }

            return;
        }

        if (state.PressedOutside && !state.PressStarted)
        {
            Clear();
            return;
        }

        if (state.PressStarted)
        {
            var (block, offset) = owner.OffsetAt(Ui.PointerPosition);
            Press(owner, block, offset, state.ClickCount);
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

        var (caretBlock, caret) = owner.OffsetAt(Ui.PointerPosition);
        if (caretBlock != _caret.Block || caret != _caret.Offset)
        {
            _caret = new Mark(owner, caretBlock, caret);
            Ui.Native.ScrollCaretIntoView(owner.TextId(caretBlock), (uint)caret);
        }
    }

    public void UpdateScope(in UIInteraction state)
    {
        if (state.PressStarted && _pressFrame != Ui.FrameNumber)
        {
            Clear();
            _active = this;
            return;
        }

        if (state.PressedOutside && !state.PressStarted)
        {
            Clear();
            return;
        }

        if (!state.Pressed)
        {
            _dragging = false;
            _autoScrolling = false;
            return;
        }

        if (_dragging)
        {
            Drag();
        }
    }

    public bool Range(ISelectableText owner, int block, int length, out uint start, out uint end)
    {
        start = end = 0;
        if (!HasSelection)
        {
            return false;
        }

        var (from, to) = Ordered();
        var index = Order(owner);
        var first = Order(from.Owner);
        var last = Order(to.Owner);
        if (index < 0 || index < first || index > last || (index == first && block < from.Block) || (index == last && block > to.Block))
        {
            return false;
        }

        start = (uint)Math.Min(index == first && block == from.Block ? from.Offset : 0, length);
        end = (uint)Math.Min(index == last && block == to.Block ? to.Offset : length, length);
        return start < end;
    }

    public void SelectAll()
    {
        if (_scope is { Count: 0 })
        {
            return;
        }

        var first = _scope?.Member(0) ?? _owner!;
        var last = _scope?.Member(_scope.Count - 1) ?? _owner!;
        var lastBlock = last.BlockCount - 1;
        _anchor = new Mark(first, 0, 0);
        _caret = new Mark(last, Math.Max(0, lastBlock), lastBlock >= 0 ? last.Block(lastBlock).Length : 0);
        _dragging = false;
        _active = this;
    }

    public void Clear()
    {
        _anchor = _caret = new Mark(_owner, 0, 0);
        _dragging = false;
        _autoScrolling = false;
        if (_active == this)
        {
            _active = null;
        }
    }

    public void Forget(ISelectableText owner)
    {
        if (_anchor.Owner != owner && _caret.Owner != owner)
        {
            return;
        }

        var active = _active == this;
        Clear();
        if (active)
        {
            _active = this;
        }
    }

    internal static bool HandleKey(in KeyboardEventData key)
    {
        if (_active is not { } active || !active.Host.IsShown || !Keys.Shortcut(key.Mod) || Keys.Shift(key.Mod) || Keys.Alt(key.Mod))
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

    internal static void Describe(UiWidgetInfo info, bool selectable, string selected)
    {
        if (selectable)
        {
            info.Set("selectable", true);
        }

        if (selected.Length > 0)
        {
            info.Set("selectedText", selected);
        }
    }

    private void Press(ISelectableText owner, int block, int offset, uint clicks)
    {
        var extend = _active == this && _anchor.Owner != null && Keys.Shift(InputSystem.GetModState());
        _active = this;
        _dragging = false;
        if (clicks >= 3)
        {
            Select(owner, block, 0, owner.Block(block).Length);
        }
        else if (clicks == 2)
        {
            var (start, end) = TextEditor.WordAt(owner.Block(block), offset);
            Select(owner, block, start, end);
        }
        else
        {
            if (!extend)
            {
                _anchor = new Mark(owner, block, offset);
            }

            _caret = new Mark(owner, block, offset);
            _dragging = true;
            _scroller = _scope != null ? ScrollerOf(owner.Widget) : null;
        }
    }

    private void Select(ISelectableText owner, int block, int start, int end)
    {
        _anchor = new Mark(owner, block, start);
        _caret = new Mark(owner, block, end);
    }

    private void Drag()
    {
        var point = Ui.PointerPosition;
        ScrollView? scroller = null;
        var view = default(Rectangle);
        _autoScrolling = false;
        if (_scroller is { IsShown: true } && Ui.TryGetRect(_scroller.Id, out var rect))
        {
            scroller = _scroller;
            view = Rectangle.From(rect);
            AutoScroll(scroller, view, point);
        }

        if (_scope!.Nearest(point, scroller, view) is not { } hit)
        {
            return;
        }

        var (block, offset) = hit.Owner.OffsetAt(Vector2.Clamp(point, hit.Rect.Position, hit.Rect.Position + hit.Rect.Size));
        _caret = new Mark(hit.Owner, block, offset);
    }

    private void AutoScroll(ScrollView scroller, Rectangle view, Vector2 point)
    {
        var horizontal = scroller.Horizontal;
        var at = horizontal ? point.X : point.Y;
        var start = horizontal ? view.X : view.Y;
        var end = start + (horizontal ? view.Width : view.Height);
        var over = at < start ? at - start : at > end ? at - end : 0;
        if (over == 0 || !Ui.TryGetScroll(scroller.Id, out var scroll))
        {
            return;
        }

        var offset = horizontal ? scroll.Offset.X : scroll.Offset.Y;
        var max = horizontal ? scroll.MaxOffset.X : scroll.MaxOffset.Y;
        if ((over < 0 && offset <= 0) || (over > 0 && offset >= max))
        {
            return;
        }

        var next = Math.Clamp(offset + over * AutoScrollRate * Ui.DeltaTime, 0, max);
        scroller.ScrollTo(horizontal ? new Vector2(next, scroll.Offset.Y) : new Vector2(scroll.Offset.X, next));
        _autoScrolling = true;
    }

    private static ScrollView? ScrollerOf(Widget widget)
    {
        for (var parent = widget.Parent; parent != null; parent = parent.Parent)
        {
            if (parent is ScrollView scroller)
            {
                return scroller;
            }
        }

        return null;
    }

    private int Order(ISelectableText? owner)
    {
        return owner == null ? -1 : _scope?.IndexOf(owner) ?? 0;
    }

    private (Mark Start, Mark End) Ordered()
    {
        var anchor = Order(_anchor.Owner);
        var caret = Order(_caret.Owner);
        return anchor < caret || (anchor == caret && (_anchor.Block < _caret.Block || (_anchor.Block == _caret.Block && _anchor.Offset <= _caret.Offset)))
            ? (_anchor, _caret)
            : (_caret, _anchor);
    }

    private readonly record struct Mark(ISelectableText? Owner, int Block, int Offset);
}
