using DenOfIz;

namespace NiziKit.UI.Widgets;

public class Dropdown : HStack
{
    private readonly Label _label;
    private readonly Label _caret;
    private readonly Popup _popup;
    private int _selectedIndex = -1;

    public Dropdown()
    {
        Width = 180;
        Height = 32;
        Padding = new UiThickness(10, 0);
        Gap = 8;
        CornerRadius = 6;
        Background = UiColor.Rgb(28, 30, 38);
        BorderColor = UiColor.Rgb(70, 76, 94);
        Focusable = true;
        _label = new Label { Wrap = false };
        _caret = new Label(FontAwesome.CaretDown) { Wrap = false, FontId = FontAwesome.FontId };
        Children.Add(_label);
        Children.Add(new Spacer());
        Children.Add(_caret);
        _popup = new Popup { Owner = this, Padding = 4, Gap = 2 };
    }

    public Dropdown(IEnumerable<string> items) : this()
    {
        Items.AddRange(items);
    }

    public List<string> Items { get; } = new();
    public string Placeholder { get; set; } = "Select...";
    public int FontSize { get; set; } = 14;
    public ushort FontId { get; set; } = UiFonts.DefaultFontId;
    public float MaxListHeight { get; set; } = 240;
    public float RowHeight { get; set; } = 28;
    public UiColor TextColor { get; set; } = UiColor.Rgb(235, 235, 240);
    public UiColor DisabledTextColor { get; set; } = UiColor.Rgb(160, 165, 180);
    public UiColor PlaceholderColor { get; set; } = UiColor.Rgb(120, 125, 140);
    public UiColor CaretColor { get; set; } = UiColor.Rgb(160, 165, 180);
    public UiColor HoverBackground { get; set; } = UiColor.Rgb(44, 48, 60);
    public UiColor PressedBackground { get; set; } = UiColor.Rgb(46, 52, 70);
    public UiColor DisabledBackground { get; set; } = UiColor.Rgb(46, 50, 62);
    public UiColor FocusBorderColor { get; set; } = UiColor.Rgb(88, 130, 240);
    public UiColor RowHoverBackground { get; set; } = UiColor.Rgb(110, 150, 250);
    public UiColor RowSelectedBackground { get; set; } = UiColor.Rgba(88, 130, 240, 90);

    public Popup Popup => _popup;

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

    public bool IsOpen => _popup.IsOpen;

    public Func<string, int, Widget>? ItemTemplate { get; set; }

    public event Action<Dropdown>? SelectionChanged;

    protected override bool TracksPointer => true;

    protected internal override bool IsKeyActivatable => true;

    public void Open()
    {
        if (!IsEnabled || Items.Count == 0)
        {
            return;
        }

        RebuildRows();
        var width = Ui.Clay.PixelsToPoints(Bounds.Width);
        _popup.Width = width > 0 ? UiSize.Fixed(width) : UiSize.Fit;
        _popup.Height = UiSize.Fit.WithMax(MaxListHeight);
        _popup.ScrollVertical = Items.Count * (RowHeight + _popup.Gap) > MaxListHeight;
        _popup.ClipChildren = _popup.ScrollVertical;
        _popup.Open(this);
    }

    public void Close()
    {
        _popup.Close();
    }

    private void RebuildRows()
    {
        _popup.Children.Clear();
        for (var i = 0; i < Items.Count; i++)
        {
            _popup.Children.Add(new DropdownRow(this, i));
        }
    }

    private void Select(int index)
    {
        SelectedIndex = index;
        Close();
        Focus();
    }

    protected override void OnClick(UiMouseButton button)
    {
        if (button != UiMouseButton.Left)
        {
            return;
        }

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
        if (!IsEnabled)
        {
            return false;
        }

        switch (key.KeyCode)
        {
            case KeyCode.Up:
                if (Items.Count > 0)
                {
                    SelectedIndex = _selectedIndex <= 0 ? 0 : _selectedIndex - 1;
                }

                return true;
            case KeyCode.Down:
                if (Items.Count > 0)
                {
                    SelectedIndex = Math.Min(Items.Count - 1, _selectedIndex + 1);
                }

                return true;
            case KeyCode.Escape when IsOpen:
                Close();
                return true;
            default:
                return false;
        }
    }

    protected override void ApplyDeclaration(ref ClayElementDeclaration decl)
    {
        base.ApplyDeclaration(ref decl);
        var selected = SelectedItem;
        _label.Text = selected ?? Placeholder;
        _label.FontSize = FontSize;
        _label.FontId = FontId;
        _label.Color = !IsEnabled ? DisabledTextColor : selected != null ? TextColor : PlaceholderColor;
        _caret.FontSize = FontSize;
        _caret.Color = IsEnabled ? CaretColor : DisabledTextColor;

        UiColor? color = !IsEnabled ? DisabledBackground : IsPressed ? PressedBackground : IsHovered || IsOpen ? HoverBackground : Background;
        if (color is { } background)
        {
            decl.BackgroundColor = background.ToClay();
        }

        UiColor? border = IsFocused || IsOpen ? FocusBorderColor : BorderColor;
        if (border is { } borderColor)
        {
            decl.Border.Color = borderColor.ToClay();
            decl.Border.Width = ClayBorderWidth.CreateUniform(BorderWidth);
        }
    }

    private sealed class DropdownRow : Widget
    {
        private readonly Dropdown _owner;
        private readonly int _index;
        private readonly Widget? _content;

        public DropdownRow(Dropdown owner, int index)
        {
            _owner = owner;
            _index = index;
            if (owner.ItemTemplate != null && index < owner.Items.Count)
            {
                _content = owner.ItemTemplate(owner.Items[index], index);
                _content.Parent = this;
            }
            Width = UiSize.Grow;
            Height = owner.RowHeight;
            Padding = new UiThickness(8, 0);
            CornerRadius = 4;
        }

        protected override bool TracksPointer => true;

        protected override void OnClick(UiMouseButton button)
        {
            if (button == UiMouseButton.Left)
            {
                _owner.Select(_index);
            }
        }

        protected override void ApplyDeclaration(ref ClayElementDeclaration decl)
        {
            base.ApplyDeclaration(ref decl);
            decl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
            UiColor? color = IsHovered ? _owner.RowHoverBackground : _index == _owner._selectedIndex ? _owner.RowSelectedBackground : null;
            if (color is { } background)
            {
                decl.BackgroundColor = background.ToClay();
            }
        }

        protected override void CollectChildren(List<Widget> frame)
        {
            _content?.CollectFrame(frame);
        }

        protected override void BuildContent()
        {
            if (_index >= _owner.Items.Count)
            {
                return;
            }

            if (_content != null)
            {
                _content.Build();
                return;
            }

            var desc = ClayTextDesc.Default();
            desc.TextColor = _owner.TextColor.ToClay();
            desc.FontId = _owner.FontId;
            desc.FontSize = (ushort)_owner.FontSize;
            desc.WrapMode = ClayTextWrapMode.None;
            Ui.Clay.Text(_owner.Items[_index], in desc);
        }
    }
}
