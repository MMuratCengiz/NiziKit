using System.Numerics;

namespace UIGallery;

internal sealed partial class Gallery
{
    private readonly List<Image> _visualizerImages = new();
    private int _roundedSelection = 3;

    public bool VisualizerShown
    {
        get
        {
            var viewport = Ui.ViewportSize;
            foreach (var image in _visualizerImages)
            {
                var bounds = image.Bounds;
                if (image.IsShown && bounds.Right > 0 && bounds.Bottom > 0 && bounds.X < viewport.X && bounds.Y < viewport.Y)
                {
                    return true;
                }
            }

            return false;
        }
    }

    private Widget MediaPage()
    {
        return Page(
            Card("Render target as an image", "A second UI renders into its own texture each frame; its semaphore is waited on before the gallery samples it. Each fit mode places the 320x180 texture in a 150 point tall box.", 0,
                new Row
                {
                    Gap = 16,
                    Align = UIAlign.Start,
                    Children = [FitColumn("Fill", UIImageFit.Fill), FitColumn("Contain", UIImageFit.Contain), FitColumn("Cover", UIImageFit.Cover)]
                }),
            Card("Images", "Image leaves are measured from the texture, one texel per point, and keep its aspect ratio when one axis is constrained.", 0,
                new Row
                {
                    Gap = 16,
                    Wrap = true,
                    LineGap = 16,
                    Align = UIAlign.Start,
                    Children =
                    [
                        Captioned("Natural size", new Image(_pattern), false),
                        Captioned("Width 200, height from the aspect ratio", new Image(_pattern) { Width = 200 }, false),
                        Captioned("Rounded, radius 28", new Image(_pattern) { Width = 200, Radius = 28 }, false),
                        Captioned("Tint", new Image(_pattern) { Width = 200, Radius = 8, Tint = Color.Hex(0x8FD1FFFF) }, false),
                        Captioned("UV, top left quarter", new Image(_pattern) { Width = 200, Height = 112, Radius = 8, Uv = new Vector4(0, 0, 0.5f, 0.5f) }, false),
                        Captioned("Overlay on hover", new Image(_pattern) { Width = 200, Radius = 8, HoverOverlay = Color.Accent.WithAlpha(150) }, false)
                    ]
                }),
            Card("Rounded clipping", "Rows scroll inside a list clipped to its rounded padding box; hit testing follows the clip.", 0,
                new Row
                {
                    Gap = 16,
                    Align = UIAlign.Start,
                    Children =
                    [
                        new ScrollView
                        {
                            Width = 280,
                            Height = 220,
                            Border = Color.Border,
                            Radius = 18,
                            Children = [.. Enumerable.Range(0, 40).Select(RoundedRow)]
                        },
                        new Column
                        {
                            Width = 240,
                            Clip = true,
                            Border = Color.Border,
                            Radius = 18,
                            Background = Color.SurfaceAlt,
                            Children =
                            [
                                new Image(_pattern) { Fit = UIImageFit.Cover, Height = 120 },
                                new Column
                                {
                                    Padding = 14,
                                    Gap = 4,
                                    Children =
                                    [
                                        new Label("Card with a cover image", TextStyle.Sized(15)),
                                        new Paragraph("The image has no radius of its own; the card's rounded clip cuts its corners.", TextStyle.Small)
                                    ]
                                }
                            ]
                        }
                    ]
                }));
    }

    private Column FitColumn(string caption, UIImageFit fit)
    {
        var image = new Image { Source = Bind(_visualizer), Fit = fit, Height = 150, Background = Color.Hex(0x05070CFF), Radius = 8 };
        _visualizerImages.Add(image);
        return Captioned(caption, image);
    }

    private static Column Captioned(string caption, Widget content, bool fill)
    {
        return new Column { Gap = 6, Flex = fill ? 1 : 0, Children = [content, Caption(caption)] };
    }

    private Widget RoundedRow(int i)
    {
        return new Row
        {
            Padding = (16, 10),
            Background = Bind(() => i == _roundedSelection ? Color.Accent : (i & 1) == 0 ? Color.Surface : Color.SurfaceAlt),
            HoverBackground = Color.Hover,
            OnClick = () => _roundedSelection = i,
            Children = [new Label($"Row {i + 1}", TextStyle.Sized(14)) { Color = Bind(() => i == _roundedSelection ? Color.OnAccent : Color.Text) }]
        };
    }
}
