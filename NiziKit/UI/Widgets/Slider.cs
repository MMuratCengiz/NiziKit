using System.Globalization;
using System.Numerics;
using DenOfIz;

namespace NiziKit.UI.Widgets;

public class Slider : Widget
{
    private readonly uint _trackId = Ui.AllocateWidgetId();
    private bool _dragging;
    private float _value;
    private bool _vertical;
    private string _valueText = "";
    private float _valueTextValue = float.NaN;
    private string? _valueTextFormat;

    public Slider()
    {
        Width = UiSize.Grow;
        Height = 20;
        Focusable = true;
    }

    public Slider(float min, float max, float value) : this()
    {
        Min = min;
        Max = max;
        _value = Math.Clamp(value, min, max);
    }

    public float Min { get; set; }
    public float Max { get; set; } = 1;
    public float Step { get; init; }
    public float KnobSize { get; set; } = 14;
    public float TrackHeight { get; set; } = 4;
    public UiColor? TrackColor { get; set; }
    public UiColor? FillColor { get; set; }
    public UiColor? KnobColor { get; set; }
    public SliderStyle Style { get; set; } = new();
    public bool ShowValue { get; init; }
    public string ValueFormat { get; init; } = "0.##";
    public float ValueWidth { get; set; } = 40;
    public UiColor ValueColor { get; set; } = UiColor.Rgb(160, 165, 180);
    public UiColor DisabledValueColor { get; set; } = UiColor.Rgb(120, 125, 140);
    public int FontSize { get; set; } = 14;
    public ushort FontId { get; set; } = UiFonts.DefaultFontId;

    public bool Vertical
    {
        get => _vertical;
        set
        {
            if (_vertical == value)
            {
                return;
            }

            _vertical = value;
            (Width, Height) = (Height, Width);
        }
    }

    public float Value
    {
        get => _value;
        set
        {
            var clamped = Math.Clamp(Snap(value), Math.Min(Min, Max), Math.Max(Min, Max));
            if (Math.Abs(clamped - _value) < float.Epsilon)
            {
                return;
            }

            _value = clamped;
            ValueChanged?.Invoke(this);
        }
    }

    public float Normalized => Max - Min == 0 ? 0 : (_value - Min) / (Max - Min);

    public new bool IsDragging => _dragging;

    public event Action<Slider>? ValueChanged;

    protected override bool TracksPointer => true;

    private float KeyStep => Step > 0 ? Step : (Max - Min) / 100f;

    private string ValueText
    {
        get
        {
            if (_valueTextValue != _value || _valueTextFormat != ValueFormat)
            {
                _valueTextValue = _value;
                _valueTextFormat = ValueFormat;
                _valueText = _value.ToString(ValueFormat, CultureInfo.InvariantCulture);
            }

            return _valueText;
        }
    }

    private float Snap(float value)
    {
        if (Step <= 0)
        {
            return value;
        }

        return Min + MathF.Round((value - Min) / Step) * Step;
    }

    protected override void OnPoll()
    {
        if (Ui.PointerPressedThisFrame && IsHovered && IsEnabled)
        {
            _dragging = true;
        }

        if (_dragging)
        {
            var track = Ui.Clay.GetElementBoundingBox(_trackId);
            var knob = Ui.Clay.PointsToPixels(KnobSize);
            if (_vertical)
            {
                var usable = track.Height - knob;
                if (usable > 0)
                {
                    var t = 1 - Math.Clamp((Ui.PointerPosition.Y - track.Y - knob * 0.5f) / usable, 0, 1);
                    Value = Min + t * (Max - Min);
                }
            }
            else
            {
                var usable = track.Width - knob;
                if (usable > 0)
                {
                    var t = Math.Clamp((Ui.PointerPosition.X - track.X - knob * 0.5f) / usable, 0, 1);
                    Value = Min + t * (Max - Min);
                }
            }
        }

        if (Ui.PointerReleasedThisFrame || !Ui.PointerDown)
        {
            _dragging = false;
        }
    }

    protected internal override bool OnKeyDown(in KeyboardEventData key)
    {
        if (!IsEnabled)
        {
            return false;
        }

        switch (key.KeyCode)
        {
            case KeyCode.Left:
            case KeyCode.Down:
                Value -= KeyStep;
                return true;
            case KeyCode.Right:
            case KeyCode.Up:
                Value += KeyStep;
                return true;
            case KeyCode.Home:
                Value = Min;
                return true;
            case KeyCode.End:
                Value = Max;
                return true;
            default:
                return false;
        }
    }

    protected override void ApplyDeclaration(ref ClayElementDeclaration decl)
    {
        base.ApplyDeclaration(ref decl);
        decl.Layout.ChildGap = 8;
        if (_vertical)
        {
            decl.Layout.LayoutDirection = ClayLayoutDirection.TopToBottom;
            decl.Layout.ChildAlignment.X = ClayAlignmentX.Center;
        }
        else
        {
            decl.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
        }
    }

