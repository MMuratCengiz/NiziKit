using VttDemo.Model;
using VttDemo.UiKit;

namespace VttDemo.Views;

public sealed class CharacterSheet
{
    private enum SheetTab
    {
        Overview,
        Skills,
        Combat,
        Spells,
        Gear,
        Notes
    }

    private static readonly TextStyle ColumnHeader = TextStyle.Sized(10, Color.TextMuted);

    private readonly Campaign _campaign;
    private readonly Tabs<SheetTab> _tabs;
    private SheetTab _tab;
    private string _skillFilter = "";
    private SkillDef? _selectedSkill;

    public CharacterSheet(Campaign campaign, Character character)
    {
        _campaign = campaign;
        Character = character;
        _tabs = new Tabs<SheetTab>
        {
            Selected = Bind(() => _tab, tab => _tab = tab),
            Pages =
            [
                new(SheetTab.Overview, "Overview", FontAwesome.User, OverviewPage()),
                new(SheetTab.Skills, "Skills", FontAwesome.ListCheck, SkillsPage()),
                new(SheetTab.Combat, "Combat", FontAwesome.Crosshairs, CombatPage()),
                new(SheetTab.Spells, "Spells", FontAwesome.Lightbulb, SpellsPage()),
                new(SheetTab.Gear, "Gear", FontAwesome.Cube, GearPage()),
                new(SheetTab.Notes, "Notes", FontAwesome.Edit, NotesPage())
            ]
        };

        Window = new FloatingWindow(character.Name, _tabs)
        {
            Icon = character.Faction == Faction.Party ? FontAwesome.User : FontAwesome.Bug,
            Accent = character.Color,
            Rect = new(316, 52, 620, 660),
            MinSize = new(380, 300)
        };
    }

    public Character Character { get; }
    public FloatingWindow Window { get; }

    public int SelectedTab
    {
        get => (int)_tab;
        set => _tab = (SheetTab)value;
    }

    public int TabCount => _tabs.Pages.Count;

    private void Roll(string formula, string label)
    {
        _campaign.Roll(Character.Name, formula, label);
    }

    private void RollSkill(SkillDef skill)
    {
        Roll(Dice.WithModifier("1d20", Character.SkillTotal(skill)), skill.Name);
    }

    private static Row Tiles(params Widget[] tiles)
    {
        return new Row { Gap = 8, Wrap = true, LineGap = 8, Children = [.. tiles] };
    }

    private Widget OverviewPage()
    {
        var c = Character;
        var down = Kit.Badge("DOWN", Palette.Bad.WithAlpha(50), Palette.Bad);
        down.Visible = Bind(() => c.IsDown);
        return new ScrollView
        {
            Padding = 14,
            Gap = 10,
            Children =
            [
                new Row
                {
                    Gap = 14,
                    Children =
                    [
                        Kit.Portrait(c, 64),
                        new Column
                        {
                            Flex = 1,
                            Gap = 3,
                            Children =
                            [
                                new Label(c.Name, TextStyle.Sized(21)),
                                new Label(c.Subtitle, TextStyle.Sized(13, Color.TextMuted)),
                                new Row
                                {
                                    Gap = 6,
                                    Wrap = true,
                                    LineGap = 4,
                                    Margin = (0, 4, 0, 0),
                                    Children =
                                    [
                                        Kit.Badge($"Speed {c.Speed} ft.", Color.SurfaceAlt, Color.TextMuted),
                                        Kit.Badge($"Init {Fmt.Sign(c.InitBonus)}", Color.SurfaceAlt, Color.TextMuted),
                                        Kit.Badge($"BAB {Fmt.Sign(c.Bab)}", Color.SurfaceAlt, Color.TextMuted),
                                        down
                                    ]
                                }
                            ]
                        }
                    ]
                },
                HitPoints(),
                Kit.SectionHeader("DEFENSE"),
                Tiles(
                    Kit.StatTile("AC", () => Fmt.Int(c.Ac), "armor class", 92),
                    Kit.StatTile("TOUCH", () => Fmt.Int(c.Touch), "", 76),
                    Kit.StatTile("FLAT", () => Fmt.Int(c.FlatFooted), "", 76),
                    Kit.StatTile("CMB", () => Fmt.Sign(c.Cmb), "roll", 76, () => Roll(Dice.WithModifier("1d20", c.Cmb), "Combat maneuver"), "Combat maneuver bonus"),
                    Kit.StatTile("CMD", () => Fmt.Int(c.Cmd), "", 76, null, "Combat maneuver defense")),
                Kit.SectionHeader("SAVING THROWS"),
                Tiles(
                    Kit.StatTile("FORTITUDE", () => Fmt.Sign(c.Fort), "roll", 110, () => Roll(Dice.WithModifier("1d20", c.Fort), "Fortitude save")),
                    Kit.StatTile("REFLEX", () => Fmt.Sign(c.Ref), "roll", 110, () => Roll(Dice.WithModifier("1d20", c.Ref), "Reflex save")),
                    Kit.StatTile("WILL", () => Fmt.Sign(c.Will), "roll", 110, () => Roll(Dice.WithModifier("1d20", c.Will), "Will save"))),
                Kit.SectionHeader("ABILITY SCORES"),
                Tiles([.. Enum.GetValues<Ability>().Select(AbilityTile)]),
                Kit.SectionHeader("CONDITIONS"),
                new Row
                {
                    Gap = 4,
                    Wrap = true,
                    LineGap = 2,
                    Children = [.. Character.AllConditions.Select(ConditionBox)]
                }
            ]
        };
    }

