using NiziKit.UI.Widgets;
using VttDemo.Model;

namespace VttDemo.UiKit;

public enum ChipKind
{
    Normal,
    Accent,
    Danger,
    Subtle
}

/// <summary>Small immediate-mode building blocks shared by the views. Everything reads <see cref="Ui.Theme"/> per call, so theme switches apply at once.</summary>
public static class Im
{
    public static readonly UIColor White = UIColor.Hex(0xFFFFFFFF);
    public static readonly UIColor Good = UIColor.Hex(0x4CB860FF);
    public static readonly UIColor Warn = UIColor.Hex(0xE0A33AFF);
    public static readonly UIColor Bad = UIColor.Hex(0xD9484BFF);
    public static readonly UIColor TempHp = UIColor.Hex(0x5AA9E6FF);

    // An Immediate widget that fills its parent as a column; it bridges immediate drawing into TabView pages and windows.
    public static Immediate View(Action draw, bool scroll = false, float padding = 0, float gap = 0)
    {
        var declare = scroll
            ? () =>
            {
                using (Ui.Column().Fill().ScrollY().Padding(padding, padding, padding + 6, padding).Gap(gap).Open())
                {
                    draw();
                }
            }
            : draw;
        var view = new Immediate(declare) { Fill = true };
        ref var layout = ref view.Layout;
        layout.Direction = UIDirection.Column;
        if (!scroll)
        {
            layout.Padding = UILengthEdges.All(padding);
            layout.Gap = gap;
        }

        return view;
    }

    public static UIColor Alpha(UIColor color, byte alpha)
    {
        return new UIColor(color.R, color.G, color.B, alpha);
    }

