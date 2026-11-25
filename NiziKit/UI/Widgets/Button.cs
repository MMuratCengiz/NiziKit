using DenOfIz;

namespace NiziKit.UI.Widgets;

public enum ButtonKind
{
    Secondary,
    Primary,
    Ghost,
    Danger
}

public class Button : Widget
{
    public Button()
    {
        ref var layout = ref Desc.Layout;
        layout.Justify = UIJustify.Center;
        layout.AlignItems = UIAlign.Center;
        layout.Gap = 8;
        layout.Padding = UILengthEdges.Symmetric(14, 7);
        Desc.Style.BorderWidth = UIEdges.All(1);
    }

    public Button(string text) : this()
    {
        Text = text;
    }

    public Button(string text, Action onClick) : this(text)
    {
        Clicked += _ => onClick();
    }

    public string Text { get; set; } = "";

    public string? Icon { get; set; }

    public ButtonKind Kind { get; set; }

    public bool Checked { get; set; }

    public float FontSize { get; set; }

    protected override bool Focusable => true;

    protected override void OnBuild()
    {
        var theme = Ui.Theme;
        var hovered = IsHovered;
        var pressed = IsPressed;
        UIColor background;
        UIColor border;
        UIColor text;
        switch (Kind)
        {
            case ButtonKind.Primary:
                background = pressed ? theme.AccentPressed : hovered ? theme.AccentHover : theme.Accent;
                border = background;
                text = theme.OnAccent;
                break;
            case ButtonKind.Danger:
                background = pressed ? theme.DangerPressed : hovered ? theme.DangerHover : theme.Danger;
                border = background;
                text = theme.OnAccent;
                break;
            case ButtonKind.Ghost:
                background = pressed ? theme.Pressed : hovered ? theme.Hover : default;
                border = default;
                text = theme.Text;
                break;
            default:
                background = pressed ? theme.Pressed : hovered ? theme.Hover : theme.Surface;
                border = hovered ? theme.BorderStrong : theme.Border;
                text = theme.Text;
                break;
        }

        if (Checked)
        {
            background = pressed ? theme.Pressed : theme.AccentSoft;
            border = theme.Accent;
            text = Kind is ButtonKind.Primary or ButtonKind.Danger ? text : theme.Accent;
        }

        if (!Enabled)
        {
            background = Kind == ButtonKind.Ghost ? default : theme.Disabled;
            border = Kind == ButtonKind.Ghost ? default : theme.Disabled;
            text = theme.TextDisabled;
        }

        if (IsFocused && Ui.FocusVisible)
        {
            border = theme.FocusRing;
        }

        ref var desc = ref Desc;
        desc.Cursor = Enabled ? SystemCursor.Pointer : SystemCursor.NotAllowed;
        desc.Style.BackgroundColor = background;
        desc.Style.BorderColor = border;
        desc.Style.CornerRadius = UICorners.All(theme.CornerRadius);
        desc.Transition.Duration = theme.TransitionDuration;
        desc.Transition.Properties = (uint)UITransitionPropertyFlagBits.Colors;

        using (OpenSelf())
        {
            var style = Ui.TextStyle(FontSize, text);
            style.Wrap = UITextWrap.None;
            if (Icon != null && FontAwesome.IsInitialized)
            {
                var icon = style;
                icon.FontId = FontAwesome.FontId;
                Ui.Text(Icon, in icon);
            }

            if (Text.Length > 0)
            {
                Ui.Text(Text, in style);
            }
        }
    }
}
