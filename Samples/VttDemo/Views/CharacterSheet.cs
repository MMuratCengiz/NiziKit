using System.Numerics;
using DenOfIz;
using NiziKit.UI;
using NiziKit.UI.Widgets;
using VttDemo.Model;
using VttDemo.UiKit;

namespace VttDemo.Views;

/// <summary>
/// A character sheet window: a TabView whose pages mix immediate-mode drawing (stat tiles, spell cards, the inventory
/// table) with retained widgets (a filtered skill ListView, condition checkboxes, a notes TextArea).
/// </summary>
public sealed class CharacterSheet
{
    private readonly Campaign _campaign;
    private readonly uint _baseId;
    private readonly TabView _tabs;

    private readonly Button[] _hpButtons;
    private readonly Dictionary<string, Checkbox> _conditions = new();

    private readonly TextBox _skillFilter;
    private readonly ListView _skillList;
    private readonly List<SkillDef> _visibleSkills = new();
    private string? _appliedFilter;

    private readonly HashSet<Spell> _expanded = new();
    private readonly string[] _attackLabels;

    private readonly TextBox _newItem;
    private readonly Button _addItem;
    private readonly TextArea _notes;

    public CharacterSheet(Campaign campaign, Character character)
    {
        _campaign = campaign;
        Character = character;
        _baseId = Ui.Id("Sheet", character.Id);

        _hpButtons =
        [
            HpButton("-5", "Take 5 damage", () => campaign.Damage(character, 5)),
            HpButton("-1", "Take 1 damage", () => campaign.Damage(character, 1)),
            HpButton("+1", "Heal 1", () => campaign.Heal(character, 1)),
            HpButton("+5", "Heal 5", () => campaign.Heal(character, 5))
        ];

        foreach (var condition in Character.AllConditions)
        {
            var checkbox = new Checkbox(condition) { BoxSize = 15 };
            checkbox.Changed += c => campaign.SetCondition(character, condition, c.Checked);
            _conditions[condition] = checkbox;
        }

        _skillFilter = new TextBox { Placeholder = "Filter skills…", Width = UIValue.Auto };
        _skillList = new ListView { Fill = true, Height = UIValue.Auto, RowPadding = UILengthEdges.Symmetric(10, 6), ItemTemplate = DrawSkillRow };
        _skillList.ItemActivated += (_, index) => RollSkill(_visibleSkills[index]);

        _attackLabels = character.Attacks.Select(a => $"Attack {Fmt.Sign(a.Bonus)}").ToArray();

        _newItem = new TextBox { Placeholder = "Add an item and press Enter", Fill = true };
        _newItem.Submitted += _ => AddItem();
        _addItem = new Button("Add") { Icon = FontAwesome.Plus };
        _addItem.Clicked += _ => AddItem();

        _notes = new TextArea(character.Notes) { Fill = true, Height = UIValue.Auto, Placeholder = "Backstory, session notes, grudges…" };
        _notes.TextChanged += t => character.Notes = t.Text;

        _tabs = new TabView { Fill = true };
        _tabs.Add("Overview", Im.View(DrawOverview, scroll: true, padding: 14, gap: 10), FontAwesome.User);
        _tabs.Add("Skills", Im.View(DrawSkills, padding: 12, gap: 8), FontAwesome.ListCheck);
        _tabs.Add("Combat", Im.View(DrawCombat, scroll: true, padding: 14, gap: 10), FontAwesome.Crosshairs);
        _tabs.Add("Spells", Im.View(DrawSpells, scroll: true, padding: 14, gap: 8), FontAwesome.Lightbulb);
        _tabs.Add("Gear", Im.View(DrawInventory, padding: 12, gap: 8), FontAwesome.Cube);
        _tabs.Add("Notes", Im.View(DrawNotes, padding: 12, gap: 8), FontAwesome.Edit);

        Window = new FloatingWindow(character.Name, _tabs)
        {
            Icon = character.Faction == Faction.Party ? FontAwesome.User : FontAwesome.Bug,
            Accent = character.Color,
            Size = new Vector2(580, 660),
            MinSize = new Vector2(380, 300)
        };
    }

    public Character Character { get; }
    public FloatingWindow Window { get; }

    public int SelectedTab
    {
        get => _tabs.SelectedIndex;
        set => _tabs.SelectedIndex = value;
    }

    public int TabCount => _tabs.Tabs.Count;

    private uint Id(uint n)
    {
        return Ui.Id(_baseId, n);
    }

    private void Roll(string formula, string label)
    {
        _campaign.Roll(Character.Name, formula, label);
    }

