using NiziKit.UI.Widgets;

namespace UIGallery;

internal sealed partial class Gallery
{
    private static readonly (string Name, UIJustify Justify)[] Justifies =
    [
        ("START", UIJustify.Start), ("CENTER", UIJustify.Center), ("END", UIJustify.End),
        ("SPACE_BETWEEN", UIJustify.SpaceBetween), ("SPACE_AROUND", UIJustify.SpaceAround), ("SPACE_EVENLY", UIJustify.SpaceEvenly)
    ];

    private static readonly string[] Tags = ["flexbox", "yoga", "wrap", "gap", "line gap", "immediate", "retained", "points", "percent", "auto margin", "absolute", "display none", "contents", "dividers"];

    private readonly uint _growId = Ui.Id("Gallery.Grow");
    private readonly uint _hiddenListId = Ui.Id("Gallery.HiddenList");
    private bool _expanded;
    private bool _listHidden;

    private Widget LayoutPage()
    {
        var hide = new Switch("Hide the list (Display NONE keeps its scroll offset)");
        hide.Changed += s => _listHidden = s.On;
        return Page(
            new Card("Justify", "Distribution of three fixed boxes along a row.") { new Immediate(DrawJustify) },
            new Card("Wrap and gaps", "Wrap WRAP with Gap between items and LineGap between lines.") { new Immediate(DrawWrap) },
            new Card("Auto margins, absolute and equal columns", "Margin left AUTO pushes to the end, ABSOLUTE insets place a badge, three Fill columns share the row.")
            {
                new Immediate(DrawMargins)
            },
            new Card("Display and dividers", "Dividers between in-flow children; a list hidden with Display NONE keeps its state; clicking the box animates its size.")
            {
                hide,
                new Immediate(DrawDisplay)
            });
    }

    private static void DrawJustify()
    {
        var theme = Ui.Theme;
        using (Ui.Column().Gap(8).Open())
        {
            foreach (var (name, justify) in Justifies)
            {
                using (Ui.Row().AlignItems(UIAlign.Center).Gap(12).Open())
                {
                    using (Ui.Row().Width(120).Shrink(0).Open())
                    {
                        Ui.Text(name, 12, theme.TextMuted);
                    }

                    using (Ui.Row().Fill().Justify(justify).Padding(6).Background(theme.SurfaceAlt).CornerRadius(6).Open())
                    {
                        for (var i = 0; i < 3; i++)
                        {
                            Ui.Element().Size(48, 24).Background(theme.Accent).CornerRadius(4).Leaf();
                        }
                    }
                }
            }
        }
    }

    private static void DrawWrap()
    {
        var theme = Ui.Theme;
        using (Ui.Row().Wrap().Gap(8).LineGap(8).Open())
        {
            foreach (var tag in Tags)
            {
                using (Ui.Row().Padding(10, 5).Background(theme.AccentSoft).CornerRadius(12).Open())
                {
                    Ui.Text(tag, 13, theme.Accent);
                }
            }
        }
    }

    private static void DrawMargins()
    {
        var theme = Ui.Theme;
        using (Ui.Column().Gap(16).Open())
        {
            using (Ui.Row().AlignItems(UIAlign.Center).Gap(8).Padding(10).Background(theme.SurfaceAlt).CornerRadius(8).Open())
            {
                Ui.Text(FontAwesome.Bars, 14, theme.Text, FontAwesome.FontId);
                Ui.Text("Toolbar", 14, theme.Text);
                using (Ui.Row().Margin(left: UILength.Auto).Gap(8).Open())
                {
                    Ui.Text("pushed right by Margin.Left = Auto", 12, theme.TextMuted);
                    Ui.Text(FontAwesome.Gear, 14, theme.Text, FontAwesome.FontId);
                }
            }

            using (Ui.Row().Gap(16).Open())
            {
                for (var i = 0; i < 3; i++)
                {
                    using (Ui.Column().Fill().Height(90).Padding(12).Background(theme.SurfaceAlt).CornerRadius(8).Open())
                    {
                        Ui.Text($"Column {i + 1}", 14, theme.Text);
                        Ui.Text("Grow 1, Shrink 1, Basis 0", 12, theme.TextMuted);
                        if (i == 1)
                        {
                            using (Ui.Row().Absolute(right: -6, top: -6).Padding(7, 2).Background(theme.Danger).CornerRadius(9).Open())
                            {
                                Ui.Text("3", 11, theme.OnAccent);
                            }
                        }
                    }
                }
            }
        }
    }

    private void DrawDisplay()
    {
        var theme = Ui.Theme;
        if (Ui.Interaction(_growId).Clicked)
        {
            _expanded = !_expanded;
        }

        using (Ui.Row().Gap(16).AlignItems(UIAlign.Start).Open())
        {
            using (Ui.Column(_hiddenListId)
                       .Width(220)
                       .Height(150)
                       .ScrollY()
                       .Hidden(_listHidden)
                       .Dividers(theme.Border)
                       .Background(theme.SurfaceAlt)
                       .CornerRadius(8)
                       .Open())
            {
                for (var i = 0; i < 20; i++)
                {
                    using (Ui.Row().Padding(12, 8).Shrink(0).Open())
                    {
                        Ui.Text($"Divided row {i + 1}", 13, theme.Text);
                    }
                }
            }

            using (Ui.Column(_growId)
                       .Size(_expanded ? 260 : 140, _expanded ? 150 : 80)
                       .Center()
                       .Background(_expanded ? theme.Accent : theme.AccentSoft)
                       .CornerRadius(_expanded ? 24 : 8)
                       .Cursor(SystemCursor.Pointer)
                       .Animate(UITransitionPropertyFlagBits.All, 0.35f, UIEasing.EaseOutBack)
                       .Open())
            {
                Ui.Text(_expanded ? "Click to shrink" : "Click to grow", 13, _expanded ? theme.OnAccent : theme.Accent);
            }
        }
    }
}
