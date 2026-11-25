using System.Numerics;
using NiziKit.UI.Widgets;
using VttDemo.Model;
using VttDemo.UiKit;

namespace VttDemo.Views;

// Turn order. Rows are keyed by character, so re-rolling initiative slides them to their new places.
public sealed class InitiativeView
{
    private readonly Campaign _campaign;
    private readonly MapView _map;
    private readonly uint _listId = Ui.Id("Initiative.List");
    private readonly Button _roll;
    private readonly Button _previous;
    private readonly Button _next;
    private Character? _shownCurrent;

    public InitiativeView(Campaign campaign, MapView map, Action<Character> openSheet)
    {
        _campaign = campaign;
        _map = map;
        _roll = new Button { Icon = FontAwesome.Cube, Text = "Roll", Tooltip = "Roll initiative for everyone", FontSize = 12, Padding = UILengthEdges.Symmetric(10, 5) };
        _roll.Clicked += _ => campaign.RollInitiative();
        _previous = new Button { Icon = FontAwesome.AngleLeft, Tooltip = "Previous turn", Padding = UILengthEdges.Symmetric(9, 5) };
        _previous.Clicked += _ => campaign.PreviousTurn();
        _next = new Button { Icon = FontAwesome.AngleRight, Text = "Next", Kind = ButtonKind.Primary, Tooltip = "Next turn (N)", FontSize = 12, Padding = UILengthEdges.Symmetric(10, 5) };
        _next.Clicked += _ => campaign.NextTurn();
        OpenSheet = openSheet;
        Window = new FloatingWindow("Initiative", Im.View(Draw)) { Icon = FontAwesome.ListOl, MinSize = new Vector2(250, 200) };
    }

    public FloatingWindow Window { get; }
    private Action<Character> OpenSheet { get; }

    private void Draw()
    {
        var theme = Ui.Theme;
        using (Ui.Row().Padding(12, 8).Gap(6).AlignItems(UIAlign.Center).Shrink(0).Border(theme.Border, new UIEdges(0, 0, 0, 1)).Open())
        {
            using (Ui.Column().Gap(0).Open())
            {
                Im.Line("ROUND", 10, theme.TextMuted);
                Ui.Text($"{_campaign.Round}", 22, theme.Text);
            }

            Im.Spacer();
            Ui.Draw(_roll);
            Ui.Draw(_previous);
            Ui.Draw(_next);
        }

        var current = _campaign.Current;
        using (Ui.Column(_listId).Fill().ScrollY().Padding(8, 8, 14, 8).Gap(3).Open())
        {
            foreach (var c in _campaign.Order)
            {
                var id = Ui.Id(_listId, c.Id);
                var interaction = Ui.Interaction(id);
                if (interaction.Clicked)
                {
                    _map.Selected = c;
                }

                if (interaction.DoubleClicked)
                {
                    OpenSheet(c);
                }

                var isCurrent = ReferenceEquals(c, current);
                var selected = ReferenceEquals(c, _map.Selected);
                using (Ui.Row(id)
                           .Padding(8, 6)
                           .Gap(10)
                           .AlignItems(UIAlign.Center)
                           .Shrink(0)
                           .CornerRadius(6)
                           .Background(isCurrent ? theme.AccentSoft : interaction.Hovered ? theme.Hover : default)
                           .Border(isCurrent ? theme.Accent : selected ? theme.BorderStrong : default, 1)
                           .Overlay(c.IsDown ? UIColor.Hex(0x00000060) : default)
                           .Cursor(SystemCursor.Pointer)
                           .Animate(UITransitionPropertyFlagBits.Position | UITransitionPropertyFlagBits.Colors, 0.28f, UIEasing.EaseInOut)
                           .Open())
                {
                    using (Ui.Row().Size(30, 24).Shrink(0).Center().Background(isCurrent ? theme.Accent : theme.SurfaceAlt).CornerRadius(5).Open())
                    {
                        Im.Line(Fmt.Int(c.Initiative), 13, isCurrent ? theme.OnAccent : theme.Text);
                    }

                    Im.Portrait(c, 26);
                    using (Ui.Column().Fill().Gap(4).Open())
                    {
                        Im.Line(c.Name, 13, c.IsDown ? theme.TextDisabled : theme.Text);
                        Im.HpBar(Ui.Id(id, 1), c.Hp, c.MaxHp, c.TempHp, 4);
                    }

                    if (c.Conditions.Count > 0)
                    {
                        var warnId = Ui.Id(id, 2);
                        using (Ui.Row(warnId).Gap(3).AlignItems(UIAlign.Center).Shrink(0).Open())
                        {
                            Im.Icon(FontAwesome.TriangleExclamation, 11, Im.Warn);
                            Im.Line(Fmt.Int(c.Conditions.Count), 11, Im.Warn);
                        }

                        if (Ui.Interaction(warnId).Hovered)
                        {
                            Ui.Tooltip(warnId, string.Join(", ", c.Conditions));
                        }
                    }
                }
            }
        }

        if (!ReferenceEquals(current, _shownCurrent))
        {
            _shownCurrent = current;
            if (current != null)
            {
                Ui.ScrollIntoView(Ui.Id(_listId, current.Id));
            }
        }

        using (Ui.Row().Padding(12, 6).Shrink(0).Border(theme.Border, new UIEdges(0, 1, 0, 0)).Open())
        {
            Im.Line("Click to select · double-click for the sheet", 11, theme.TextDisabled);
        }
    }
}

