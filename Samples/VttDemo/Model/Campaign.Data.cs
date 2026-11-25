using DenOfIz;

namespace VttDemo.Model;

public sealed partial class Campaign
{
    private static List<Character> CreateCharacters()
    {
        var brannoc = new Character
        {
            Name = "Brannoc Ashvale", Initials = "BA", Race = "Human", ClassName = "Fighter", Level = 5, Alignment = "LN",
            Faction = Faction.Party, Color = UIColor.Hex(0x4F8FE8FF),
            Scores = [18, 14, 14, 10, 12, 8], MaxHp = 49, Hp = 41, TempHp = 0,
            Ac = 21, Touch = 12, FlatFooted = 19, Bab = 5, FortBase = 4, RefBase = 1, WillBase = 1, Speed = 20,
            Ranks = new() { ["Climb"] = 5, ["Intimidate"] = 5, ["Perception"] = 2, ["Ride"] = 3, ["Survival"] = 2, ["Swim"] = 3 },
            ClassSkills = ["Climb", "Craft", "Handle Animal", "Intimidate", "Knowledge (Dungeoneering)", "Profession", "Ride", "Survival", "Swim"],
            Attacks =
            [
                new Attack { Name = "+1 Longsword", Bonus = 11, Damage = "1d8+8", Critical = "19-20/×2", Notes = "Weapon Focus, Weapon Specialization, Power Attack (-2 attack / +4 damage)." },
                new Attack { Name = "Heavy shield bash", Bonus = 9, Damage = "1d4+4", Notes = "Loses the shield bonus to AC until your next turn." },
                new Attack { Name = "Composite longbow", Bonus = 7, Damage = "1d8+4", Critical = "20/×3", Notes = "Range 110 ft. Strength rating +4." }
            ],
            Inventory =
            [
                new Item { Name = "+1 longsword", Weight = 4 }, new Item { Name = "+1 breastplate", Weight = 30 },
                new Item { Name = "Heavy steel shield", Weight = 15 }, new Item { Name = "Composite longbow (+4 Str)", Weight = 3 },
                new Item { Name = "Arrows", Quantity = 20, Weight = 0.15f }, new Item { Name = "Potion of cure light wounds", Quantity = 3 },
                new Item { Name = "Silk rope (50 ft.)", Weight = 5 }, new Item { Name = "Trail rations", Quantity = 5, Weight = 1 },
                new Item { Name = "Waterskin", Weight = 4 }, new Item { Name = "Whetstone", Weight = 1 }
            ],
            Gold = 212,
            Notes = """
                    Brannoc was the third son of a ferry-keeper on the Gray Mere, which meant he inherited nothing but a strong back and an opinion about the weather. He signed on with the Ashvale Free Company at sixteen, mostly because they fed you twice a day, and stayed for eleven years because it turned out he was good at standing in front of people who needed standing in front of.

                    The company broke at the siege of Kettle Ford. Brannoc carried the captain's banner out of the river mud and has carried the name ever since, although he will tell anyone who asks that he is not a nobleman and that "Ashvale" is a debt, not a title.

                    He is slow to speak and quick to step forward. He keeps a ledger of every companion he has lost, written in a careful, laborious hand, and he reads it once a year on the anniversary of the Ford.

                    Session notes:
                    - Owes Mother Hesk at the Sunken Lantern 40 gp for the shield repair.
                    - Promised Tamsin he would "not mention the thing with the duke's cousin" again.
                    - Suspects the ferryman at the crypt causeway is the same man who sold the company out.
                    """
        };

        var ilyra = new Character
        {
            Name = "Ilyra Moonwhisper", Initials = "IM", Race = "Elf", ClassName = "Wizard", Level = 5, Alignment = "CG",
            Faction = Faction.Party, Color = UIColor.Hex(0xA77BF3FF),
            Scores = [8, 16, 12, 20, 12, 10], MaxHp = 32, Hp = 32, TempHp = 0,
            Ac = 17, Touch = 13, FlatFooted = 14, Bab = 2, FortBase = 1, RefBase = 1, WillBase = 4, Speed = 30,
            Ranks = new()
            {
                ["Appraise"] = 2, ["Knowledge (Arcana)"] = 5, ["Knowledge (Dungeoneering)"] = 5, ["Knowledge (Religion)"] = 3,
                ["Knowledge (Local)"] = 2, ["Linguistics"] = 3, ["Perception"] = 2, ["Spellcraft"] = 5, ["Fly"] = 1
            },
            ClassSkills = ["Appraise", "Craft", "Fly", "Knowledge (Arcana)", "Knowledge (Dungeoneering)", "Knowledge (Local)", "Knowledge (Nature)", "Knowledge (Religion)", "Linguistics", "Profession", "Spellcraft"],
            Attacks =
            [
                new Attack { Name = "Quarterstaff", Bonus = 1, Damage = "1d6-1" },
                new Attack { Name = "Ray (ranged touch)", Bonus = 5, Damage = "varies", Notes = "Scorching ray, ray of frost and similar effects." }
            ],
            SpellSlots = [4, 5, 4, 2],
            SpellSlotsUsed = [0, 2, 1, 0, 0, 0, 0, 0, 0, 0],
            Spells = Spells.Wizard,
            Inventory =
            [
                new Item { Name = "Quarterstaff", Weight = 4 }, new Item { Name = "Spellbook", Weight = 3 },
                new Item { Name = "Spell component pouch", Weight = 2 }, new Item { Name = "Wand of magic missile (23 charges)" },
                new Item { Name = "Scroll of fly", Quantity = 2 }, new Item { Name = "Ink and quills", Weight = 0.5f },
                new Item { Name = "Headband of vast intelligence +2" }, new Item { Name = "Bedroll", Weight = 5 }
            ],
            Gold = 87,
            Notes = """
                    Ilyra left the Moonwhisper enclave a hundred and twelve years ago over a disagreement about whether fire is a school of magic or a character flaw. She has not been back, though she writes to her aunt every winter solstice and receives, every spring, a single pressed flower in reply.

                    Her spellbook is bound in blue leather and annotated in three languages, one of which she invented at the age of forty and still uses for anything embarrassing. The margins of the evocation chapter are almost entirely diagrams of how to make fireballs smaller, which she maintains is the harder problem.

                    She is curious about the drowned crypt because the inscriptions on the causeway use a dialect of the old river script that should not exist this far north, and she would very much like to know who carved them.
                    """
        };

        var tamsin = new Character
        {
            Name = "Tamsin Quickfingers", Initials = "TQ", Race = "Halfling", ClassName = "Rogue", Level = 5, Alignment = "CN",
            Faction = Faction.Party, Color = UIColor.Hex(0x46B37AFF),
            Scores = [10, 20, 12, 14, 10, 12], MaxHp = 36, Hp = 22, TempHp = 0,
            Ac = 19, Touch = 16, FlatFooted = 14, Bab = 3, FortBase = 2, RefBase = 5, WillBase = 2, Speed = 20, InitMisc = 2,
            Ranks = new()
            {
                ["Acrobatics"] = 5, ["Bluff"] = 3, ["Climb"] = 2, ["Disable Device"] = 5, ["Escape Artist"] = 3, ["Perception"] = 5,
                ["Sense Motive"] = 2, ["Sleight of Hand"] = 5, ["Stealth"] = 5, ["Use Magic Device"] = 3, ["Knowledge (Local)"] = 2
            },
            ClassSkills = ["Acrobatics", "Appraise", "Bluff", "Climb", "Craft", "Diplomacy", "Disable Device", "Disguise", "Escape Artist", "Intimidate", "Knowledge (Dungeoneering)", "Knowledge (Local)", "Linguistics", "Perception", "Perform", "Profession", "Sense Motive", "Sleight of Hand", "Stealth", "Swim", "Use Magic Device"],
            Attacks =
            [
                new Attack { Name = "Rapier", Bonus = 9, Damage = "1d4", Critical = "18-20/×2", Notes = "Weapon Finesse. Sneak attack +3d6 when flanking or against a flat-footed target." },
                new Attack { Name = "Sneak attack", Bonus = 9, Damage = "1d4+3d6", Critical = "18-20/×2", Notes = "Rapier with sneak attack dice. Precision damage is not multiplied on a critical hit." },
                new Attack { Name = "Shortbow", Bonus = 9, Damage = "1d4", Critical = "20/×3", Notes = "Range 60 ft." }
            ],
            Inventory =
            [
                new Item { Name = "Rapier", Weight = 1 }, new Item { Name = "Shortbow", Weight = 1 }, new Item { Name = "Arrows", Quantity = 30, Weight = 0.075f },
                new Item { Name = "Masterwork thieves' tools", Weight = 1 }, new Item { Name = "+1 studded leather", Weight = 10 },
                new Item { Name = "Cloak of resistance +1", Weight = 0.5f }, new Item { Name = "Caltrops", Quantity = 2, Weight = 0.5f },
                new Item { Name = "Smokestick", Quantity = 3, Weight = 0.5f }, new Item { Name = "A suspicious amount of other people's buttons" }
            ],
            Gold = 341,
            Notes = """
                    Tamsin's official story is that she is a locksmith's apprentice from Lower Brindle who got lost on the way to a delivery. Her unofficial story changes every time someone buys her a drink.

                    What is known for certain: she can open any lock in the Sunken Lantern in under a minute (the party timed it), she is afraid of deep water, and she has a standing arrangement with a fence named Corvo that nobody else in the party is allowed to know about.

                    She has decided the drowned crypt is a terrible idea and has also decided to go first, because "if there's a trap I want to be the one who finds it, not the one who finds it with my face".
                    """
        };

        var odile = new Character
        {
            Name = "Sister Odile", Initials = "SO", Race = "Human", ClassName = "Cleric", Level = 5, Alignment = "NG",
            Faction = Faction.Party, Color = UIColor.Hex(0xE8924AFF),
            Scores = [12, 10, 14, 10, 18, 14], MaxHp = 38, Hp = 38, TempHp = 6,
            Ac = 18, Touch = 10, FlatFooted = 18, Bab = 3, FortBase = 4, RefBase = 1, WillBase = 4, Speed = 20,
            Ranks = new() { ["Heal"] = 5, ["Knowledge (Religion)"] = 5, ["Diplomacy"] = 3, ["Sense Motive"] = 3, ["Spellcraft"] = 2 },
            ClassSkills = ["Appraise", "Craft", "Diplomacy", "Heal", "Knowledge (Arcana)", "Knowledge (Religion)", "Linguistics", "Profession", "Sense Motive", "Spellcraft"],
            Attacks =
            [
                new Attack { Name = "Heavy mace", Bonus = 4, Damage = "1d8+1" },
                new Attack { Name = "Spiritual weapon", Bonus = 7, Damage = "1d8+1", Notes = "Force effect; attacks with your base attack bonus plus Wisdom modifier." }
            ],
            SpellSlots = [4, 5, 4, 2],
            SpellSlotsUsed = [0, 1, 2, 1, 0, 0, 0, 0, 0, 0],
            Spells = Spells.Cleric,
            Inventory =
            [
                new Item { Name = "Heavy mace", Weight = 8 }, new Item { Name = "Banded mail", Weight = 35 }, new Item { Name = "Light wooden shield", Weight = 5 },
                new Item { Name = "Silver holy symbol of the Dawnmother", Weight = 1 }, new Item { Name = "Healer's kit (7 uses)", Weight = 1 },
                new Item { Name = "Wand of cure light wounds (31 charges)" }, new Item { Name = "Holy water", Quantity = 4, Weight = 1 },
                new Item { Name = "Prayer book", Weight = 1 }
            ],
            Gold = 54,
            Notes = """
                    Odile tends the shrine of the Dawnmother at Hesk's Landing, a shrine so small that the congregation fits on a single bench, provided nobody brings a dog. She has been its keeper for nine years.

                    When the fishermen started pulling up grave-goods in their nets, it was Odile who recognised the funerary marks and Odile who insisted that whatever was buried under the mere should stay buried, and when it became clear it would not, it was Odile who went looking for people foolish enough to go down after it.

                    She hums hymns when she is frightened, which the party has learned to treat as a perception check.
                    """
        };

        var goblinA = Monster("Goblin Skirmisher", "G1", 0xC9483FFF, hp: 6, ac: 16, initiative: 17, attack: new Attack { Name = "Short sword", Bonus = 2, Damage = "1d4" });
        var goblinB = Monster("Goblin Archer", "G2", 0xC9483FFF, hp: 5, ac: 16, initiative: 12, attack: new Attack { Name = "Short bow", Bonus = 4, Damage = "1d4", Critical = "20/×3" });
        var goblinC = Monster("Goblin Skulker", "G3", 0xC9483FFF, hp: 3, ac: 16, initiative: 9, attack: new Attack { Name = "Dogslicer", Bonus = 2, Damage = "1d6" });
        var bugbear = Monster("Bugbear Brute", "BB", 0xB0413AFF, hp: 22, ac: 17, initiative: 14, attack: new Attack { Name = "Morningstar", Bonus = 5, Damage = "1d8+3" }, cr: 2);
        var ogre = Monster("Drowned Ogre", "OG", 0x8E5A3FFF, hp: 30, ac: 17, initiative: 6, attack: new Attack { Name = "Greatclub", Bonus = 8, Damage = "2d8+7" }, cr: 3, size: 2);
        ogre.Conditions.Add("Sickened");

        brannoc.Initiative = 15;
        ilyra.Initiative = 18;
        tamsin.Initiative = 22;
        odile.Initiative = 11;

        Place(brannoc, 11, 11);
        Place(tamsin, 12, 9);
        Place(ilyra, 6, 11);
        Place(odile, 7, 12);
        Place(goblinA, 17, 8);
        Place(goblinB, 20, 5);
        Place(goblinC, 21, 12);
        Place(bugbear, 16, 6);
        Place(ogre, 25, 10);
        tamsin.Conditions.Add("Shaken");

        return [brannoc, ilyra, tamsin, odile, goblinA, goblinB, goblinC, bugbear, ogre];
    }

