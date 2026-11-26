using System.Globalization;
using DenOfIz;

namespace NiziKit.UI;

public class Button : Widget
{
    public Button()
    {
    }

    public Button(string text, string? icon = null)
    {
        Text = text;
        Icon = icon;
    }

    public Button(TextSource text, string? icon = null)
    {
        Text = new TextValue(text);
        Icon = icon;
    }

    public TextValue Text { get; set; }
    public string? Icon { get; set; }
    public Tone Tone { get; set; }
    public bool Small { get; set; }
    public Bound<bool>? Checked { get; set; }
    public Action<bool>? OnChange { get; set; }

    protected override bool Clickable => true;
    protected override bool KeepsSize => true;

    protected override void OnActivate()
    {
        if (Checked is { } current)
        {
            var value = !current.Value;
            current.Set(value);
            Checked = current;
            OnChange?.Invoke(value);
        }

        base.OnActivate();
    }

    internal override void Render()
    {
        var theme = Ui.Theme;
        var enabled = EnabledNow;
        var hovered = IsHovered && enabled;
        var pressed = IsPressed && enabled;
        UIColor background;
        UIColor border;
        UIColor text;
        switch (Tone)
        {
            case Tone.Primary:
                background = pressed ? theme.AccentPressed : hovered ? theme.AccentHover : theme.Accent;
                border = background;
                text = theme.OnAccent;
                break;
            case Tone.Danger:
                background = pressed ? theme.DangerPressed : hovered ? theme.DangerHover : theme.Danger;
                border = background;
                text = theme.OnAccent;
                break;
            case Tone.Ghost:
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

        if (Checked is { Value: true })
        {
            background = pressed ? theme.Pressed : theme.AccentSoft;
            border = theme.Accent;
            text = Tone is Tone.Primary or Tone.Danger ? text : theme.Accent;
        }

        if (!enabled)
        {
            background = Tone == Tone.Ghost ? default : theme.Disabled;
            border = Tone == Tone.Ghost ? default : theme.Disabled;
            text = theme.TextDisabled;
        }

        if (IsFocused && Ui.FocusVisible)
        {
            border = theme.FocusRing;
        }

        var iconOnly = Text.IsEmpty && Icon != null;
        var desc = Desc();
        ref var layout = ref desc.Layout;
        layout.Justify = UIJustify.Center;
        layout.AlignItems = UIAlign.Center;
        layout.Gap = Small ? 6 : 8;
        layout.Padding = (iconOnly ? Small ? new Edges(7, 4) : new Edges(9, 6) : Small ? new Edges(10, 4) : new Edges(14, 7)).ToLength();
        desc.Style.BackgroundColor = background;
        desc.Style.BorderColor = border;
        desc.Style.BorderWidth = UIEdges.All(1);
        desc.Style.CornerRadius = UICorners.All(theme.CornerRadius);
        desc.Transition.Duration = theme.TransitionDuration;
        desc.Transition.Properties = (uint)UITransitionPropertyFlagBits.Colors;

        Ui.Open(in desc);
        OfferTooltip();
        var size = Small ? theme.SmallFontSize : theme.FontSize;
        if (Icon != null)
        {
            Ui.Glyph(Icon, size, text);
        }

        if (!Text.IsEmpty)
        {
            Ui.Line(Text.Read(), size, text);
        }

        Ui.Close();
    }
}

public class Checkbox : Widget
{
    public Checkbox(string text)
    {
        Text = text;
    }

    public string Text { get; set; }
    public Bound<bool> Checked { get; set; }
    public Action<bool>? OnChange { get; set; }
    public float BoxSize { get; set; } = 16;

    protected override bool Clickable => true;
    protected override bool KeepsSize => true;

    protected override void OnActivate()
    {
        var checkedState = Checked;
        var value = !checkedState.Value;
        checkedState.Set(value);
        Checked = checkedState;
        OnChange?.Invoke(value);
        base.OnActivate();
    }

