using DenOfIz;

namespace NiziKit.UI.Widgets;

public class Button : StackPanel
{
    private Label? _label;
    private int _fontSize = 14;
    private ushort _fontId = UiFonts.DefaultFontId;

    public Button() : base(UiOrientation.Horizontal)
    {
        Height = 32;
        Padding = new UiThickness(14, 0);
        Gap = 8;
        AlignX = UiAlign.Center;
        AlignY = UiAlign.Center;
        CornerRadius = 6;
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
                _label = new Label { Wrap = false, FontSize = _fontSize, FontId = _fontId };
                Children.Add(_label);
            }

            _label.Text = value;
        }
    }

    public int FontSize
    {
        get => _fontSize;
        set
        {
            _fontSize = value;
            if (_label != null)
            {
                _label.FontSize = value;
            }
        }
    }

    public ushort FontId
    {
        get => _fontId;
        set
        {
            _fontId = value;
            if (_label != null)
            {
                _label.FontId = value;
            }
        }
    }

    public Label? TextLabel => _label;

    public UiColor? TextColor { get; set; }
    public UiColor? NormalBackground { get; init; }
    public UiColor? HoverBackground { get; init; }
    public UiColor? PressedBackground { get; init; }
    public UiColor? DisabledBackground { get; set; }
    public bool AnimateHover { get; set; } = true;

    public UiStyle Style { get; set; } = new()
    {
        Normal = new UiStyleState { Background = UiColor.Rgb(58, 66, 86), Text = UiColor.Rgb(235, 235, 240) },
        Hover = new UiStyleState { Background = UiColor.Rgb(74, 84, 108) },
        Pressed = new UiStyleState { Background = UiColor.Rgb(46, 52, 70) },
        Disabled = new UiStyleState { Background = UiColor.Rgb(46, 50, 62), Text = UiColor.Rgb(160, 165, 180) },
        Focused = new UiStyleState { Border = UiColor.Rgb(88, 130, 240), BorderWidth = 1 }
    };

    protected override bool TracksPointer => true;

    protected internal override bool IsKeyActivatable => true;

    protected override void ApplyDeclaration(ref ClayElementDeclaration decl)
    {
        base.ApplyDeclaration(ref decl);
        var state = Style.Resolve(this);

        var background = !IsEnabled ? DisabledBackground : IsPressed ? PressedBackground : IsHovered ? HoverBackground : NormalBackground;
        if ((background ?? state.Background) is { } backgroundColor)
        {
            decl.BackgroundColor = backgroundColor.ToClay();
        }

        if (state.CornerRadius is { } radius)
        {
            decl.BorderRadius = ClayBorderRadius.CreateUniform(radius);
        }

        if (BorderColor == null && state.Border is { } borderColor)
        {
            decl.Border.Color = borderColor.ToClay();
            decl.Border.Width = ClayBorderWidth.CreateUniform(state.BorderWidth ?? 1);
        }

        if (AnimateHover && Transition == null)
        {
            decl.Transition = UiStyle.HoverTransition;
        }

        if (_label != null && (TextColor ?? state.Text) is { } text)
        {
            _label.Color = text;
        }
    }
}