    private void RollSkill(SkillDef skill)
    {
        Roll(Dice.WithModifier("1d20", Character.SkillTotal(skill)), skill.Name);
    }

    private Button HpButton(string text, string tooltip, Action action)
    {
        var button = new Button(text) { Tooltip = tooltip, FontSize = 12, Padding = UILengthEdges.Symmetric(10, 4) };
        button.Clicked += _ => action();
        return button;
    }

    private void DrawOverview()
    {
        var theme = Ui.Theme;
        var c = Character;

        using (Ui.Row().Gap(14).AlignItems(UIAlign.Center).Shrink(0).Open())
        {
            Im.Portrait(c, 64);
            using (Ui.Column().Fill().Gap(3).Open())
            {
                Im.Line(c.Name, 21, theme.Text);
                Im.Line(c.Subtitle, 13, theme.TextMuted);
                using (Ui.Row().Gap(6).Wrap().LineGap(4).Margin(0, 4, 0, 0).Open())
                {
                    Im.Badge($"Speed {c.Speed} ft.", theme.SurfaceAlt, theme.TextMuted);
                    Im.Badge($"Init {Fmt.Sign(c.InitBonus)}", theme.SurfaceAlt, theme.TextMuted);
                    Im.Badge($"BAB {Fmt.Sign(c.Bab)}", theme.SurfaceAlt, theme.TextMuted);
                    if (c.IsDown)
                    {
                        Im.Badge("DOWN", Im.Alpha(Im.Bad, 50), Im.Bad);
                    }
                }
            }
        }

        using (Ui.Column().Gap(8).Padding(12).Shrink(0).Background(theme.SurfaceAlt).Border(theme.Border, 1).CornerRadius(8).Open())
        {
            using (Ui.Row().Gap(8).AlignItems(UIAlign.Center).Open())
            {
                Im.Icon(FontAwesome.Heart, 13, Im.Bad);
                Im.Line("Hit Points", 13, theme.Text);
                Im.Spacer();
                Ui.Text($"{c.Hp} / {c.MaxHp}", 16, theme.Text);
                if (c.TempHp > 0)
                {
                    Ui.Text($"+{c.TempHp} temp", 12, Im.TempHp);
                }
            }

            Im.HpBar(Id(1), c.Hp, c.MaxHp, c.TempHp, 12);
            using (Ui.Row().Gap(6).Wrap().LineGap(6).AlignItems(UIAlign.Center).Open())
            {
                foreach (var button in _hpButtons)
                {
                    Ui.Draw(button);
                }

                Im.Spacer();
                Im.Line("Damage eats temporary HP first", 11, theme.TextDisabled);
            }
        }

        Im.SectionHeader("DEFENSE");
        using (Ui.Row().Gap(8).Wrap().LineGap(8).Shrink(0).Open())
        {
            Im.StatTile(Id(10), "AC", Fmt.Int(c.Ac), "armor class", 92, false);
            Im.StatTile(Id(11), "TOUCH", Fmt.Int(c.Touch), "", 76, false);
            Im.StatTile(Id(12), "FLAT", Fmt.Int(c.FlatFooted), "", 76, false);
            if (Im.StatTile(Id(13), "CMB", Fmt.Sign(c.Cmb), "roll", 76, true, "Combat maneuver bonus"))
            {
                Roll(Dice.WithModifier("1d20", c.Cmb), "Combat maneuver");
            }

            Im.StatTile(Id(14), "CMD", Fmt.Int(c.Cmd), "", 76, false, "Combat maneuver defense");
        }

        Im.SectionHeader("SAVING THROWS");
        using (Ui.Row().Gap(8).Wrap().LineGap(8).Shrink(0).Open())
        {
            if (Im.StatTile(Id(20), "FORTITUDE", Fmt.Sign(c.Fort), "roll", 110, true))
            {
                Roll(Dice.WithModifier("1d20", c.Fort), "Fortitude save");
            }

            if (Im.StatTile(Id(21), "REFLEX", Fmt.Sign(c.Ref), "roll", 110, true))
            {
                Roll(Dice.WithModifier("1d20", c.Ref), "Reflex save");
            }

            if (Im.StatTile(Id(22), "WILL", Fmt.Sign(c.Will), "roll", 110, true))
            {
                Roll(Dice.WithModifier("1d20", c.Will), "Will save");
            }
        }

        Im.SectionHeader("ABILITY SCORES");
        using (Ui.Row().Gap(8).Wrap().LineGap(8).Shrink(0).Open())
        {
            for (var i = 0; i < 6; i++)
            {
                var ability = (Ability)i;
                if (Im.StatTile(Id(30 + (uint)i), Character.AbilityNames[i], Fmt.Int(c.Score(ability)), Fmt.Sign(c.Mod(ability)), 74, true, "Click to roll an ability check"))
                {
                    Roll(Dice.WithModifier("1d20", c.Mod(ability)), $"{Character.AbilityNames[i]} check");
                }
            }
        }

        Im.SectionHeader("CONDITIONS");
        using (Ui.Row().Gap(4).Wrap().LineGap(2).Shrink(0).Open())
        {
            foreach (var (name, checkbox) in _conditions)
            {
                checkbox.Checked = c.Conditions.Contains(name);
                Ui.Draw(checkbox);
            }
        }
    }

