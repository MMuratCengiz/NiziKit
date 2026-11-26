using System.Numerics;
using DenOfIz;

namespace NiziKit.UI;

public enum ColorRole : byte
{
    None,
    Background,
    Surface,
    SurfaceAlt,
    Border,
    BorderStrong,
    Text,
    TextMuted,
    TextDisabled,
    Accent,
    AccentHover,
    AccentPressed,
    AccentSoft,
    OnAccent,
    Danger,
    DangerHover,
    DangerPressed,
    Hover,
    Pressed,
    Disabled,
    Selection,
    Backdrop,
    TooltipBackground,
    TooltipText,
    FocusRing,
    Success,
    Warning
}

public readonly struct Color : IEquatable<Color>
{
    private readonly UIColor _rgba;
    private readonly ColorRole _role;
    private readonly short _alpha;

    private Color(UIColor rgba, ColorRole role, short alpha)
    {
        _rgba = rgba;
        _role = role;
        _alpha = alpha;
    }

    private Color(ColorRole role) : this(default, role, -1)
    {
    }

    public static Color Transparent => default;
    public static Color Background => new(ColorRole.Background);
    public static Color Surface => new(ColorRole.Surface);
    public static Color SurfaceAlt => new(ColorRole.SurfaceAlt);
    public static Color Border => new(ColorRole.Border);
    public static Color BorderStrong => new(ColorRole.BorderStrong);
    public static Color Text => new(ColorRole.Text);
    public static Color TextMuted => new(ColorRole.TextMuted);
    public static Color TextDisabled => new(ColorRole.TextDisabled);
    public static Color Accent => new(ColorRole.Accent);
    public static Color AccentHover => new(ColorRole.AccentHover);
    public static Color AccentPressed => new(ColorRole.AccentPressed);
    public static Color AccentSoft => new(ColorRole.AccentSoft);
    public static Color OnAccent => new(ColorRole.OnAccent);
    public static Color Danger => new(ColorRole.Danger);
    public static Color DangerHover => new(ColorRole.DangerHover);
    public static Color DangerPressed => new(ColorRole.DangerPressed);
    public static Color Hover => new(ColorRole.Hover);
    public static Color Pressed => new(ColorRole.Pressed);
    public static Color Disabled => new(ColorRole.Disabled);
    public static Color Selection => new(ColorRole.Selection);
    public static Color Backdrop => new(ColorRole.Backdrop);
    public static Color TooltipBackground => new(ColorRole.TooltipBackground);
    public static Color TooltipText => new(ColorRole.TooltipText);
    public static Color FocusRing => new(ColorRole.FocusRing);
    public static Color Success => new(ColorRole.Success);
    public static Color Warning => new(ColorRole.Warning);

    public bool IsSet => _role != ColorRole.None || _rgba.A != 0 || _alpha > 0;

    public static Color Hex(uint rgba)
    {
        return UIColor.Hex(rgba);
    }

    public static Color Rgba(byte r, byte g, byte b, byte a = 255)
    {
        return new UIColor(r, g, b, a);
    }

    public static Color Mix(Color a, Color b, float t)
    {
        var x = a.Resolve();
        var y = b.Resolve();
        return new UIColor(
            (byte)(x.R + (y.R - x.R) * t),
            (byte)(x.G + (y.G - x.G) * t),
            (byte)(x.B + (y.B - x.B) * t),
            (byte)(x.A + (y.A - x.A) * t));
    }

    public Color WithAlpha(byte alpha)
    {
        return new Color(_rgba, _role, alpha);
    }

    public UIColor Resolve()
    {
        return Resolve(Ui.Theme);
    }

    public UIColor Resolve(Theme theme)
    {
        var color = _role switch
        {
            ColorRole.None => _rgba,
            ColorRole.Background => theme.Background,
            ColorRole.Surface => theme.Surface,
            ColorRole.SurfaceAlt => theme.SurfaceAlt,
            ColorRole.Border => theme.Border,
            ColorRole.BorderStrong => theme.BorderStrong,
            ColorRole.Text => theme.Text,
            ColorRole.TextMuted => theme.TextMuted,
            ColorRole.TextDisabled => theme.TextDisabled,
            ColorRole.Accent => theme.Accent,
            ColorRole.AccentHover => theme.AccentHover,
            ColorRole.AccentPressed => theme.AccentPressed,
            ColorRole.AccentSoft => theme.AccentSoft,
            ColorRole.OnAccent => theme.OnAccent,
            ColorRole.Danger => theme.Danger,
            ColorRole.DangerHover => theme.DangerHover,
            ColorRole.DangerPressed => theme.DangerPressed,
            ColorRole.Hover => theme.Hover,
            ColorRole.Pressed => theme.Pressed,
            ColorRole.Disabled => theme.Disabled,
            ColorRole.Selection => theme.Selection,
            ColorRole.Backdrop => theme.Backdrop,
            ColorRole.TooltipBackground => theme.TooltipBackground,
            ColorRole.TooltipText => theme.TooltipText,
            ColorRole.FocusRing => theme.FocusRing,
            ColorRole.Success => theme.Success,
            ColorRole.Warning => theme.Warning,
            _ => _rgba
        };
        return _alpha >= 0 ? new UIColor(color.R, color.G, color.B, (byte)_alpha) : color;
    }

    public bool Equals(Color other)
    {
        return _role == other._role && _alpha == other._alpha && _rgba.R == other._rgba.R && _rgba.G == other._rgba.G && _rgba.B == other._rgba.B &&
               _rgba.A == other._rgba.A;
    }

    public override bool Equals(object? obj)
    {
        return obj is Color other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_role, _alpha, _rgba.R, _rgba.G, _rgba.B, _rgba.A);
    }

    public static bool operator ==(Color a, Color b) => a.Equals(b);
    public static bool operator !=(Color a, Color b) => !a.Equals(b);

    public static implicit operator Color(UIColor color)
    {
        return new Color(color, ColorRole.None, -1);
    }
}