    internal override void Render()
    {
        var theme = Ui.Theme;
        var enabled = EnabledNow;
        var on = Checked.Value;
        var desc = Desc();
        desc.Layout.AlignItems = UIAlign.Center;
        desc.Layout.Gap = 8;
        desc.Layout.Padding = new Edges(4, 2).ToLength();
        desc.Style.BorderWidth = UIEdges.All(1);
        desc.Style.BorderColor = IsFocused && Ui.FocusVisible ? theme.FocusRing : default;
        desc.Style.CornerRadius = UICorners.All(theme.CornerRadius);

        var box = enabled ? on ? theme.Accent : theme.Surface : theme.Disabled;
        var border = !enabled ? theme.Disabled : on || IsHovered ? theme.Accent : theme.BorderStrong;
        Ui.Open(in desc);
        OfferTooltip();
        using (Element.Row().Size(BoxSize, BoxSize).Center().Background(box).Border(border, 1.5f).CornerRadius(4).Animate().Open())
        {
            if (on)
            {
                var mark = enabled ? theme.OnAccent : theme.TextDisabled;
                if (FontAwesome.IsInitialized)
                {
                    Ui.Glyph(FontAwesome.Check, BoxSize * 0.62f, mark);
                }
                else
                {
                    Element.Box().Size(BoxSize * 0.45f, BoxSize * 0.45f).Background(mark).CornerRadius(2).Leaf();
                }
            }
        }

        if (Text.Length > 0)
        {
            Ui.Line(Text, 0, enabled ? theme.Text : theme.TextDisabled);
        }

        Ui.Close();
    }
}

public class Switch : Widget
{
    public Switch(string text)
    {
        Text = text;
    }

    public string Text { get; set; }
    public Bound<bool> On { get; set; }
    public Action<bool>? OnChange { get; set; }

    protected override bool Clickable => true;
    protected override bool KeepsSize => true;

    protected override void OnActivate()
    {
        var state = On;
        var value = !state.Value;
        state.Set(value);
        On = state;
        OnChange?.Invoke(value);
        base.OnActivate();
    }

    internal override void Render()
    {
        var theme = Ui.Theme;
        var enabled = EnabledNow;
        var on = On.Value;
        var desc = Desc();
        desc.Layout.AlignItems = UIAlign.Center;
        desc.Layout.Gap = 10;
        desc.Layout.Padding = new Edges(4, 2).ToLength();
        desc.Style.BorderWidth = UIEdges.All(1);
        desc.Style.BorderColor = IsFocused && Ui.FocusVisible ? theme.FocusRing : default;
        desc.Style.CornerRadius = UICorners.All(theme.CornerRadius);

        var hovered = IsHovered && enabled;
        var track = !enabled ? theme.Disabled : on ? hovered ? theme.AccentHover : theme.Accent : hovered ? theme.TextMuted : theme.BorderStrong;
        Ui.Open(in desc);
        OfferTooltip();
        using (Element.Row()
                   .Size(40, 22)
                   .Padding(2)
                   .AlignItems(UIAlign.Center)
                   .Justify(on ? UIJustify.End : UIJustify.Start)
                   .Background(track)
                   .CornerRadius(11)
                   .Animate()
                   .Open())
        {
            Element.Box()
                .Size(18, 18)
                .Background(enabled ? theme.OnAccent : theme.TextDisabled)
                .CornerRadius(9)
                .Animate(UITransitionPropertyFlagBits.Position, 0.18f, UIEasing.EaseInOut)
                .Leaf();
        }

        if (Text.Length > 0)
        {
            Ui.Line(Text, 0, enabled ? theme.Text : theme.TextDisabled);
        }

        Ui.Close();
    }
}

public class Slider : Widget
{
    private readonly char[] _valueText = new char[32];
    private int _valueLength;
    private float _formattedValue = float.NaN;
    private uint _trackId;
    private bool _dragging;
    private float _dragValue;

    public Slider(float min, float max)
    {
        Min = min;
        Max = max;
        Value = min;
    }

    public float Min { get; set; }
    public float Max { get; set; }
    public float Step { get; set; }
    public Bound<float> Value { get; set; }
    public Action<float>? OnChange { get; set; }
    public string? Format { get; set; }
    public float ValueWidth { get; set; } = 44;
    public bool Vertical { get; set; }
    public bool Live { get; set; } = true;
    public float KnobSize { get; set; } = 16;
    public float TrackThickness { get; set; } = 6;

    public float Current => _dragging && !Live ? _dragValue : Value.Value;

    protected override bool Focusable => true;
    internal override bool Interactive => true;

    internal override void Dispatch()
    {
        base.Dispatch();
        if (_trackId == 0)
        {
            return;
        }

        var track = Ui.Interaction(_trackId);
        var pressed = (State.Pressed || track.Pressed) && EnabledNow;
        var length = Vertical ? track.Rect.Height : track.Rect.Width;
        if (pressed && track.Pressed && length > KnobSize)
        {
            var along = Vertical ? length - track.PointerPosition.Y : track.PointerPosition.X;
            var value = Clamp(Min + Math.Clamp((along - KnobSize * 0.5f) / (length - KnobSize), 0, 1) * (Max - Min));
            if (Live)
            {
                Commit(value);
            }
            else
            {
                _dragValue = value;
                _dragging = true;
            }
        }
        else if (_dragging)
        {
            _dragging = false;
            Commit(_dragValue);
        }
    }