    private void DrawSkills()
    {
        var theme = Ui.Theme;
        if (_appliedFilter != _skillFilter.Text)
        {
            _appliedFilter = _skillFilter.Text;
            _visibleSkills.Clear();
            foreach (var skill in Character.Skills)
            {
                if (skill.Name.Contains(_appliedFilter, StringComparison.OrdinalIgnoreCase))
                {
                    _visibleSkills.Add(skill);
                }
            }

            _skillList.Count = _visibleSkills.Count;
            _skillList.SelectedIndex = -1;
        }

        Ui.Draw(_skillFilter);
        using (Ui.Row().Padding(22, 0, 24, 0).Gap(12).Shrink(0).Open())
        {
            using (Ui.Row().Fill().Open())
            {
                Im.Line("SKILL", 10, theme.TextMuted);
            }

            using (Ui.Row().Width(36).Open())
            {
                Im.Line("ABIL", 10, theme.TextMuted);
            }

            using (Ui.Row().Width(40).Open())
            {
                Im.Line("RANKS", 10, theme.TextMuted);
            }

            using (Ui.Row().Width(54).Justify(UIJustify.Center).Open())
            {
                Im.Line("TOTAL", 10, theme.TextMuted);
            }
        }

        if (_visibleSkills.Count == 0)
        {
            Im.Empty(FontAwesome.MagnifyingGlass, "No skills match", "Try a shorter filter.");
            return;
        }

        Ui.Draw(_skillList);
        Im.Line("Accent dot = class skill (+3 with a rank). Click a total or double-click a row to roll.", 11, theme.TextDisabled);
    }

    private void DrawSkillRow(int index, bool selected)
    {
        var theme = Ui.Theme;
        var skill = _visibleSkills[index];
        var c = Character;
        var classSkill = c.IsClassSkill(skill);
        Ui.Element().Size(6, 6).Shrink(0).CornerRadius(3).Background(classSkill ? theme.Accent : theme.Border).Leaf();
        using (Ui.Row().Fill().Open())
        {
            Im.Line(skill.Name, 14, selected ? theme.Accent : theme.Text);
        }

        using (Ui.Row().Width(36).Shrink(0).Open())
        {
            Im.Line(Character.AbilityNames[(int)skill.Ability], 11, theme.TextMuted);
        }

        using (Ui.Row().Width(40).Shrink(0).Open())
        {
            var ranks = c.RanksIn(skill);
            Im.Line(ranks > 0 ? Fmt.Int(ranks) : "-", 13, ranks > 0 ? theme.Text : theme.TextDisabled);
        }

        var totalId = Id(1000 + (uint)Array.IndexOf(Character.Skills, skill));
        if (Im.Chip(totalId, null, Fmt.Sign(c.SkillTotal(skill)), selected ? ChipKind.Accent : ChipKind.Normal, $"Roll {skill.Name}"))
        {
            RollSkill(skill);
        }
    }

    private void DrawCombat()
    {
        var theme = Ui.Theme;
        var c = Character;
        using (Ui.Row().Gap(8).Wrap().LineGap(8).Shrink(0).Open())
        {
            if (Im.StatTile(Id(100), "INITIATIVE", Fmt.Sign(c.InitBonus), "roll", 110, true))
            {
                Roll(Dice.WithModifier("1d20", c.InitBonus), "Initiative");
            }

            Im.StatTile(Id(101), "BAB", Fmt.Sign(c.Bab), "", 80, false, "Base attack bonus");
            Im.StatTile(Id(102), "SPEED", $"{c.Speed} ft", "", 90, false);
        }

        Im.SectionHeader("ATTACKS");
        for (var i = 0; i < c.Attacks.Count; i++)
        {
            var attack = c.Attacks[i];
            using (Ui.Column().Gap(6).Padding(12).Shrink(0).Background(theme.SurfaceAlt).Border(theme.Border, 1).CornerRadius(8).Open())
            {
                using (Ui.Row().Gap(8).AlignItems(UIAlign.Center).Wrap().LineGap(6).Open())
                {
                    Im.Icon(FontAwesome.Crosshairs, 12, c.Color);
                    Im.Line(attack.Name, 15, theme.Text);
                    Im.Spacer();
                    if (Im.Chip(Id(200 + (uint)i * 4), FontAwesome.Cube, _attackLabels[i], ChipKind.Accent))
                    {
                        Roll(Dice.WithModifier("1d20", attack.Bonus), $"{attack.Name} attack");
                    }

                    if (Im.Chip(Id(201 + (uint)i * 4), null, attack.Damage, tooltip: "Roll damage", enabled: attack.Damage != "varies"))
                    {
                        Roll(attack.Damage, $"{attack.Name} damage");
                    }
                }

                using (Ui.Row().Gap(16).Open())
                {
                    Im.KeyValue("CRITICAL", attack.Critical);
                    Im.KeyValue("DAMAGE", attack.Damage);
                }

                if (attack.Notes.Length > 0)
                {
                    Im.Para(attack.Notes, 12, theme.TextMuted);
                }
            }
        }
    }

