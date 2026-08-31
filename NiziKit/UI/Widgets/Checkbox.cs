using DenOfIz;

namespace NiziKit.UI.Widgets;

public class Checkbox : HStack
{
    private readonly CheckBoxMark _box;
    private readonly Label _label;
    private bool _checked;

    public Checkbox()
    {
        Gap = 8;
        Height = 32;
        Focusable = true;
        _box = new CheckBoxMark(this);
        _box.Style = new Style
        {
            Normal = new StyleState { Background = Color.Rgb(28, 30, 38), Border = Color.Rgb(70, 76, 94), BorderWidth = 1, Text = Color.Rgb(235, 235, 240) },
            Hover = new StyleState { Border = Color.Rgb(113, 149, 242) },
            Checked = new StyleState { Background = Color.Rgb(88, 130, 240), Border = Color.Rgb(88, 130, 240) },
            Disabled = new StyleState { Background = Color.Rgb(46, 50, 62), Border = Color.Rgb(46, 50, 62), Text = Color.Rgb(160, 165, 180) },
            Focused = new StyleState { Border = Color.Rgb(88, 130, 240), BorderWidth = 2 }
        };
        _label = new Label { Wrap = false };
        Children.Add(_box);
        Children.Add(_label);
    }

    public Checkbox(string text) : this()
    {
        Text = text;
    }

    public Checkbox(string text, bool isChecked) : this(text)
    {
        _checked = isChecked;
    }

    public string Text
    {
        get => _label.Text;
        set => _label.Text = value;
    }

    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value)
            {
                return;
            }

            _checked = value;
            Changed?.Invoke(this);
        }
    }

    public float BoxSize { get; set; } = 18;

    public int FontSize
    {
        get => _label.FontSize;
        set => _label.FontSize = value;
    }

    public ushort FontId
    {
        get => _label.FontId;
        set => _label.FontId = value;
    }

    public Style BoxStyle
    {
        get => _box.Style;
        set => _box.Style = value;
    }

    public bool AnimateHover { get; set; } = true;

    public event Action<Checkbox>? Changed;

    protected override bool TracksPointer => true;

    protected internal override bool IsKeyActivatable => true;

    protected override void OnClick(MouseButton button)
    {
        if (button == MouseButton.Left)
        {
            Checked = !Checked;
        }
    }

    protected override void ApplyDeclaration(ref ClayElementDeclaration decl)
    {
        base.ApplyDeclaration(ref decl);
        if (BoxStyle.Resolve(this, Checked).Text is { } text)
        {
            _label.Color = text;
        }
    }

    private sealed class CheckBoxMark : Widget
    {
        private readonly Checkbox _owner;
        private Color _markColor;

        public CheckBoxMark(Checkbox owner)
        {
            _owner = owner;
            CornerRadius = 4;
        }

        protected override Widget StyleSource => _owner;

        protected override bool IsChecked => _owner.Checked;

        protected override void OnStyleResolved(in StyleState state)
        {
            _markColor = state.Text ?? Color.Rgb(235, 235, 240);
        }

        protected override void ApplyDeclaration(ref ClayElementDeclaration decl)
        {
            Width = _owner.BoxSize;
            Height = _owner.BoxSize;
            base.ApplyDeclaration(ref decl);

            if (_owner.AnimateHover)
            {
                decl.Transition = Style.HoverTransition;
            }

            decl.Layout.ChildAlignment.X = ClayAlignmentX.Center;
            decl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
        }

        protected override void BuildContent()
        {
            if (!_owner.Checked)
            {
                return;
            }

            var mark = ClayElementDeclaration.Default();
            var size = _owner.BoxSize * 0.5f;
            mark.Layout.Sizing.Width = ClaySizingAxis.Fixed(size);
            mark.Layout.Sizing.Height = ClaySizingAxis.Fixed(size);
            mark.BackgroundColor = _markColor.ToClay();
            mark.BorderRadius = ClayBorderRadius.CreateUniform(2);
            Ui.Clay.OpenElement(in mark);
            Ui.Clay.CloseElement();
        }
    }
}
