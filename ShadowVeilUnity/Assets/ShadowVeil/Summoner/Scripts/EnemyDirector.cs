using UnityEngine;

namespace ShadowVeil.Summoner
{
    /// <summary>
    /// Plays the enemy side like a defence deck: it banks mana over time and spends it on waves
    /// that march down the lane from the enemy pedestal, with guards posted at the gem.
    /// </summary>
    public sealed class EnemyDirector
    {
        private static readonly string[] Pool = { "skeleton_warrior", "skeleton_warrior", "skeleton_archer", "mana_ghost", "orc_shaman", "ogre" };

        private readonly BattleController battle;
        private float mana = 6f;
        private float nextWave = 6f;
        private string queued;
        private bool bossSpawned;

        public EnemyDirector(BattleController battle)
        {
            this.battle = battle;
        }

        public void PostGuards()
        {
            CardDefinition warrior = CardLibrary.Get("skeleton_warrior");
            CardDefinition archer = CardLibrary.Get("skeleton_archer");
            battle.Summon(warrior, Team.Enemy, new Vector3(0f, 0f, Arena.GemZ - 3f), 0.8f, true);
            battle.Summon(archer, Team.Enemy, new Vector3(0f, 0f, Arena.GemZ + 3f), 0.8f, true);
            battle.Summon(warrior, Team.Enemy, new Vector3(0f, 0f, Arena.EnemyPedestalZ - 6f), 0.9f, true);
            battle.Summon(CardLibrary.Get("orc_shaman"), Team.Enemy, new Vector3(2f, 0f, Arena.EnemyPedestalZ - 4f), 0.9f, true);
        }

        public void Update(float dt, float elapsed)
        {
            // Income ramps up so a slow push gets punished, like the clock pressure in the original.
            mana += (0.9f + elapsed / 150f) * dt;
            nextWave -= dt;

            Unit pedestal = battle.EnemyPedestal;
            if (!bossSpawned && pedestal != null && pedestal.hp < pedestal.maxHp * 0.5f)
            {
                bossSpawned = true;
                Unit boss = battle.Summon(CardLibrary.Get("ogre"), Team.Enemy, new Vector3(0f, 0f, Arena.EnemyPedestalZ - 5f), 3.2f, false)[0];
                boss.isBoss = true;
                boss.displayName = "Pestilence Warden";
                boss.transform.localScale = Vector3.one * 1.45f;
                battle.Toast("The Pestilence Warden awakens!");
            }

            if (nextWave > 0f) return;
            if (queued == null) queued = Pool[Random.Range(0, Pool.Length)];
            CardDefinition card = CardLibrary.Get(queued);
            if (mana < card.mana) return;
            mana -= card.mana;
            queued = null;
            float power = 0.75f + elapsed / 400f;
            Vector3 at = new Vector3(Random.Range(-2.5f, 2.5f), 0f, Arena.EnemyPedestalZ - 4f);
            battle.Summon(card, Team.Enemy, at, power, false);
            nextWave = Random.Range(3.5f, 6.5f);
        }
    }
}
