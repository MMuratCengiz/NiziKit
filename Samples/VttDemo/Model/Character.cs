using DenOfIz;
using NiziKit.UI;

namespace VttDemo.Model;

public enum Faction
{
    Party,
    Enemy
}

public enum Ability
{
    Str,
    Dex,
    Con,
    Int,
    Wis,
    Cha
}

public sealed record SkillDef(string Name, Ability Ability);

public sealed class Attack
{
    public required string Name { get; init; }
    public int Bonus { get; init; }
    public required string Damage { get; init; }
    public string Critical { get; init; } = "20/×2";
    public string Notes { get; init; } = "";
}

public sealed class Spell
{
    public required string Name { get; init; }
    public int Level { get; init; }
    public required string School { get; init; }
    public string CastingTime { get; init; } = "1 standard action";
    public string Range { get; init; } = "Close";
    public string Duration { get; init; } = "Instantaneous";
    public string Save { get; init; } = "None";
    public required string Description { get; init; }
}

public sealed class Item
{
    public required string Name { get; set; }
    public int Quantity { get; set; } = 1;
    public float Weight { get; set; }
}

public sealed class Character
{
    public static readonly string[] AbilityNames = ["STR", "DEX", "CON", "INT", "WIS", "CHA"];

    public static readonly string[] AllConditions =
        ["Blinded", "Confused", "Dazed", "Entangled", "Fatigued", "Frightened", "Grappled", "Invisible", "Prone", "Shaken", "Sickened", "Stunned"];

    public static readonly SkillDef[] Skills =
    [
        new("Acrobatics", Ability.Dex), new("Appraise", Ability.Int), new("Bluff", Ability.Cha), new("Climb", Ability.Str),
        new("Craft", Ability.Int), new("Diplomacy", Ability.Cha), new("Disable Device", Ability.Dex), new("Disguise", Ability.Cha),
        new("Escape Artist", Ability.Dex), new("Fly", Ability.Dex), new("Handle Animal", Ability.Cha), new("Heal", Ability.Wis),
        new("Intimidate", Ability.Cha), new("Knowledge (Arcana)", Ability.Int), new("Knowledge (Dungeoneering)", Ability.Int),
        new("Knowledge (Local)", Ability.Int), new("Knowledge (Nature)", Ability.Int), new("Knowledge (Religion)", Ability.Int),
        new("Linguistics", Ability.Int), new("Perception", Ability.Wis), new("Perform", Ability.Cha), new("Profession", Ability.Wis),
        new("Ride", Ability.Dex), new("Sense Motive", Ability.Wis), new("Sleight of Hand", Ability.Dex), new("Spellcraft", Ability.Int),
        new("Stealth", Ability.Dex), new("Survival", Ability.Wis), new("Swim", Ability.Str), new("Use Magic Device", Ability.Cha)
    ];

    private static uint _nextId;

    public Character()
    {
        Id = ++_nextId;
    }

    public uint Id { get; }

    public required string Name { get; init; }
    public required string Initials { get; init; }
    public string Race { get; init; } = "";
    public string ClassName { get; init; } = "";
    public int Level { get; init; } = 1;
    public string Alignment { get; init; } = "N";
    public Faction Faction { get; init; }
    public UIColor Color { get; init; }

    public int[] Scores { get; init; } = [10, 10, 10, 10, 10, 10];
    public int MaxHp { get; init; }
    public int Hp { get; set; }
    public int TempHp { get; set; }
    public int Ac { get; init; }
    public int Touch { get; init; }
    public int FlatFooted { get; init; }
    public int Bab { get; init; }
    public int FortBase { get; init; }
    public int RefBase { get; init; }
    public int WillBase { get; init; }
    public int Speed { get; init; } = 30;
    public int InitMisc { get; init; }

    public Dictionary<string, int> Ranks { get; init; } = new();
    public HashSet<string> ClassSkills { get; init; } = new();
    public List<Attack> Attacks { get; init; } = new();
    public List<Spell> Spells { get; init; } = new();

    /// <summary>Spell slots per spell level (index 0 is cantrips/orisons).</summary>
    public int[] SpellSlots { get; init; } = [];
    public int[] SpellSlotsUsed { get; set; } = new int[10];

    public List<Item> Inventory { get; init; } = new();
    public int Gold { get; set; }
    public string Notes { get; set; } = "";
    public HashSet<string> Conditions { get; } = new();

    /// <summary>Squares the token occupies per side: 1 for Medium and smaller, 2 for Large.</summary>
    public int SizeCells { get; init; } = 1;

    public int CellX { get; set; }
    public int CellY { get; set; }
    public int Initiative { get; set; }

    public string Subtitle => Faction == Faction.Party ? $"{Race} {ClassName} {Level} · {Alignment}" : $"{ClassName} · CR {Level}";

    public bool IsDown => Hp <= 0;

    public int Score(Ability ability)
    {
        return Scores[(int)ability];
    }

    public int Mod(Ability ability)
    {
        return (int)Math.Floor((Score(ability) - 10) / 2.0);
    }

    public int Fort => FortBase + Mod(Ability.Con);
    public int Ref => RefBase + Mod(Ability.Dex);
    public int Will => WillBase + Mod(Ability.Wis);
    public int InitBonus => Mod(Ability.Dex) + InitMisc;
    public int Cmb => Bab + Mod(Ability.Str);
    public int Cmd => 10 + Bab + Mod(Ability.Str) + Mod(Ability.Dex);

    public int RanksIn(SkillDef skill)
    {
        return Ranks.GetValueOrDefault(skill.Name);
    }

    public bool IsClassSkill(SkillDef skill)
    {
        return ClassSkills.Contains(skill.Name);
    }

    public int SkillTotal(SkillDef skill)
    {
        var ranks = RanksIn(skill);
        return ranks + Mod(skill.Ability) + (ranks > 0 && IsClassSkill(skill) ? 3 : 0);
    }

    public float CarriedWeight
    {
        get
        {
            var total = 0f;
            foreach (var item in Inventory)
            {
                total += item.Weight * item.Quantity;
            }

            return total;
        }
    }
}