public readonly struct Edges
{
    public readonly float Left;
    public readonly float Top;
    public readonly float Right;
    public readonly float Bottom;

    public Edges(float left, float top, float right, float bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public Edges(float horizontal, float vertical) : this(horizontal, vertical, horizontal, vertical)
    {
    }

    public Edges(float all) : this(all, all, all, all)
    {
    }

    internal UILengthEdges ToLength()
    {
        return new UILengthEdges(Left, Top, Right, Bottom);
    }

    internal UIEdges ToEdges()
    {
        return new UIEdges(Left, Top, Right, Bottom);
    }

    public static implicit operator Edges(float all)
    {
        return new Edges(all);
    }

    public static implicit operator Edges((float Horizontal, float Vertical) edges)
    {
        return new Edges(edges.Horizontal, edges.Vertical);
    }

    public static implicit operator Edges((float Left, float Top, float Right, float Bottom) edges)
    {
        return new Edges(edges.Left, edges.Top, edges.Right, edges.Bottom);
    }
}

public readonly struct Corners
{
    public readonly float TopLeft;
    public readonly float TopRight;
    public readonly float BottomRight;
    public readonly float BottomLeft;

    public Corners(float topLeft, float topRight, float bottomRight, float bottomLeft)
    {
        TopLeft = topLeft;
        TopRight = topRight;
        BottomRight = bottomRight;
        BottomLeft = bottomLeft;
    }

    public static Corners Top(float radius)
    {
        return new Corners(radius, radius, 0, 0);
    }

    internal UICorners ToUI()
    {
        return new UICorners(TopLeft, TopRight, BottomRight, BottomLeft);
    }

    public static implicit operator Corners(float radius)
    {
        return new Corners(radius, radius, radius, radius);
    }
}

public readonly struct Border
{
    public readonly Color Color;
    public readonly Edges Width;
    private readonly bool _set;

    public Border(Color color, float width = 1)
    {
        Color = color;
        Width = width;
        _set = true;
    }

    public Border(Color color, Edges width)
    {
        Color = color;
        Width = width;
        _set = true;
    }

    public static Border Top(Color color, float width = 1) => new(color, new Edges(0, width, 0, 0));
    public static Border Bottom(Color color, float width = 1) => new(color, new Edges(0, 0, 0, width));
    public static Border Left(Color color, float width = 1) => new(color, new Edges(width, 0, 0, 0));
    public static Border Right(Color color, float width = 1) => new(color, new Edges(0, 0, width, 0));

    /// <summary>
    /// False only for <c>default</c>, on which a <see cref="Panel"/> draws no border, or 1 pt on every side when <see cref="Panel.BorderColor"/>
    /// is set. A border built with <see cref="NiziKit.UI.Color.Transparent"/> is set: it draws nothing itself, its widths apply when the panel
    /// also has a BorderColor or <see cref="Panel.HoverBorder"/> and under the focus ring, and without those it takes no space.
    /// </summary>
    public bool IsSet => _set;

    public static implicit operator Border(Color color)
    {
        return new Border(color);
    }

    public static implicit operator Border(UIColor color)
    {
        return new Border(color);
    }
}

public readonly record struct Rectangle(float X, float Y, float Width, float Height)
{
    public Rectangle(Vector2 position, Vector2 size) : this(position.X, position.Y, size.X, size.Y)
    {
    }

    public Vector2 Position => new(X, Y);
    public Vector2 Size => new(Width, Height);
    public Vector2 Center => new(X + Width * 0.5f, Y + Height * 0.5f);
    public float Right => X + Width;
    public float Bottom => Y + Height;

    public bool Contains(Vector2 point)
    {
        return point.X >= X && point.Y >= Y && point.X < Right && point.Y < Bottom;
    }

    internal static Rectangle From(in UIRect rect)
    {
        return new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
    }
}

public enum Tone
{
    Secondary,
    Primary,
    Ghost,
    Danger
}

public enum TextRole
{
    Body,
    Muted,
    Small,
    Caption,
    Hint,
    Title,
    Heading
}

public struct TextStyle
{
    public TextRole Role;
    public float Size;
    public Color Color;
    public ushort Font;
    public float LetterSpacing;
    public float LineHeight;

    /// <summary>
    /// Draws the text in <see cref="Theme.MonoFontId"/> when <see cref="Font"/> is 0, also when <see cref="Theme.FontId"/> is another font.
    /// </summary>
    public bool Mono;

    /// <summary>
    /// The face registered with <see cref="Fonts.SetVariant"/> for this weight, else Medium and Bold are synthesized from the font.
    /// </summary>
    public FontWeight Weight;

    public static TextStyle Body => default;
    public static TextStyle Muted => new() { Role = TextRole.Muted };
    public static TextStyle Small => new() { Role = TextRole.Small };
    public static TextStyle Caption => new() { Role = TextRole.Caption };
    public static TextStyle Hint => new() { Role = TextRole.Hint };
    public static TextStyle Title => new() { Role = TextRole.Title };
    public static TextStyle Heading => new() { Role = TextRole.Heading };

    public static TextStyle Sized(float size, Color color = default)
    {
        return new TextStyle { Size = size, Color = color };
    }

    internal readonly UITextDesc Resolve(Theme theme, bool enabled = true)
    {
        var size = Size > 0 ? Size : Role switch
        {
            TextRole.Small or TextRole.Hint => theme.SmallFontSize,
            TextRole.Caption => theme.CaptionFontSize,
            TextRole.Title => theme.TitleFontSize,
            TextRole.Heading => theme.HeadingFontSize,
            _ => theme.FontSize
        };
        var color = !enabled ? theme.TextDisabled : Color.IsSet ? Color.Resolve(theme) : Role switch
        {
            TextRole.Muted or TextRole.Small or TextRole.Caption => theme.TextMuted,
            TextRole.Hint => theme.TextDisabled,
            _ => theme.Text
        };
        return new UITextDesc
        {
            FontSize = size,
            Color = color,
            FontId = Fonts.Styled(Family(theme), Weight, false),
            LetterSpacing = LetterSpacing != 0 ? LetterSpacing : Role == TextRole.Caption ? 0.7f : 0,
            LineHeight = LineHeight
        };
    }

    internal readonly ushort Family(Theme theme)
    {
        return Font != 0 ? Font : Mono ? theme.MonoFontId : theme.FontId;
    }
}

public record struct Style
{
    public UIValue? Width { get; init; }
    public UIValue? Height { get; init; }
    public UIValue? MinWidth { get; init; }
    public UIValue? MinHeight { get; init; }
    public UIValue? MaxWidth { get; init; }
    public UIValue? MaxHeight { get; init; }
    public float? Flex { get; init; }
    public Edges? Padding { get; init; }
    public Edges? Margin { get; init; }
    public float? Gap { get; init; }
    public float? LineGap { get; init; }
    public UIAlign? Align { get; init; }
    public UIJustify? Justify { get; init; }
    public bool? Wrap { get; init; }
    public Color? Background { get; init; }
    public Color? HoverBackground { get; init; }
    public Color? PressedBackground { get; init; }
    public Border? Border { get; init; }
    public Color? HoverBorder { get; init; }
    public Corners? Radius { get; init; }
    public bool? Clip { get; init; }
}

public static class Motion
{
    public static UITransitionState Fade => UITransitionState.FromOpacity(0);
    public static UITransitionState SlideDown => UITransitionState.FromOpacity(0).WithOffset(new Vector2(0, -6));
    public static UITransitionState SlideUp => UITransitionState.FromOpacity(0).WithOffset(new Vector2(0, 12));
    public static UITransitionState Pop => UITransitionState.FromOpacity(0).WithScale(0.96f);
}
