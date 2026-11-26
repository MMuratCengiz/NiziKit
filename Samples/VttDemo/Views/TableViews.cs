using VttDemo.Model;
using VttDemo.UiKit;

namespace VttDemo.Views;

// Turn order. Rows are keyed by character, so re-rolling initiative slides them to their new places.
public sealed class InitiativeView
{
    private readonly Campaign _campaign;
    private readonly MapView _map;
    private readonly Action<Character> _openSheet;
    private readonly ForEach<Character> _rows;
    private readonly ScrollView _list;
    private Character? _shownCurrent;

    public InitiativeView(Campaign campaign, MapView map, Action<Character> openSheet)
    {
        _campaign = campaign;
        _map = map;
        _openSheet = openSheet;
        _rows = new ForEach<Character>(() => campaign.Order, OrderRow);
        _list = new ScrollView { Padding = (8, 8, 14, 8), Gap = 3, Children = [_rows], OnUpdate = RevealCurrent };
        var root = new Column { Flex = 1, Children = [Header(), _list, Footer()] };
        Window = new FloatingWindow("Initiative", root) { Icon = FontAwesome.ListOl, MinSize = new(250, 200) };
    }

    public FloatingWindow Window { get; }

    private Widget Header()
    {
        return new Row
        {
            Padding = (12, 8),
            Gap = 6,
            Border = Border.Bottom(Color.Border),
            Children =
            [
                new Column { Children = [new Label("ROUND", TextStyle.Caption), new Label(() => Fmt.Int(_campaign.Round), TextStyle.Sized(22))] },
                new Spacer(),
                new Button("Roll", FontAwesome.Cube) { Small = true, Tooltip = "Roll initiative for everyone", OnClick = _campaign.RollInitiative },
                new Button { Icon = FontAwesome.AngleLeft, Tooltip = "Previous turn", OnClick = _campaign.PreviousTurn },
                new Button("Next", FontAwesome.AngleRight) { Tone = Tone.Primary, Small = true, Tooltip = "Next turn (N)", OnClick = _campaign.NextTurn }
            ]
        };
    }

    private Widget OrderRow(Character c)
    {
        return new Row
        {
            Padding = (8, 6),
            Gap = 10,
            Radius = 6,
            Background = Bind(() => IsCurrent(c) ? Color.AccentSoft : Color.Transparent),
            HoverBackground = Color.Hover,
            BorderColor = Bind(() => IsCurrent(c) ? Color.Accent : ReferenceEquals(c, _map.Selected) ? Color.BorderStrong : Color.Transparent),
            Overlay = Bind(() => c.IsDown ? Color.Hex(0x00000060) : Color.Transparent),
            Animate = UITransitionPropertyFlagBits.Position | UITransitionPropertyFlagBits.Colors,
            AnimateDuration = 0.28f,
            Easing = UIEasing.EaseInOut,
            OnClick = () => _map.Selected = c,
            OnDoubleClick = () => _openSheet(c),
            Children =
            [
                new Row
                {
                    Width = 30,
                    Height = 24,
                    Justify = UIJustify.Center,
                    Radius = 5,
                    Background = Bind(() => IsCurrent(c) ? Color.Accent : Color.SurfaceAlt),
                    Children = [new Label(() => Fmt.Int(c.Initiative), TextStyle.Sized(13)) { Color = Bind(() => IsCurrent(c) ? Color.OnAccent : Color.Text) }]
                },
                Kit.Portrait(c, 26),
                new Column
                {
                    Flex = 1,
                    Gap = 4,
                    Children = [new Label(c.Name, TextStyle.Sized(13)) { Color = Bind(() => c.IsDown ? Color.TextDisabled : Color.Text) }, Kit.HpBar(c, 4)]
                },
                new Row
                {
                    Gap = 3,
                    Visible = Bind(() => c.Conditions.Count > 0),
                    Tooltip = Bind<string?>(() => string.Join(", ", c.Conditions)),
                    Children =
                    [
                        new Icon(FontAwesome.TriangleExclamation) { Size = 11, Color = Palette.Warn },
                        new Label(() => Fmt.Int(c.Conditions.Count), TextStyle.Sized(11, Palette.Warn))
                    ]
                }
            ]
        };
    }

