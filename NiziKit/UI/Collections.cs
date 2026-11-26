using DenOfIz;

namespace NiziKit.UI;

public sealed class ForEach<T> : Widget
{
    private sealed class Entry
    {
        public required Widget Widget;
        public int Seen;
    }

    private readonly Func<IEnumerable<T>> _source;
    private readonly Func<T, Widget> _template;
    private readonly Dictionary<T, Entry> _byItem;
    private readonly Dictionary<long, Entry> _byKey = new();
    private readonly List<T> _staleItems = new();
    private readonly List<long> _staleKeys = new();
    private int _pass;

    public ForEach(Func<IEnumerable<T>> source, Func<T, Widget> template)
    {
        _source = source;
        _template = template;
        var comparer = typeof(T).IsValueType ? EqualityComparer<T>.Default : (IEqualityComparer<T>)(object)ReferenceEqualityComparer.Instance;
        _byItem = new Dictionary<T, Entry>(comparer);
    }

    public Func<T, bool>? Filter { get; set; }
    public Func<T, long>? Key { get; set; }

    internal Action<T, Widget, int>? BeforeBuild { get; set; }

    public Widget? WidgetFor(T item)
    {
        var found = Key != null ? _byKey.TryGetValue(Key(item), out var keyed) ? keyed : null : item is not null && _byItem.TryGetValue(item, out var entry) ? entry : null;
        return found?.Widget;
    }

    internal override void Render()
    {
        _pass++;
        var seen = 0;
        var shown = 0;
        var source = _source();
        if (source is IReadOnlyList<T> list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                Visit(list[i], ref seen, ref shown);
            }
        }
        else
        {
            foreach (var item in source)
            {
                Visit(item, ref seen, ref shown);
            }
        }

        if (Key != null ? seen != _byKey.Count : seen != _byItem.Count)
        {
            DropStale();
        }
    }

    private void Visit(T item, ref int seen, ref int shown)
    {
        if (item is null)
        {
            return;
        }

        Entry? entry;
        if (Key != null)
        {
            var key = Key(item);
            if (!_byKey.TryGetValue(key, out entry))
            {
                entry = Create(item);
                _byKey[key] = entry;
            }
        }
        else if (!_byItem.TryGetValue(item, out entry))
        {
            entry = Create(item);
            _byItem[item] = entry;
        }

        if (entry.Seen == _pass)
        {
            return;
        }

        entry.Seen = _pass;
        seen++;
        if (Filter != null && !Filter(item))
        {
            return;
        }

        BeforeBuild?.Invoke(item, entry.Widget, shown);
        shown++;
        entry.Widget.BuildFrame();
    }

    private Entry Create(T item)
    {
        var widget = _template(item);
        widget.Parent = this;
        return new Entry { Widget = widget };
    }

    private void DropStale()
    {
        if (Key != null)
        {
            _staleKeys.Clear();
            foreach (var (key, entry) in _byKey)
            {
                if (entry.Seen != _pass)
                {
                    _staleKeys.Add(key);
                }
            }

            foreach (var key in _staleKeys)
            {
                _byKey.Remove(key, out var entry);
                entry!.Widget.Parent = null;
            }

            return;
        }

        _staleItems.Clear();
        foreach (var (item, entry) in _byItem)
        {
            if (entry.Seen != _pass)
            {
                _staleItems.Add(item);
            }
        }

        foreach (var item in _staleItems)
        {
            _byItem.Remove(item, out var entry);
            entry!.Widget.Parent = null;
        }
    }
}

public class ListView<T> : ScrollView where T : class
{
    private readonly ForEach<T> _items;
    private readonly List<T> _order = new();
    private Bound<T?> _selected;
    private bool _selectable;
    private T? _reveal;
    private bool _viewKnown;
    private bool _remeasure;
    private float _viewTop;
    private float _viewBottom;
    private float _viewWidth;

    public ListView(Func<IEnumerable<T>> source, Func<T, Widget> template)
    {
        _items = new ForEach<T>(source, item => new ListRow(this, item, template(item)))
        {
            BeforeBuild = Visit
        };
        Children.Add(_items);
    }

    public Func<T, bool>? Filter
    {
        get => _items.Filter;
        set => _items.Filter = value;
    }

    public Func<T, long>? Key
    {
        get => _items.Key;
        set => _items.Key = value;
    }

    public Bound<T?> Selected
    {
        get => _selected;
        set
        {
            _selected = value;
            _selectable = true;
        }
    }

    public Action<T>? OnActivate { get; set; }
    public bool Striped { get; set; }
    public Edges? RowPadding { get; set; }
    public Corners RowRadius { get; set; }

    public bool Selectable => _selectable || OnActivate != null;
    public bool Virtualize { get; set; } = true;

