using DenOfIz;

namespace NiziKit.UI.Widgets;

public delegate void ListItemTemplate(int index, bool selected);

public class ListView : Widget
{
    private readonly List<uint> _rowIds = new();
    private int _selectedIndex = -1;
    private bool _reveal;

    public ListView()
    {
        ref var layout = ref Desc.Layout;
        layout.Direction = UIDirection.Column;
        layout.Height = 240;
        layout.Padding = UILengthEdges.All(1);
        Desc.Style.BorderWidth = UIEdges.All(1);
        Desc.Scroll.OverflowX = UIOverflow.Clip;
        Desc.Scroll.OverflowY = UIOverflow.Scroll;
    }

    public ListView(IReadOnlyList<string> items) : this()
    {
        Items = items;
    }

    public IReadOnlyList<string>? Items { get; set; }

    public int Count { get; set; }

    public ListItemTemplate? ItemTemplate { get; set; }

    public bool Striped { get; set; } = true;

    public UILengthEdges RowPadding { get; set; } = UILengthEdges.Symmetric(12, 7);

    public int RowCount => Items?.Count ?? Count;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            var clamped = RowCount == 0 ? -1 : Math.Clamp(value, -1, RowCount - 1);
            if (clamped == _selectedIndex)
            {
                return;
            }

            _selectedIndex = clamped;
            SelectionChanged?.Invoke(this);
        }
    }

    public event Action<ListView>? SelectionChanged;

    public event Action<ListView, int>? ItemActivated;

    protected override bool Focusable => true;

    public uint RowId(int index)
    {
        while (_rowIds.Count <= index)
        {
            _rowIds.Add(Ui.Id(Id, (uint)_rowIds.Count + 1));
        }

        return _rowIds[index];
    }

    public void ScrollToSelected()
    {
        _reveal = true;
    }

    protected internal override bool OnKeyDown(in KeyboardEventData key)
    {
        var count = RowCount;
        if (count == 0)
        {
            return false;
        }

        switch (key.KeyCode)
        {
            case KeyCode.Up:
                MoveSelection(_selectedIndex < 0 ? count - 1 : _selectedIndex - 1);
                return true;
            case KeyCode.Down:
                MoveSelection(_selectedIndex + 1);
                return true;
            case KeyCode.Pageup:
                MoveSelection(Math.Max(0, _selectedIndex) - PageRows());
                return true;
            case KeyCode.Pagedown:
                MoveSelection(Math.Max(0, _selectedIndex) + PageRows());
                return true;
            case KeyCode.Home:
                MoveSelection(0);
                return true;
            case KeyCode.End:
                MoveSelection(count - 1);
                return true;
            case KeyCode.Return:
            case KeyCode.KeypadEnter:
                if (_selectedIndex >= 0)
                {
                    ItemActivated?.Invoke(this, _selectedIndex);
                }

                return true;
            default:
                return false;
        }
    }

    protected override void OnBuild()
    {
        var theme = Ui.Theme;
        ref var desc = ref Desc;
        desc.Style.BackgroundColor = theme.Surface;
        desc.Style.BorderColor = IsFocused && Ui.FocusVisible ? theme.FocusRing : theme.Border;
        desc.Style.CornerRadius = UICorners.All(theme.CornerRadius + 2);

        var count = RowCount;
        using (OpenSelf())
        {
            for (var i = 0; i < count; i++)
            {
                var id = RowId(i);
                var interaction = Ui.Interaction(id);
                if (interaction.Clicked)
                {
                    SelectedIndex = i;
                }

                if (interaction.DoubleClicked)
                {
                    ItemActivated?.Invoke(this, i);
                }

                var selected = i == _selectedIndex;
                var background = selected ? theme.AccentSoft : interaction.Hovered ? theme.Hover : Striped && (i & 1) == 1 ? theme.SurfaceAlt : default;
                var row = Ui.Row(id)
                    .AlignItems(UIAlign.Center)
                    .Gap(12)
                    .Shrink(0)
                    .Background(background)
                    .Cursor(SystemCursor.Pointer)
                    .Animate(UITransitionPropertyFlagBits.BackgroundColor, 0.08f);
                row.Desc.Layout.Padding = RowPadding;
                using (row.Open())
                {
                    if (ItemTemplate != null)
                    {
                        ItemTemplate(i, selected);
                    }
                    else if (Items != null)
                    {
                        Ui.Text(Items[i], theme.FontSize, selected ? theme.Accent : theme.Text);
                    }
                }
            }
        }

        if (_reveal && _selectedIndex >= 0 && _selectedIndex < count)
        {
            Ui.ScrollIntoView(RowId(_selectedIndex));
        }

        _reveal = false;
    }

    private void MoveSelection(int index)
    {
        SelectedIndex = Math.Clamp(index, 0, RowCount - 1);
        _reveal = true;
    }

    private int PageRows()
    {
        if (_selectedIndex >= 0 && Ui.TryGetScroll(Id, out var scroll) && Ui.TryGetRect(RowId(_selectedIndex), out var row) && row.Height > 0)
        {
            return Math.Max(1, (int)(scroll.ViewportSize.Y / row.Height) - 1);
        }

        return 10;
    }
}