    private Widget HitPoints()
    {
        var c = Character;
        return new Column
        {
            Style = Kit.Card,
            Children =
            [
                new Row
                {
                    Gap = 8,
                    Children =
                    [
                        new Icon(FontAwesome.Heart) { Size = 13, Color = Palette.Bad },
                        new Label("Hit Points", TextStyle.Sized(13)),
                        new Spacer(),
                        new Label(() => $"{c.Hp} / {c.MaxHp}", TextStyle.Sized(16)),
                        new Label(() => $"+{c.TempHp} temp", TextStyle.Sized(12, Palette.TempHp)) { Visible = Bind(() => c.TempHp > 0) }
                    ]
                },
                Kit.HpBar(c, 12),
                new Row
                {
                    Gap = 6,
                    Wrap = true,
                    LineGap = 6,
                    Children =
                    [
                        HpButton("-5", "Take 5 damage", () => _campaign.Damage(c, 5)),
                        HpButton("-1", "Take 1 damage", () => _campaign.Damage(c, 1)),
                        HpButton("+1", "Heal 1", () => _campaign.Heal(c, 1)),
                        HpButton("+5", "Heal 5", () => _campaign.Heal(c, 5)),
                        new Spacer(),
                        new Label("Damage eats temporary HP first", TextStyle.Sized(11, Color.TextDisabled))
                    ]
                }
            ]
        };
    }

    private static Button HpButton(string text, string tooltip, Action action)
    {
        return new Button(text) { Small = true, Tooltip = tooltip, OnClick = action };
    }

    private Widget AbilityTile(Ability ability)
    {
        var c = Character;
        var name = Character.AbilityNames[(int)ability];
        return Kit.StatTile(name, () => Fmt.Int(c.Score(ability)), Fmt.Sign(c.Mod(ability)), 74,
            () => Roll(Dice.WithModifier("1d20", c.Mod(ability)), $"{name} check"), "Click to roll an ability check");
    }

    private Widget ConditionBox(string name)
    {
        var c = Character;
        return new Checkbox(name)
        {
            BoxSize = 15,
            Checked = Bind(() => c.Conditions.Contains(name), on => _campaign.SetCondition(c, name, on))
        };
    }

    private Widget SkillsPage()
    {
        var list = new ListView<SkillDef>(() => Character.Skills, SkillRow)
        {
            RowPadding = (10, 6),
            Filter = SkillMatches,
            Selected = Bind(() => _selectedSkill, skill => _selectedSkill = skill),
            OnActivate = RollSkill,
            Visible = Bind(() => AnySkillMatches())
        };
        var empty = Kit.Empty(FontAwesome.MagnifyingGlass, "No skills match", () => "Try a shorter filter.");
        empty.Visible = Bind(() => !AnySkillMatches());

        return new Column
        {
            Flex = 1,
            Padding = 12,
            Gap = 8,
            Children =
            [
                new TextField
                {
                    Width = UIValue.Auto,
                    Placeholder = "Filter skills…",
                    Text = Bind(() => _skillFilter, text =>
                    {
                        _skillFilter = text;
                        _selectedSkill = null;
                    })
                },
                new Row
                {
                    Padding = (28, 0, 24, 0),
                    Gap = 12,
                    Children =
                    [
                        new Label("SKILL", ColumnHeader) { Flex = 1 },
                        new Label("ABIL", ColumnHeader) { Width = 36 },
                        new Label("RANKS", ColumnHeader) { Width = 40 },
                        new Label("TOTAL", ColumnHeader) { Width = 54, Align = UITextAlign.Center }
                    ]
                },
                list,
                empty,
                new Label("Accent dot = class skill (+3 with a rank). Click a total or double-click a row to roll.", TextStyle.Sized(11, Color.TextDisabled))
                {
                    Visible = Bind(() => AnySkillMatches())
                }
            ]
        };
    }

