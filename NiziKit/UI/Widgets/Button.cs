using DenOfIz;

namespace NiziKit.UI.Widgets;

public class Button : StackPanel
{
    private Label? _label;
    private Label? _icon;

    public Button() : base(UiOrientation.Horizontal)
    {
        Height = UiTheme.ControlHeight;
        Padding = new UiThickness(14, 0);
        Gap = 8;
        AlignX = UiAlign.Center;
        AlignY = UiAlign.Center;
        CornerRadius = UiTheme.CornerRadius;
        Focusable = true;
    }

    public Button(string text) : this()
    {
        Text = text;
    }

    public Button(string text, Action onClick) : this(text)
    {
        Clicked += _ => onClick();
    }

    public string Text
    {
        get => _label?.Text ?? "";
        set
        {
            if (_label == null)
            {
                _label = new Label { Wrap = false };
                Children.Insert(_icon != null ? 1 : 0, _label);
            }

            _label.Text = value;
        }
    }

    public string? Icon
    {
        get => _icon?.Text;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                if (_icon != null)
                {
                    Children.Remove(_icon);
                    _icon = null;
                }

                return;
            }

            if (_icon == null)
            {
                _icon = new Label { Wrap = false, FontSize = _label?.FontSize ?? 0 };
                Children.Insert(0, _icon);
            }

            _icon.Text = value;
        }
    }

    public int FontSize
    {
        get => _label?.FontSize ?? _icon?.FontSize ?? 0;
        set
        {
            if (_label != null)
            {
                _label.FontSize = value;
            }

            if (_icon != null)
            {
                _icon.FontSize = value;
            }
        }
    }

    public ushort? IconFontId { get; set; }
    public UiColor? TextColor { get; set; }
    public UiColor? NormalBackground { get; init; }
    public UiColor? HoverBackground { get; init; }
    public UiColor? PressedBackground { get; init; }
    public UiColor? DisabledBackground { get; set; }
    public bool Primary { get; set; }
    public bool Danger { get; init; }
    public UiStyle? Style { get; init; }
    public bool AnimateHover { get; set; } = true;

    protected override bool TracksPointer => true;

    protected internal override bool IsKeyActivatable => true;

    protected UiStyle ResolvedStyle => Style ?? (Danger ? UiTheme.DangerStyle : Primary ? UiTheme.PrimaryButtonStyle : UiTheme.ButtonStyle);

    protected override void ApplyDeclaration(ref ClayElementDeclaration decl)
    {
        base.ApplyDeclaration(ref decl);
        var state = ResolvedStyle.Resolve(this);

        var background = !IsEnabled ? DisabledBackground : IsPressed ? PressedBackground : IsHovered ? HoverBackground : NormalBackground;
        decl.BackgroundColor = (background ?? state.Background ?? UiTheme.ButtonBackground).ToClay();

        if (state.CornerRadius is { } radius)
        {
            decl.BorderRadius = ClayBorderRadius.CreateUniform(radius);
        }

        if (BorderColor == null)
        {
            var border = state.Border ?? (IsFocused ? UiTheme.InputFocusBorder : null);
            if (border is { } borderColor)
            {
                decl.Border.Color = borderColor.ToClay();
                decl.Border.Width = ClayBorderWidth.CreateUniform(state.BorderWidth ?? 1);
            }
        }

        if (AnimateHover && Transition == null)
        {
            decl.Transition = UiStyle.HoverTransition;
        }

        var text = TextColor ?? state.Text ?? (IsEnabled ? UiTheme.Text : UiTheme.TextMuted);
        if (_label != null)
        {
            _label.Color = text;
        }

        if (_icon != null)
        {
            _icon.Color = text;
            _icon.FontId = IconFontId ?? UiTheme.IconFontId;
        }
    }
}