    private static Character Monster(string name, string initials, uint color, int hp, int ac, int initiative, Attack attack, int cr = 1, int size = 1)
    {
        return new Character
        {
            Name = name, Initials = initials, ClassName = size > 1 ? "Large humanoid" : "Humanoid", Level = cr,
            Faction = Faction.Enemy, Color = UIColor.Hex(color),
            Scores = size > 1 ? [21, 8, 15, 6, 10, 7] : [11, 15, 12, 10, 9, 6],
            MaxHp = hp, Hp = hp, Ac = ac, Touch = ac - 4, FlatFooted = ac - 2, Bab = cr, FortBase = 2 + cr, RefBase = 1, WillBase = 0,
            Initiative = initiative, SizeCells = size,
            Ranks = new() { ["Stealth"] = 4, ["Perception"] = 2 },
            ClassSkills = ["Stealth", "Perception"],
            Attacks = [attack],
            Notes = "A creature of the drowned crypt. Its notes are for the Game Master's eyes only."
        };
    }

    private static void Place(Character character, int x, int y)
    {
        character.CellX = x;
        character.CellY = y;
    }

    private static List<Terrain> CreateTerrain()
    {
        return
        [
            new(TerrainKind.Carpet, 15, 4, 3, 9),
            new(TerrainKind.Water, 11, 14, 12, 6, "Flooded nave"),
            new(TerrainKind.Water, 25, 17, 6, 4),
            new(TerrainKind.Rubble, 3, 16, 4, 3, "Collapsed ossuary"),
            new(TerrainKind.Rubble, 27, 3, 3, 2),
            new(TerrainKind.Altar, 15, 2, 3, 2, "Altar of the Drowned"),
            new(TerrainKind.Wall, 1, 1, 32, 1),
            new(TerrainKind.Wall, 1, 22, 32, 1),
            new(TerrainKind.Wall, 1, 1, 1, 22),
            new(TerrainKind.Wall, 32, 1, 1, 22),
            new(TerrainKind.Wall, 9, 1, 1, 9),
            new(TerrainKind.Wall, 9, 14, 1, 8),
            new(TerrainKind.Wall, 24, 1, 1, 8),
            new(TerrainKind.Wall, 24, 13, 1, 9),
            new(TerrainKind.Pillar, 12, 4, 1, 1),
            new(TerrainKind.Pillar, 12, 10, 1, 1),
            new(TerrainKind.Pillar, 20, 4, 1, 1),
            new(TerrainKind.Pillar, 20, 10, 1, 1)
        ];
    }

