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
        Padding = new Thickness(10, 0);
        Gap = 8;
        CornerRadius = 6;
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
    public ushort FontId { get; set; } = Fonts.DefaultFontId;
    public float MaxListHeight { get; set; } = 240;
    public float RowHeight { get; set; } = 28;

    /// <summary>Color of the <see cref="Placeholder"/> text. Having no selection is not an interaction state, so it sits outside <see cref="Style"/>.</summary>
    public Color PlaceholderColor { get; set; } = Color.Rgb(120, 125, 140);

    /// <summary>Color of the caret glyph, which is a sub-element rather than a state of the box.</summary>
    public Color CaretColor { get; set; } = Color.Rgb(160, 165, 180);

    /// <summary>Theming for the closed box. An open dropdown resolves as hovered and focused.</summary>
    public Style Style { get; set; } = new()
    {
        Normal = new StyleState { Background = Color.Rgb(28, 30, 38), Border = Color.Rgb(70, 76, 94), BorderWidth = 1, Text = Color.Rgb(235, 235, 240) },
        Hover = new StyleState { Background = Color.Rgb(44, 48, 60) },
        Pressed = new StyleState { Background = Color.Rgb(46, 52, 70) },
        Disabled = new StyleState { Background = Color.Rgb(46, 50, 62), Text = Color.Rgb(160, 165, 180) },
        Focused = new StyleState { Border = Color.Rgb(88, 130, 240), BorderWidth = 1 }
    };

    /// <summary>Theming for the list rows. The selected row resolves through <see cref="Widgets.Style.Checked"/>.</summary>
    public Style RowStyle { get; set; } = new()
    {
        Normal = new StyleState { Text = Color.Rgb(235, 235, 240) },
        Checked = new StyleState { Background = Color.Rgba(88, 130, 240, 90) },
        Hover = new StyleState { Background = Color.Rgb(110, 150, 250) },
        Disabled = new StyleState { Text = Color.Rgb(160, 165, 180) }
    };

    public Popup Popup => _popup;
    public Label TextLabel => _label;
    public Label Caret => _caret;

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

    /// <summary>Builds the widget for a row. Rows built here are left entirely to the caller; <see cref="RowStyle"/> only paints the row background.</summary>
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
        _popup.Width = width > 0 ? Sizing.Fixed(width) : Sizing.Fit;
        _popup.Height = Sizing.Fit.WithMax(MaxListHeight);
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

    protected override void OnClick(MouseButton button)
    {
        if (button != MouseButton.Left)
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

        var state = Style.Resolve(this);
        if (IsOpen)
        {
            state.Layer(in Style.Hover);
            state.Layer(in Style.Focused);
        }

        state.Apply(this, ref decl);

        var selected = SelectedItem;
        _label.Text = selected ?? Placeholder;
        _label.FontSize = FontSize;
        _label.FontId = FontId;
        _label.Color = selected == null && IsEnabled ? PlaceholderColor : state.Text ?? PlaceholderColor;
        _caret.FontSize = FontSize;
        _caret.Color = IsEnabled ? CaretColor : state.Text ?? CaretColor;
    }

    private sealed class DropdownRow : Widget
    {
        private readonly Dropdown _owner;
        private readonly int _index;
        private readonly Widget _content;
        private readonly Label? _label;

        public DropdownRow(Dropdown owner, int index)
        {
            _owner = owner;
            _index = index;
            var item = owner.Items[index];
            if (owner.ItemTemplate != null)
            {
                _content = owner.ItemTemplate(item, index);
            }
            else
            {
                _label = new Label(item) { Wrap = false, FontSize = owner.FontSize, FontId = owner.FontId };
                _content = _label;
            }

            _content.Parent = this;
            Width = Sizing.Grow;
            Height = owner.RowHeight;
            Padding = new Thickness(8, 0);
            CornerRadius = 4;
        }

        protected override bool TracksPointer => true;

        protected override void OnClick(MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                _owner.Select(_index);
            }
        }

        protected override void ApplyDeclaration(ref ClayElementDeclaration decl)
        {
            base.ApplyDeclaration(ref decl);
            decl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;

            var state = _owner.RowStyle.Resolve(this, _index == _owner.SelectedIndex);
            state.Apply(this, ref decl);
            if (_label != null && state.Text is { } text)
            {
                _label.Color = text;
            }
        }

        protected override void CollectChildren(List<Widget> frame)
        {
            _content.CollectFrame(frame);
        }

        protected override void BuildContent()
        {
            _content.Build();
        }
    }
}