    private Widget SkillRow(SkillDef skill)
    {
        var c = Character;
        var ranks = c.RanksIn(skill);
        return new Row
        {
            Flex = 1,
            Gap = 12,
            Children =
            [
                new Row { Width = 6, Height = 6, Radius = 3, Background = c.IsClassSkill(skill) ? Color.Accent : Color.Border },
                new Label(skill.Name, TextStyle.Sized(14))
                {
                    Flex = 1,
                    Color = Bind(() => ReferenceEquals(_selectedSkill, skill) ? Color.Accent : Color.Text)
                },
                new Label(Character.AbilityNames[(int)skill.Ability], TextStyle.Sized(11, Color.TextMuted)) { Width = 36 },
                new Label(ranks > 0 ? Fmt.Int(ranks) : "-", TextStyle.Sized(13, ranks > 0 ? Color.Text : Color.TextDisabled)) { Width = 40 },
                new Button(Fmt.Sign(c.SkillTotal(skill))) { Small = true, Width = 54, Tooltip = $"Roll {skill.Name}", OnClick = () => RollSkill(skill) }
            ]
        };
    }

    private bool SkillMatches(SkillDef skill)
    {
        return skill.Name.Contains(_skillFilter, StringComparison.OrdinalIgnoreCase);
    }

    private bool AnySkillMatches()
    {
        foreach (var skill in Character.Skills)
        {
            if (SkillMatches(skill))
            {
                return true;
            }
        }

        return false;
    }

    private Widget CombatPage()
    {
        var c = Character;
        return new ScrollView
        {
            Padding = 14,
            Gap = 10,
            Children =
            [
                Tiles(
                    Kit.StatTile("INITIATIVE", () => Fmt.Sign(c.InitBonus), "roll", 110, () => Roll(Dice.WithModifier("1d20", c.InitBonus), "Initiative")),
                    Kit.StatTile("BAB", () => Fmt.Sign(c.Bab), "", 80, null, "Base attack bonus"),
                    Kit.StatTile("SPEED", () => $"{c.Speed} ft", "", 90)),
                Kit.SectionHeader("ATTACKS"),
                .. c.Attacks.Select(AttackCard)
            ]
        };
    }

    private Widget AttackCard(Attack attack)
    {
        var c = Character;
        var card = new Column
        {
            Style = Kit.Card with { Gap = 6 },
            Children =
            [
                new Row
                {
                    Gap = 8,
                    Wrap = true,
                    LineGap = 6,
                    Children =
                    [
                        new Icon(FontAwesome.Crosshairs) { Size = 12, Color = c.Color },
                        new Label(attack.Name, TextStyle.Sized(15)),
                        new Spacer(),
                        new Button($"Attack {Fmt.Sign(attack.Bonus)}", FontAwesome.Cube)
                        {
                            Tone = Tone.Primary,
                            Small = true,
                            OnClick = () => Roll(Dice.WithModifier("1d20", attack.Bonus), $"{attack.Name} attack")
                        },
                        new Button(attack.Damage)
                        {
                            Small = true,
                            Tooltip = "Roll damage",
                            Enabled = attack.Damage != "varies",
                            OnClick = () => Roll(attack.Damage, $"{attack.Name} damage")
                        }
                    ]
                },
                new Row
                {
                    Gap = 16,
                    Align = UIAlign.Start,
                    Children = [Kit.KeyValue("CRITICAL", attack.Critical), Kit.KeyValue("DAMAGE", attack.Damage)]
                }
            ]
        };
        if (attack.Notes.Length > 0)
        {
            card.Children.Add(new Paragraph(attack.Notes, TextStyle.Sized(12, Color.TextMuted)));
        }

        return card;
    }

