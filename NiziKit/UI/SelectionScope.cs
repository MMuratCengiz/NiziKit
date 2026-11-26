using System.Numerics;
using DenOfIz;

namespace NiziKit.UI;

/// <summary>
/// Makes one text selection span the selectable Label, Paragraph and RichText widgets inside <see cref="Content"/>, such as the rows of a
/// transcript ListView; each still needs <c>Selectable = true</c>. A drag that starts in one selects across the others in build order, which is
/// visual order in a Column, Row or ListView, and while the pointer is past the edge of the ScrollView or ListView around the pressed text it
/// scrolls that view. Shift+click extends across widgets, Cmd+A (Ctrl+A) after a press anywhere in the scope selects all of it, Cmd+C (Ctrl+C)
/// copies plain text with a newline between widgets, and a press outside the scope clears the selection. The content lays out as if the scope
/// were not there.
/// The selection covers the text laid out under the scope: ListView rows that are offscreen placeholders keep their place and text, while rows
/// a ListView has not built yet and hidden or filtered widgets are left out.
/// </summary>
public sealed class SelectionScope : Widget
{
    private static SelectionScope? _building;

    private readonly TextSelection _selection;
    private readonly Dictionary<ISelectableText, int> _index = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<ISelectableText, int> _positions = new(ReferenceEqualityComparer.Instance);
    private readonly List<(int After, int Order, ISelectableText Member)> _kept = new();
    private List<ISelectableText> _built = new();
    private List<ISelectableText> _previous = new();
    private List<ISelectableText> _members = new();
    private List<ISelectableText> _next = new();

    public SelectionScope(Widget content)
    {
        Content = content;
        content.Parent = this;
        _selection = new TextSelection(this);
    }

    public Widget Content { get; }

    public bool HasSelection => _selection.HasSelection;

    /// <summary>
    /// The selected text across the scope, the copy Cmd+C makes: plain text with a newline between widgets and between RichText paragraphs. Empty
    /// without a selection.
    /// </summary>
    public string SelectedText => _selection.Text;

    /// <summary>
    /// Selects all text in the scope, as Cmd+A does, and makes the scope take the next Cmd+C.
    /// </summary>
    public void SelectAll()
    {
        _selection.SelectAll();
    }

    public void ClearSelection()
    {
        _selection.Clear();
    }

    internal override bool Interactive => true;

    internal int Count => _members.Count;

    internal ISelectableText Member(int index)
    {
        return _members[index];
    }

    internal int IndexOf(ISelectableText member)
    {
        return _index.TryGetValue(member, out var index) ? index : -1;
    }

    internal static TextSelection? Join(ISelectableText member)
    {
        if (_building is not { } scope)
        {
            return null;
        }

        scope._built.Add(member);
        return scope._selection;
    }

    internal (ISelectableText Owner, Rectangle Rect)? Nearest(Vector2 point, ScrollView? scroller, Rectangle view)
    {
        (ISelectableText, Rectangle)? nearest = null;
        var distance = float.MaxValue;
        foreach (var member in _built)
        {
            var rect = member.Widget.Bounds;
            if (scroller != null && Inside(member.Widget, scroller) && !Clip(ref rect, view))
            {
                continue;
            }

            var dx = MathF.Max(0, MathF.Max(rect.X - point.X, point.X - rect.Right));
            var dy = MathF.Max(0, MathF.Max(rect.Y - point.Y, point.Y - rect.Y - rect.Height));
            var d = dx * dx + dy * dy;
            if (d < distance)
            {
                distance = d;
                nearest = (member, rect);
            }
        }

        return nearest;
    }

    protected internal override void Describe(UiWidgetInfo info)
    {
        info.Set("members", _members.Count);
        TextSelection.Describe(info, false, _selection.Text);
    }

    internal override void Dispatch()
    {
        base.Dispatch();
        _selection.UpdateScope(in State);
    }

    internal override void Render()
    {
        (_built, _previous) = (_previous, _built);
        _built.Clear();
        var desc = new UIElementDesc { Id = Id };
        desc.Layout.Display = UIDisplay.Contents;
        Ui.Open(in desc);
        var outer = _building;
        _building = this;
        Content.BuildFrame();
        _building = outer;
        Ui.Close();
        Merge();
    }

    // Members built this frame take their build order. A member that was not built stays after the member it followed when it is under a
    // ListView placeholder row, and leaves the scope otherwise.
    private void Merge()
    {
        if (Unchanged())
        {
            return;
        }

        _positions.Clear();
        for (var i = 0; i < _built.Count; i++)
        {
            _positions[_built[i]] = i;
        }

        _kept.Clear();
        var after = -1;
        for (var i = 0; i < _members.Count; i++)
        {
            var member = _members[i];
            if (_positions.TryGetValue(member, out var position))
            {
                after = position;
            }
            else if (Virtualized(member.Widget))
            {
                _kept.Add((after, i, member));
            }
            else
            {
                _selection.Forget(member);
            }
        }

        _kept.Sort(static (a, b) => a.After != b.After ? a.After.CompareTo(b.After) : a.Order.CompareTo(b.Order));
        _next.Clear();
        var k = 0;
        for (var i = -1; i < _built.Count; i++)
        {
            if (i >= 0)
            {
                _next.Add(_built[i]);
            }

            while (k < _kept.Count && _kept[k].After == i)
            {
                _next.Add(_kept[k++].Member);
            }
        }

        (_members, _next) = (_next, _members);
        _index.Clear();
        for (var i = 0; i < _members.Count; i++)
        {
            _index[_members[i]] = i;
        }
    }

    private bool Unchanged()
    {
        if (_built.Count != _previous.Count)
        {
            return false;
        }

        for (var i = 0; i < _built.Count; i++)
        {
            if (_built[i] != _previous[i])
            {
                return false;
            }
        }

        if (_members.Count == _built.Count)
        {
            return true;
        }

        foreach (var member in _members)
        {
            if (!member.Widget.BuiltThisFrame && !Virtualized(member.Widget))
            {
                return false;
            }
        }

        return true;
    }

    private bool Virtualized(Widget widget)
    {
        for (var parent = widget.Parent; parent != null && parent != this; parent = parent.Parent)
        {
            if (parent.BuiltThisFrame)
            {
                return parent.IsPlaceholder;
            }
        }

        return false;
    }

    private static bool Inside(Widget widget, Widget ancestor)
    {
        for (var parent = widget.Parent; parent != null; parent = parent.Parent)
        {
            if (parent == ancestor)
            {
                return true;
            }
        }

        return false;
    }

    private static bool Clip(ref Rectangle rect, Rectangle view)
    {
        var x = MathF.Max(rect.X, view.X);
        var y = MathF.Max(rect.Y, view.Y);
        var right = MathF.Min(rect.Right, view.Right);
        var bottom = MathF.Min(rect.Y + rect.Height, view.Y + view.Height);
        if (right < x || bottom < y)
        {
            return false;
        }

        rect = new Rectangle(x, y, right - x, bottom - y);
        return true;
    }
}
