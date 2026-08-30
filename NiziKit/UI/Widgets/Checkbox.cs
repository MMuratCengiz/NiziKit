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
        Height = UiTheme.ControlHeight;
        Focusable = true;
        _box = new CheckBoxMark(this);
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
    public UiColor? TextColor { get; set; }
    public UiStyle? Style { get; set; }
    public bool AnimateHover { get; set; } = true;

    public event Action<Checkbox>? Changed;

    protected override bool TracksPointer => true;

    protected internal override bool IsKeyActivatable => true;

    protected override void OnClick(UiMouseButton button)
    {
        if (button == UiMouseButton.Left)
        {
            Checked = !Checked;
        }
    }

    protected override void ApplyDeclaration(ref ClayElementDeclaration decl)
    {
        base.ApplyDeclaration(ref decl);
        _label.Color = TextColor ?? (IsEnabled ? UiTheme.Text : UiTheme.TextMuted);
    }

    private sealed class CheckBoxMark : Widget
    {
        private readonly Checkbox _owner;
        private UiColor _markColor;

        public CheckBoxMark(Checkbox owner)
        {
            _owner = owner;
            CornerRadius = 4;
        }

        protected override void ApplyDeclaration(ref ClayElementDeclaration decl)
        {
            Width = _owner.BoxSize;
            Height = _owner.BoxSize;
            base.ApplyDeclaration(ref decl);

            var state = (_owner.Style ?? UiTheme.CheckboxStyle).Resolve(_owner, _owner.Checked);
            decl.BackgroundColor = (state.Background ?? UiTheme.InputBackground).ToClay();
            decl.Border.Color = (state.Border ?? UiTheme.InputBorder).ToClay();
            decl.Border.Width = ClayBorderWidth.CreateUniform(state.BorderWidth ?? 1);
            if (state.CornerRadius is { } radius)
            {
                decl.BorderRadius = ClayBorderRadius.CreateUniform(radius);
            }

            if (_owner.AnimateHover)
            {
                decl.Transition = UiStyle.HoverTransition;
            }

            decl.Layout.ChildAlignment.X = ClayAlignmentX.Center;
            decl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
            _markColor = state.Text ?? UiTheme.Text;
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