    private Widget SpellsPage()
    {
        var c = Character;
        if (c.Spells.Count == 0)
        {
            return Kit.Empty(FontAwesome.Lightbulb, "No spellcasting",
                () => $"{c.Name} doesn't prepare or cast spells. Switch to a caster's sheet to see spell cards and slots.");
        }

        var page = new ScrollView { Padding = 14, Gap = 8 };
        page.Children.Add(Kit.SectionHeader("SPELL SLOTS"));
        for (var level = 0; level < c.SpellSlots.Length; level++)
        {
            page.Children.Add(SlotRow(level));
        }

        for (var level = 0; level < 10; level++)
        {
            var header = false;
            foreach (var spell in c.Spells)
            {
                if (spell.Level != level)
                {
                    continue;
                }

                if (!header)
                {
                    page.Children.Add(Kit.SectionHeader(level == 0 ? "CANTRIPS" : $"LEVEL {level}"));
                    header = true;
                }

                page.Children.Add(SpellCard(spell));
            }
        }

        return page;
    }

    private Widget SlotRow(int level)
    {
        var c = Character;
        var slots = c.SpellSlots[level];
        var row = new Row { Gap = 6, Children = [new Label(level == 0 ? "Cantrips" : $"Level {level}", TextStyle.Sized(13)) { Width = 76 }] };
        if (level == 0)
        {
            row.Children.Add(new Label("at will", TextStyle.Sized(12, Color.TextMuted)));
            return row;
        }

        for (var k = 0; k < slots; k++)
        {
            row.Children.Add(SlotPip(level, k));
        }

        row.Children.Add(new Label(() => $"  {slots - c.SpellSlotsUsed[level]} of {slots} left", TextStyle.Sized(12, Color.TextMuted)));
        return row;
    }

    private Widget SlotPip(int level, int k)
    {
        var c = Character;
        var pip = new Row
        {
            Width = 16,
            Height = 16,
            Radius = 8,
            Border = new Border(Color.Accent, 1.5f),
            HoverBorder = Color.AccentHover,
            OnClick = () => c.SpellSlotsUsed[level] = k < c.SpellSlotsUsed[level] ? k : k + 1
        };
        pip.Background = Bind(() => k < c.SpellSlotsUsed[level] ? Color.Transparent : pip.IsHovered ? Color.AccentHover : Color.Accent);
        return pip;
    }

    private Widget SpellCard(Spell spell)
    {
        var expanded = false;
        return new Column
        {
            Background = Color.SurfaceAlt,
            BorderColor = Bind(() => expanded ? Color.BorderStrong : Color.Border),
            Radius = 8,
            Clip = true,
            Animate = UITransitionPropertyFlagBits.Colors,
            Children =
            [
                new Row
                {
                    Padding = (10, 8),
                    Gap = 8,
                    HoverBackground = Color.Hover,
                    OnClick = () => expanded = !expanded,
                    Children =
                    [
                        new Icon(() => expanded ? FontAwesome.AngleDown : FontAwesome.AngleRight) { Size = 12, Color = Color.TextMuted },
                        new Label(spell.Name, TextStyle.Sized(14)),
                        new Label(spell.School, TextStyle.Sized(12, Color.TextMuted)) { Flex = 1 },
                        new Button("Cast", FontAwesome.Star)
                        {
                            Tone = Tone.Primary,
                            Small = true,
                            Enabled = Bind(() => CanCast(spell)),
                            Tooltip = Bind<string?>(() => CanCast(spell) ? null : "No slots left at this level"),
                            OnClick = () => Cast(spell)
                        }
                    ]
                },
                new Column
                {
                    Padding = (12, 2, 12, 12),
                    Gap = 10,
                    Visible = Bind(() => expanded),
                    Enter = Motion.SlideDown,
                    Children =
                    [
                        new Row
                        {
                            Wrap = true,
                            Gap = 18,
                            LineGap = 8,
                            Align = UIAlign.Start,
                            Children =
                            [
                                Kit.KeyValue("CASTING TIME", spell.CastingTime),
                                Kit.KeyValue("RANGE", spell.Range),
                                Kit.KeyValue("DURATION", spell.Duration),
                                Kit.KeyValue("SAVE", spell.Save)
                            ]
                        },
                        new Paragraph(spell.Description, TextStyle.Sized(14) with { LineHeight = 21 })
                    ]
                }
            ]
        };
    }

