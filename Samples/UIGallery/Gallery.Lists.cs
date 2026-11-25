using NiziKit.UI.Widgets;

namespace UIGallery;

internal sealed partial class Gallery
{
    private const int FileCount = 1000;
    private static readonly string[] Kinds = ["Texture", "Mesh", "Material", "Shader", "Audio", "Scene", "Script"];

    private readonly uint _stripId = Ui.Id("Gallery.Strip");
    private int _opened = -1;

    public ListView Files { get; private set; } = null!;

    private Widget ListsPage()
    {
        Files = new ListView { Count = FileCount, Height = 360, ItemTemplate = DrawFileRow };
        Files.ItemActivated += (_, index) => _opened = index;

        var top = new Button("Scroll to top") { Icon = FontAwesome.ArrowUp };
        top.Clicked += _ => Ui.ScrollTo(Files.Id, default);
        var bottom = new Button("Scroll to bottom") { Icon = FontAwesome.ArrowDown };
        bottom.Clicked += _ => Ui.ScrollToEnd(Files.Id);
        var select = new Button("Select row 500");
        select.Clicked += _ =>
        {
            Files.SelectedIndex = 500;
            Files.ScrollToSelected();
            Files.Focus();
        };

        var stripStart = new Button("Strip start") { Kind = ButtonKind.Ghost };
        stripStart.Clicked += _ => Ui.ScrollTo(_stripId, default);
        var stripEnd = new Button("Strip end") { Kind = ButtonKind.Ghost };
        stripEnd.Clicked += _ => Ui.ScrollTo(_stripId, new System.Numerics.Vector2(float.MaxValue, 0));

        return Page(
            new Card("1,000 rows", "Explicit ids from Ui.Id(list, index). Click selects, double click or Enter opens; Up, Down, Page Up, Page Down, Home and End move the selection, which is scrolled into view. The rows are clipped to the list's rounded corners.")
            {
                Row(8, top, bottom, select, new Immediate(DrawListState)),
                Files
            },
            new Card("Horizontal strip", "OverflowX SCROLL with fixed-size cards; scroll with a horizontal wheel or trackpad, or drag the scrollbar.")
            {
                Row(8, stripStart, stripEnd),
                new Immediate(DrawStrip)
            });
    }

    private void DrawFileRow(int index, bool selected)
    {
        var theme = Ui.Theme;
        Ui.Text($"Row {index:0000}", 13, selected ? theme.Accent : theme.Text);
        Ui.Text(Kinds[index % Kinds.Length], 12, theme.TextMuted);
        using (Ui.Row().Margin(left: UILength.Auto).Open())
        {
            Ui.Text($"{index * 37 % 1000}.{index % 10} KB", 12, theme.TextMuted);
        }
    }

    private void DrawListState()
    {
        var theme = Ui.Theme;
        Ui.TryGetScroll(Files.Id, out var scroll);
        using (Ui.Column().Margin(left: UILength.Auto).AlignItems(UIAlign.End).Open())
        {
            Ui.Text($"offset {scroll.Offset.Y:0} of {scroll.MaxOffset.Y:0}, viewport {scroll.ViewportSize.Y:0}, content {scroll.ContentSize.Y:0}", 12, theme.TextMuted);
            Ui.Text($"selected {Files.SelectedIndex}, opened {_opened}", 12, theme.TextMuted);
        }
    }

    private void DrawStrip()
    {
        var theme = Ui.Theme;
        using (Ui.Row(_stripId)
                   .Gap(12)
                   .Padding(12)
                   .AlignItems(UIAlign.Start)
                   .ScrollX()
                   .Background(theme.SurfaceAlt)
                   .CornerRadius(14)
                   .Open())
        {
            for (var i = 0; i < 24; i++)
            {
                var hue = (byte)(i * 10);
                using (Ui.Column()
                           .Size(150, 96)
                           .Shrink(0)
                           .Padding(12)
                           .Justify(UIJustify.SpaceBetween)
                           .Background(UIColor.Rgba((byte)(60 + hue), (byte)(110 + hue / 2), (byte)(230 - hue)))
                           .CornerRadius(10)
                           .Open())
                {
                    Ui.Text($"Card {i + 1}", 16, theme.OnAccent);
                    Ui.Text("OverflowX SCROLL", 11, theme.OnAccent);
                }
            }
        }
    }
}
