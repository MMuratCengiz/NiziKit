using DenOfIz;

namespace VttDemo.Model;

public enum TerrainKind
{
    Wall,
    Water,
    Pillar,
    Rubble,
    Carpet,
    Altar
}

public readonly record struct Terrain(TerrainKind Kind, int X, int Y, int Width, int Height, string? Label = null);

/// <summary>The table state: characters on the map, initiative, the chat log and the rules actions that post to it.</summary>
public sealed partial class Campaign
{
    public const string GmName = "Game Master";
    public static readonly UIColor GmColor = UIColor.Hex(0xE8B94AFF);

    private int _turn;

    public Campaign()
    {
        Characters = CreateCharacters();
        Party = Characters.Where(c => c.Faction == Faction.Party).ToList();
        Terrain = CreateTerrain();
        Journal = CreateJournal();
        Order = new List<Character>(Characters);
        Order.Sort((a, b) => b.Initiative.CompareTo(a.Initiative));
        SeedChat();
    }

    public string SceneName => "The Drowned Crypt";
    public int Columns => 34;
    public int Rows => 24;

    public List<Character> Characters { get; }
    public IReadOnlyList<Character> Party { get; }
    public List<Terrain> Terrain { get; }
    public List<JournalEntry> Journal { get; }
    public ChatLog Chat { get; } = new();

    public List<Character> Order { get; }
    public int Round { get; private set; } = 3;
    public int Turn => _turn;
    public Character? Current => Order.Count > 0 ? Order[_turn] : null;

    /// <summary>Short notifications for the table: (text, accent color).</summary>
    public event Action<string, UIColor>? Toast;

    public Character? Find(string name)
    {
        return Characters.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
               ?? Characters.FirstOrDefault(c => c.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase));
    }

    public UIColor ColorOf(string speaker)
    {
        return Find(speaker)?.Color ?? GmColor;
    }

    public RollResult Roll(string speaker, string formula, string? label)
    {
        var roll = Dice.Roll(formula);
        Chat.PostRoll(speaker, ColorOf(speaker), roll, label);
        if (roll.IsCritical)
        {
            Toast?.Invoke($"{speaker}: natural 20 on {label ?? "a d20"}!", UIColor.Hex(0x46B37AFF));
        }
        else if (roll.IsFumble)
        {
            Toast?.Invoke($"{speaker} rolled a natural 1…", UIColor.Hex(0xD9484BFF));
        }

        return roll;
    }

    public void Damage(Character target, int amount)
    {
        var before = target.Hp;
        var absorbed = Math.Min(target.TempHp, amount);
        target.TempHp -= absorbed;
        target.Hp -= amount - absorbed;
        var temp = absorbed > 0 ? $" ({absorbed} absorbed by temporary hit points)" : "";
        Chat.System($"{target.Name} takes {amount} damage{temp}: {before} → {target.Hp} HP.");
        if (before > 0 && target.Hp <= 0)
        {
            Toast?.Invoke($"{target.Name} is down!", UIColor.Hex(0xD9484BFF));
        }
    }

    public void Heal(Character target, int amount)
    {
        var before = target.Hp;
        target.Hp = Math.Min(target.MaxHp, target.Hp + amount);
        Chat.System($"{target.Name} heals {target.Hp - before}: {before} → {target.Hp} HP.");
    }

    public void SetCondition(Character target, string condition, bool on)
    {
        if (on ? !target.Conditions.Add(condition) : !target.Conditions.Remove(condition))
        {
            return;
        }

        Chat.System(on ? $"{target.Name} is now {condition}." : $"{target.Name} is no longer {condition}.");
    }

    public void RollInitiative()
    {
        foreach (var character in Characters)
        {
            character.Initiative = Dice.Random.Next(1, 21) + character.InitBonus;
        }

        Order.Sort((a, b) => b.Initiative != a.Initiative ? b.Initiative.CompareTo(a.Initiative) : b.InitBonus.CompareTo(a.InitBonus));
        _turn = 0;
        Round = 1;
        Chat.System("Initiative: " + string.Join(", ", Order.Select(c => $"{c.Name} {c.Initiative}")) + ".");
        AnnounceTurn();
    }

    public void NextTurn()
    {
        if (Order.Count == 0)
        {
            return;
        }

        for (var i = 0; i < Order.Count; i++)
        {
            _turn++;
            if (_turn >= Order.Count)
            {
                _turn = 0;
                Round++;
            }

            if (!Order[_turn].IsDown)
            {
                break;
            }
        }

        AnnounceTurn();
    }

    public void PreviousTurn()
    {
        if (Order.Count == 0 || (_turn == 0 && Round == 1))
        {
            return;
        }

        _turn--;
        if (_turn < 0)
        {
            _turn = Order.Count - 1;
            Round--;
        }

        AnnounceTurn();
    }

    private void AnnounceTurn()
    {
        if (Current is { } current)
        {
            Chat.System($"Round {Round} - {current.Name}'s turn.");
        }
    }
}