// The party roster as a retained ListView with an item template; selection follows the map.
public sealed class PartyView
{
    private readonly Campaign _campaign;
    private readonly MapView _map;
    private readonly List<Character> _party;
    private readonly ListView _list;
    private readonly Button _openAll;

    public PartyView(Campaign campaign, MapView map, Action<Character> openSheet)
    {
        _campaign = campaign;
        _map = map;
        _party = campaign.Party.ToList();
        _list = new ListView { Count = _party.Count, Fill = true, Height = UIValue.Auto, Striped = false, RowPadding = UILengthEdges.Symmetric(10, 8), ItemTemplate = DrawRow };
        _list.SelectionChanged += l =>
        {
            if (l.SelectedIndex >= 0)
            {
                map.Selected = _party[l.SelectedIndex];
            }
        };
        _list.ItemActivated += (_, index) => openSheet(_party[index]);
        _openAll = new Button("Open all sheets") { Icon = FontAwesome.Users, FontSize = 12, Padding = UILengthEdges.Symmetric(10, 5) };
        _openAll.Clicked += _ =>
        {
            foreach (var pc in _party)
            {
                openSheet(pc);
            }
        };
        Window = new FloatingWindow("Party", Im.View(Draw, padding: 8, gap: 8)) { Icon = FontAwesome.Users, MinSize = new Vector2(240, 180) };
    }

    public FloatingWindow Window { get; }

    private void Draw()
    {
        var theme = Ui.Theme;
        _list.SelectedIndex = _map.Selected is { } selected ? _party.IndexOf(selected) : -1;
        Ui.Draw(_list);
        using (Ui.Row().Gap(8).AlignItems(UIAlign.Center).Shrink(0).Open())
        {
            Ui.Draw(_openAll);
            Im.Spacer();
            var total = 0;
            var max = 0;
            foreach (var pc in _party)
            {
                total += Math.Max(0, pc.Hp);
                max += pc.MaxHp;
            }

            Ui.Text($"{total}/{max} HP", 11, theme.TextMuted);
        }
    }

    private void DrawRow(int index, bool selected)
    {
        var theme = Ui.Theme;
        var pc = _party[index];
        Im.Portrait(pc, 34);
        using (Ui.Column().Fill().Gap(4).Open())
        {
            using (Ui.Row().Gap(6).AlignItems(UIAlign.Center).Open())
            {
                Im.Line(pc.Name, 14, selected ? theme.Accent : theme.Text);
                Im.Spacer();
                Ui.Text($"{pc.Hp}/{pc.MaxHp}", 12, theme.TextMuted);
            }

            Im.Line($"{pc.Race} {pc.ClassName} {pc.Level}", 11, theme.TextMuted);
            Im.HpBar(Ui.Id(_list.RowId(index), 1), pc.Hp, pc.MaxHp, pc.TempHp, 5);
        }
    }
}

// Long-form text: read-aloud boxes, headings, bullets and GM notes, re-wrapped live by the text size slider.
public sealed class JournalView
{
    private readonly Campaign _campaign;
    private readonly uint _scrollId = Ui.Id("Journal.Body");
    private readonly Dropdown _entries;
    private readonly Slider _size;
    private readonly Button _share;

