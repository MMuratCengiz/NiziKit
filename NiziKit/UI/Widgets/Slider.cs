using System.Globalization;
using DenOfIz;

namespace NiziKit.UI.Widgets;

public class Slider : Widget
{
    private readonly char[] _valueText = new char[32];
    private int _valueLength;
    private float _formattedValue = float.NaN;
    private uint _trackId;
    private uint _valueId;
    private float _value;

    public Slider()
    {
        ref var layout = ref Desc.Layout;
        layout.Grow = 1;
        layout.Shrink = 1;
        layout.Basis = UIValue.Points(0);
        layout.MinWidth = 60;
        Desc.Pointer = UIPointerMode.Block;
    }

    public Slider(float min, float max, float value) : this()
    {
        Min = min;
        Max = max;
        _value = Math.Clamp(value, Math.Min(min, max), Math.Max(min, max));
    }

    public float Min { get; set; }
    public float Max { get; set; } = 1;

    public float Step { get; set; }

    public bool Vertical { get; set; }
    public float KnobSize { get; set; } = 16;
    public float TrackThickness { get; set; } = 6;

    public bool ShowValue { get; set; }

    public string ValueFormat { get; set; } = "0.##";
    public float ValueWidth { get; set; } = 44;

    public float Value
    {
        get => _value;
        set
        {
            var clamped = Math.Clamp(Snap(value), Math.Min(Min, Max), Math.Max(Min, Max));
            if (clamped == _value)
            {
                return;
            }

            _value = clamped;
            ValueChanged?.Invoke(this);
        }
    }

    public float Normalized => Max == Min ? 0 : Math.Clamp((_value - Min) / (Max - Min), 0, 1);

    public event Action<Slider>? ValueChanged;

    protected override bool Focusable => true;

    protected internal override bool OnKeyDown(in KeyboardEventData key)
    {
        var step = Step > 0 ? Step : (Max - Min) / 100;
        switch (key.KeyCode)
        {
            case KeyCode.Left:
            case KeyCode.Down:
                Value -= step;
                return true;
            case KeyCode.Right:
            case KeyCode.Up:
                Value += step;
                return true;
            case KeyCode.Pagedown:
                Value -= step * 10;
                return true;
            case KeyCode.Pageup:
                Value += step * 10;
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

    protected override void OnBuild()
    {
        if (_trackId == 0)
        {
            _trackId = Ui.Id(Id, 2);
            _valueId = Ui.Id(Id, 1);
        }

        ref readonly var interaction = ref Interaction;
        var knob = KnobSize;
        if (interaction.Pressed && Enabled)
        {
            var track = Ui.Interaction(_trackId);
            var length = Vertical ? track.Rect.Height : track.Rect.Width;
            if (track.Pressed && length > knob)
            {
                var along = Vertical ? length - track.PointerPosition.Y : track.PointerPosition.X;
                Value = Min + Math.Clamp((along - knob * 0.5f) / (length - knob), 0, 1) * (Max - Min);
            }
        }

        var theme = Ui.Theme;
        var t = Normalized;
        var active = interaction.Pressed || IsHovered;
        var focusRing = IsFocused && Ui.FocusVisible;
        var thickness = TrackThickness;

        ref var desc = ref Desc;
        desc.Layout.Direction = Vertical ? UIDirection.Column : UIDirection.Row;
        desc.Layout.AlignItems = UIAlign.Center;
        desc.Layout.Gap = 10;
        desc.Cursor = !Enabled ? SystemCursor.NotAllowed : interaction.Pressed ? Vertical ? SystemCursor.NsResize : SystemCursor.EwResize : SystemCursor.Pointer;

        using (OpenSelf())
        {
            var track = Ui.Element(_trackId)
                .Direction(Vertical ? UIDirection.Column : UIDirection.Row)
                .Fill()
                .AlignSelf(UIAlignSelf.Stretch)
                .Justify(Vertical ? UIJustify.End : UIJustify.Start);
            if (Vertical)
            {
                track.Width(knob).Padding(0, knob * 0.5f).MinHeight(knob * 2);
            }
            else
            {
                track.Height(knob).Padding(knob * 0.5f, 0).MinWidth(knob * 2);
            }

            using (track.AlignItems(UIAlign.Center).Open())
            {
                var rail = Ui.Element()
                    .Direction(Vertical ? UIDirection.ColumnReverse : UIDirection.Row)
                    .Fill()
                    .Background(theme.Border)
                    .CornerRadius(thickness * 0.5f);
                if (Vertical)
                {
                    rail.Width(thickness);
                }
                else
                {
                    rail.Height(thickness);
                }

                using (rail.Open())
                {
                    var fill = Ui.Element()
                        .Background(Enabled ? theme.Accent : theme.TextDisabled)
                        .CornerRadius(thickness * 0.5f);
                    if (Vertical)
                    {
                        fill.Height(UIValue.Percent(t));
                    }
                    else
                    {
                        fill.Width(UIValue.Percent(t));
                    }

                    fill.Leaf();

                    var thumb = Ui.Element()
                        .Size(knob, knob)
                        .Background(interaction.Pressed ? theme.Accent : theme.Surface)
                        .Border(!Enabled ? theme.TextDisabled : focusRing ? theme.FocusRing : active ? theme.AccentHover : theme.Accent, 2)
                        .CornerRadius(knob * 0.5f)
                        .Animate();
                    if (Vertical)
                    {
                        thumb.Absolute(left: (thickness - knob) * 0.5f, bottom: UIValue.Percent(t)).Margin(bottom: -knob * 0.5f);
                    }
                    else
                    {
                        thumb.Absolute(left: UIValue.Percent(t), top: (thickness - knob) * 0.5f).Margin(left: -knob * 0.5f);
                    }

                    thumb.Leaf();
                }
            }

            if (ShowValue)
            {
                FormatValue();
                using (Ui.Row(_valueId).Width(ValueWidth).Shrink(0).Open())
                {
                    Ui.Text(_valueText.AsSpan(0, _valueLength), theme.FontSize, Enabled ? theme.TextMuted : theme.TextDisabled);
                }
            }
        }
    }

    private void FormatValue()
    {
        if (_formattedValue.Equals(_value))
        {
            return;
        }

        _formattedValue = _value;
        _valueLength = _value.TryFormat(_valueText, out var written, ValueFormat, CultureInfo.InvariantCulture) ? written : 0;
    }

    private float Snap(float value)
    {
        return Step > 0 ? Min + MathF.Round((value - Min) / Step) * Step : value;
    }
}