    public static UIColor Mix(UIColor a, UIColor b, float t)
    {
        return new UIColor(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t),
            (byte)(a.A + (b.A - a.A) * t));
    }

    public static UIColor HpColor(float fraction)
    {
        return fraction > 0.5f ? Good : fraction > 0.25f ? Warn : Bad;
    }

    public static void Icon(string glyph, float size, UIColor color)
    {
        if (FontAwesome.IsInitialized)
        {
            Ui.Text(glyph, size, color, FontAwesome.FontId);
        }
    }

    /// <summary>A single line that ends with an ellipsis when it does not fit.</summary>
    public static void Line(ReadOnlySpan<char> text, float size, UIColor color)
    {
        var desc = Ui.TextStyle(size, color);
        desc.Wrap = UITextWrap.None;
        desc.Overflow = UITextOverflow.Ellipsis;
        Ui.Text(text, in desc);
    }

    /// <summary>Word-wrapped text; a line height of zero is the font's.</summary>
    public static void Para(ReadOnlySpan<char> text, float size, UIColor color, float lineHeight = 0)
    {
        var desc = Ui.TextStyle(size, color);
        desc.Wrap = UITextWrap.Words;
        desc.LineHeight = lineHeight;
        Ui.Text(text, in desc);
    }

    public static void Spacer()
    {
        Ui.Element().Fill().Leaf();
    }

    public static void SectionHeader(ReadOnlySpan<char> text)
    {
        var theme = Ui.Theme;
        using (Ui.Row().Gap(10).AlignItems(UIAlign.Center).Margin(0, 6, 0, 0).Shrink(0).Open())
        {
            var desc = Ui.TextStyle(11, theme.TextMuted);
            desc.Wrap = UITextWrap.None;
            desc.LetterSpacing = 0.8f;
            Ui.Text(text, in desc);
            Ui.Element().Fill().Height(1).Background(theme.Border).Leaf();
        }
    }

    public static void Badge(ReadOnlySpan<char> text, UIColor background, UIColor foreground)
    {
        using (Ui.Row().Padding(7, 2).Shrink(0).Background(background).CornerRadius(4).Open())
        {
            Line(text, 11, foreground);
        }
    }

    /// <summary>A compact clickable pill. Returns true on the frame it is clicked.</summary>
    public static bool Chip(uint id, string? icon, ReadOnlySpan<char> text, ChipKind kind = ChipKind.Normal, string? tooltip = null, bool enabled = true)
    {
        var theme = Ui.Theme;
        var interaction = Ui.Interaction(id);
        var hovered = interaction.Hovered && enabled;
        var pressed = interaction.Pressed && enabled;
        UIColor background, border, foreground;
        switch (kind)
        {
            case ChipKind.Accent:
                background = pressed ? theme.AccentPressed : hovered ? theme.AccentHover : theme.Accent;
                border = background;
                foreground = theme.OnAccent;
                break;
            case ChipKind.Danger:
                background = pressed ? theme.DangerPressed : hovered ? theme.DangerHover : Alpha(theme.Danger, 40);
                border = Alpha(theme.Danger, 140);
                foreground = hovered ? theme.OnAccent : theme.Danger;
                break;
            case ChipKind.Subtle:
                background = pressed ? theme.Pressed : hovered ? theme.Hover : default;
                border = default;
                foreground = hovered ? theme.Text : theme.TextMuted;
                break;
            default:
                background = pressed ? theme.Pressed : hovered ? theme.Hover : theme.Surface;
                border = hovered ? theme.Accent : theme.Border;
                foreground = hovered ? theme.Accent : theme.Text;
                break;
        }

        if (!enabled)
        {
            background = kind == ChipKind.Subtle ? default : theme.Disabled;
            border = default;
            foreground = theme.TextDisabled;
        }

        using (Ui.Row(id)
                   .AlignItems(UIAlign.Center)
                   .Gap(6)
                   .Padding(9, 4)
                   .Shrink(0)
                   .Background(background)
                   .Border(border, 1)
                   .CornerRadius(theme.CornerRadius)
                   .Pointer(UIPointerMode.Block)
                   .Cursor(enabled ? SystemCursor.Pointer : SystemCursor.NotAllowed)
                   .Disabled(!enabled)
                   .Animate()
                   .Open())
        {
            if (icon != null)
            {
                Icon(icon, 11, foreground);
            }

            if (!text.IsEmpty)
            {
                Line(text, 12, foreground);
            }
        }

        if (tooltip != null && interaction.Hovered)
        {
            Ui.Tooltip(id, tooltip);
        }

        return enabled && interaction.Clicked;
    }

    public static void HpBar(uint id, int hp, int max, int temp, float height)
    {
        var theme = Ui.Theme;
        var fraction = max > 0 ? Math.Clamp(hp / (float)max, 0, 1) : 0;
        var tempFraction = max > 0 ? Math.Clamp(temp / (float)max, 0, 1 - fraction) : 0;
        using (Ui.Row(id).Height(height).Shrink(0).Background(Alpha(theme.BorderStrong, 90)).CornerRadius(height * 0.5f).Clip().Open())
        {
            Ui.Element()
                .Width(UIValue.Percent(fraction))
                .Background(HpColor(fraction))
                .Animate(UITransitionPropertyFlagBits.Size | UITransitionPropertyFlagBits.BackgroundColor, 0.3f)
                .Leaf();
            Ui.Element()
                .Width(UIValue.Percent(tempFraction))
                .Background(TempHp)
                .Animate(UITransitionPropertyFlagBits.Size, 0.3f)
                .Leaf();
        }
    }

    public static void Portrait(Character character, float size, uint id = 0)
    {
        using (Ui.Row(id)
                   .Size(size, size)
                   .Shrink(0)
                   .Center()
                   .Background(character.Color)
                   .Border(Alpha(White, 70), Math.Max(1, size / 24))
                   .CornerRadius(size * 0.5f)
                   .Overlay(character.IsDown ? UIColor.Hex(0x000000A0) : default)
                   .Open())
        {
            var desc = Ui.TextStyle(size * 0.38f, White);
            desc.Wrap = UITextWrap.None;
            Ui.Text(character.Initials, in desc);
        }
    }

    /// <summary>A labelled number box; when <paramref name="rollable"/> it highlights on hover and returns true when clicked.</summary>
    public static bool StatTile(uint id, ReadOnlySpan<char> label, ReadOnlySpan<char> value, ReadOnlySpan<char> caption, float width, bool rollable, string? tooltip = null)
    {
        var theme = Ui.Theme;
        var interaction = Ui.Interaction(id);
        var hot = rollable && interaction.Hovered;
        using (Ui.Column(id)
                   .Width(width)
                   .Shrink(0)
                   .Padding(6, 7)
                   .Gap(2)
                   .AlignItems(UIAlign.Center)
                   .Background(hot ? theme.Hover : theme.SurfaceAlt)
                   .Border(hot ? theme.Accent : theme.Border, 1)
                   .CornerRadius(theme.CornerRadius)
                   .Cursor(rollable ? SystemCursor.Pointer : SystemCursor.Default)
                   .Animate()
                   .Open())
        {
            var small = Ui.TextStyle(10, theme.TextMuted);
            small.Wrap = UITextWrap.None;
            small.LetterSpacing = 0.6f;
            Ui.Text(label, in small);
            Line(value, 20, theme.Text);
            if (!caption.IsEmpty)
            {
                Line(caption, 12, hot ? theme.Accent : theme.TextMuted);
            }
        }

        if (tooltip != null && interaction.Hovered)
        {
            Ui.Tooltip(id, tooltip);
        }

        return rollable && interaction.Clicked;
    }

    public static void KeyValue(ReadOnlySpan<char> key, ReadOnlySpan<char> value)
    {
        var theme = Ui.Theme;
        using (Ui.Column().Gap(1).Shrink(0).MaxWidth(UIValue.Percent(1)).Open())
        {
            Line(key, 10, theme.TextMuted);
            Para(value, 12, theme.Text);
        }
    }

    /// <summary>A centered muted message for empty states.</summary>
    public static void Empty(string icon, ReadOnlySpan<char> title, ReadOnlySpan<char> detail)
    {
        var theme = Ui.Theme;
        using (Ui.Column().Fill().Center().Gap(8).Padding(24).Open())
        {
            Icon(icon, 28, theme.TextDisabled);
            Line(title, 15, theme.TextMuted);
            var desc = Ui.TextStyle(12, theme.TextDisabled);
            desc.Align = UITextAlign.Center;
            Ui.Text(detail, in desc);
        }
    }
}

/// <summary>Cached number strings so per-frame stat displays do not allocate.</summary>
public static class Fmt
{
    private const int SignedMin = -50;
    private static readonly string[] SignedCache = Enumerable.Range(SignedMin, 151).Select(i => i >= 0 ? $"+{i}" : $"-{-i}").ToArray();
    private static readonly string[] IntCache = Enumerable.Range(0, 1000).Select(i => i.ToString()).ToArray();

    public static string Sign(int value)
    {
        var index = value - SignedMin;
        return index >= 0 && index < SignedCache.Length ? SignedCache[index] : value >= 0 ? $"+{value}" : $"-{-value}";
    }

    public static string Int(int value)
    {
        return value >= 0 && value < IntCache.Length ? IntCache[value] : value.ToString();
    }
}
