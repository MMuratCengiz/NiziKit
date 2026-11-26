namespace UIGallery;

internal sealed partial class Gallery
{
    private static readonly (string Name, UIJustify Justify)[] Justifies =
    [
        ("START", UIJustify.Start), ("CENTER", UIJustify.Center), ("END", UIJustify.End),
        ("SPACE_BETWEEN", UIJustify.SpaceBetween), ("SPACE_AROUND", UIJustify.SpaceAround), ("SPACE_EVENLY", UIJustify.SpaceEvenly)
    ];

    private static readonly string[] Tags = ["flexbox", "yoga", "wrap", "gap", "line gap", "retained", "bindings", "points", "percent", "spacer", "float", "visible", "dividers"];

    private static readonly string[] BorderTabs = ["Agent", "Changes", "Terminal"];

    private bool _expanded;
    private bool _listHidden;
    private int _borderTab;
    private int _borderRow;

    private Widget LayoutPage()
    {
        return Page(
            Card("Justify", "Distribution of three fixed boxes along a row.", 0,
                new Column
                {
                    Gap = 8,
                    Children =
                    [
                        .. Justifies.Select(j => new Row
                        {
                            Gap = 12,
                            Children =
                            [
                                new Label(j.Name, TextStyle.Small) { Width = 120 },
                                new Row
                                {
                                    Flex = 1,
                                    Justify = j.Justify,
                                    Padding = 6,
                                    Background = Color.SurfaceAlt,
                                    Radius = 6,
                                    Children = [Swatch(), Swatch(), Swatch()]
                                }
                            ]
                        })
                    ]
                }),
            Card("Wrap and gaps", "Wrap with Gap between items and LineGap between lines.", 0,
                new Row
                {
                    Wrap = true,
                    Gap = 8,
                    LineGap = 8,
                    Children =
                    [
                        .. Tags.Select(tag => new Row
                        {
                            Padding = (10, 5),
                            Background = Color.AccentSoft,
                            Radius = 12,
                            Children = [new Label(tag, TextStyle.Sized(13, Color.Accent))]
                        })
                    ]
                }),
            Card("Spacers, floating badges and equal columns", "A Spacer pushes to the end, Float places a badge over its parent, three Flex columns share the row. Shrink the window: the toolbar text ellipsizes instead of overflowing.", 0,
                new Row
                {
                    Gap = 8,
                    Padding = 10,
                    Background = Color.SurfaceAlt,
                    Radius = 8,
                    Children =
                    [
                        new Icon(FontAwesome.Bars),
                        new Label("Toolbar"),
                        new Spacer(),
                        new Label("pushed right by a Spacer", TextStyle.Small),
                        new Icon(FontAwesome.Gear)
                    ]
                },
                new Row
                {
                    Gap = 16,
                    Children = [.. Enumerable.Range(0, 3).Select(EqualColumn)]
                }),
            Card("Visibility and dividers", "Dividers between children; a hidden list is not built, and its scroll offset comes back when it is shown; clicking the box animates its size.", 0,
                new Switch("Hide the list") { On = Bind(() => _listHidden, hidden => _listHidden = hidden) },
                new Row
                {
                    Gap = 16,
                    Align = UIAlign.Start,
                    Children =
                    [
                        new ScrollView
                        {
                            Width = 220,
                            Height = 150,
                            Visible = Bind(() => !_listHidden),
                            Dividers = Color.Border,
                            Background = Color.SurfaceAlt,
                            Radius = 8,
                            Children = [.. Enumerable.Range(0, 20).Select(i => new Row { Padding = (12, 8), Children = [new Label($"Divided row {i + 1}", TextStyle.Sized(13))] })]
                        },
                        GrowBox()
                    ]
                }),
            BordersCard());
    }

    private Widget BordersCard()
    {
        return Card("Transparent borders",
            "A Border built with Color.Transparent is set: BorderColor colors its sides and widths, and it takes no space while nothing colors it. " +
            "Click the tabs and rows. A Border left unset with a BorderColor draws 1 pt on every side.", 0,
            new Row { Gap = 4, Children = [.. BorderTabs.Select(BorderTab)] },
            new Row
            {
                Gap = 16,
                Align = UIAlign.Start,
                Children =
                [
                    new Column { Width = 300, Gap = 2, Children = [.. Enumerable.Range(0, 3).Select(BorderRow)] },
                    new Column
                    {
                        Gap = 8,
                        Children =
                        [
                            new Row { Name = "layout.border.unset", Padding = (12, 6), BorderColor = Color.Accent, Children = [Caption("unset Border, BorderColor = Accent")] },
                            new Row { Name = "layout.border.clear", Padding = (12, 6), Border = Color.Transparent, Children = [Caption("Border = Color.Transparent alone")] }
                        ]
                    }
                ]
            });
    }

    private Widget BorderTab(string title, int index)
    {
        return new Row
        {
            Name = $"layout.border.tab.{index}",
            Padding = (12, 6),
            Border = Border.Bottom(Color.Transparent, 2),
            BorderColor = Bind(() => _borderTab == index ? Color.Accent : Color.Transparent),
            HoverBackground = Color.Hover,
            OnClick = () => _borderTab = index,
            Children = [new Label(title, TextStyle.Sized(13))]
        };
    }

    private Widget BorderRow(int index)
    {
        return new Row
        {
            Name = $"layout.border.row.{index}",
            Padding = (10, 6),
            Radius = 3,
            Border = Border.Left(Color.Transparent, 3),
            BorderColor = Bind(() => _borderRow == index ? Color.Accent : Color.Transparent),
            Background = Bind(() => _borderRow == index ? Color.AccentSoft : Color.Transparent),
            HoverBackground = Color.Hover,
            OnClick = () => _borderRow = index,
            Children = [new Label($"Row {index + 1}: Border.Left(Transparent, 3)", TextStyle.Small)]
        };
    }

    private static Widget Swatch()
    {
        return new Row { Width = 48, Height = 24, Background = Color.Accent, Radius = 4 };
    }

    private static Widget EqualColumn(int i)
    {
        var column = new Column
        {
            Flex = 1,
            Height = 90,
            Padding = 12,
            Background = Color.SurfaceAlt,
            Radius = 8,
            Children = [new Label($"Column {i + 1}"), new Label("Flex = 1", TextStyle.Small)]
        };
        if (i == 1)
        {
            column.Children.Add(new Row
            {
                Float = UIAnchor.TopRight,
                Offset = new(6, -6),
                Padding = (7, 2),
                Background = Color.Danger,
                Radius = 9,
                Children = [new Label("3", TextStyle.Sized(11, Color.OnAccent))]
            });
        }

        return column;
    }

    private Widget GrowBox()
    {
        var box = new Column
        {
            Width = 140,
            Height = 80,
            Justify = UIJustify.Center,
            Align = UIAlign.Center,
            Background = Color.AccentSoft,
            Radius = 8,
            Animate = UITransitionPropertyFlagBits.All,
            AnimateDuration = 0.35f,
            Easing = UIEasing.EaseOutBack,
            Children = [new Label(() => _expanded ? "Click to shrink" : "Click to grow", TextStyle.Sized(13)) { Color = Bind(() => _expanded ? Color.OnAccent : Color.Accent) }]
        };
        box.OnClick = () =>
        {
            _expanded = !_expanded;
            box.Width = _expanded ? 260 : 140;
            box.Height = _expanded ? 150 : 80;
            box.Radius = _expanded ? 24 : 8;
            box.Background = _expanded ? Color.Accent : Color.AccentSoft;
        };
        return box;
    }
}
