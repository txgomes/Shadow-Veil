using System.Collections.Generic;
using UnityEngine;

namespace ShadowVeil.Summoner
{
    public enum Rarity { Common, Rare, Epic, Legendary }

    public enum CardKind { Summon, Spell }

    public enum UnitRole { Melee, Ranged, Healer, Flyer, Tank, King }

    public enum SpellEffect { None, DarkWind, FrozenField, Mend }

    /// <summary>Static description of one card, modelled on the First Summoner card sheet.</summary>
    public sealed class CardDefinition
    {
        public string id;
        public string title;
        public string blurb;
        public Rarity rarity;
        public CardKind kind;
        public int mana;
        public float cooldown;
        public int level = 1;

        // Summon stats (per unit).
        public UnitRole role;
        public int unitCount = 1;
        public float health;
        public float damage;
        public float attackRate = 1f;
        public float range = 1.7f;
        public float speed = 3.5f;
        public bool airborne;
        public bool hitsAir;
        public string modelId;
        public Color tint = Color.gray;

        // Spell stats.
        public SpellEffect effect;
        public float radius;
        public float power;
        public float duration;

        /// <summary>Card level scales health and damage by 8% per level, like the "Level Up" sheet.</summary>
        public float LevelScale => 1f + (level - 1) * 0.08f;
    }

    public static class CardLibrary
    {
        public static readonly Color[] RarityColors =
        {
            new Color(0.62f, 0.60f, 0.56f),
            new Color(0.27f, 0.52f, 0.86f),
            new Color(0.62f, 0.33f, 0.86f),
            new Color(0.93f, 0.64f, 0.20f),
        };

        public static readonly List<CardDefinition> All = new List<CardDefinition>
        {
            new CardDefinition
            {
                id = "skeleton_archer", title = "Skeleton Archer", blurb = "Summons two basic Skeleton Archers.",
                rarity = Rarity.Common, kind = CardKind.Summon, mana = 5, cooldown = 25f, level = 4,
                role = UnitRole.Ranged, unitCount = 2, health = 238f, damage = 75f, attackRate = 1.3f, range = 8f, speed = 2.6f,
                hitsAir = true, modelId = "skeleton_archer", tint = new Color(0.76f, 0.72f, 0.63f),
            },
            new CardDefinition
            {
                id = "skeleton_warrior", title = "Skeleton Warrior", blurb = "Summons three sword-bearing skeletons.",
                rarity = Rarity.Common, kind = CardKind.Summon, mana = 4, cooldown = 18f, level = 3,
                role = UnitRole.Melee, unitCount = 3, health = 260f, damage = 55f, attackRate = 1.1f, range = 1.7f, speed = 3.2f,
                modelId = "skeleton_warrior", tint = new Color(0.80f, 0.76f, 0.66f),
            },
            new CardDefinition
            {
                id = "mana_ghost", title = "Mana Ghost", blurb = "A cheap flying spirit that harries the front line.",
                rarity = Rarity.Rare, kind = CardKind.Summon, mana = 2, cooldown = 8f, level = 3,
                role = UnitRole.Flyer, unitCount = 1, health = 150f, damage = 40f, attackRate = 0.8f, range = 1.8f, speed = 5.2f,
                airborne = true, hitsAir = true, modelId = "mana_ghost", tint = new Color(0.45f, 0.62f, 0.95f),
            },
            new CardDefinition
            {
                id = "troll_healer", title = "T-roll Healer", blurb = "Heals the weakest ally and the hero.",
                rarity = Rarity.Epic, kind = CardKind.Summon, mana = 5, cooldown = 30f, level = 2,
                role = UnitRole.Healer, unitCount = 1, health = 420f, damage = 60f, attackRate = 1.4f, range = 5f, speed = 3f,
                modelId = "troll_healer", tint = new Color(0.40f, 0.62f, 0.38f),
            },
            new CardDefinition
            {
                id = "ogre", title = "Ogre", blurb = "A slow wall of muscle that soaks damage.",
                rarity = Rarity.Rare, kind = CardKind.Summon, mana = 8, cooldown = 30f, level = 2,
                role = UnitRole.Tank, unitCount = 1, health = 1400f, damage = 95f, attackRate = 1.6f, range = 2f, speed = 2.2f,
                modelId = "ogre", tint = new Color(0.55f, 0.47f, 0.36f),
            },
            new CardDefinition
            {
                id = "succubus", title = "Succubus", blurb = "Winged demon that strikes air and ground.",
                rarity = Rarity.Epic, kind = CardKind.Summon, mana = 7, cooldown = 28f, level = 1,
                role = UnitRole.Flyer, unitCount = 1, health = 520f, damage = 130f, attackRate = 0.9f, range = 2f, speed = 4.4f,
                airborne = true, hitsAir = true, modelId = "succubus", tint = new Color(0.66f, 0.20f, 0.30f),
            },
            new CardDefinition
            {
                id = "orc_shaman", title = "Orc Shaman", blurb = "Hurls hexes from behind the line.",
                rarity = Rarity.Rare, kind = CardKind.Summon, mana = 6, cooldown = 22f, level = 2,
                role = UnitRole.Ranged, unitCount = 1, health = 360f, damage = 120f, attackRate = 1.5f, range = 7f, speed = 2.8f,
                hitsAir = true, modelId = "orc_shaman", tint = new Color(0.33f, 0.50f, 0.30f),
            },
            new CardDefinition
            {
                id = "skeleton_king", title = "Skeleton King", blurb = "Enemies he slays rise again as your skeletons.",
                rarity = Rarity.Legendary, kind = CardKind.Summon, mana = 20, cooldown = 60f, level = 1,
                role = UnitRole.King, unitCount = 1, health = 2600f, damage = 210f, attackRate = 1.2f, range = 2.2f, speed = 2.4f,
                modelId = "skeleton_king", tint = new Color(0.86f, 0.74f, 0.42f),
            },
            new CardDefinition
            {
                id = "dark_wind", title = "Dark Wind", blurb = "A gust of shadow that damages and pushes enemies back.",
                rarity = Rarity.Epic, kind = CardKind.Spell, mana = 2, cooldown = 12f, level = 2,
                effect = SpellEffect.DarkWind, radius = 3.5f, power = 160f, tint = new Color(0.45f, 0.25f, 0.60f),
            },
            new CardDefinition
            {
                id = "frozen_field", title = "Frozen Field", blurb = "Slows enemy movement and attacks in an area.",
                rarity = Rarity.Rare, kind = CardKind.Spell, mana = 4, cooldown = 20f, level = 1,
                effect = SpellEffect.FrozenField, radius = 4.5f, power = 0.45f, duration = 6f, tint = new Color(0.40f, 0.75f, 0.95f),
            },
            new CardDefinition
            {
                id = "mend", title = "Mending Light", blurb = "Restores health to the hero and nearby allies.",
                rarity = Rarity.Common, kind = CardKind.Spell, mana = 4, cooldown = 18f, level = 1,
                effect = SpellEffect.Mend, radius = 5f, power = 260f, tint = new Color(0.95f, 0.85f, 0.45f),
            },
        };

        /// <summary>The six cards the prototype starts with.</summary>
        public static readonly string[] StarterDeck = { "skeleton_archer", "mana_ghost", "troll_healer", "ogre", "dark_wind", "frozen_field" };

        public static CardDefinition Get(string id)
        {
            foreach (CardDefinition card in All) if (card.id == id) return card;
            return null;
        }
    }
}