    protected override void BuildContent()
    {
        var style = Style;
        var bounds = Ui.Clay.GetElementBoundingBox(_trackId);
        var trackLength = Ui.Clay.PixelsToPoints(_vertical ? bounds.Height : bounds.Width);
        var offset = KnobSize * 0.5f + Normalized * MathF.Max(0, trackLength - KnobSize);
        var active = _dragging || IsHovered || IsFocused;

        var track = ClayElementDeclaration.Default();
        track.Id = _trackId;
        if (_vertical)
        {
            track.Layout.Sizing.Width = ClaySizingAxis.Fixed(TrackHeight);
            track.Layout.Sizing.Height = ClaySizingAxis.Grow(0, float.MaxValue);
            track.Layout.LayoutDirection = ClayLayoutDirection.TopToBottom;
            track.Layout.ChildAlignment.X = ClayAlignmentX.Center;
            track.Layout.ChildAlignment.Y = ClayAlignmentY.Bottom;
        }
        else
        {
            track.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
            track.Layout.Sizing.Height = ClaySizingAxis.Fixed(TrackHeight);
            track.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
        }

        track.BackgroundColor = (TrackColor ?? style.Track).ToClay();
        track.BorderRadius = ClayBorderRadius.CreateUniform(TrackHeight * 0.5f);
        Ui.Clay.OpenElement(in track);
        {
            var fill = ClayElementDeclaration.Default();
            if (_vertical)
            {
                fill.Layout.Sizing.Width = ClaySizingAxis.Grow(0, float.MaxValue);
                fill.Layout.Sizing.Height = ClaySizingAxis.Fixed(offset);
            }
            else
            {
                fill.Layout.Sizing.Width = ClaySizingAxis.Fixed(offset);
                fill.Layout.Sizing.Height = ClaySizingAxis.Grow(0, float.MaxValue);
            }

            var fillColor = IsEnabled ? style.Fill : style.FillDisabled;
            fill.BackgroundColor = (FillColor ?? fillColor).ToClay();
            fill.BorderRadius = ClayBorderRadius.CreateUniform(TrackHeight * 0.5f);
            Ui.Clay.OpenElement(in fill);
            Ui.Clay.CloseElement();

            var knob = ClayElementDeclaration.Default();
            knob.Layout.Sizing.Width = ClaySizingAxis.Fixed(KnobSize);
            knob.Layout.Sizing.Height = ClaySizingAxis.Fixed(KnobSize);
            var knobColor = active ? style.KnobHover : style.Knob;
            knob.BackgroundColor = (KnobColor ?? knobColor).ToClay();
            knob.BorderRadius = ClayBorderRadius.CreateUniform(KnobSize * 0.5f);
            if (IsFocused)
            {
                knob.Border.Color = style.Focus.ToClay();
                knob.Border.Width = ClayBorderWidth.CreateUniform(2);
            }

            knob.Floating.AttachTo = ClayFloatingAttachTo.Parent;

            knob.Floating.ZIndex = Ui.FloatingZIndex;
            knob.Floating.ElementAttachPoint = ClayFloatingAttachPoint.CenterCenter;
            if (_vertical)
            {
                knob.Floating.ParentAttachPoint = ClayFloatingAttachPoint.CenterBottom;
                knob.Floating.Offset = new Vector2(0, -offset);
            }
            else
            {
                knob.Floating.ParentAttachPoint = ClayFloatingAttachPoint.LeftCenter;
                knob.Floating.Offset = new Vector2(offset, 0);
            }

            knob.Floating.PointerCaptureMode = ClayPointerCaptureMode.Passthrough;
            Ui.Clay.OpenElement(in knob);
            Ui.Clay.CloseElement();
        }
        Ui.Clay.CloseElement();

        if (!ShowValue)
        {
            return;
        }

        var label = ClayElementDeclaration.Default();
        label.Layout.Sizing.Width = ValueWidth > 0 ? ClaySizingAxis.Fixed(ValueWidth) : ClaySizingAxis.Fit(0, float.MaxValue);
        label.Layout.Sizing.Height = ClaySizingAxis.Fit(0, float.MaxValue);
        label.Layout.ChildAlignment.X = _vertical ? ClayAlignmentX.Center : ClayAlignmentX.Left;
        label.Layout.ChildAlignment.Y = ClayAlignmentY.Center;
        Ui.Clay.OpenElement(in label);
        {
            var desc = ClayTextDesc.Default();
            desc.TextColor = (IsEnabled ? ValueColor : DisabledValueColor).ToClay();
            desc.FontId = FontId;
            desc.FontSize = (ushort)FontSize;
            desc.WrapMode = ClayTextWrapMode.None;
            Ui.Clay.Text(ValueText, in desc);
        }
        Ui.Clay.CloseElement();
    }
}