    protected internal override bool OnKeyDown(in KeyboardEventData key)
    {
        var step = Step > 0 ? Step : (Max - Min) / 100;
        var value = Value.Value;
        switch (key.KeyCode)
        {
            case KeyCode.Left:
            case KeyCode.Down:
                Commit(value - step);
                return true;
            case KeyCode.Right:
            case KeyCode.Up:
                Commit(value + step);
                return true;
            case KeyCode.Pagedown:
                Commit(value - step * 10);
                return true;
            case KeyCode.Pageup:
                Commit(value + step * 10);
                return true;
            case KeyCode.Home:
                Commit(Min);
                return true;
            case KeyCode.End:
                Commit(Max);
                return true;
            default:
                return false;
        }
    }

    internal override void Render()
    {
        if (_trackId == 0)
        {
            _trackId = Ui.Id(Id, 2);
        }

        var theme = Ui.Theme;
        var enabled = EnabledNow;
        var value = Current;
        var t = Max == Min ? 0 : Math.Clamp((value - Min) / (Max - Min), 0, 1);
        var pressed = IsPressed || _dragging;
        var active = pressed || IsHovered;
        var focusRing = IsFocused && Ui.FocusVisible;
        var knob = KnobSize;
        var thickness = TrackThickness;

        var desc = Desc();
        ref var layout = ref desc.Layout;
        if (Flex <= 0 && (Vertical ? Height : Width).Type == UIValueType.Auto)
        {
            layout.Grow = 1;
            layout.Shrink = 1;
            layout.Basis = UIValue.Points(0);
            layout.MinWidth = 60;
        }

        layout.Direction = Vertical ? UIDirection.Column : UIDirection.Row;
        layout.AlignItems = UIAlign.Center;
        layout.Gap = 10;
        desc.Pointer = UIPointerMode.Block;
        desc.Cursor = !enabled ? SystemCursor.NotAllowed : pressed ? Vertical ? SystemCursor.NsResize : SystemCursor.EwResize : SystemCursor.Pointer;

        Ui.Open(in desc);
        OfferTooltip();
        var track = Element.Box(_trackId)
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
            var rail = Element.Box()
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
                var fill = Element.Box().Background(enabled ? theme.Accent : theme.TextDisabled).CornerRadius(thickness * 0.5f);
                if (Vertical)
                {
                    fill.Height(UIValue.Percent(t));
                }
                else
                {
                    fill.Width(UIValue.Percent(t));
                }

                fill.Leaf();

                var thumb = Element.Box()
                    .Size(knob, knob)
                    .Background(pressed ? theme.Accent : theme.Surface)
                    .Border(!enabled ? theme.TextDisabled : focusRing ? theme.FocusRing : active ? theme.AccentHover : theme.Accent, 2)
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

        if (Format != null)
        {
            if (!_formattedValue.Equals(value))
            {
                _formattedValue = value;
                _valueLength = value.TryFormat(_valueText, out var written, Format, CultureInfo.InvariantCulture) ? written : 0;
            }

            using (Element.Row().Width(ValueWidth).Open())
            {
                Ui.Line(_valueText.AsSpan(0, _valueLength), theme.FontSize, enabled ? theme.TextMuted : theme.TextDisabled);
            }
        }

        Ui.Close();
    }

    private void Commit(float value)
    {
        value = Clamp(value);
        var bound = Value;
        if (bound.Value.Equals(value))
        {
            return;
        }

        bound.Set(value);
        Value = bound;
        OnChange?.Invoke(value);
    }

    private float Clamp(float value)
    {
        if (Step > 0)
        {
            value = Min + MathF.Round((value - Min) / Step) * Step;
        }

        return Math.Clamp(value, Math.Min(Min, Max), Math.Max(Min, Max));
    }
}

public readonly record struct Choice<T>(T Value, string Text, string? Icon = null);

public class Segmented<T> : Row
{
    public Segmented(IReadOnlyList<Choice<T>> choices)
    {
        Gap = 2;
        foreach (var choice in choices)
        {
            var value = choice.Value;
            Children.Add(new Button(choice.Text, choice.Icon)
            {
                Tone = Tone.Ghost,
                Small = true,
                Checked = Binding.Bind(() => EqualityComparer<T>.Default.Equals(Selected.Value, value)),
                OnClick = () => Select(value)
            });
        }
    }

