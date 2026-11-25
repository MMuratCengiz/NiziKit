using NiziKit.UI.Widgets;

namespace UIGallery;

internal sealed class Card : VStack
{
    public Card(string title, string description, float basis = 0)
    {
        Gap = 12;
        Padding = UILengthEdges.All(16);
        BorderWidth = 1;
        CornerRadius = 10;
        if (basis > 0)
        {
            Grow = 1;
            Shrink = 1;
            Basis = basis;
        }

        Add(new Label(title) { FontSize = 16 });
        Add(new Label(description) { Muted = true, FontSize = 13 });
    }

    protected override void OnBuild()
    {
        Background = Ui.Theme.Surface;
        BorderColor = Ui.Theme.Border;
        base.OnBuild();
    }
}

internal sealed partial class Gallery
{
    private static readonly float[] Zooms = [0.75f, 0.9f, 1f, 1.25f, 1.5f];

    private readonly uint _rootId = Ui.Id("Gallery");
    private readonly uint _topBarId = Ui.Id("Gallery.TopBar");
    private readonly uint _statusId = Ui.Id("Gallery.Status");
    private readonly Switch _darkMode = new("Dark", true);
    private readonly Switch _outlines = new("Outlines");
    private readonly Button _zoomOut = new() { Icon = FontAwesome.Minus, Kind = ButtonKind.Ghost, Tooltip = "Zoom out (SetScale)" };
    private readonly Button _zoomIn = new() { Icon = FontAwesome.Plus, Kind = ButtonKind.Ghost, Tooltip = "Zoom in (SetScale)" };
    private readonly Func<Texture?> _visualizer;
    private readonly Texture _pattern;
    private int _zoom = 2;

    public Gallery(Func<Texture?> visualizer, Texture pattern)
    {
        _visualizer = visualizer;
        _pattern = pattern;
        _darkMode.Changed += s => Ui.Theme = s.On ? Theme.Dark() : Theme.Light();
        _outlines.Changed += s => Ui.DebugFlags = s.On ? UIDebugFlagBits.Outlines | UIDebugFlagBits.Hovered : UIDebugFlagBits.None;
        _zoomOut.Clicked += _ => SetZoom(_zoom - 1);
        _zoomIn.Clicked += _ => SetZoom(_zoom + 1);

        Tabs = new TabView { Fill = true };
        Tabs.Add("Controls", ControlsPage(), FontAwesome.Sliders);
        Tabs.Add("Text", TextPage(), FontAwesome.Keyboard);
        Tabs.Add("Lists", ListsPage(), FontAwesome.List);
        Tabs.Add("Overlays", OverlaysPage(), FontAwesome.LayerGroup);
        Tabs.Add("Media", MediaPage(), FontAwesome.Image);
        Tabs.Add("Layout", LayoutPage(), FontAwesome.TableCells);
    }

    public TabView Tabs { get; }

    public float FrameMilliseconds { get; set; }
    public long AllocatedPerFrame { get; set; }

    public void Draw()
    {
        var theme = Ui.Theme;
        using (Ui.Column(_rootId).Grow().Background(theme.Background).Open())
        {
            using (Ui.Row(_topBarId)
                       .AlignItems(UIAlign.Center)
                       .Gap(12)
                       .Padding(16, 8)
                       .Shrink(0)
                       .Background(theme.Surface)
                       .Border(theme.Border, new UIEdges(0, 0, 0, 1))
                       .Open())
            {
                using (Ui.Row().Size(28, 28).Center().Background(theme.Accent).CornerRadius(7).Open())
                {
                    Ui.Text("Nz", 12, theme.OnAccent);
                }

                Ui.Text("NiziKit UI Gallery", theme.TitleFontSize, theme.Text);
                using (Ui.Row().Margin(left: UILength.Auto).AlignItems(UIAlign.Center).Gap(4).Open())
                {
                    Ui.Draw(_zoomOut);
                    using (Ui.Row().Width(44).Justify(UIJustify.Center).Open())
                    {
                        Ui.Text($"{Ui.Scale * 100:0}%", theme.SmallFontSize, theme.TextMuted);
                    }

                    Ui.Draw(_zoomIn);
                }

                Ui.Draw(_outlines);
                Ui.Draw(_darkMode);
            }

            Ui.Draw(Tabs);

            using (Ui.Row(_statusId)
                       .Gap(24)
                       .Padding(16, 6)
                       .Shrink(0)
                       .Background(theme.Surface)
                       .Border(theme.Border, new UIEdges(0, 1, 0, 0))
                       .Open())
            {
                var muted = Ui.TextStyle(theme.SmallFontSize, theme.TextMuted);
                Ui.Text($"{FrameMilliseconds:0.00} ms", in muted);
                Ui.Text($"{AllocatedPerFrame} B allocated per frame", in muted);
                var pointer = Ui.PointerPosition;
                Ui.Text($"pointer {pointer.X:0},{pointer.Y:0} pt", in muted);
                Ui.Text($"hovered {Ui.HoveredId:X8}  focus {Ui.Focus:X8}", in muted);
                using (Ui.Row().Margin(left: UILength.Auto).Open())
                {
                    Ui.Text($"{Ui.ViewportSize.X:0}x{Ui.ViewportSize.Y:0} pt at {Ui.PixelsPerPoint:0.##} px/pt", in muted);
                }
            }
        }
    }

    private void SetZoom(int index)
    {
        _zoom = Math.Clamp(index, 0, Zooms.Length - 1);
        Ui.Scale = Zooms[_zoom];
    }

    private static ScrollView Page(params ReadOnlySpan<Widget> content)
    {
        var page = new ScrollView { Padding = UILengthEdges.All(24), Gap = 16 };
        page.Add(content);
        return page;
    }

    private static Panel Grid(params ReadOnlySpan<Widget> cards)
    {
        var grid = new Panel { Wrap = UIFlexWrap.Wrap, Gap = 16, LineGap = 16, AlignItems = UIAlign.Start };
        grid.Add(cards);
        return grid;
    }

    private static HStack Row(float gap, params ReadOnlySpan<Widget> content)
    {
        var row = new HStack { Gap = gap, Wrap = UIFlexWrap.Wrap, LineGap = gap };
        row.Add(content);
        return row;
    }

    private static Label Caption(string text)
    {
        return new Label(text) { Muted = true, FontSize = 12 };
    }
}
