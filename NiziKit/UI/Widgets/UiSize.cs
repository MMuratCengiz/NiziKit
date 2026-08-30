using DenOfIz;

namespace NiziKit.UI.Widgets;

public enum UiSizeKind
{
    Fit,
    Grow,
    Fixed,
    Percent
}

public readonly struct UiSize
{
    public readonly UiSizeKind Kind;
    public readonly float Value;
    public readonly float Min;
    public readonly float Max;

    private UiSize(UiSizeKind kind, float value, float min, float max)
    {
        Kind = kind;
        Value = value;
        Min = min;
        Max = max;
    }

    public static UiSize Fit => new(UiSizeKind.Fit, 0, 0, float.MaxValue);
    public static UiSize Grow => new(UiSizeKind.Grow, 0, 0, float.MaxValue);
    public static UiSize Auto => Fit;
    public static UiSize Fill => Grow;
    public static UiSize Fixed(float value) => new(UiSizeKind.Fixed, value, value, value);
    public static UiSize Percent(float percent) => new(UiSizeKind.Percent, percent, 0, 0);
    public static UiSize FitRange(float min, float max) => new(UiSizeKind.Fit, 0, min, max);
    public static UiSize GrowRange(float min, float max) => new(UiSizeKind.Grow, 0, min, max);
    public static UiSize Range(float min, float max) => FitRange(min, max);
    public static UiSize Star(float weight = 1) => new(UiSizeKind.Grow, MathF.Max(weight, 0), 0, float.MaxValue);

    public UiSize WithMin(float min) => new(Kind, Value, min, Max);
    public UiSize WithMax(float max) => new(Kind, Value, Min, max);

    public bool IsFit => Kind == UiSizeKind.Fit;
    public bool IsGrow => Kind == UiSizeKind.Grow;
    public bool IsFixed => Kind == UiSizeKind.Fixed;
    public bool IsPercent => Kind == UiSizeKind.Percent;
    public float Weight => Kind == UiSizeKind.Grow && Value > 0 ? Value : 1;

    public static implicit operator UiSize(float value) => Fixed(value);
    public static implicit operator UiSize(int value) => Fixed(value);

    public ClaySizingAxis ToClay()
    {
        return Kind switch
        {
            UiSizeKind.Grow => ClaySizingAxis.Grow(Min, Max),
            UiSizeKind.Fixed => ClaySizingAxis.Fixed(Value),
            UiSizeKind.Percent => ClaySizingAxis.Percent(Value),
            _ => ClaySizingAxis.Fit(Min, Max)
        };
    }
}

public readonly struct UiThickness(float left, float right, float top, float bottom)
{
    public readonly float Left = left;
    public readonly float Right = right;
    public readonly float Top = top;
    public readonly float Bottom = bottom;

    public UiThickness(float all) : this(all, all, all, all)
    {
    }

    public UiThickness(float horizontal, float vertical) : this(horizontal, horizontal, vertical, vertical)
    {
    }

    public static UiThickness Zero => default;
    public static UiThickness Symmetric(float horizontal, float vertical) => new(horizontal, vertical);
    public static UiThickness Only(float left = 0, float top = 0, float right = 0, float bottom = 0) => new(left, right, top, bottom);

    public float Horizontal => Left + Right;
    public float Vertical => Top + Bottom;

    public UiThickness Add(UiThickness other) => new(Left + other.Left, Right + other.Right, Top + other.Top, Bottom + other.Bottom);
    public UiThickness WithLeft(float left) => new(left, Right, Top, Bottom);
    public UiThickness WithRight(float right) => new(Left, right, Top, Bottom);
    public UiThickness WithTop(float top) => new(Left, Right, top, Bottom);
    public UiThickness WithBottom(float bottom) => new(Left, Right, Top, bottom);

    public static UiThickness operator +(UiThickness a, UiThickness b) => a.Add(b);

    public static implicit operator UiThickness(float all) => new(all);
    public static implicit operator UiThickness(int all) => new(all);
    public static implicit operator UiThickness((float horizontal, float vertical) v) => new(v.horizontal, v.vertical);

    public ClayPadding ToClay()
    {
        return ClayPadding.Create(Left, Right, Top, Bottom);
    }
}

public enum UiAlign
{
    Start,
    Center,
    End
}

public enum UiOrientation
{
    Horizontal,
    Vertical
}

internal static class UiAlignExtensions
{
    public static ClayAlignmentX ToClayX(this UiAlign align)
    {
        return align switch
        {
            UiAlign.Center => ClayAlignmentX.Center,
            UiAlign.End => ClayAlignmentX.Right,
            _ => ClayAlignmentX.Left
        };
    }

    public static ClayAlignmentY ToClayY(this UiAlign align)
    {
        return align switch
        {
            UiAlign.Center => ClayAlignmentY.Center,
            UiAlign.End => ClayAlignmentY.Bottom,
            _ => ClayAlignmentY.Top
        };
    }

    public static ClayTextAlignment ToClayText(this UiAlign align)
    {
        return align switch
        {
            UiAlign.Center => ClayTextAlignment.Center,
            UiAlign.End => ClayTextAlignment.Right,
            _ => ClayTextAlignment.Left
        };
    }

    public static UiOrientation Opposite(this UiOrientation orientation)
    {
        return orientation == UiOrientation.Horizontal ? UiOrientation.Vertical : UiOrientation.Horizontal;
    }
}
