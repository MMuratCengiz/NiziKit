namespace VttDemo.Model;

/// <summary>Sample spell lists. The descriptions are intentionally long to exercise word wrapping.</summary>
public static class Spells
{
    public static List<Spell> Wizard =>
    [
        new Spell
        {
            Name = "Detect Magic", Level = 0, School = "Divination", Range = "60 ft. cone", Duration = "Concentration, up to 1 min./level",
            Description = "You sense the presence of magical auras within a cone emanating from you. The longer you concentrate on the same area, the more you learn: the first round reveals whether any aura is present, the second the number of distinct auras and the strength of the strongest, and the third the strength and location of each one. Thick stone, a thin sheet of lead or a few feet of earth blocks the spell."
        },
        new Spell
        {
            Name = "Ray of Frost", Level = 0, School = "Evocation [cold]", Duration = "Instantaneous",
            Description = "A thin ray of pale blue cold springs from your fingertip. You must succeed on a ranged touch attack to hit, dealing 1d3 points of cold damage. Frost rimes the target's armor for a moment before it melts."
        },
        new Spell
        {
            Name = "Mage Hand", Level = 0, School = "Transmutation", Duration = "Concentration",
            Description = "You point your finger at an unattended object weighing no more than five pounds and move it up to fifteen feet in any direction each round, as if carried by an invisible hand. The spell ends if the distance between you and the object ever exceeds the spell's range."
        },
        new Spell
        {
            Name = "Magic Missile", Level = 1, School = "Evocation [force]", Range = "Medium (100 ft. + 10 ft./level)", Duration = "Instantaneous",
            Description = "Darts of shimmering violet force leap from your hand and unerringly strike their targets. Each missile deals 1d4+1 points of force damage. You gain one missile at first level and an additional missile for every two caster levels beyond first, to a maximum of five. You may aim the missiles at a single creature or spread them among several, but you must designate the targets before rolling damage, and every target must be visible. The missiles strike even targets in total concealment if you know where they are, and they ignore cover short of total cover."
        },
        new Spell
        {
            Name = "Shield", Level = 1, School = "Abjuration [force]", CastingTime = "1 standard action", Range = "Personal", Duration = "1 min./level",
            Description = "An invisible disc of force hovers in front of you, granting a +4 shield bonus to Armor Class and negating magic missiles directed at you. The disc moves with you and has no armor check penalty or arcane spell failure chance associated with it."
        },
        new Spell
        {
            Name = "Mage Armor", Level = 1, School = "Conjuration (creation) [force]", Range = "Touch", Duration = "1 hour/level",
            Description = "An invisible but tangible field of force surrounds the subject, providing a +4 armor bonus to AC. Because the field is made of force, incorporeal creatures cannot bypass it the way they would ordinary armor."
        },
        new Spell
        {
            Name = "Grease", Level = 1, School = "Conjuration (creation)", Range = "Close (25 ft. + 5 ft./2 levels)", Duration = "1 min./level", Save = "Reflex negates",
            Description = "A slick film of grease covers a 10-foot square or a single object. Any creature in the area when the spell is cast must succeed on a Reflex save or fall prone. A creature can walk within or through the area at half speed with a DC 10 Acrobatics check; failure means it can't move that round, and failure by 5 or more means it falls. Creatures that do not move need not make a check and are not considered flat-footed."
        },
        new Spell
        {
            Name = "Scorching Ray", Level = 2, School = "Evocation [fire]", Range = "Close (25 ft. + 5 ft./2 levels)", Duration = "Instantaneous",
            Description = "You blast your enemies with fiery rays. You may fire one ray, plus one additional ray for every four levels beyond third, to a maximum of three rays at eleventh level. Each ray requires a ranged touch attack to hit and deals 4d6 points of fire damage. The rays may be fired at the same or different targets, but all rays must be aimed at targets within 30 feet of each other and fired simultaneously."
        },
        new Spell
        {
            Name = "Glitterdust", Level = 2, School = "Conjuration (creation)", Range = "Medium (100 ft. + 10 ft./level)", Duration = "1 round/level", Save = "Will negates (blinding only)",
            Description = "A cloud of golden particles covers everyone and everything in a 10-foot-radius spread, blinding creatures that fail their save and visibly outlining invisible things for the duration. Each round at the end of their turn, blinded creatures may attempt new saving throws to end the blindness. Anyone covered by the dust takes a -40 penalty on Stealth checks."
        },
        new Spell
        {
            Name = "Invisibility", Level = 2, School = "Illusion (glamer)", Range = "Personal or touch", Duration = "1 min./level",
            Description = "The creature or object touched becomes invisible. If the recipient is a creature carrying gear, that vanishes too. The spell ends if the subject attacks any creature; for the purposes of this spell, an attack includes any spell targeting a foe or whose area or effect includes a foe."
        },
        new Spell
        {
            Name = "Fireball", Level = 3, School = "Evocation [fire]", Range = "Long (400 ft. + 40 ft./level)", Duration = "Instantaneous", Save = "Reflex half",
            Description = "A bead of ember-orange light streaks from your pointing finger to a point you choose and blossoms with a low roar into a sphere of flame. Every creature within a 20-foot-radius spread takes 1d6 points of fire damage per caster level, to a maximum of 10d6. Unattended combustibles catch fire, and the heat can melt soft metals such as lead, gold, copper, silver and bronze. If the bead strikes a material body or solid barrier before reaching the prescribed range, it detonates early - which is why Ilyra has written, three times and underlined twice, CHECK THE DOORWAY in the margin of her spellbook."
        },
        new Spell
        {
            Name = "Haste", Level = 3, School = "Transmutation", Range = "Close (25 ft. + 5 ft./2 levels)", Duration = "1 round/level",
            Description = "The transmuted creatures move and act more quickly than normal. A hasted creature may make one extra attack when making a full attack, gains a +1 bonus on attack rolls and a +1 dodge bonus to AC and Reflex saves, and all of its modes of movement increase by 30 feet, to a maximum of twice its normal speed. Multiple haste effects don't stack."
        }
    ];

