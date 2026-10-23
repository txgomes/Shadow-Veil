using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace ShadowVeil.Summoner
{
    /// <summary>
    /// One First Summoner style stage: drag Rachel up the lane, spend mana on a six-card deck,
    /// break the blue gem for +20 mana and destroy the enemy pedestal before the clock runs out.
    /// </summary>
    public sealed class BattleController : MonoBehaviour
    {
        public const float StageTime = 300f;
        public const float GemBonus = 20f;
        private const float ManaRegen = 0.9f;
        private const float HeroReviveTime = 8f;
        private const int MaxSummons = 12;
        private const float SummonLeash = 7f;

        private static readonly string[] Objectives =
        {
            "Destroy the enemy pedestal",
            "Keep your pedestal above 50% health",
            "Win within 3:00",
        };

        private readonly List<CardDefinition> deck = new List<CardDefinition>();
        private readonly List<Unit> units = new List<Unit>();
        private readonly HashSet<Unit> guards = new HashSet<Unit>();
        private readonly Dictionary<Collider, Unit> byCollider = new Dictionary<Collider, Unit>();
        private float[] cooldowns;

        private Camera view;
        private BattleHud hud;
        private EnemyDirector director;
        private Fx fx;
        private Unit focus;
        private bool dragging;
        private bool hasDestination;
        private Vector3 destination;
        private bool started;

        public IReadOnlyList<CardDefinition> Deck => deck;
        public Unit Hero { get; private set; }
        public Unit PlayerPedestal { get; private set; }
        public Unit EnemyPedestal { get; private set; }
        public Unit Gem { get; private set; }
        public float Mana { get; private set; } = 10f;
        public float MaxMana { get; } = 30f;
        public float TimeLeft { get; private set; } = StageTime;
        public float Elapsed => StageTime - TimeLeft;
        public float HeroReviveIn { get; private set; }
        public bool Ended { get; private set; }

        private void Start()
        {
            Application.targetFrameRate = 60;
            foreach (string id in CardLibrary.StarterDeck) deck.Add(CardLibrary.Get(id));
            cooldowns = new float[deck.Count];

            Arena.Build();
            fx = new Fx { onHit = (source, target, damage) => Damage(source, target, damage) };
            PlayerPedestal = Register(UnitFactory.CreatePedestal(Team.Player, new Vector3(0f, 0f, Arena.PlayerPedestalZ), 6000f));
            EnemyPedestal = Register(UnitFactory.CreatePedestal(Team.Enemy, new Vector3(0f, 0f, Arena.EnemyPedestalZ), 9000f));
            Gem = Register(UnitFactory.CreateManaGem(new Vector3(0f, 0f, Arena.GemZ), 1800f));
            Hero = Register(UnitFactory.CreateHero(new Vector3(0f, 0f, Arena.PlayerPedestalZ + 4f)));

            BuildCamera();
            director = new EnemyDirector(this);
            director.PostGuards();
            hud = new BattleHud(this);
            hud.ShowIntro("Stage 1 · Pestilence Castle", Objectives, () => started = true);
        }

        private void BuildCamera()
        {
            GameObject obj = new GameObject("Battle camera");
            obj.tag = "MainCamera";
            view = obj.AddComponent<Camera>();
            view.orthographic = true;
            view.orthographicSize = 11f;
            view.backgroundColor = new Color(0.04f, 0.02f, 0.02f);
            view.clearFlags = CameraClearFlags.SolidColor;
            obj.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            obj.transform.position = CameraTarget();
        }

        private Vector3 CameraTarget()
        {
            // Keep Rachel in the lower third so more of the lane ahead is visible, as in the original.
            return Hero.Position + new Vector3(-Hero.Position.x * 0.6f, 16f, -8f);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            fx.Update(dt);
            foreach (Unit unit in units) if (unit != null) unit.FaceBarTo(view);
            view.transform.position = Vector3.Lerp(view.transform.position, CameraTarget(), 1f - Mathf.Exp(-4f * dt));
            hud.Refresh(dt);
            if (!started || Ended) return;

            TimeLeft -= dt;
            Mana = Mathf.Min(MaxMana, Mana + ManaRegen * dt);
            for (int i = 0; i < cooldowns.Length; i++) cooldowns[i] = Mathf.Max(0f, cooldowns[i] - dt);

            ReadInput();
            UpdateHero(dt);
            for (int i = units.Count - 1; i >= 0; i--)
            {
                Unit unit = units[i];
                if (unit == null || !unit.Alive || unit.kind == UnitKind.Hero) continue;
                unit.Tick(dt);
                if (unit.IsStructure) UpdateStructure(unit, dt);
                else if (unit.role == UnitRole.Healer && unit.team == Team.Player) UpdateHealer(unit, dt);
                else UpdateFighter(unit, dt);
            }
            director.Update(dt, Elapsed);
            if (TimeLeft <= 0f) Finish(false);
        }

        // ---------- Input and hero ----------

        private void ReadInput()
        {
            bool down, held, up;
            Vector2 point;
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                point = touch.position;
                down = touch.phase == TouchPhase.Began;
                held = touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary;
                up = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
                if (down && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return;
            }
            else
            {
                point = Input.mousePosition;
                down = Input.GetMouseButtonDown(0);
                held = Input.GetMouseButton(0);
                up = Input.GetMouseButtonUp(0);
                if (down && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            }

            if (down)
            {
                Unit tapped = UnitAt(point);
                if (tapped != null && tapped.team == Team.Enemy) { SetFocus(tapped); return; }
                dragging = true;
            }
            if (dragging && (down || held)) SetDestination(point);
            if (up) dragging = false;
        }

        private Unit UnitAt(Vector2 screen)
        {
            Ray ray = view.ScreenPointToRay(screen);
            Unit best = null;
            float nearest = float.MaxValue;
            foreach (RaycastHit hit in Physics.RaycastAll(ray, 200f))
            {
                if (hit.distance < nearest && byCollider.TryGetValue(hit.collider, out Unit unit) && unit != null && unit.Alive)
                {
                    best = unit;
                    nearest = hit.distance;
                }
            }
            return best;
        }

        private void SetFocus(Unit target)
        {
            if (focus != null && focus.selectionRing != null) focus.selectionRing.SetActive(false);
            focus = target;
            if (focus != null && focus.selectionRing != null) focus.selectionRing.SetActive(true);
        }

        private void SetDestination(Vector2 screen)
        {
            Ray ray = view.ScreenPointToRay(screen);
            if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float distance))
            {
                destination = Arena.Clamp(ray.GetPoint(distance));
                hasDestination = true;
            }
        }

        private void UpdateHero(float dt)
        {
            Unit hero = Hero;
            if (!hero.Alive)
            {
                HeroReviveIn -= dt;
                if (HeroReviveIn <= 0f) ReviveHero();
                return;
            }
            hero.Tick(dt);
            bool moving = false;
            if (hasDestination)
            {
                Vector3 delta = destination - hero.Position;
                delta.y = 0f;
                if (delta.magnitude < 0.2f) hasDestination = false;
                else
                {
                    hero.MoveTowards(destination, dt, Vector3.zero);
                    hero.transform.position = Arena.Clamp(hero.Position);
                    moving = true;
                }
            }

            if (focus != null && !focus.Alive) SetFocus(null);
            Unit target = focus != null && InRange(hero, focus) ? focus : Nearest(hero, Team.Enemy, hero.range, true);
            hero.cooldown -= dt;
            if (target != null)
            {
                if (!moving) hero.Face(target.Position - hero.Position, dt);
                if (hero.cooldown <= 0f)
                {
                    hero.cooldown = hero.attackRate;
                    hero.BeginAttack();
                    fx.Shoot(hero, target, hero.damage, new Color(1f, 0.55f, 0.2f));
                }
            }
            hero.Animate(dt, moving);
        }

        private void ReviveHero()
        {
            Hero.transform.position = new Vector3(0f, 0f, Arena.PlayerPedestalZ + 4f);
            Hero.transform.rotation = Quaternion.identity;
            Hero.gameObject.SetActive(true);
            Hero.hp = 1f;
            Hero.Heal(Hero.maxHp);
            hasDestination = false;
            fx.Flash(Hero.Position + Vector3.up, new Color(1f, 0.85f, 0.4f), 0.3f, Vector3.one * 3f, 0.6f);
            Toast("Rachel returns!");
        }

        // ---------- Unit behaviour ----------

        private void UpdateFighter(Unit unit, float dt)
        {
            bool guard = guards.Contains(unit);
            float aggro = guard ? 8f : 11f;
            Unit target = Nearest(unit, Opponent(unit.team), aggro, false);
            if (target != null)
            {
                Engage(unit, target, dt);
                return;
            }
            if (guard) { unit.Animate(dt, false); return; }

            Vector3 goal;
            if (unit.team == Team.Enemy) goal = new Vector3(unit.Position.x * 0.9f, 0f, Arena.PlayerPedestalZ);
            else
            {
                // Player summons escort Rachel: they push ahead of her but do not run off alone.
                float leash = Hero.Alive ? Hero.Position.z + SummonLeash : unit.Position.z;
                goal = new Vector3(unit.Position.x * 0.9f, 0f, Mathf.Min(leash, Arena.EnemyPedestalZ));
            }
            bool moving = Mathf.Abs(goal.z - unit.Position.z) > 0.6f;
            if (moving) unit.MoveTowards(goal, dt, Separation(unit));
            unit.Animate(dt, moving);
        }

        private void UpdateHealer(Unit healer, float dt)
        {
            Unit patient = null;
            float lowest = 0.98f;
            foreach (Unit unit in units)
            {
                if (unit == null || !unit.Alive || unit.team != Team.Player || unit.IsStructure || unit == healer) continue;
                float ratio = unit.hp / unit.maxHp;
                if (ratio < lowest && Vector3.Distance(unit.Position, healer.Position) < 12f) { lowest = ratio; patient = unit; }
            }
            if (patient == null) { UpdateFighter(healer, dt); return; }
            if (!InRange(healer, patient))
            {
                healer.MoveTowards(patient.Position, dt, Separation(healer));
                healer.Animate(dt, true);
                return;
            }
            healer.Face(patient.Position - healer.Position, dt);
            healer.cooldown -= dt;
            if (healer.cooldown <= 0f)
            {
                healer.cooldown = healer.attackRate;
                healer.BeginAttack();
                patient.Heal(healer.damage * 2.2f);
                fx.Flash(patient.Position + Vector3.up * 1.4f, new Color(0.3f, 0.95f, 0.55f), 0.2f, new Vector3(0.8f, 1.4f, 0.8f), 0.5f);
            }
            healer.Animate(dt, false);
        }

        private void UpdateStructure(Unit structure, float dt)
        {
            if (structure.damage <= 0f || structure == Gem) return;
            structure.cooldown -= dt;
            if (structure.cooldown > 0f) return;
            Unit target = Nearest(structure, Opponent(structure.team), structure.range, true);
            if (target == null) return;
            structure.cooldown = structure.attackRate;
            fx.Shoot(structure, target, structure.damage, structure.team == Team.Player ? new Color(0.4f, 0.7f, 1f) : new Color(1f, 0.25f, 0.15f));
        }

        private void Engage(Unit unit, Unit target, float dt)
        {
            if (!InRange(unit, target))
            {
                unit.MoveTowards(target.Position, dt, Separation(unit));
                unit.Animate(dt, true);
                return;
            }
            unit.Face(target.Position - unit.Position, dt);
            unit.cooldown -= dt * unit.SpeedScale;
            if (unit.cooldown <= 0f)
            {
                unit.cooldown = unit.attackRate;
                unit.BeginAttack();
                if (unit.range > 3f) fx.Shoot(unit, target, unit.damage, unit.team == Team.Player ? new Color(0.6f, 0.85f, 1f) : new Color(0.8f, 0.4f, 1f));
                else
                {
                    Damage(unit, target, unit.damage);
                    fx.Flash(target.AimPoint, new Color(1f, 0.45f, 0.2f), 0.15f, Vector3.one * 0.6f, 0.2f);
                }
            }
            unit.Animate(dt, false);
        }

        private bool InRange(Unit unit, Unit target)
        {
            Vector3 delta = target.Position - unit.Position;
            delta.y = 0f;
            return delta.magnitude <= unit.range + target.radius + unit.radius;
        }

        /// <summary>Nearest hittable opponent; units are preferred over structures so troops clear the path first.</summary>
        private Unit Nearest(Unit from, Team team, float within, bool withinRangeOnly)
        {
            Unit best = null;
            float bestScore = float.MaxValue;
            foreach (Unit unit in units)
            {
                if (unit == null || unit.team != team || !unit.Alive || !from.CanHit(unit)) continue;
                if (unit == Gem && from.team == Team.Enemy) continue;
                float distance = Vector3.Distance(from.Position, unit.Position) - unit.radius;
                float limit = withinRangeOnly ? from.range + from.radius : within;
                if (distance > limit) continue;
                float score = distance + (unit.IsStructure ? 6f : 0f);
                if (score < bestScore) { bestScore = score; best = unit; }
            }
            return best;
        }

        private Vector3 Separation(Unit unit)
        {
            Vector3 push = Vector3.zero;
            foreach (Unit other in units)
            {
                if (other == null || other == unit || other.team != unit.team || !other.Alive || other.airborne != unit.airborne) continue;
                Vector3 away = unit.Position - other.Position;
                away.y = 0f;
                float spacing = unit.radius + other.radius + 0.6f;
                float distance = away.magnitude;
                if (distance > 0.01f && distance < spacing) push += away.normalized * (spacing - distance) / spacing;
            }
            return push * 1.3f;
        }

        private static Team Opponent(Team team) => team == Team.Player ? Team.Enemy : Team.Player;

        // ---------- Damage, death and rewards ----------

        private void Damage(Unit source, Unit target, float amount)
        {
            if (target == null || !target.Alive || Ended) return;
            target.TakeDamage(amount, source);
            if (!target.Alive) OnDeath(target, source);
        }

        private void OnDeath(Unit unit, Unit killer)
        {
            if (unit == focus) SetFocus(null);
            if (unit == Hero)
            {
                HeroReviveIn = HeroReviveTime;
                fx.Flash(Hero.AimPoint, new Color(1f, 0.3f, 0.2f), 0.3f, Vector3.one * 2.5f, 0.5f);
                Hero.gameObject.SetActive(false);
                Toast("Rachel has fallen!");
                return;
            }
            units.Remove(unit);
            guards.Remove(unit);
            unit.Die();

            if (unit == Gem)
            {
                Mana = Mathf.Min(MaxMana, Mana + GemBonus);
                fx.Flash(unit.AimPoint, new Color(0.3f, 0.6f, 1f), 0.5f, Vector3.one * 6f, 0.8f);
                Toast("Blue Gem shattered  +" + GemBonus + " mana");
            }
            else if (unit == EnemyPedestal) Finish(true);
            else if (unit == PlayerPedestal) Finish(false);
            else if (unit.team == Team.Enemy)
            {
                Mana = Mathf.Min(MaxMana, Mana + 1f);
                if (killer != null && killer.role == UnitRole.King && killer.team == Team.Player)
                {
                    // The Skeleton King raises what he slays.
                    Summon(CardLibrary.Get("skeleton_warrior"), Team.Player, unit.Position, 0.8f, false, 1);
                }
                if (unit.isBoss) Toast("The Warden is slain!");
            }
        }

        private Unit Register(Unit unit)
        {
            units.Add(unit);
            Collider body = unit.GetComponent<Collider>();
            if (body != null) byCollider[body] = unit;
            return unit;
        }

        public List<Unit> Summon(CardDefinition card, Team team, Vector3 at, float power, bool guard, int count = -1)
        {
            List<Unit> made = new List<Unit>();
            int total = count > 0 ? count : card.unitCount;
            for (int i = 0; i < total; i++)
            {
                Vector3 offset = new Vector3((i - (total - 1) * 0.5f) * 1.3f, 0f, (i % 2) * 0.6f);
                Unit unit = Register(UnitFactory.CreateSummon(card, team, Arena.Clamp(at + offset), power));
                if (guard) guards.Add(unit);
                fx.Flash(unit.Position + Vector3.up * 0.2f, team == Team.Player ? new Color(0.5f, 0.3f, 1f) : new Color(1f, 0.2f, 0.2f), 0.3f, new Vector3(2f, 0.3f, 2f), 0.4f);
                made.Add(unit);
            }
            return made;
        }

        // ---------- Cards ----------

        public float CooldownLeft(int index) => cooldowns != null ? cooldowns[index] : 0f;

        public void PlayCard(int index)
        {
            if (!started || Ended) return;
            CardDefinition card = deck[index];
            if (cooldowns[index] > 0f || Mana < card.mana) return;

            if (card.kind == CardKind.Summon)
            {
                int alive = 0;
                foreach (Unit unit in units) if (unit != null && unit.team == Team.Player && unit.kind == UnitKind.Summon) alive++;
                if (alive + card.unitCount > MaxSummons) { Toast("Too many summons"); return; }
                Vector3 anchor = Hero.Alive ? Hero.Position : PlayerPedestal.Position + Vector3.forward * 4f;
                Summon(card, Team.Player, anchor + new Vector3(0f, 0f, 1.6f), 1f, false);
            }
            else CastSpell(card);

            Mana -= card.mana;
            cooldowns[index] = card.cooldown;
        }

        private void CastSpell(CardDefinition card)
        {
            Vector3 heroPos = Hero.Alive ? Hero.Position : PlayerPedestal.Position;
            Unit aim = focus != null && focus.Alive ? focus : Nearest(Hero, Team.Enemy, 16f, false);
            Vector3 center = card.effect == SpellEffect.Mend ? heroPos : aim != null ? aim.Position : heroPos + Vector3.forward * 6f;
            float power = card.power * card.LevelScale;

            switch (card.effect)
            {
                case SpellEffect.DarkWind:
                    fx.Flash(center + Vector3.up * 0.5f, card.tint, 0.5f, new Vector3(card.radius * 2f, 1.2f, card.radius * 2f), 0.45f);
                    foreach (Unit unit in Targets(Team.Enemy, center, card.radius))
                    {
                        Damage(Hero, unit, power);
                        if (unit.Alive && !unit.IsStructure && !unit.isBoss) unit.transform.position = Arena.Clamp(unit.Position + Vector3.forward * 3f);
                    }
                    break;
                case SpellEffect.FrozenField:
                    fx.Ring(center, card.tint, card.radius, card.duration);
                    foreach (Unit unit in Targets(Team.Enemy, center, card.radius))
                        if (!unit.IsStructure) unit.ApplySlow(card.power, card.duration);
                    break;
                case SpellEffect.Mend:
                    fx.Ring(center, card.tint, card.radius, 0.8f);
                    foreach (Unit unit in Targets(Team.Player, center, card.radius))
                        if (!unit.IsStructure) unit.Heal(power);
                    break;
            }
        }

        private List<Unit> Targets(Team team, Vector3 center, float radius)
        {
            List<Unit> hits = new List<Unit>();
            foreach (Unit unit in units)
                if (unit != null && unit.Alive && unit.team == team && Vector3.Distance(unit.Position, center) <= radius + unit.radius) hits.Add(unit);
            return hits;
        }

        // ---------- Objectives and result ----------

        public bool ObjectiveOnTrack(int index)
        {
            switch (index)
            {
                case 0: return EnemyPedestal != null && (!EnemyPedestal.Alive || PlayerPedestal.Alive);
                case 1: return PlayerPedestal != null && PlayerPedestal.hp >= PlayerPedestal.maxHp * 0.5f;
                default: return Elapsed <= 180f;
            }
        }

        public void Toast(string message) => hud?.Toast(message);

        private void Finish(bool won)
        {
            if (Ended) return;
            Ended = true;
            bool[] stars = { won, won && ObjectiveOnTrack(1), won && ObjectiveOnTrack(2) };
            int earned = 0;
            foreach (bool star in stars) if (star) earned++;
            int gold = 300 + earned * 150;
            hud.ShowResult(won, stars, Objectives, gold, () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
        }

        private void OnDestroy()
        {
            Art.ReleaseAll();
            Ui.ReleaseCache();
        }
    }
}
