namespace NiziKit.UI.Widgets;

public class UiThemeData
{
    private UiStyle? _buttonStyle;
    private UiStyle? _primaryButtonStyle;
    private UiStyle? _dangerStyle;
    private UiStyle? _checkboxStyle;
    private SliderStyle? _sliderStyle;

    public int FontSize { get; set; } = 14;
    public int HeadingFontSize { get; set; } = 20;
    public int SmallFontSize { get; set; } = 12;
    public ushort FontId { get; set; }
    public ushort IconFontId { get; set; }

    public UiColor Text { get; set; } = UiColor.Rgb(235, 235, 240);
    public UiColor TextMuted { get; set; } = UiColor.Rgb(160, 165, 180);
    public UiColor Placeholder { get; set; } = UiColor.Rgb(120, 125, 140);

    public UiColor Background { get; set; } = UiColor.Rgb(24, 26, 32);
    public UiColor Surface { get; set; } = UiColor.Rgb(34, 37, 46);
    public UiColor SurfaceRaised { get; set; } = UiColor.Rgb(44, 48, 60);
    public UiColor Border { get; set; } = UiColor.Rgb(70, 76, 94);

    public UiColor Accent { get; set; } = UiColor.Rgb(88, 130, 240);
    public UiColor AccentHover { get; set; } = UiColor.Rgb(110, 150, 250);
    public UiColor AccentPressed { get; set; } = UiColor.Rgb(70, 108, 210);

    public UiColor ButtonBackground { get; set; } = UiColor.Rgb(58, 66, 86);
    public UiColor ButtonHover { get; set; } = UiColor.Rgb(74, 84, 108);
    public UiColor ButtonPressed { get; set; } = UiColor.Rgb(46, 52, 70);
    public UiColor ButtonDisabled { get; set; } = UiColor.Rgb(46, 50, 62);

    public UiColor InputBackground { get; set; } = UiColor.Rgb(28, 30, 38);
    public UiColor InputBorder { get; set; } = UiColor.Rgb(70, 76, 94);
    public UiColor InputFocusBorder { get; set; } = UiColor.Rgb(88, 130, 240);

    public UiColor TableHeader { get; set; } = UiColor.Rgb(44, 48, 60);
    public UiColor TableRow { get; set; } = UiColor.Rgba(0, 0, 0, 0);
    public UiColor TableRowAlternate { get; set; } = UiColor.Rgba(255, 255, 255, 8);
    public UiColor TableRowHover { get; set; } = UiColor.Rgba(255, 255, 255, 18);
    public UiColor TableRowSelected { get; set; } = UiColor.Rgba(88, 130, 240, 90);

    public UiColor Danger { get; set; } = UiColor.Rgb(220, 72, 72);
    public UiColor Success { get; set; } = UiColor.Rgb(72, 180, 110);
    public UiColor Warning { get; set; } = UiColor.Rgb(235, 170, 50);

    public float CornerRadius { get; set; } = 6;
    public float ControlHeight { get; set; } = 32;

    public Dictionary<string, UiStyle> Styles { get; } = new();

    public UiStyle ButtonStyle
    {
        get => _buttonStyle ??= UiStyle.Button(ButtonBackground, ButtonHover, ButtonPressed, Text);
        set => _buttonStyle = value;
    }

    public UiStyle PrimaryButtonStyle
    {
        get => _primaryButtonStyle ??= UiStyle.Button(Accent, AccentHover, AccentPressed);
        set => _primaryButtonStyle = value;
    }

    public UiStyle DangerStyle
    {
        get => _dangerStyle ??= UiStyle.Button(Danger);
        set => _dangerStyle = value;
    }

    public UiStyle CheckboxStyle
    {
        get => _checkboxStyle ??= UiStyle.Checkable(InputBackground, InputBorder, Accent, Text);
        set => _checkboxStyle = value;
    }

    public SliderStyle SliderStyle
    {
        get => _sliderStyle ??= new SliderStyle
        {
            Track = SurfaceRaised,
            Fill = Accent,
            FillDisabled = ButtonDisabled,
            Knob = TextMuted,
            KnobHover = Text,
            Focus = InputFocusBorder
        };
        set => _sliderStyle = value;
    }
}

public static class UiTheme
{
    private static UiThemeData _current = new();

    public static UiThemeData Current
    {
        get => _current;
        set
        {
            _current = value;
            Changed?.Invoke();
        }
    }

    public static event Action? Changed;

    public static void NotifyChanged()
    {
        Changed?.Invoke();
    }

    public static int FontSize => _current.FontSize;
    public static int HeadingFontSize => _current.HeadingFontSize;
    public static int SmallFontSize => _current.SmallFontSize;
    public static ushort FontId => _current.FontId;
    public static ushort IconFontId => _current.IconFontId;

    public static UiColor Text => _current.Text;
    public static UiColor TextMuted => _current.TextMuted;
    public static UiColor Placeholder => _current.Placeholder;

    public static UiColor Background => _current.Background;
    public static UiColor Surface => _current.Surface;
    public static UiColor SurfaceRaised => _current.SurfaceRaised;
    public static UiColor Border => _current.Border;

    public static UiColor Accent => _current.Accent;
    public static UiColor AccentHover => _current.AccentHover;
    public static UiColor AccentPressed => _current.AccentPressed;

    public static UiColor ButtonBackground => _current.ButtonBackground;
    public static UiColor ButtonHover => _current.ButtonHover;
    public static UiColor ButtonPressed => _current.ButtonPressed;
    public static UiColor ButtonDisabled => _current.ButtonDisabled;

    public static UiColor InputBackground => _current.InputBackground;
    public static UiColor InputBorder => _current.InputBorder;
    public static UiColor InputFocusBorder => _current.InputFocusBorder;

    public static UiColor TableHeader => _current.TableHeader;
    public static UiColor TableRow => _current.TableRow;
    public static UiColor TableRowAlternate => _current.TableRowAlternate;
    public static UiColor TableRowHover => _current.TableRowHover;
    public static UiColor TableRowSelected => _current.TableRowSelected;

    public static UiColor Danger => _current.Danger;
    public static UiColor Success => _current.Success;
    public static UiColor Warning => _current.Warning;

    public static float CornerRadius => _current.CornerRadius;
    public static float ControlHeight => _current.ControlHeight;

    public static UiStyle ButtonStyle => _current.ButtonStyle;
    public static UiStyle PrimaryButtonStyle => _current.PrimaryButtonStyle;
    public static UiStyle DangerStyle => _current.DangerStyle;
    public static UiStyle CheckboxStyle => _current.CheckboxStyle;
    public static SliderStyle SliderStyle => _current.SliderStyle;
}