    public static List<Spell> Cleric =>
    [
        new Spell
        {
            Name = "Guidance", Level = 0, School = "Divination", Range = "Touch", Duration = "1 minute or until discharged",
            Description = "This spell imbues the subject with a touch of divine guidance. The creature gets a +1 competence bonus on a single attack roll, saving throw or skill check, which it must choose to use before making the roll."
        },
        new Spell
        {
            Name = "Stabilize", Level = 0, School = "Conjuration (healing)", Range = "Close", Duration = "Instantaneous",
            Description = "Upon casting this spell, you target a living creature that has negative hit points. That creature is automatically stabilized and does not lose any further hit points. If the creature later takes damage, it continues dying normally."
        },
        new Spell
        {
            Name = "Bless", Level = 1, School = "Enchantment (compulsion) [mind-affecting]", Range = "50 ft.", Duration = "1 min./level",
            Description = "Bless fills your allies with courage. Each ally gains a +1 morale bonus on attack rolls and on saving throws against fear effects. Odile sings it rather than speaks it, slightly flat, and nobody has ever had the heart to tell her."
        },
        new Spell
        {
            Name = "Cure Light Wounds", Level = 1, School = "Conjuration (healing)", Range = "Touch", Duration = "Instantaneous", Save = "Will half (harmless)",
            Description = "When laying your hand upon a living creature, you channel positive energy that cures 1d8 points of damage plus 1 point per caster level, to a maximum of +5. Because undead are powered by negative energy, this spell deals damage to them instead of curing their wounds; an undead creature can apply spell resistance and attempt a Will save to take half damage."
        },
        new Spell
        {
            Name = "Shield of Faith", Level = 1, School = "Abjuration", Range = "Touch", Duration = "1 min./level",
            Description = "This spell creates a shimmering, magical field around the touched creature that averts and deflects attacks. The spell grants the subject a +2 deflection bonus to AC, with an additional +1 to the bonus for every six levels you have."
        },
        new Spell
        {
            Name = "Aid", Level = 2, School = "Enchantment (compulsion) [mind-affecting]", Range = "Touch", Duration = "1 min./level",
            Description = "Aid grants the target a +1 morale bonus on attack rolls and saving throws against fear effects, plus temporary hit points equal to 1d8 + caster level, to a maximum of 1d8+10 at tenth level."
        },
        new Spell
        {
            Name = "Spiritual Weapon", Level = 2, School = "Evocation [force]", Range = "Medium", Duration = "1 round/level",
            Description = "A weapon made of force appears and attacks foes at a distance, as you direct it, dealing 1d8 force damage per hit, +1 point per three caster levels. The weapon takes the shape of a weapon favored by your deity - in Odile's case a shepherd's crook that glows the color of early morning - and strikes the opponent you designate, starting with one attack in the round the spell is cast and continuing each round thereafter on your turn."
        },
        new Spell
        {
            Name = "Prayer", Level = 3, School = "Enchantment (compulsion) [mind-affecting]", Range = "40 ft.", Duration = "1 round/level",
            Description = "You bring special favor upon yourself and your allies while bringing disfavor to your enemies. You and each of your allies gain a +1 luck bonus on attack rolls, weapon damage rolls, saves and skill checks, while each of your foes takes a -1 penalty on such rolls."
        },
        new Spell
        {
            Name = "Searing Light", Level = 3, School = "Evocation", Range = "Medium", Duration = "Instantaneous",
            Description = "Focusing divine power like a ray of the sun, you project a blast of light from your open palm. You must succeed on a ranged touch attack to strike your target. A creature struck by this ray of light takes 1d8 points of damage per two caster levels, to a maximum of 5d8. An undead creature takes 1d6 points of damage per caster level, and an undead creature particularly vulnerable to bright light takes 1d8 per caster level."
        }
    ];
}