    private static Widget Footer()
    {
        return new Row
        {
            Padding = (12, 6),
            Border = Border.Top(Color.Border),
            Children = [new Label("Click to select · double-click for the sheet", TextStyle.Sized(11, Color.TextDisabled))]
        };
    }

    private bool IsCurrent(Character character)
    {
        return ReferenceEquals(character, _campaign.Current);
    }

    private void RevealCurrent()
    {
        var current = _campaign.Current;
        if (ReferenceEquals(current, _shownCurrent))
        {
            return;
        }

        _shownCurrent = current;
        if (current != null && _rows.WidgetFor(current) is { } row)
        {
            _list.ScrollIntoView(row);
        }
    }
}

// The party roster as a ListView; selection follows the map.
public sealed class PartyView
{
    public PartyView(Campaign campaign, MapView map, Action<Character> openSheet)
    {
        var list = new ListView<Character>(() => campaign.Party, pc => MemberRow(pc, map))
        {
            Padding = 1,
            Background = Color.Surface,
            Border = Color.Border,
            Radius = 8,
            RowPadding = (10, 8),
            Selected = Bind<Character?>(() => map.Selected is { Faction: Faction.Party } selected ? selected : null, pc => map.Selected = pc),
            OnActivate = openSheet
        };

        var openAll = new Button("Open all sheets", FontAwesome.Users)
        {
            Small = true,
            OnClick = () =>
            {
                foreach (var pc in campaign.Party)
                {
                    openSheet(pc);
                }
            }
        };

        var total = new Label(() =>
        {
            var party = campaign.Party;
            var hp = 0;
            var max = 0;
            for (var i = 0; i < party.Count; i++)
            {
                hp += Math.Max(0, party[i].Hp);
                max += party[i].MaxHp;
            }

            return $"{hp}/{max} HP";
        }, TextStyle.Sized(11, Color.TextMuted));

        var root = new Column { Flex = 1, Padding = 8, Gap = 8, Children = [list, new Row { Gap = 8, Children = [openAll, new Spacer(), total] }] };
        Window = new FloatingWindow("Party", root) { Icon = FontAwesome.Users, MinSize = new(240, 180) };
    }

    public FloatingWindow Window { get; }

    private static Widget MemberRow(Character pc, MapView map)
    {
        return new Row
        {
            Flex = 1,
            Gap = 12,
            Children =
            [
                Kit.Portrait(pc, 34),
                new Column
                {
                    Flex = 1,
                    Gap = 4,
                    Children =
                    [
                        new Row
                        {
                            Gap = 6,
                            Children =
                            [
                                new Label(pc.Name, TextStyle.Sized(14)) { Color = Bind(() => ReferenceEquals(map.Selected, pc) ? Color.Accent : Color.Text) },
                                new Spacer(),
                                new Label(() => $"{pc.Hp}/{pc.MaxHp}", TextStyle.Sized(12, Color.TextMuted))
                            ]
                        },
                        new Label($"{pc.Race} {pc.ClassName} {pc.Level}", TextStyle.Sized(11, Color.TextMuted)),
                        Kit.HpBar(pc, 5)
                    ]
                }
            ]
        };
    }
}

// Long-form text: read-aloud boxes, headings, bullets and GM notes, rebuilt when the entry or the text size changes.
public sealed class JournalView
{
    private readonly Campaign _campaign;
    private readonly ScrollView _body;
    private int _entry;
    private float _size = 15;

