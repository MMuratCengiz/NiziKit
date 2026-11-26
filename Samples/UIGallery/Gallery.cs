namespace UIGallery;

internal enum GalleryPage
{
    Controls,
    Text,
    Lists,
    Overlays,
    Media,
    Layout,
    RichText
}

internal sealed partial class Gallery
{
    private static readonly float[] Zooms = [0.75f, 0.9f, 1f, 1.25f, 1.5f];

    private readonly Func<Texture?> _visualizer;
    private readonly Texture _pattern;
    private int _zoom = 2;
    private bool _dark = true;
    private bool _outlines;

    public Gallery(Func<Texture?> visualizer, Texture pattern)
    {
        _visualizer = visualizer;
        _pattern = pattern;
        Tabs = new Tabs<GalleryPage>
        {
            Pages =
            [
                new(GalleryPage.Controls, "Controls", FontAwesome.Sliders, ControlsPage()),
                new(GalleryPage.Text, "Text", FontAwesome.Keyboard, TextPage()),
                new(GalleryPage.Lists, "Lists", FontAwesome.List, ListsPage()),
                new(GalleryPage.Overlays, "Overlays", FontAwesome.LayerGroup, OverlaysPage()),
                new(GalleryPage.Media, "Media", FontAwesome.Image, MediaPage()),
                new(GalleryPage.Layout, "Layout", FontAwesome.TableCells, LayoutPage()),
                new(GalleryPage.RichText, "Rich text", FontAwesome.FileCode, RichTextPage())
            ]
        };
        Root = new Column
        {
            Flex = 1,
            Background = Color.Background,
            Children = [TopBar(), Tabs, StatusBar()]
        };
    }

    public Widget Root { get; }
    public Tabs<GalleryPage> Tabs { get; }
    public float FrameMilliseconds { get; set; }
    public long AllocatedPerFrame { get; set; }

    private Widget TopBar()
    {
        return new Row
        {
            Gap = 12,
            Padding = (16, 8),
            Background = Color.Surface,
            Border = Border.Bottom(Color.Border),
            Children =
            [
                new Row
                {
                    Width = 28,
                    Height = 28,
                    Justify = UIJustify.Center,
                    Background = Color.Accent,
                    Radius = 7,
                    Children = [new Label("Nz", TextStyle.Small with { Color = Color.OnAccent })]
                },
                new Label("NiziKit UI Gallery", TextStyle.Title),
                new Spacer(),
                new Button { Icon = FontAwesome.Minus, Tone = Tone.Ghost, Tooltip = "Zoom out (Ui.Scale)", OnClick = () => SetZoom(_zoom - 1) },
                new Label(() => $"{Ui.Scale * 100:0}%", TextStyle.Small) { Width = 44, Align = UITextAlign.Center },
                new Button { Icon = FontAwesome.Plus, Tone = Tone.Ghost, Tooltip = "Zoom in (Ui.Scale)", OnClick = () => SetZoom(_zoom + 1) },
                new Switch("Outlines")
                {
                    On = Bind(() => _outlines, on =>
                    {
                        _outlines = on;
                        Ui.DebugFlags = on ? UIDebugFlagBits.Outlines | UIDebugFlagBits.Hovered : UIDebugFlagBits.None;
                    })
                },
                new Switch("Dark")
                {
                    On = Bind(() => _dark, on =>
                    {
                        _dark = on;
                        Ui.Theme = on ? Theme.Dark() : Theme.Light();
                    })
                }
            ]
        };
    }

    private Widget StatusBar()
    {
        return new Row
        {
            Gap = 24,
            Padding = (16, 6),
            Background = Color.Surface,
            Border = Border.Top(Color.Border),
            Children =
            [
                new Label(() => $"{FrameMilliseconds:0.00} ms", TextStyle.Small),
                new Label(() => $"{AllocatedPerFrame} B allocated per frame", TextStyle.Small),
                new Label(() => $"pointer {Ui.PointerPosition.X:0},{Ui.PointerPosition.Y:0} pt", TextStyle.Small),
                new Spacer(),
                new Label(() => $"{Ui.ViewportSize.X:0}x{Ui.ViewportSize.Y:0} pt at {Ui.PixelsPerPoint:0.##} px/pt", TextStyle.Small)
            ]
        };
    }

    private void SetZoom(int index)
    {
        _zoom = Math.Clamp(index, 0, Zooms.Length - 1);
        Ui.Scale = Zooms[_zoom];
    }

    private static ScrollView Page(params Widget[] content)
    {
        return new ScrollView { Padding = 24, Gap = 16, Children = [.. content] };
    }

    private static Row Grid(params Widget[] cards)
    {
        return new Row { Wrap = true, Gap = 16, LineGap = 16, Align = UIAlign.Start, Children = [.. cards] };
    }

    private static Row Line(float gap, params Widget[] content)
    {
        return new Row { Gap = gap, Wrap = true, LineGap = gap, Children = [.. content] };
    }

    private static Column Card(string title, string description, float basis, params Widget[] content)
    {
        var card = new Column
        {
            Gap = 12,
            Padding = 16,
            Background = Color.Surface,
            Border = Color.Border,
            Radius = 10,
            Children = [new Label(title, TextStyle.Sized(16)), new Paragraph(description, TextStyle.Muted with { Size = 13 }), .. content]
        };
        if (basis > 0)
        {
            card.Flex = 1;
            card.Basis = basis;
        }

        return card;
    }

    private static Label Caption(string text)
    {
        return new Label(text, TextStyle.Small);
    }
}
