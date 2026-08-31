using DenOfIz;

namespace NiziKit.UI.Widgets;

public enum SizingKind
{
    Fit,
    Grow,
    Fixed,
    Percent
}

public readonly struct Sizing
{
    public readonly SizingKind Kind;
    public readonly float Value;
    public readonly float Min;
    public readonly float Max;

    private Sizing(SizingKind kind, float value, float min, float max)
    {
        Kind = kind;
        Value = value;
        Min = min;
        Max = max;
    }

    public static Sizing Fit => new(SizingKind.Fit, 0, 0, float.MaxValue);
    public static Sizing Grow => new(SizingKind.Grow, 0, 0, float.MaxValue);
    public static Sizing Fixed(float value) => new(SizingKind.Fixed, value, value, value);
    public static Sizing Percent(float percent) => new(SizingKind.Percent, percent, 0, 0);
    public static Sizing FitRange(float min, float max) => new(SizingKind.Fit, 0, min, max);
    public static Sizing GrowRange(float min, float max) => new(SizingKind.Grow, 0, min, max);

    public Sizing WithMin(float min) => new(Kind, Value, min, Max);
    public Sizing WithMax(float max) => new(Kind, Value, Min, max);

    public bool IsFit => Kind == SizingKind.Fit;
    public bool IsGrow => Kind == SizingKind.Grow;
    public bool IsFixed => Kind == SizingKind.Fixed;
    public bool IsPercent => Kind == SizingKind.Percent;

    public static implicit operator Sizing(float value) => Fixed(value);
    public static implicit operator Sizing(int value) => Fixed(value);

    public ClaySizingAxis ToClay()
    {
        return Kind switch
        {
            SizingKind.Grow => ClaySizingAxis.Grow(Min, Max),
            SizingKind.Fixed => ClaySizingAxis.Fixed(Value),
            SizingKind.Percent => ClaySizingAxis.Percent(Value),
            _ => ClaySizingAxis.Fit(Min, Max)
        };
    }
}

public readonly struct Thickness(float left, float right, float top, float bottom)
{
    public readonly float Left = left;
    public readonly float Right = right;
    public readonly float Top = top;
    public readonly float Bottom = bottom;

    public Thickness(float all) : this(all, all, all, all)
    {
    }

    public Thickness(float horizontal, float vertical) : this(horizontal, horizontal, vertical, vertical)
    {
    }

    public static Thickness Zero => default;

    public float Horizontal => Left + Right;
    public float Vertical => Top + Bottom;

    public Thickness Add(Thickness other) => new(Left + other.Left, Right + other.Right, Top + other.Top, Bottom + other.Bottom);
    public Thickness WithLeft(float left) => new(left, Right, Top, Bottom);
    public Thickness WithRight(float right) => new(Left, right, Top, Bottom);
    public Thickness WithTop(float top) => new(Left, Right, top, Bottom);
    public Thickness WithBottom(float bottom) => new(Left, Right, Top, bottom);

    public static Thickness operator +(Thickness a, Thickness b) => a.Add(b);

    public static implicit operator Thickness(float all) => new(all);
    public static implicit operator Thickness(int all) => new(all);
    public static implicit operator Thickness((float horizontal, float vertical) v) => new(v.horizontal, v.vertical);

    public ClayPadding ToClay()
    {
        return ClayPadding.Create(Left, Right, Top, Bottom);
    }
}

public readonly struct CornerRadius(float topLeft, float topRight, float bottomLeft, float bottomRight)
{
    public readonly float TopLeft = topLeft;
    public readonly float TopRight = topRight;
    public readonly float BottomLeft = bottomLeft;
    public readonly float BottomRight = bottomRight;

    public CornerRadius(float all) : this(all, all, all, all)
    {
    }

    public bool IsZero => TopLeft <= 0 && TopRight <= 0 && BottomLeft <= 0 && BottomRight <= 0;

    public static implicit operator CornerRadius(float all) => new(all);
    public static implicit operator CornerRadius(int all) => new(all);
    public static implicit operator CornerRadius((float topLeft, float topRight, float bottomLeft, float bottomRight) v) => new(v.topLeft, v.topRight, v.bottomLeft, v.bottomRight);

    public ClayBorderRadius ToClay()
    {
        return ClayBorderRadius.Create(TopLeft, TopRight, BottomLeft, BottomRight);
    }
}

public enum Align
{
    Start,
    Center,
    End
}

public enum Orientation
{
    Horizontal,
    Vertical
}

internal static class AlignExtensions
{
    public static ClayAlignmentX ToClayX(this Align align)
    {
        return align switch
        {
            Align.Center => ClayAlignmentX.Center,
            Align.End => ClayAlignmentX.Right,
            _ => ClayAlignmentX.Left
        };
    }

    public static ClayAlignmentY ToClayY(this Align align)
    {
        return align switch
        {
            Align.Center => ClayAlignmentY.Center,
            Align.End => ClayAlignmentY.Bottom,
            _ => ClayAlignmentY.Top
        };
    }

    public static ClayTextAlignment ToClayText(this Align align)
    {
        return align switch
        {
            Align.Center => ClayTextAlignment.Center,
            Align.End => ClayTextAlignment.Right,
            _ => ClayTextAlignment.Left
        };
    }

    public static Orientation Opposite(this Orientation orientation)
    {
        return orientation == Orientation.Horizontal ? Orientation.Vertical : Orientation.Horizontal;
    }
}
