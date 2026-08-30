using DenOfIz;

namespace NiziKit.UI.Widgets;

public struct UiStyleState
{
    public UiColor? Background;
    public UiColor? Border;
    public UiColor? Text;
    public float? BorderWidth;
    public float? CornerRadius;

    public void Layer(in UiStyleState over)
    {
        if (over.Background.HasValue)
        {
            Background = over.Background;
        }

        if (over.Border.HasValue)
        {
            Border = over.Border;
        }

        if (over.Text.HasValue)
        {
            Text = over.Text;
        }

        if (over.BorderWidth.HasValue)
        {
            BorderWidth = over.BorderWidth;
        }

        if (over.CornerRadius.HasValue)
        {
            CornerRadius = over.CornerRadius;
        }
    }

    public void Apply(ref ClayElementDeclaration decl)
    {
        if (Background is { } background)
        {
            decl.BackgroundColor = background.ToClay();
        }

        if (CornerRadius is { } radius)
        {
            decl.BorderRadius = ClayBorderRadius.CreateUniform(radius);
        }

        if (Border is { } border)
        {
            decl.Border.Color = border.ToClay();
            decl.Border.Width = ClayBorderWidth.CreateUniform(BorderWidth ?? 1);
        }
    }
}

public sealed class UiStyle
{
    private static ClayTransitionDesc? _hoverTransition;

    public UiStyleState Normal;
    public UiStyleState Hover;
    public UiStyleState Pressed;
    public UiStyleState Disabled;
    public UiStyleState Focused;
    public UiStyleState Checked;

    public static ClayTransitionDesc HoverTransition => _hoverTransition ??= CreateHoverTransition(0.12f);

    public static ClayTransitionDesc CreateHoverTransition(float duration, ClayTransitionEasing easing = ClayTransitionEasing.EaseOut)
    {
        var desc = ClayTransitionDesc.Create(duration, ClayTransitionPropertyFlagBits.BackgroundColor | ClayTransitionPropertyFlagBits.Border, easing);
        desc.InteractionHandling = ClayTransitionInteractionHandling.AllowWhileMoving;
        return desc;
    }

    public UiStyleState Resolve(Widget w, bool isChecked = false)
    {
        var result = Normal;
        if (isChecked)
        {
            result.Layer(in Checked);
        }

        if (w.IsFocused)
        {
            result.Layer(in Focused);
        }

        if (w.IsHovered)
        {
            result.Layer(in Hover);
        }

        if (w.IsPressed)
        {
            result.Layer(in Pressed);
        }

        if (!w.IsEnabled)
        {
            result.Layer(in Disabled);
        }

        return result;
    }

    public static UiStyle Button(UiColor baseColor, UiColor? textColor = null)
    {
        return Button(baseColor, baseColor.Lighten(0.12f), baseColor.Darken(0.15f), textColor);
    }

    public static UiStyle Button(UiColor normal, UiColor hover, UiColor pressed, UiColor? textColor = null)
    {
        return new UiStyle
        {
            Normal = new UiStyleState { Background = normal, Text = textColor ?? normal.ContrastText() },
            Hover = new UiStyleState { Background = hover },
            Pressed = new UiStyleState { Background = pressed },
            Disabled = new UiStyleState { Background = UiTheme.ButtonDisabled, Text = UiTheme.TextMuted },
            Focused = new UiStyleState { Border = UiTheme.InputFocusBorder, BorderWidth = 1 }
        };
    }

    public static UiStyle Checkable(UiColor background, UiColor border, UiColor accent, UiColor mark)
    {
        return new UiStyle
        {
            Normal = new UiStyleState { Background = background, Border = border, BorderWidth = 1, Text = mark },
            Hover = new UiStyleState { Border = accent.Lighten(0.15f) },
            Checked = new UiStyleState { Background = accent, Border = accent },
            Disabled = new UiStyleState { Background = UiTheme.ButtonDisabled, Border = UiTheme.ButtonDisabled, Text = UiTheme.TextMuted },
            Focused = new UiStyleState { Border = UiTheme.InputFocusBorder, BorderWidth = 2 }
        };
    }
}

public sealed class SliderStyle
{
    public UiColor? Track;
    public UiColor? Fill;
    public UiColor? FillDisabled;
    public UiColor? Knob;
    public UiColor? KnobHover;
    public UiColor? Focus;
}

public static class UiColorExtensions
{
    public static UiColor Mix(this UiColor color, UiColor other, float t)
    {
        t = Math.Clamp(t, 0, 1);
        return new UiColor(
            (byte)MathF.Round(color.R + (other.R - color.R) * t),
            (byte)MathF.Round(color.G + (other.G - color.G) * t),
            (byte)MathF.Round(color.B + (other.B - color.B) * t),
            color.A);
    }

    public static UiColor Lighten(this UiColor color, float amount)
    {
        return color.Mix(UiColor.Rgb(255, 255, 255), amount);
    }

    public static UiColor Darken(this UiColor color, float amount)
    {
        return color.Mix(UiColor.Rgb(0, 0, 0), amount);
    }

    public static float Luminance(this UiColor color)
    {
        return (0.2126f * color.R + 0.7152f * color.G + 0.0722f * color.B) / 255f;
    }

    public static UiColor ContrastText(this UiColor color)
    {
        return color.Luminance() > 0.55f ? UiColor.Rgb(28, 30, 36) : UiColor.Rgb(245, 245, 250);
    }
}
