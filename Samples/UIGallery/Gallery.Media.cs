using System.Numerics;
using NiziKit.UI.Widgets;

namespace UIGallery;

internal sealed partial class Gallery
{
    private readonly uint _roundedListId = Ui.Id("Gallery.RoundedList");
    private readonly uint _hoverImageId = Ui.Id("Gallery.HoverImage");
    private int _roundedSelection = 3;

    private Widget MediaPage()
    {
        return Page(
            new Card("Render target as an image", "A second UI renders into its own texture each frame; its semaphore is waited on before the gallery samples it. Each fit mode places the 320x180 texture in a 150 point tall box.")
            {
                Columns(
                    FitColumn("Fill", UIImageFit.Fill),
                    FitColumn("Contain", UIImageFit.Contain),
                    FitColumn("Cover", UIImageFit.Cover))
            },
            new Card("Images", "Image leaves are measured from the texture, one texel per point, and keep its aspect ratio when one axis is constrained.")
            {
                new Immediate(DrawImages)
            },
            new Card("Rounded clipping", "Rows scroll inside a list clipped to its rounded padding box; hit testing follows the clip.")
            {
                new Immediate(DrawRoundedList)
            });
    }

    private VStack FitColumn(string caption, UIImageFit fit)
    {
        return Column(caption, new Image { Source = _visualizer, Fit = fit, Height = 150, Background = UIColor.Hex(0x05070CFF), CornerRadius = 8 });
    }

    private void DrawImages()
    {
        var theme = Ui.Theme;
        using (Ui.Row().Gap(16).Wrap().LineGap(16).AlignItems(UIAlign.Start).Open())
        {
            Captioned("Natural size", Ui.Element().Image(_pattern));
            Captioned("Width 200, height from the aspect ratio", Ui.Element().Image(_pattern).Width(200));
            Captioned("Rounded, radius 28", Ui.Element().Image(_pattern).Width(200).CornerRadius(28));
            Captioned("Tint", Ui.Element().Image(_pattern).Width(200).CornerRadius(8).Tint(UIColor.Hex(0x8FD1FFFF)));
            Captioned("UV, top left quarter", Ui.Element().Image(_pattern).Size(200, 112).CornerRadius(8).Uv(new Vector4(0, 0, 0.5f, 0.5f)));

            var hovered = Ui.Interaction(_hoverImageId).Hovered;
            Captioned("Overlay on hover",
                Ui.Element(_hoverImageId).Image(_pattern).Width(200).CornerRadius(8)
                    .Overlay(hovered ? theme.Accent.WithAlpha(150) : default)
                    .Animate(UITransitionPropertyFlagBits.OverlayColor, 0.2f));
        }
    }

    private static void Captioned(string caption, in Element image)
    {
        using (Ui.Column().Gap(6).Open())
        {
            image.Leaf();
            Ui.Text(caption, 12, Ui.Theme.TextMuted);
        }
    }

    private void DrawRoundedList()
    {
        var theme = Ui.Theme;
        using (Ui.Row().Gap(16).AlignItems(UIAlign.Start).Open())
        {
            using (Ui.Column(_roundedListId)
                       .Width(280)
                       .Height(220)
                       .ScrollY()
                       .Border(theme.Border, 1)
                       .CornerRadius(18)
                       .Open())
            {
                for (var i = 0; i < 40; i++)
                {
                    var id = Ui.Id(_roundedListId, (uint)i);
                    var interaction = Ui.Interaction(id);
                    if (interaction.Clicked)
                    {
                        _roundedSelection = i;
                    }

                    var selected = i == _roundedSelection;
                    using (Ui.Row(id)
                               .Padding(16, 10)
                               .Shrink(0)
                               .Background(selected ? theme.Accent : interaction.Hovered ? theme.Hover : (i & 1) == 0 ? theme.Surface : theme.SurfaceAlt)
                               .Cursor(SystemCursor.Pointer)
                               .Animate()
                               .Open())
                    {
                        Ui.Text($"Row {i + 1}", 14, selected ? theme.OnAccent : theme.Text);
                    }
                }
            }

            using (Ui.Column()
                       .Width(240)
                       .Clip()
                       .Border(theme.Border, 1)
                       .CornerRadius(18)
                       .Background(theme.SurfaceAlt)
                       .Open())
            {
                Ui.Element().Image(_pattern, UIImageFit.Cover).Height(120).Leaf();
                using (Ui.Column().Padding(14).Gap(4).Open())
                {
                    Ui.Text("Card with a cover image", 15, theme.Text);
                    Ui.Text("The image has no radius of its own; the card's rounded clip cuts its corners.", 12, theme.TextMuted);
                }
            }
        }
    }
}