    protected override bool Focusable => Selectable;

    public void Reveal(T item)
    {
        _reveal = item;
    }

    internal void Select(T item)
    {
        if (ReferenceEquals(_selected.Value, item))
        {
            return;
        }

        _selected.Set(item);
    }

    internal void Activate(T item)
    {
        OnActivate?.Invoke(item);
    }

    protected internal override bool OnKeyDown(in KeyboardEventData key)
    {
        if (!Selectable || _order.Count == 0)
        {
            return false;
        }

        var index = _selected.Value is { } current ? _order.IndexOf(current) : -1;
        var page = Math.Max(1, (int)(Bounds.Height / 32) - 1);
        switch (key.KeyCode)
        {
            case KeyCode.Up:
                Move(index < 0 ? _order.Count - 1 : index - 1);
                return true;
            case KeyCode.Down:
                Move(index + 1);
                return true;
            case KeyCode.Pageup:
                Move(Math.Max(0, index) - page);
                return true;
            case KeyCode.Pagedown:
                Move(Math.Max(0, index) + page);
                return true;
            case KeyCode.Home:
                Move(0);
                return true;
            case KeyCode.End:
                Move(_order.Count - 1);
                return true;
            case KeyCode.Return:
            case KeyCode.KeypadEnter:
                if (_selected.Value is { } selected)
                {
                    Activate(selected);
                }

                return true;
            default:
                return false;
        }
    }

    private protected override void BuildChildren()
    {
        _order.Clear();
        _viewKnown = Ui.TryGetRect(Id, out var view) && view.Height > 0;
        _viewTop = view.Y - view.Height;
        _viewBottom = view.Y + view.Height * 2;
        _remeasure = MathF.Abs(view.Width - _viewWidth) > 0.5f;
        _viewWidth = view.Width;
        base.BuildChildren();
        if (_reveal != null && _items.WidgetFor(_reveal) is { } widget)
        {
            ScrollIntoView(widget);
        }

        _reveal = null;
    }

    private void Visit(T item, Widget widget, int index)
    {
        _order.Add(item);
        if (widget is ListRow row)
        {
            row.Index = index;
        }
    }

    private void Move(int index)
    {
        var item = _order[Math.Clamp(index, 0, _order.Count - 1)];
        Select(item);
        _reveal = item;
    }

    private sealed class ListRow : Panel
    {
        private readonly ListView<T> _list;
        private readonly T _item;

        public ListRow(ListView<T> list, T item, Widget content) : base(UIDirection.Row)
        {
            _list = list;
            _item = item;
            Children.Add(content);
        }

        protected override bool Clickable => _list.Selectable;

        protected override void OnActivate()
        {
            _list.Select(_item);
        }

        internal override void Dispatch()
        {
            base.Dispatch();
            if (State.DoubleClicked && _list.Selectable && !Consumed)
            {
                _list.Activate(_item);
            }
        }

        public int Index { get; set; }

        protected override bool Focusable => false;
        private protected override bool FillsChildren => true;

        private float _height;
        private bool _placeholder;

        internal override bool IsPlaceholder => _placeholder;

        internal override void Render()
        {
            _placeholder = Offscreen();
            if (_placeholder)
            {
                var placeholder = Desc();
                placeholder.Layout.Height = _height;
                placeholder.Layout.Shrink = 0;
                Ui.Open(in placeholder);
                Ui.Close();
                return;
            }

            var selectable = _list.Selectable;
            var selected = selectable && ReferenceEquals(_list._selected.Value, _item);
            Padding = _list.RowPadding ?? (selectable ? new Edges(12, 7) : default);
            Radius = _list.RowRadius;
            Gap = 12;
            Background = selected ? Color.AccentSoft : selectable && State.Hovered ? Color.Hover : _list.Striped && (Index & 1) == 1 ? Color.SurfaceAlt : Color.Transparent;
            Animate = UITransitionPropertyFlagBits.BackgroundColor;
            AnimateDuration = 0.08f;
            base.Render();
        }

        private bool Offscreen()
        {
            var list = _list;
            if (!list.Virtualize || !list._viewKnown || !Ui.TryGetRect(Id, out var rect) || rect.Height <= 0)
            {
                return false;
            }

            _height = rect.Height;
            if (list._remeasure || ReferenceEquals(list._reveal, _item))
            {
                return false;
            }

            return rect.Y + rect.Height < list._viewTop || rect.Y > list._viewBottom;
        }
    }
}

public sealed record Tab<T>(T Value, string Title, string? Icon, Widget Page);

public class Tabs<T> : Widget
{
    private readonly List<uint> _ids = new();
    private uint _stripId;
    private uint _scrollId;
    private uint _pagesId;
    private int _revealed = -1;

