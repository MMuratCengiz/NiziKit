using DenOfIz;

namespace NiziKit.UI;

public sealed class Theme
{
    public UIColor Background { get; set; }
    public UIColor Surface { get; set; }
    public UIColor SurfaceAlt { get; set; }
    public UIColor Border { get; set; }
    public UIColor BorderStrong { get; set; }
    public UIColor Text { get; set; }
    public UIColor TextMuted { get; set; }
    public UIColor TextDisabled { get; set; }
    public UIColor Accent { get; set; }
    public UIColor AccentHover { get; set; }
    public UIColor AccentPressed { get; set; }
    public UIColor AccentSoft { get; set; }
    public UIColor OnAccent { get; set; }
    public UIColor Danger { get; set; }
    public UIColor DangerHover { get; set; }
    public UIColor DangerPressed { get; set; }
    public UIColor Success { get; set; }
    public UIColor Warning { get; set; }
    public UIColor Hover { get; set; }
    public UIColor Pressed { get; set; }
    public UIColor Disabled { get; set; }
    public UIColor Selection { get; set; }
    public UIColor InactiveSelection { get; set; }
    public UIColor Backdrop { get; set; }
    public UIColor TooltipBackground { get; set; }
    public UIColor TooltipText { get; set; }
    public UIColor FocusRing { get; set; }

    public ushort FontId { get; set; }

    /// <summary>
    /// The font of <see cref="TextStyle.Mono"/> text and of RichText <c>code</c>, <c>kbd</c>, <c>samp</c>, <c>tt</c> and <c>pre</c>. 0, the
    /// default, is the built-in JetBrains Mono, also when <see cref="FontId"/> is another font.
    /// </summary>
    public ushort MonoFontId { get; set; }

    public float FontSize { get; set; } = 14;
    public float SmallFontSize { get; set; } = 12;
    public float CaptionFontSize { get; set; } = 10;
    public float HeadingFontSize { get; set; } = 20;
    public float TitleFontSize { get; set; } = 18;
    public float CornerRadius { get; set; } = 6;
    public float TransitionDuration { get; set; } = 0.12f;

    public static Theme Dark()
    {
        return new Theme
        {
            Background = UIColor.Hex(0x16181EFF),
            Surface = UIColor.Hex(0x22252EFF),
            SurfaceAlt = UIColor.Hex(0x272B35FF),
            Border = UIColor.Hex(0x363B49FF),
            BorderStrong = UIColor.Hex(0x575E72FF),
            Text = UIColor.Hex(0xEBEBF0FF),
            TextMuted = UIColor.Hex(0xA0A5B4FF),
            TextDisabled = UIColor.Hex(0x6B7080FF),
            Accent = UIColor.Hex(0x5882F0FF),
            AccentHover = UIColor.Hex(0x6E96FAFF),
            AccentPressed = UIColor.Hex(0x466CD2FF),
            AccentSoft = UIColor.Hex(0x5882F03C),
            OnAccent = UIColor.Hex(0xFFFFFFFF),
            Danger = UIColor.Hex(0xD9484BFF),
            DangerHover = UIColor.Hex(0xE85F61FF),
            DangerPressed = UIColor.Hex(0xB93A3DFF),
            Success = UIColor.Hex(0x4CB860FF),
            Warning = UIColor.Hex(0xE0A33AFF),
            Hover = UIColor.Hex(0x2F3440FF),
            Pressed = UIColor.Hex(0x2A2E39FF),
            Disabled = UIColor.Hex(0x272A33FF),
            Selection = UIColor.Hex(0x5882F066),
            InactiveSelection = UIColor.Hex(0xA0A5B433),
            Backdrop = UIColor.Hex(0x0000008C),
            TooltipBackground = UIColor.Hex(0x0B0C10F2),
            TooltipText = UIColor.Hex(0xEBEBF0FF),
            FocusRing = UIColor.Hex(0x8FB0FFFF)
        };
    }

    public static Theme Light()
    {
        return new Theme
        {
            Background = UIColor.Hex(0xF3F4F6FF),
            Surface = UIColor.Hex(0xFFFFFFFF),
            SurfaceAlt = UIColor.Hex(0xF5F7FAFF),
            Border = UIColor.Hex(0xD9DEE5FF),
            BorderStrong = UIColor.Hex(0xAEB6C2FF),
            Text = UIColor.Hex(0x1F2937FF),
            TextMuted = UIColor.Hex(0x6B7280FF),
            TextDisabled = UIColor.Hex(0x9CA3AFFF),
            Accent = UIColor.Hex(0x2563EBFF),
            AccentHover = UIColor.Hex(0x1D4ED8FF),
            AccentPressed = UIColor.Hex(0x1E40AFFF),
            AccentSoft = UIColor.Hex(0xDBEAFEFF),
            OnAccent = UIColor.Hex(0xFFFFFFFF),
            Danger = UIColor.Hex(0xDC2626FF),
            DangerHover = UIColor.Hex(0xB91C1CFF),
            DangerPressed = UIColor.Hex(0x991B1BFF),
            Success = UIColor.Hex(0x16A34AFF),
            Warning = UIColor.Hex(0xD97706FF),
            Hover = UIColor.Hex(0xE5E9F0FF),
            Pressed = UIColor.Hex(0xD5DBE4FF),
            Disabled = UIColor.Hex(0xE5E7EBFF),
            Selection = UIColor.Hex(0x2563EB59),
            InactiveSelection = UIColor.Hex(0x9CA3AF40),
            Backdrop = UIColor.Hex(0x0F172A73),
            TooltipBackground = UIColor.Hex(0x111827F2),
            TooltipText = UIColor.Hex(0xFFFFFFFF),
            FocusRing = UIColor.Hex(0x111827FF)
        };
    }
}