    private void DrawSpells()
    {
        var theme = Ui.Theme;
        var c = Character;
        if (c.Spells.Count == 0)
        {
            Im.Empty(FontAwesome.Lightbulb, "No spellcasting", $"{c.Name} doesn't prepare or cast spells. Switch to a caster's sheet to see spell cards and slots.");
            return;
        }

        Im.SectionHeader("SPELL SLOTS");
        for (var level = 0; level < c.SpellSlots.Length; level++)
        {
            var slots = c.SpellSlots[level];
            using (Ui.Row().Gap(6).AlignItems(UIAlign.Center).Shrink(0).Open())
            {
                using (Ui.Row().Width(76).Shrink(0).Open())
                {
                    Im.Line(level == 0 ? "Cantrips" : $"Level {level}", 13, theme.Text);
                }

                if (level == 0)
                {
                    Im.Line("at will", 12, theme.TextMuted);
                    continue;
                }

                var used = c.SpellSlotsUsed[level];
                for (var k = 0; k < slots; k++)
                {
                    var pip = Id(400 + (uint)(level * 16 + k));
                    var spent = k < used;
                    var hovered = Ui.Interaction(pip).Hovered;
                    Ui.Element(pip).Size(16, 16).Shrink(0).CornerRadius(8)
                        .Background(spent ? default : hovered ? theme.AccentHover : theme.Accent)
                        .Border(hovered ? theme.AccentHover : theme.Accent, 1.5f)
                        .Cursor(SystemCursor.Pointer)
                        .Animate()
                        .Leaf();
                    if (Ui.Interaction(pip).Clicked)
                    {
                        c.SpellSlotsUsed[level] = spent ? k : k + 1;
                    }
                }

                Ui.Text($"  {slots - used} of {slots} left", 12, theme.TextMuted);
            }
        }

        for (var level = 0; level < 10; level++)
        {
            var any = false;
            foreach (var spell in c.Spells)
            {
                if (spell.Level != level)
                {
                    continue;
                }

                if (!any)
                {
                    Im.SectionHeader(level == 0 ? "CANTRIPS" : $"LEVEL {level}");
                    any = true;
                }

                DrawSpell(spell);
            }
        }
    }

    private void DrawSpell(Spell spell)
    {
        var theme = Ui.Theme;
        var c = Character;
        var index = (uint)c.Spells.IndexOf(spell);
        var headerId = Id(300 + index * 2);
        var castId = Id(301 + index * 2);
        var header = Ui.Interaction(headerId);
        var expanded = _expanded.Contains(spell);
        var canCast = spell.Level == 0 || c.SpellSlotsUsed[spell.Level] < c.SpellSlots[spell.Level];
        var cast = false;

        using (Ui.Column().Shrink(0).Background(theme.SurfaceAlt).Border(expanded ? theme.BorderStrong : theme.Border, 1).CornerRadius(8).Clip().Animate().Open())
        {
            using (Ui.Row(headerId).Padding(10, 8).Gap(8).AlignItems(UIAlign.Center).Background(header.Hovered ? theme.Hover : default).Cursor(SystemCursor.Pointer).Open())
            {
                Im.Icon(expanded ? FontAwesome.AngleDown : FontAwesome.AngleRight, 12, theme.TextMuted);
                Im.Line(spell.Name, 14, theme.Text);
                using (Ui.Row().Fill().Open())
                {
                    Im.Line(spell.School, 12, theme.TextMuted);
                }

                cast = Im.Chip(castId, FontAwesome.Star, "Cast", ChipKind.Accent, canCast ? null : "No slots left at this level", canCast);
            }

            if (expanded)
            {
                using (Ui.Column().Padding(12, 2, 12, 12).Gap(10)
                           .Enter(UITransitionState.FromOpacity(0).WithOffset(new Vector2(0, -6)))
                           .Open())
                {
                    using (Ui.Row().Wrap().Gap(18).LineGap(8).Open())
                    {
                        Im.KeyValue("CASTING TIME", spell.CastingTime);
                        Im.KeyValue("RANGE", spell.Range);
                        Im.KeyValue("DURATION", spell.Duration);
                        Im.KeyValue("SAVE", spell.Save);
                    }

                    Im.Para(spell.Description, 14, theme.Text, 21);
                }
            }
        }

        if (cast)
        {
            if (spell.Level > 0)
            {
                c.SpellSlotsUsed[spell.Level]++;
            }

            _campaign.Chat.Post(MessageKind.Emote, c.Name, c.Color, $"casts {spell.Name}.");
        }
        else if (header.Clicked)
        {
            if (!_expanded.Remove(spell))
            {
                _expanded.Add(spell);
            }
        }
    }

