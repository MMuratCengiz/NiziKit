using DenOfIz;

namespace NiziKit.UI.Widgets;

public class Dropdown : Widget
{
    private readonly DropdownList _list;
    private int _selectedIndex = -1;

    public Dropdown()
    {
        ref var layout = ref Desc.Layout;
        layout.Width = 180;
        layout.Justify = UIJustify.SpaceBetween;
        layout.AlignItems = UIAlign.Center;
        layout.Gap = 8;
        layout.Padding = UILengthEdges.Symmetric(10, 6);
        Desc.Style.BorderWidth = UIEdges.All(1);
        _list = new DropdownList(this);
    }

    public Dropdown(IEnumerable<string> items, int selectedIndex = 0) : this()
    {
        Items.AddRange(items);
        _selectedIndex = selectedIndex < 0 || Items.Count == 0 ? -1 : Math.Min(selectedIndex, Items.Count - 1);
    }

    public List<string> Items { get; } = new();
    public string Placeholder { get; set; } = "Select...";
    public float MaxListHeight { get; set; } = 240;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            var clamped = value < 0 || value >= Items.Count ? -1 : value;
            if (clamped == _selectedIndex)
            {
                return;
            }

            _selectedIndex = clamped;
            SelectionChanged?.Invoke(this);
        }
    }

    public string? SelectedItem
    {
        get => _selectedIndex >= 0 && _selectedIndex < Items.Count ? Items[_selectedIndex] : null;
        set => SelectedIndex = value == null ? -1 : Items.IndexOf(value);
    }

    public bool IsOpen => _list.IsOpen;

    public event Action<Dropdown>? SelectionChanged;

    protected override bool Focusable => true;

    public void Open()
    {
        if (!Enabled || Items.Count == 0)
        {
            return;
        }

        _list.Highlight = Math.Max(0, _selectedIndex);
        _list.Reveal = true;
        _list.Open(this);
    }

    public void Close()
    {
        _list.Close();
    }

    protected override void OnClick()
    {
        if (IsOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    protected internal override bool OnKeyDown(in KeyboardEventData key)
    {
        if (IsOpen)
        {
            return _list.OnKeyDown(in key);
        }

        switch (key.KeyCode)
        {
            case KeyCode.Up:
                SelectedIndex = Math.Max(0, _selectedIndex - 1);
                return true;
            case KeyCode.Down:
                SelectedIndex = Math.Min(Items.Count - 1, _selectedIndex + 1);
                return true;
            default:
                return false;
        }
    }

    protected override void OnBuild()
    {
        if (IsOpen && (!Visible || !Enabled))
        {
            Close();
        }

        var theme = Ui.Theme;
        var open = IsOpen;
        ref var desc = ref Desc;
        desc.Cursor = Enabled ? SystemCursor.Pointer : SystemCursor.NotAllowed;
        desc.Style.BackgroundColor = !Enabled ? theme.Disabled : open || IsHovered ? theme.Hover : theme.Surface;
        desc.Style.BorderColor = !Enabled ? theme.Disabled : IsFocused && Ui.FocusVisible ? theme.FocusRing : open ? theme.Accent : IsHovered ? theme.BorderStrong : theme.Border;
        desc.Style.CornerRadius = UICorners.All(theme.CornerRadius);
        desc.Transition.Duration = theme.TransitionDuration;
        desc.Transition.Properties = (uint)UITransitionPropertyFlagBits.Colors;

        using (OpenSelf())
        {
            var selected = SelectedItem;
            var style = Ui.TextStyle(0, !Enabled ? theme.TextDisabled : selected == null ? theme.TextMuted : theme.Text);
            style.Wrap = UITextWrap.None;
            style.Overflow = UITextOverflow.Ellipsis;
            Ui.Text(selected ?? Placeholder, in style);

            var caret = Ui.TextStyle(theme.SmallFontSize, Enabled ? theme.TextMuted : theme.TextDisabled);
            if (FontAwesome.IsInitialized)
            {
                caret.FontId = FontAwesome.FontId;
                Ui.Text(open ? FontAwesome.AngleUp : FontAwesome.AngleDown, in caret);
            }
            else
            {
                Ui.Text(open ? "^" : "v", in caret);
            }
        }
    }

    private void Pick(int index)
    {
        SelectedIndex = index;
        Close();
    }

    private sealed class DropdownList : Popup
    {
        private readonly Dropdown _owner;
        private readonly List<uint> _rowIds = new();

        public DropdownList(Dropdown owner)
        {
            _owner = owner;
            Layout.Width = UIValue.Percent(1);
            Layout.Padding = UILengthEdges.All(4);
            Desc.Scroll.OverflowY = UIOverflow.Scroll;
        }

        public int Highlight { get; set; }
        public bool Reveal { get; set; }

        protected internal override bool OnKeyDown(in KeyboardEventData key)
        {
            var count = _owner.Items.Count;
            switch (key.KeyCode)
            {
                case KeyCode.Up:
                    Move(Math.Max(0, Highlight - 1));
                    return true;
                case KeyCode.Down:
                    Move(Math.Min(count - 1, Highlight + 1));
                    return true;
                case KeyCode.Home:
                    Move(0);
                    return true;
                case KeyCode.End:
                    Move(count - 1);
                    return true;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                case KeyCode.Space:
                    _owner.Pick(Highlight);
                    return true;
                default:
                    return false;
            }
        }

        protected override void BuildChildren()
        {
            var theme = Ui.Theme;
            var items = _owner.Items;
            while (_rowIds.Count < items.Count)
            {
                _rowIds.Add(Ui.Id(Id, (uint)_rowIds.Count + 1));
            }

            Layout.MaxHeight = _owner.MaxListHeight;
            for (var i = 0; i < items.Count; i++)
            {
                var id = _rowIds[i];
                var interaction = Ui.Interaction(id);
                if (interaction.HoverStarted)
                {
                    Highlight = i;
                }

                if (interaction.Clicked)
                {
                    _owner.Pick(i);
                }

                var selected = i == _owner.SelectedIndex;
                var highlighted = i == Highlight;
                using (Ui.Row(id)
                           .AlignItems(UIAlign.Center)
                           .Padding(8, 6)
                           .Shrink(0)
                           .CornerRadius(theme.CornerRadius - 2)
                           .Background(selected ? theme.AccentSoft : highlighted ? theme.Hover : default)
                           .Cursor(SystemCursor.Pointer)
                           .Open())
                {
                    var style = Ui.TextStyle(0, selected ? theme.Accent : theme.Text);
                    style.Wrap = UITextWrap.None;
                    style.Overflow = UITextOverflow.Ellipsis;
                    Ui.Text(items[i], in style);
                }
            }

            if (Reveal && Highlight >= 0 && Highlight < items.Count)
            {
                Ui.ScrollIntoView(_rowIds[Highlight]);
                Reveal = false;
            }
        }

        private void Move(int index)
        {
            Highlight = index;
            Reveal = true;
        }
    }
}
