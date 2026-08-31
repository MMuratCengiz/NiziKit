using System.Numerics;
using DenOfIz;

namespace NiziKit.UI.Widgets;

public class Label : Widget
{
    private readonly TextCache _cache = new();

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
    public int FontSize { get; set; } = 14;
    public ushort FontId { get; set; } = Fonts.DefaultFontId;
    public Color Color { get; set; } = Color.Rgb(235, 235, 240);
    public bool Wrap { get; init; } = true;
    public Align TextAlign { get; init; } = Align.Start;
    public Align VerticalAlign { get; init; } = Align.Start;

    public Vector2 Measure()
    {
        return _cache.Measure(Text, FontId, (ushort)FontSize);
    }

    protected override void ApplyDeclaration(ref ClayElementDeclaration decl)
    {
        base.ApplyDeclaration(ref decl);
        decl.Layout.ChildAlignment.X = TextAlign.ToClayX();
        decl.Layout.ChildAlignment.Y = VerticalAlign.ToClayY();
    }

    protected override void OnStyleResolved(in StyleState state)
    {
        if (state.Text is { } text)
        {
            Color = text;
        }
    }

    protected override void BuildContent()
    {
        if (TextBinding != null)
        {
            var bound = TextBinding();
            if (!ReferenceEquals(bound, Text) && !string.Equals(bound, Text, StringComparison.Ordinal))
            {
                Text = bound;
            }
        }

        var view = _cache.Get(Text);
        if (view.NumChars == 0)
        {
            return;
        }

        var desc = ClayTextDesc.Default();
        desc.TextColor = Color.ToClay();
        desc.FontId = FontId;
        desc.FontSize = (ushort)FontSize;
        desc.WrapMode = Wrap ? ClayTextWrapMode.Words : ClayTextWrapMode.None;
        desc.TextAlignment = TextAlign.ToClayText();
        Ui.Clay.Text(view, in desc);
    }
}