    private bool CanCast(Spell spell)
    {
        var c = Character;
        return spell.Level == 0 || c.SpellSlotsUsed[spell.Level] < c.SpellSlots[spell.Level];
    }

    private void Cast(Spell spell)
    {
        var c = Character;
        if (spell.Level > 0)
        {
            c.SpellSlotsUsed[spell.Level]++;
        }

        _campaign.Chat.Post(MessageKind.Emote, c.Name, c.Color, $"casts {spell.Name}.");
    }

    private Widget GearPage()
    {
        var c = Character;
        var newItem = "";
        var inventory = new ListView<Item>(() => c.Inventory, ItemRow)
        {
            Gap = 2,
            Padding = (0, 0, 10, 0),
            Background = Color.Surface,
            Striped = true,
            RowPadding = (10, 5),
            RowRadius = 5
        };

        void AddItem()
        {
            var name = newItem.Trim();
            if (name.Length == 0)
            {
                return;
            }

            c.Inventory.Add(new Item { Name = name });
            newItem = "";
            inventory.ScrollToEnd();
        }

        return new Column
        {
            Flex = 1,
            Padding = 12,
            Gap = 8,
            Children =
            [
                new Row
                {
                    Gap = 8,
                    Children =
                    [
                        new Icon(FontAwesome.Cube) { Size = 12, Color = Color.TextMuted },
                        new Label(() => $"{c.Inventory.Count} items · {c.CarriedWeight:0.#} lb carried", TextStyle.Sized(13)),
                        new Spacer(),
                        new Icon(FontAwesome.Circle) { Size = 10, Color = Palette.Warn },
                        new Label(() => $"{c.Gold} gp", TextStyle.Sized(13))
                    ]
                },
                inventory,
                new Row
                {
                    Gap = 6,
                    Children =
                    [
                        new TextField
                        {
                            Flex = 1,
                            Placeholder = "Add an item and press Enter",
                            Text = Bind(() => newItem, text => newItem = text),
                            OnSubmit = _ => AddItem()
                        },
                        new Button("Add", FontAwesome.Plus) { OnClick = AddItem }
                    ]
                }
            ]
        };
    }

    private Widget ItemRow(Item item)
    {
        var c = Character;
        return new Row
        {
            Flex = 1,
            Gap = 8,
            Children =
            [
                new Paragraph(item.Name, TextStyle.Sized(14)),
                new Spacer(),
                new Button
                {
                    Icon = FontAwesome.Minus,
                    Tone = Tone.Ghost,
                    Small = true,
                    Enabled = Bind(() => item.Quantity > 1),
                    OnClick = () => item.Quantity--
                },
                new Label(() => Fmt.Int(item.Quantity), TextStyle.Sized(13)) { Width = 26, Align = UITextAlign.Center },
                new Button { Icon = FontAwesome.Plus, Tone = Tone.Ghost, Small = true, OnClick = () => item.Quantity++ },
                new Label(() => $"{item.Weight * item.Quantity:0.##} lb", TextStyle.Sized(12, Color.TextMuted)) { Width = 56, Align = UITextAlign.Right },
                new Button
                {
                    Icon = FontAwesome.TrashCan,
                    Tone = Tone.Ghost,
                    Small = true,
                    Tooltip = "Remove",
                    OnClick = () =>
                    {
                        _campaign.Chat.System($"{c.Name} dropped {item.Name}.");
                        c.Inventory.Remove(item);
                    }
                }
            ]
        };
    }

    private Widget NotesPage()
    {
        var c = Character;
        var notes = new TextArea
        {
            Flex = 1,
            Placeholder = "Backstory, session notes, grudges…",
            Text = Bind(() => c.Notes, text => c.Notes = text)
        };

        return new Column
        {
            Flex = 1,
            Padding = 12,
            Gap = 8,
            Children =
            [
                new Row
                {
                    Gap = 8,
                    Children =
                    [
                        new Label("Private notes", TextStyle.Sized(13)),
                        new Spacer(),
                        new Label(() => $"{notes.LineCount} lines · {c.Notes.Length} characters", TextStyle.Sized(11, Color.TextMuted))
                    ]
                },
                notes
            ]
        };
    }
}