    private void DrawInventory()
    {
        var theme = Ui.Theme;
        var c = Character;
        using (Ui.Row().Gap(8).AlignItems(UIAlign.Center).Shrink(0).Open())
        {
            Im.Icon(FontAwesome.Cube, 12, theme.TextMuted);
            Ui.Text($"{c.Inventory.Count} items · {c.CarriedWeight:0.#} lb carried", 13, theme.Text);
            Im.Spacer();
            Im.Icon(FontAwesome.Circle, 10, Im.Warn);
            Ui.Text($"{c.Gold} gp", 13, theme.Text);
        }

        var remove = -1;
        using (Ui.Column(Id(500)).Fill().ScrollY().Gap(2).Padding(0, 0, 10, 0).Background(theme.Surface).Open())
        {
            for (var i = 0; i < c.Inventory.Count; i++)
            {
                var item = c.Inventory[i];
                var rowId = Id(510 + (uint)i * 4);
                var hovered = Ui.Interaction(rowId).Hovered;
                using (Ui.Row(rowId).Padding(10, 5).Gap(8).AlignItems(UIAlign.Center).Shrink(0).CornerRadius(5)
                           .Background(hovered ? theme.Hover : (i & 1) == 1 ? theme.SurfaceAlt : default).Open())
                {
                    using (Ui.Column().Fill().Open())
                    {
                        Im.Para(item.Name, 14, theme.Text);
                    }

                    if (Im.Chip(rowId + 1, FontAwesome.Minus, "", ChipKind.Subtle, enabled: item.Quantity > 1))
                    {
                        item.Quantity--;
                    }

                    using (Ui.Row().Width(26).Justify(UIJustify.Center).Shrink(0).Open())
                    {
                        Im.Line(Fmt.Int(item.Quantity), 13, theme.Text);
                    }

                    if (Im.Chip(rowId + 2, FontAwesome.Plus, "", ChipKind.Subtle))
                    {
                        item.Quantity++;
                    }

                    using (Ui.Row().Width(56).Justify(UIJustify.End).Shrink(0).Open())
                    {
                        Ui.Text($"{item.Weight * item.Quantity:0.##} lb", 12, theme.TextMuted);
                    }

                    if (Im.Chip(rowId + 3, FontAwesome.TrashCan, "", ChipKind.Subtle, "Remove"))
                    {
                        remove = i;
                    }
                }
            }
        }

        if (remove >= 0)
        {
            _campaign.Chat.System($"{c.Name} dropped {c.Inventory[remove].Name}.");
            c.Inventory.RemoveAt(remove);
        }

        using (Ui.Row().Gap(6).AlignItems(UIAlign.Center).Shrink(0).Open())
        {
            Ui.Draw(_newItem);
            Ui.Draw(_addItem);
        }
    }

    private void AddItem()
    {
        var name = _newItem.Text.Trim();
        if (name.Length == 0)
        {
            return;
        }

        Character.Inventory.Add(new Item { Name = name });
        _newItem.Clear();
        Ui.ScrollToEnd(Id(500));
    }

    private void DrawNotes()
    {
        var theme = Ui.Theme;
        using (Ui.Row().Gap(8).AlignItems(UIAlign.Center).Shrink(0).Open())
        {
            Im.Line("Private notes", 13, theme.Text);
            Im.Spacer();
            Ui.Text($"{_notes.LineCount} lines · {Character.Notes.Length} characters", 11, theme.TextMuted);
        }

        Ui.Draw(_notes);
    }
}
