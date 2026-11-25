using DenOfIz;

namespace NiziKit.UI.Widgets;

public class Label : Widget
{
    private uint _textId;

    public Label()
    {
    }

    public Label(string text)
    {
        Text = text;
    }

    public Label(Func<string> binding)
    {
        TextBinding = binding;
    }

    public string Text { get; set; } = "";

    public Func<string>? TextBinding { get; set; }

    public float FontSize { get; set; }

    public ushort FontId { get; set; }

    public UIColor? Color { get; set; }

    public bool Muted { get; set; }

    public UITextWrap Wrap { get; set; } = UITextWrap.Words;
    public UITextAlign Align { get; set; }

    public uint MaxLines { get; set; }

    public bool Ellipsis { get; set; }

    protected override void OnBuild()
    {
        var text = TextBinding?.Invoke() ?? Text;
        var theme = Ui.Theme;
        var color = Color ?? (!Enabled ? theme.TextDisabled : Muted ? theme.TextMuted : theme.Text);
        var desc = Ui.TextStyle(FontSize, color, FontId);
        desc.Wrap = Wrap;
        desc.Align = Align;
        desc.MaxLines = MaxLines;
        desc.Overflow = Ellipsis ? UITextOverflow.Ellipsis : UITextOverflow.Clip;

        if (!HasCustomLayout)
        {
            if (!Visible)
            {
                return;
            }

            desc.Id = Id;
            Ui.Text(text, in desc);
            return;
        }

        if (_textId == 0)
        {
            _textId = Ui.Id(Id, 1);
        }

        using (OpenSelf())
        {
            desc.Id = _textId;
            Ui.Text(text, in desc);
        }
    }
}