    public Bound<T> Selected { get; set; }
    public Action<T>? OnChange { get; set; }

    private void Select(T value)
    {
        var selected = Selected;
        if (EqualityComparer<T>.Default.Equals(selected.Value, value))
        {
            return;
        }

        selected.Set(value);
        Selected = selected;
        OnChange?.Invoke(value);
    }
}

public class Dropdown : Widget
{
    private readonly DropdownList _list;

    public Dropdown(IReadOnlyList<string> items)
    {
        Items = items;
        Width = 180;
        _list = new DropdownList(this);
    }

    public IReadOnlyList<string> Items { get; set; }
    public Bound<int> Selected { get; set; }
    public Action<int>? OnChange { get; set; }
    public string Placeholder { get; set; } = "Select...";
    public float MaxListHeight { get; set; } = 240;

    public bool IsOpen => _list.IsOpen;

    protected override bool Clickable => true;
    protected override bool KeepsSize => true;

    public void Open()
    {
        if (!EnabledNow || Items.Count == 0)
        {
            return;
        }

        _list.Highlight = Math.Max(0, Selected.Value);
        _list.Reveal = true;
        _list.Show(this);
    }

    public void Close()
    {
        _list.Close();
    }

    protected override void OnActivate()
    {
        if (IsOpen)
        {
            Close();
        }
        else
        {
            Open();
        }

        base.OnActivate();
    }

    protected internal override bool OnKeyDown(in KeyboardEventData key)
    {
        if (IsOpen)
        {
            return _list.OnKeyDown(in key);
        }

        switch (key.KeyCode)
        {
            case KeyCode.Up:
                Pick(Math.Max(0, Selected.Value - 1));
                return true;
            case KeyCode.Down:
                Pick(Math.Min(Items.Count - 1, Selected.Value + 1));
                return true;
            default:
                return false;
        }
    }

    internal override void Render()
    {
        if (IsOpen && !EnabledNow)
        {
            Close();
        }

        var theme = Ui.Theme;
        var enabled = EnabledNow;
        var open = IsOpen;
        var desc = Desc();
        ref var layout = ref desc.Layout;
        layout.Justify = UIJustify.SpaceBetween;
        layout.AlignItems = UIAlign.Center;
        layout.Gap = 8;
        layout.Padding = new Edges(10, 6).ToLength();
        desc.Style.BorderWidth = UIEdges.All(1);
        desc.Style.BackgroundColor = !enabled ? theme.Disabled : open || IsHovered ? theme.Hover : theme.Surface;
        desc.Style.BorderColor = !enabled ? theme.Disabled : IsFocused && Ui.FocusVisible ? theme.FocusRing : open ? theme.Accent : IsHovered ? theme.BorderStrong : theme.Border;
        desc.Style.CornerRadius = UICorners.All(theme.CornerRadius);
        desc.Transition.Duration = theme.TransitionDuration;
        desc.Transition.Properties = (uint)UITransitionPropertyFlagBits.Colors;

        Ui.Open(in desc);
        OfferTooltip();
        var index = Selected.Value;
        var selected = index >= 0 && index < Items.Count ? Items[index] : null;
        Ui.Line(selected ?? Placeholder, 0, !enabled ? theme.TextDisabled : selected == null ? theme.TextMuted : theme.Text);
        if (FontAwesome.IsInitialized)
        {
            Ui.Glyph(open ? FontAwesome.AngleUp : FontAwesome.AngleDown, theme.SmallFontSize, enabled ? theme.TextMuted : theme.TextDisabled);
        }
        else
        {
            Ui.Line(open ? "^" : "v", theme.SmallFontSize, enabled ? theme.TextMuted : theme.TextDisabled);
        }

        Ui.Close();
    }

    internal void Pick(int index)
    {
        var selected = Selected;
        if (selected.Value != index)
        {
            selected.Set(index);
            Selected = selected;
            OnChange?.Invoke(index);
        }

        Close();
    }

    private sealed class DropdownList : Popup
    {
        private readonly Dropdown _owner;
        private readonly List<uint> _rowIds = new();

        public DropdownList(Dropdown owner)
        {
            _owner = owner;
            Width = UIValue.Percent(1);
            Padding = 4;
            Gap = 0;
        }

        public int Highlight { get; set; }
        public bool Reveal { get; set; }

