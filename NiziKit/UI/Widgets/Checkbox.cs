using DenOfIz;

namespace NiziKit.UI.Widgets;

public class Checkbox : Widget
{
    private bool _checked;

    public Checkbox()
    {
        ref var layout = ref Desc.Layout;
        layout.AlignItems = UIAlign.Center;
        layout.AlignSelf = UIAlignSelf.Start;
        layout.Gap = 8;
        layout.Padding = UILengthEdges.Symmetric(4, 2);
        Desc.Style.BorderWidth = UIEdges.All(1);
        Desc.Cursor = SystemCursor.Pointer;
    }

    public Checkbox(string text, bool isChecked = false) : this()
    {
        Text = text;
        _checked = isChecked;
    }

    public string Text { get; set; } = "";

    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value)
            {
                return;
            }

            _checked = value;
            Changed?.Invoke(this);
        }
    }

    public float BoxSize { get; set; } = 18;

    public event Action<Checkbox>? Changed;

    protected override bool Focusable => true;

    protected override void OnClick()
    {
        Checked = !Checked;
    }

    protected override void OnBuild()
    {
        var theme = Ui.Theme;
        ref var desc = ref Desc;
        desc.Cursor = Enabled ? SystemCursor.Pointer : SystemCursor.NotAllowed;
        desc.Style.BorderColor = IsFocused && Ui.FocusVisible ? theme.FocusRing : default;
        desc.Style.CornerRadius = UICorners.All(theme.CornerRadius);

        var box = Enabled ? _checked ? theme.Accent : theme.Surface : theme.Disabled;
        var border = !Enabled ? theme.Disabled : _checked || IsHovered ? theme.Accent : theme.BorderStrong;
        using (OpenSelf())
        {
            using (Ui.Row()
                       .Size(BoxSize, BoxSize)
                       .Shrink(0)
                       .Center()
                       .Background(box)
                       .Border(border, 1.5f)
                       .CornerRadius(4)
                       .Animate()
                       .Open())
            {
                if (_checked)
                {
                    var mark = Enabled ? theme.OnAccent : theme.TextDisabled;
                    if (FontAwesome.IsInitialized)
                    {
                        Ui.Text(FontAwesome.Check, BoxSize * 0.62f, mark, FontAwesome.FontId);
                    }
                    else
                    {
                        Ui.Element().Size(BoxSize * 0.45f, BoxSize * 0.45f).Background(mark).CornerRadius(2).Leaf();
                    }
                }
            }

            if (Text.Length > 0)
            {
                Ui.Text(Text, 0, Enabled ? theme.Text : theme.TextDisabled);
            }
        }
    }
}

public class Switch : Widget
{
    private bool _on;

    public Switch()
    {
        ref var layout = ref Desc.Layout;
        layout.AlignItems = UIAlign.Center;
        layout.AlignSelf = UIAlignSelf.Start;
        layout.Gap = 10;
        layout.Padding = UILengthEdges.Symmetric(4, 2);
        Desc.Style.BorderWidth = UIEdges.All(1);
    }

    public Switch(string text, bool on = false) : this()
    {
        Text = text;
        _on = on;
    }

    public string Text { get; set; } = "";

    public bool On
    {
        get => _on;
        set
        {
            if (_on == value)
            {
                return;
            }

            _on = value;
            Changed?.Invoke(this);
        }
    }

    public event Action<Switch>? Changed;

    protected override bool Focusable => true;

    protected override void OnClick()
    {
        On = !On;
    }

    protected override void OnBuild()
    {
        var theme = Ui.Theme;
        ref var desc = ref Desc;
        desc.Cursor = Enabled ? SystemCursor.Pointer : SystemCursor.NotAllowed;
        desc.Style.BorderColor = IsFocused && Ui.FocusVisible ? theme.FocusRing : default;
        desc.Style.CornerRadius = UICorners.All(theme.CornerRadius);

        var track = !Enabled ? theme.Disabled : _on ? IsHovered ? theme.AccentHover : theme.Accent : IsHovered ? theme.TextMuted : theme.BorderStrong;
        using (OpenSelf())
        {
            using (Ui.Row()
                       .Size(40, 22)
                       .Shrink(0)
                       .Padding(2)
                       .AlignItems(UIAlign.Center)
                       .Justify(_on ? UIJustify.End : UIJustify.Start)
                       .Background(track)
                       .CornerRadius(11)
                       .Animate()
                       .Open())
            {
                Ui.Element()
                    .Size(18, 18)
                    .Background(Enabled ? theme.OnAccent : theme.TextDisabled)
                    .CornerRadius(9)
                    .Animate(UITransitionPropertyFlagBits.Position, 0.18f, UIEasing.EaseInOut)
                    .Leaf();
            }

            if (Text.Length > 0)
            {
                Ui.Text(Text, 0, Enabled ? theme.Text : theme.TextDisabled);
            }
        }
    }
}
