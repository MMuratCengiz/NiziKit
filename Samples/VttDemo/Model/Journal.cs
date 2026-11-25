namespace VttDemo.Model;

public enum BlockKind
{
    Heading,
    Paragraph,
    ReadAloud,
    Bullet,
    GmNote
}

public readonly record struct JournalBlock(BlockKind Kind, string Text);

public sealed class JournalEntry
{
    public required string Title { get; init; }
    public required string Subtitle { get; init; }
    public required List<JournalBlock> Blocks { get; init; }
}

public sealed partial class Campaign
{
    private static JournalBlock H(string text) => new(BlockKind.Heading, text);
    private static JournalBlock P(string text) => new(BlockKind.Paragraph, text);
    private static JournalBlock Box(string text) => new(BlockKind.ReadAloud, text);
    private static JournalBlock Li(string text) => new(BlockKind.Bullet, text);
    private static JournalBlock Gm(string text) => new(BlockKind.GmNote, text);

    private static List<JournalEntry> CreateJournal()
    {
        return
        [
            new JournalEntry
            {
                Title = "The Drowned Crypt",
                Subtitle = "Location · Gray Mere, beneath the old causeway",
                Blocks =
                [
                    Box("The stair ends in water. It is shallow at first, cold enough to ache, and so still that the light from your lanterns lies on its surface like a second ceiling. Pillars carved with river-serpents march away into the dark. At the far end of the nave, raised above the flood on a dais of green stone, an altar waits under a canopy of rotted silk - and on the altar, something gleams."),
                    P("The crypt was built by the river-folk who held the Gray Mere before the first human settlers came north. It was never meant to be underwater; the mere rose over two centuries as the old dams failed, and the crypt's keepers sealed the doors rather than abandon it. Most of the nave now lies under one to three feet of water, deeper in the centre where the floor has subsided."),
                    H("Features of the area"),
                    Li("Ceilings are 25 feet high and vaulted. Sound carries; any combat in the nave alerts everything in the east and west chambers."),
                    Li("The flooded nave is difficult terrain. Small creatures must swim in the deep section (DC 10 Swim); Medium creatures wade."),
                    Li("The pillars provide cover. Their carvings depict a procession of masked figures carrying a bier toward the altar - a Knowledge (Religion) DC 15 check identifies it as a funeral rite for a ruler, not a priest."),
                    Li("The collapsed ossuary in the south-west is unstable. Any creature that moves through it at more than half speed must succeed on a DC 12 Reflex save or be partially buried (entangled until it spends a move action to dig free)."),
                    Li("Light sources other than the party's are absent. Goblins and the bugbear have darkvision; the ogre does not, which is why it waits in the east chamber with a lantern hooded under its cloak."),
                    H("The altar"),
                    P("The altar is a single block of green serpentine, cold to the touch even by the standards of the crypt. Set into its top is a reliquary of tarnished silver shaped like a coiled eel, its eyes two flawed sapphires. Lifting the reliquary triggers no trap; that is what makes the river-folk's priests so unsettling to scholars. They did not guard their dead. They believed their dead would guard themselves."),
                    Gm("If the reliquary is moved, the water in the nave begins to rise one inch per round. After ten rounds the drowned dead begin to stir. Do not tell the players about the rate; describe the water touching the third step, then the fourth, and let them count.")
                ]
            },
            new JournalEntry
            {
                Title = "Session 12 Recap",
                Subtitle = "Written by Ilyra, corrected (in red ink) by Tamsin",
                Blocks =
                [
                    P("We left the Sunken Lantern at first light, which Brannoc defines as \"the moment the rooster gives up\" and everyone else defines as \"too early\". Mother Hesk packed us bread, cheese, and a stern warning about the causeway, which she says was built by people who did not want anyone to come back across it. She did not elaborate. She rarely does."),
                    P("The causeway itself is a marvel: forty yards of fitted stone, every block engraved with a single line of the old river script, so that walking from one end to the other is like reading a sentence that never quite finishes. I copied as much as I could before the light failed. The text is a list of names, I think - or possibly a list of debts. In the river-folk's grammar the two are the same word, which says something about them I have not yet decided how to feel about."),
                    P("Halfway across we were ambushed by three goblins riding what Tamsin insists were \"very large, very angry frogs\" and what I maintain were giant toads. (Tamsin: they were frogs. Ilyra: there is a difference and it is warts.) Brannoc held the line, Odile kept him standing, and I set one of the toads on fire, which I regret only because of the smell. Tamsin stole a goblin's purse during the fight. It contained nine copper pieces, a dried fish and a tooth that is not a goblin's tooth."),
                    P("At the end of the causeway we found the bronze door, a second set of inscriptions in the same hand as the causeway, and fresh scratches around the lock. Somebody has been here recently. Somebody with a key."),
                    H("Loose ends"),
                    Li("Who carved the second set of inscriptions, and why are they three centuries newer than the first?"),
                    Li("The tooth. Odile thinks it is human. Brannoc thinks we should stop carrying it around. Tamsin thinks it might be worth something. I think all three of them are right."),
                    Li("Mother Hesk knew about the door. How?"),
                    Li("The ferryman at the causeway charged us the full fare going out and offered to wait for free. Nobody offers to wait for free."),
                    H("Experience and treasure"),
                    P("1,200 XP each for the causeway ambush and the bronze door. Treasure: 47 gp in mixed coin, a masterwork dagger with a frog engraved on the pommel (Tamsin has claimed it, citing \"thematic resonance\"), and two potions that Odile has identified as cure light wounds and Ilyra has identified as \"probably not poison\".")
                ]
            },
            new JournalEntry
            {
                Title = "Table Rules",
                Subtitle = "House rules for this campaign",
                Blocks =
                [
                    P("These are the rules we agreed on in session zero. They exist so that everyone at the table - including the Game Master - has a good time. If something here stops working, bring it up and we'll change it."),
                    H("At the table"),
                    Li("Roll in the open, in the chat log, so everyone can share the joy and the despair. The GM rolls behind the screen when the result should be a surprise."),
                    Li("Critical hits roll the confirmation automatically. Natural 20s on skill checks are not automatic successes, but they are always worth describing."),
                    Li("Hero points: everyone starts each session with one. Spend it to reroll any d20, or to stabilize automatically when dying."),
                    Li("If you are away from the keyboard, say so in out-of-character chat (/ooc) so the table can keep going."),
                    H("Safety tools"),
                    P("Anyone may pause the game at any time, for any reason, without needing to explain. Type \"/x\" in chat and the scene moves on; no questions asked. Lines and veils from session zero are pinned in the GM's notes and apply to every session, including one-shots."),
                    H("Scheduling"),
                    P("We play every other Thursday, 7 pm until 10:30 pm. If three or more players can make it, we play; absent characters are assumed to be busy doing something heroic just off-screen and receive full experience. If you know you'll be late, post in the chat so the GM can plan around your entrance.")
                ]
            }
        ];
    }
}