    public JournalView(Campaign campaign)
    {
        _campaign = campaign;
        _entries = new Dropdown(campaign.Journal.Select(j => j.Title)) { Width = 210 };
        _entries.SelectionChanged += _ => Ui.ScrollTo(_scrollId, Vector2.Zero);
        _size = new Slider(12, 22, 15) { Step = 1, Fill = false, Width = 110, Tooltip = "Text size" };
        _share = new Button { Icon = FontAwesome.ShareNodes, Kind = ButtonKind.Ghost, Tooltip = "Share with the table", Padding = UILengthEdges.Symmetric(8, 5) };
        _share.Clicked += _ => campaign.Chat.System($"{Campaign.GmName} shared the journal entry \"{Entry.Title}\" with the table.");
        Window = new FloatingWindow("Journal", Im.View(Draw)) { Icon = FontAwesome.Bookmark, MinSize = new Vector2(320, 240) };
    }

    public FloatingWindow Window { get; }

    private JournalEntry Entry => _campaign.Journal[Math.Max(0, _entries.SelectedIndex)];

    private void Draw()
    {
        var theme = Ui.Theme;
        var size = _size.Value;
        using (Ui.Row().Padding(10, 8).Gap(8).AlignItems(UIAlign.Center).Shrink(0).Border(theme.Border, new UIEdges(0, 0, 0, 1)).Open())
        {
            Ui.Draw(_entries);
            Im.Spacer();
            Im.Line("A", 11, theme.TextMuted);
            Ui.Draw(_size);
            Im.Line("A", 17, theme.TextMuted);
            Ui.Draw(_share);
        }

        var entry = Entry;
        using (Ui.Column(_scrollId).Fill().ScrollY().Padding(22, 18, 28, 24).Gap(size * 0.8f).Open())
        {
            Im.Para(entry.Title, size * 1.7f, theme.Text);
            using (Ui.Column().Margin(0, -size * 0.5f, 0, 0).Open())
            {
                Im.Para(entry.Subtitle, size * 0.85f, theme.TextMuted);
            }

            Ui.Element().Height(1).Shrink(0).Background(theme.Border).Leaf();
            foreach (var block in entry.Blocks)
            {
                DrawBlock(block, size, theme);
            }

            using (Ui.Row().Padding(0, 12, 0, 0).Justify(UIJustify.Center).Open())
            {
                Im.Line("· end of entry ·", 11, theme.TextDisabled);
            }
        }
    }

    private static void DrawBlock(JournalBlock block, float size, Theme theme)
    {
        var lineHeight = MathF.Round(size * 1.55f);
        switch (block.Kind)
        {
            case BlockKind.Heading:
                using (Ui.Column().Margin(0, size * 0.4f, 0, 0).Open())
                {
                    Im.Para(block.Text, size * 1.25f, theme.Text);
                }

                break;
            case BlockKind.Paragraph:
                Im.Para(block.Text, size, theme.Text, lineHeight);
                break;
            case BlockKind.ReadAloud:
                using (Ui.Column().Padding(16, 12).Gap(6).Background(theme.AccentSoft).Border(theme.Accent, new UIEdges(3, 0, 0, 0)).CornerRadius(4).Open())
                {
                    Im.Line("READ ALOUD", 10, theme.Accent);
                    Im.Para(block.Text, size, theme.Text, lineHeight);
                }

                break;
            case BlockKind.Bullet:
                using (Ui.Row().Gap(10).AlignItems(UIAlign.Start).Margin(0, -size * 0.35f, 0, 0).Open())
                {
                    using (Ui.Row().Height(lineHeight).AlignItems(UIAlign.Center).Shrink(0).Open())
                    {
                        Ui.Element().Size(6, 6).CornerRadius(3).Background(theme.Accent).Leaf();
                    }

                    using (Ui.Column().Fill().Open())
                    {
                        Im.Para(block.Text, size, theme.Text, lineHeight);
                    }
                }

                break;
            case BlockKind.GmNote:
                using (Ui.Row().Padding(14, 12).Gap(12).AlignItems(UIAlign.Start).Background(Im.Alpha(Im.Warn, 28)).Border(Im.Alpha(Im.Warn, 120), 1).CornerRadius(6).Open())
                {
                    using (Ui.Row().Padding(0, 3, 0, 0).Shrink(0).Open())
                    {
                        Im.Icon(FontAwesome.EyeSlash, 13, Im.Warn);
                    }

                    using (Ui.Column().Fill().Gap(4).Open())
                    {
                        Im.Line("GM ONLY", 10, Im.Warn);
                        Im.Para(block.Text, size, theme.Text, lineHeight);
                    }
                }

                break;
        }
    }
}