    private void SeedChat()
    {
        var chat = Chat;
        chat.System("Welcome to the NiziKit VTT proof of concept. Type /help for chat commands; drag tokens, right-click them, or double-click the map to ping.");
        chat.Post(MessageKind.Chat, GmName, GmColor,
            "The causeway ends at a door of green bronze, half sunk in the mere. Beyond it, stone steps descend into a nave where the floodwater lies flat and black as a mirror. Columns rise out of it like the masts of drowned ships, and somewhere ahead, in the dark, something is dripping with the patience of a thing that has been dripping for three hundred years.");
        chat.Post(MessageKind.Chat, "Brannoc Ashvale", ColorOf("Brannoc"), "I'll take point. Tamsin, can you check the door before we commit to the stairs?");
        chat.Post(MessageKind.Emote, "Tamsin Quickfingers", ColorOf("Tamsin"), "cracks her knuckles, then thinks better of it and quietly un-cracks them.");
        chat.PostRoll("Tamsin Quickfingers", ColorOf("Tamsin"), Dice.Roll("1d20+13"), "Disable Device");
        chat.Post(MessageKind.Whisper, GmName, GmColor, "You notice a faint draft from the eastern wall. The mortar there is newer than the rest.", "Tamsin Quickfingers");
        chat.Post(MessageKind.Emote, "Ilyra Moonwhisper", ColorOf("Ilyra"), "traces a sigil in the air; motes of pale light drift up toward the vaulted ceiling and settle there like patient stars.");
        chat.Post(MessageKind.OutOfCharacter, "Sister Odile", ColorOf("Odile"), "brb, kettle's on. Brannoc can have my snacks if I die.");
        chat.PostRoll("Brannoc Ashvale", ColorOf("Brannoc"), Dice.Roll("1d20+11"), "+1 Longsword attack");
        chat.PostRoll("Brannoc Ashvale", ColorOf("Brannoc"), Dice.Roll("1d8+8"), "+1 Longsword damage");
        chat.System("Round 3 - Tamsin Quickfingers's turn.");
    }
}
