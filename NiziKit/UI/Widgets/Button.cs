using DenOfIz;

namespace NiziKit.UI.Widgets;

public class Button : StackPanel
{
    private Label? _label;
    private int _fontSize = 14;
    private ushort _fontId = Fonts.DefaultFontId;

    public Button() : base(Orientation.Horizontal)
    {
        Style = new Style
        {
            Normal = new StyleState { Background = Color.Rgb(58, 66, 86), Text = Color.Rgb(235, 235, 240) },
            Hover = new StyleState { Background = Color.Rgb(74, 84, 108) },
            Pressed = new StyleState { Background = Color.Rgb(46, 52, 70) },
            Checked = new StyleState { Background = Color.Rgb(88, 130, 240) },
            Disabled = new StyleState { Background = Color.Rgb(46, 50, 62), Text = Color.Rgb(160, 165, 180) },
            Focused = new StyleState { Border = Color.Rgb(88, 130, 240), BorderWidth = 1 }
        };
        Height = 32;
        Padding = new Thickness(14, 0);
        Gap = 8;
        AlignX = Align.Center;
        AlignY = Align.Center;
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

    public bool AnimateHover { get; set; } = true;

    public bool Checked { get; set; }

    protected override bool IsChecked => Checked;

    protected override bool TracksPointer => true;

    protected internal override bool IsKeyActivatable => true;

    protected override void ApplyDeclaration(ref ClayElementDeclaration decl)
    {
        base.ApplyDeclaration(ref decl);
        if (AnimateHover && Transition == null)
        {
            decl.Transition = Style.HoverTransition;
        }
    }

    protected override void OnStyleResolved(in StyleState state)
    {
        if (_label != null && state.Text is { } text)
        {
            _label.Color = text;
        }
    }
}
