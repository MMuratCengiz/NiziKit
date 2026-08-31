using DenOfIz;

namespace NiziKit.UI.Widgets;

public struct StyleState
{
    public Color? Background;
    public Color? Border;
    public Color? Text;
    public BorderThickness? BorderWidth;
    public CornerRadius? CornerRadius;

    public void Layer(in StyleState over)
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

    /// <summary>
    /// Writes the resolved state onto the declaration. Whatever the state sets wins over the
    /// widget's own <see cref="Widget.Background"/>, <see cref="Widget.BorderColor"/> and
    /// <see cref="Widget.CornerRadius"/> whatever it leaves unset falls through to them.
    /// </summary>
    public void Apply(ref ClayElementDeclaration decl, float betweenChildren = 0)
    {
        if (Background is { } background)
        {
            decl.BackgroundColor = background.ToClay();
        }

        if (CornerRadius is { } radius)
        {
            decl.BorderRadius = radius.ToClay();
        }

        if (Border is { } border)
        {
            decl.Border.Color = border.ToClay();
            decl.Border.Width = (BorderWidth ?? new BorderThickness(1)).ToClay(betweenChildren);
        }
        else if (BorderWidth is { } width)
        {
            decl.Border.Width = width.ToClay(betweenChildren);
        }
    }
}

public sealed class Style
{
    private static ClayTransitionDesc? _hoverTransition;

    public StyleState Normal;
    public StyleState Hover;
    public StyleState Pressed;
    public StyleState Disabled;
    public StyleState Focused;
    public StyleState Checked;

    public static ClayTransitionDesc HoverTransition => _hoverTransition ??= CreateHoverTransition(0.12f);

    public static ClayTransitionDesc CreateHoverTransition(float duration, ClayTransitionEasing easing = ClayTransitionEasing.EaseOut)
    {
        var desc = ClayTransitionDesc.Create(duration, ClayTransitionPropertyFlagBits.BackgroundColor | ClayTransitionPropertyFlagBits.Border, easing);
        desc.InteractionHandling = ClayTransitionInteractionHandling.AllowWhileMoving;
        return desc;
    }

    public StyleState Resolve(Widget w, bool isChecked = false)
    {
        var result = Normal;
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

        if (isChecked)
        {
            result.Layer(in Checked);
        }

        if (!w.IsEnabled)
        {
            result.Layer(in Disabled);
        }

        return result;
    }
}

/// <summary>Colors for a slider's sub-parts. Sliders draw three elements, so they take part colors rather than the interaction states of <see cref="Style"/>.</summary>
public sealed class SliderStyle
{
    public Color Track = Color.Rgb(44, 48, 60);
    public Color Fill = Color.Rgb(88, 130, 240);
    public Color FillDisabled = Color.Rgb(46, 50, 62);
    public Color Knob = Color.Rgb(160, 165, 180);
    public Color KnobHover = Color.Rgb(235, 235, 240);
    public Color Focus = Color.Rgb(88, 130, 240);
}

public static class ColorExtensions
{
    public static Color Mix(this Color color, Color other, float t)
    {
        t = Math.Clamp(t, 0, 1);
        return new Color(
            (byte)MathF.Round(color.R + (other.R - color.R) * t),
            (byte)MathF.Round(color.G + (other.G - color.G) * t),
            (byte)MathF.Round(color.B + (other.B - color.B) * t),
            color.A);
    }

    public static Color Lighten(this Color color, float amount)
    {
        return color.Mix(Color.Rgb(255, 255, 255), amount);
    }

    public static Color Darken(this Color color, float amount)
    {
        return color.Mix(Color.Rgb(0, 0, 0), amount);
    }

    public static float Luminance(this Color color)
    {
        return (0.2126f * color.R + 0.7152f * color.G + 0.0722f * color.B) / 255f;
    }

    public static Color ContrastText(this Color color)
    {
        return color.Luminance() > 0.55f ? Color.Rgb(28, 30, 36) : Color.Rgb(245, 245, 250);
    }
}