        private protected override UIOverflow OverflowY => UIOverflow.Scroll;

        internal override void Dispatch()
        {
            base.Dispatch();
            for (var i = 0; i < _rowIds.Count && i < _owner.Items.Count; i++)
            {
                var interaction = Ui.Interaction(_rowIds[i]);
                if (interaction.HoverStarted)
                {
                    Highlight = i;
                }

                if (interaction.Clicked)
                {
                    _owner.Pick(i);
                    return;
                }
            }
        }

        protected internal override bool OnKeyDown(in KeyboardEventData key)
        {
            var count = _owner.Items.Count;
            switch (key.KeyCode)
            {
                case KeyCode.Up:
                    Move(Math.Max(0, Highlight - 1));
                    return true;
                case KeyCode.Down:
                    Move(Math.Min(count - 1, Highlight + 1));
                    return true;
                case KeyCode.Home:
                    Move(0);
                    return true;
                case KeyCode.End:
                    Move(count - 1);
                    return true;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                case KeyCode.Space:
                    _owner.Pick(Highlight);
                    return true;
                default:
                    return false;
            }
        }

        private protected override void BuildChildren()
        {
            var theme = Ui.Theme;
            var items = _owner.Items;
            while (_rowIds.Count < items.Count)
            {
                _rowIds.Add(Ui.Id(Id, (uint)_rowIds.Count + 1));
            }

            MaxHeight = _owner.MaxListHeight;
            var selectedIndex = _owner.Selected.Value;
            for (var i = 0; i < items.Count; i++)
            {
                var selected = i == selectedIndex;
                var highlighted = i == Highlight;
                using (Element.Row(_rowIds[i])
                           .AlignItems(UIAlign.Center)
                           .Padding(8, 6)
                           .CornerRadius(theme.CornerRadius - 2)
                           .Background(selected ? theme.AccentSoft : highlighted ? theme.Hover : default)
                           .Cursor(SystemCursor.Pointer)
                           .Open())
                {
                    Ui.Line(items[i], 0, selected ? theme.Accent : theme.Text);
                }
            }

            if (Reveal && Highlight >= 0 && Highlight < items.Count)
            {
                Ui.ScrollIntoView(_rowIds[Highlight]);
                Reveal = false;
            }
        }

        private void Move(int index)
        {
            Highlight = index;
            Reveal = true;
        }
    }
}

public class Meter : Widget
{
    public Meter()
    {
        Height = 6;
    }

    public Prop<float> Value { get; set; }
    public Prop<float> Secondary { get; set; }
    public ColorProp Color { get; set; }
    public ColorProp SecondaryColor { get; set; }
    public ColorProp Track { get; set; }

    internal override void Render()
    {
        var theme = Ui.Theme;
        var value = Math.Clamp(Value.Value, 0, 1);
        var secondary = Math.Clamp(Secondary.Value, 0, 1 - value);
        var desc = Desc();
        if (Width.Type == UIValueType.Auto && Flex <= 0)
        {
            desc.Layout.AlignSelf = UIAlignSelf.Stretch;
            if (Ui.ParentIsRow)
            {
                desc.Layout.Grow = 1;
                desc.Layout.Shrink = 1;
                desc.Layout.Basis = UIValue.Points(0);
            }
        }

        var height = Height.Type == UIValueType.Points ? Height.Value : 6;
        desc.Style.BackgroundColor = Track.IsSet ? Track.Value.Resolve(theme) : theme.BorderStrong.WithAlpha(90);
        desc.Style.CornerRadius = UICorners.All(height * 0.5f);
        desc.Scroll.OverflowX = UIOverflow.Clip;
        desc.Scroll.OverflowY = UIOverflow.Clip;
        Ui.Open(in desc);
        OfferTooltip();
        Element.Box()
            .Width(UIValue.Percent(value))
            .Background((Color.IsSet ? Color.Value : NiziKit.UI.Color.Accent).Resolve(theme))
            .Animate(UITransitionPropertyFlagBits.Size | UITransitionPropertyFlagBits.BackgroundColor, 0.3f)
            .Leaf();
        if (secondary > 0)
        {
            Element.Box()
                .Width(UIValue.Percent(secondary))
                .Background((SecondaryColor.IsSet ? SecondaryColor.Value : NiziKit.UI.Color.AccentSoft).Resolve(theme))
                .Animate(UITransitionPropertyFlagBits.Size, 0.3f)
                .Leaf();
        }

        Ui.Close();
    }
}