    public List<Tab<T>> Pages { get; set; } = new();
    public Bound<T> Selected { get; set; }
    public Action<T>? OnChange { get; set; }

    protected internal override void Describe(UiWidgetInfo info)
    {
        var index = SelectedIndex;
        info.Value = Selected.Value?.ToString();
        info.Text = index >= 0 ? Pages[index].Title : null;
        info.Set("pages", Pages.Select(p => p.Title).ToArray());
    }

    public int SelectedIndex
    {
        get
        {
            var selected = Selected.Value;
            for (var i = 0; i < Pages.Count; i++)
            {
                if (EqualityComparer<T>.Default.Equals(Pages[i].Value, selected))
                {
                    return i;
                }
            }

            return Pages.Count > 0 ? 0 : -1;
        }
    }

    internal override bool Interactive => true;

    public void Select(T value)
    {
        var selected = Selected;
        if (EqualityComparer<T>.Default.Equals(selected.Value, value))
        {
            return;
        }

        selected.Set(value);
        Selected = selected;
        OnChange?.Invoke(value);
    }

    internal override void Dispatch()
    {
        base.Dispatch();
        if (_stripId == 0)
        {
            return;
        }

        var strip = Ui.Interaction(_stripId);
        if (strip.Focused)
        {
            Ui.TrackFocused(this);
        }

        for (var i = 0; i < Pages.Count && i < _ids.Count; i++)
        {
            if (Ui.Interaction(_ids[i]).Clicked)
            {
                Select(Pages[i].Value);
            }
        }
    }

    protected internal override bool OnKeyDown(in KeyboardEventData key)
    {
        var index = SelectedIndex;
        switch (key.KeyCode)
        {
            case KeyCode.Left when index > 0:
                Select(Pages[index - 1].Value);
                return true;
            case KeyCode.Right when index >= 0 && index < Pages.Count - 1:
                Select(Pages[index + 1].Value);
                return true;
            default:
                return false;
        }
    }

    internal override void Render()
    {
        if (_stripId == 0)
        {
            _stripId = Ui.Id(Id, 0x5791);
            _scrollId = Ui.Id(Id, 0x5C40);
            _pagesId = Ui.Id(Id, 0x9A6E);
        }

        while (_ids.Count < Pages.Count)
        {
            _ids.Add(Ui.Id(Id, (uint)_ids.Count + 1));
        }

        var theme = Ui.Theme;
        var selectedIndex = SelectedIndex;
        var strip = Ui.Interaction(_stripId);
        var focusRing = strip.Focused && Ui.FocusVisible;
        var desc = Desc();
        desc.Layout.Direction = UIDirection.Column;
        if (Flex <= 0 && Height.Type == UIValueType.Auto)
        {
            desc.Layout.Grow = 1;
            desc.Layout.Shrink = 1;
            desc.Layout.Basis = UIValue.Points(0);
        }

        Ui.Open(in desc);
        using (Element.Row(_stripId).Focusable().Border(theme.Border, new UIEdges(0, 0, 0, 1)).Open())
        using (Element.Row(_scrollId).Fill().Gap(2).Padding(8, 0).Margin(0, 0, 0, -1).ScrollX().HideScrollbar().Open())
        {
            for (var i = 0; i < Pages.Count; i++)
            {
                var tab = Pages[i];
                var id = _ids[i];
                var hovered = Ui.Interaction(id).Hovered;
                var selected = i == selectedIndex;
                var color = selected ? theme.Accent : hovered ? theme.Text : theme.TextMuted;
                using (Element.Row(id)
                           .AlignItems(UIAlign.Center)
                           .Gap(8)
                           .Padding(14, 10, 14, 8)
                           .Background(hovered && !selected ? theme.Hover : default)
                           .Border(selected ? focusRing ? theme.FocusRing : theme.Accent : default, new UIEdges(0, 0, 0, 2))
                           .CornerRadius(new UICorners(theme.CornerRadius, theme.CornerRadius, 0, 0))
                           .Cursor(SystemCursor.Pointer)
                           .Animate()
                           .Open())
                {
                    if (tab.Icon != null)
                    {
                        Ui.Glyph(tab.Icon, theme.FontSize, color);
                    }

                    Ui.Line(tab.Title, theme.FontSize, color);
                }
            }
        }

        if (_revealed != selectedIndex && selectedIndex >= 0)
        {
            Ui.ScrollIntoView(_ids[selectedIndex]);
            _revealed = selectedIndex;
        }

        using (Element.Column(_pagesId).Fill().Open())
        {
            if (selectedIndex >= 0)
            {
                var page = Pages[selectedIndex].Page;
                page.Parent ??= this;
                Ui.PushLayout(false, false);
                page.BuildFrame();
                Ui.PopLayout();
            }
        }

        Ui.Close();
    }
}