    public JournalView(Campaign campaign)
    {
        _campaign = campaign;
        _body = new ScrollView { Padding = (22, 18, 28, 24) };
        Rebuild();

        var header = new Row
        {
            Padding = (10, 8),
            Gap = 8,
            Border = Border.Bottom(Color.Border),
            Children =
            [
                new Dropdown(campaign.Journal.Select(j => j.Title).ToArray())
                {
                    Width = 210,
                    Selected = Bind(() => _entry, index => _entry = index),
                    OnChange = _ =>
                    {
                        Rebuild();
                        _body.ScrollToTop();
                    }
                },
                new Spacer(),
                new Label("A", TextStyle.Sized(11, Color.TextMuted)),
                new Slider(12, 22)
                {
                    Step = 1,
                    Width = 110,
                    Tooltip = "Text size",
                    Value = Bind(() => _size, size => _size = size),
                    OnChange = _ => Rebuild()
                },
                new Label("A", TextStyle.Sized(17, Color.TextMuted)),
                Kit.IconButton(FontAwesome.ShareNodes, "Share with the table", Share)
            ]
        };

        Window = new FloatingWindow("Journal", new Column { Flex = 1, Children = [header, _body] }) { Icon = FontAwesome.Bookmark, MinSize = new(320, 240) };
    }

    public FloatingWindow Window { get; }

    private JournalEntry Entry => _campaign.Journal[Math.Max(0, _entry)];

    private void Share()
    {
        _campaign.Chat.System($"{Campaign.GmName} shared the journal entry \"{Entry.Title}\" with the table.");
    }

    private void Rebuild()
    {
        var entry = Entry;
        var size = _size;
        _body.Gap = size * 0.8f;
        _body.Children =
        [
            new Paragraph(entry.Title, TextStyle.Sized(size * 1.7f)),
            new Column { Margin = (0, -size * 0.5f, 0, 0), Children = [new Paragraph(entry.Subtitle, TextStyle.Sized(size * 0.85f, Color.TextMuted))] },
            new Row { Height = 1, Background = Color.Border },
            .. entry.Blocks.Select(block => Block(block, size)),
            new Row { Padding = (0, 12, 0, 0), Justify = UIJustify.Center, Children = [new Label("· end of entry ·", TextStyle.Sized(11, Color.TextDisabled))] }
        ];
    }

    private static Widget Block(JournalBlock block, float size)
    {
        var lineHeight = MathF.Round(size * 1.55f);
        var body = TextStyle.Sized(size) with { LineHeight = lineHeight };
        switch (block.Kind)
        {
            case BlockKind.Heading:
                return new Column { Margin = (0, size * 0.4f, 0, 0), Children = [new Paragraph(block.Text, TextStyle.Sized(size * 1.25f))] };
            case BlockKind.ReadAloud:
                return new Column
                {
                    Padding = (16, 12),
                    Gap = 6,
                    Background = Color.AccentSoft,
                    Border = Border.Left(Color.Accent, 3),
                    Radius = 4,
                    Children = [new Label("READ ALOUD", TextStyle.Sized(10, Color.Accent)), new Paragraph(block.Text, body)]
                };
            case BlockKind.Bullet:
                return new Row
                {
                    Gap = 10,
                    Align = UIAlign.Start,
                    Margin = (0, -size * 0.35f, 0, 0),
                    Children =
                    [
                        new Row { Width = 6, Height = lineHeight, Children = [new Row { Width = 6, Height = 6, Radius = 3, Background = Color.Accent }] },
                        new Column { Flex = 1, Children = [new Paragraph(block.Text, body)] }
                    ]
                };
            case BlockKind.GmNote:
                return new Row
                {
                    Padding = (14, 12),
                    Gap = 12,
                    Align = UIAlign.Start,
                    Background = Palette.Warn.WithAlpha(28),
                    Border = Palette.Warn.WithAlpha(120),
                    Radius = 6,
                    Children =
                    [
                        new Row { Padding = (0, 3, 0, 0), Children = [new Icon(FontAwesome.EyeSlash) { Size = 13, Color = Palette.Warn }] },
                        new Column { Flex = 1, Gap = 4, Children = [new Label("GM ONLY", TextStyle.Sized(10, Palette.Warn)), new Paragraph(block.Text, body)] }
                    ]
                };
            default:
                return new Paragraph(block.Text, body);
        }
    }
}
