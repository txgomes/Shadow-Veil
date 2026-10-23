using UnityEngine;

namespace ShadowVeil.Summoner
{
    public enum Team { Player, Enemy }

    public enum UnitKind { Hero, Summon, Structure }

    /// <summary>One combatant on the lane: hero, summoned creature, pedestal or gem.</summary>
    public sealed class Unit : MonoBehaviour
    {
        public Team team;
        public UnitKind kind;
        public UnitRole role;
        public CardDefinition card;
        public string displayName;
        public float maxHp;
        public float hp;
        public float damage;
        public float attackRate = 1f;
        public float range = 1.7f;
        public float speed = 3.5f;
        public bool airborne;
        public bool hitsAir;
        public bool isBoss;
        public float radius = 0.5f;

        public float cooldown;
        public float attackTime;
        public Unit lastAttacker;
        public Animator animator;
        public UnitRig rig;
        public GameObject selectionRing;

        private float slowTimer;
        private float slowFactor = 1f;
        private Transform barRoot;
        private Transform barFill;
        private float barWidth;

        public bool Alive => hp > 0f;
        public bool IsStructure => kind == UnitKind.Structure;
        public float SpeedScale => slowTimer > 0f ? slowFactor : 1f;
        public Vector3 Position => transform.position;
        public Vector3 AimPoint => transform.position + Vector3.up * (airborne ? 2.4f : IsStructure ? 1.6f : 1f);

        public void Init(float health)
        {
            maxHp = hp = health;
        }

        public void TakeDamage(float amount, Unit source)
        {
            if (!Alive) return;
            hp = Mathf.Max(0f, hp - amount);
            lastAttacker = source;
            RefreshBar();
        }

        public void Heal(float amount)
        {
            if (!Alive) return;
            hp = Mathf.Min(maxHp, hp + amount);
            RefreshBar();
        }

        public void ApplySlow(float factor, float duration)
        {
            slowFactor = Mathf.Min(SpeedScale, factor);
            slowTimer = Mathf.Max(slowTimer, duration);
        }

        public void Tick(float dt)
        {
            if (slowTimer > 0f) slowTimer -= dt;
            attackTime = Mathf.Max(0f, attackTime - dt);
        }

        public bool CanHit(Unit target)
        {
            return target != null && target.Alive && (!target.airborne || hitsAir);
        }

        public void MoveTowards(Vector3 point, float dt, Vector3 separation)
        {
            Vector3 delta = point - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude < 0.0004f) return;
            Vector3 direction = (delta.normalized + separation);
            direction.y = 0f;
            direction.Normalize();
            transform.position += direction * speed * SpeedScale * dt;
            Face(direction, dt);
        }

        public void Face(Vector3 direction, float dt)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), 600f * dt);
        }

        public void BeginAttack()
        {
            attackTime = 0.36f;
            if (animator != null) animator.SetTrigger("Attack");
        }

        public void Animate(float dt, bool moving)
        {
            if (animator != null) animator.SetBool("Moving", moving);
            if (rig != null) rig.Animate(dt, moving, attackTime, SpeedScale);
        }

        public void BuildHealthBar(float width, float height, Color color)
        {
            barWidth = width;
            barRoot = new GameObject("Health bar").transform;
            barRoot.SetParent(transform, false);
            barRoot.localPosition = Vector3.up * height;
            Art.Primitive("Bar back", PrimitiveType.Cube, barRoot.position, new Vector3(width + 0.08f, 0.16f, 0.04f), Art.Flat(new Color(0.05f, 0.04f, 0.05f)), barRoot);
            barFill = Art.Primitive("Bar fill", PrimitiveType.Cube, barRoot.position + Vector3.back * 0.03f, new Vector3(width, 0.1f, 0.04f), Art.Glow(color, 0.6f), barRoot).transform;
            RefreshBar();
        }

        public void FaceBarTo(Camera view)
        {
            if (barRoot == null || view == null) return;
            barRoot.rotation = view.transform.rotation;
            barRoot.gameObject.SetActive(Alive && (hp < maxHp || IsStructure || isBoss));
        }

        private void RefreshBar()
        {
            if (barFill == null) return;
            float ratio = maxHp > 0f ? hp / maxHp : 0f;
            barFill.localScale = new Vector3(barWidth * ratio, barFill.localScale.y, barFill.localScale.z);
            barFill.localPosition = new Vector3(-barWidth * (1f - ratio) * 0.5f, 0f, -0.03f);
        }

        public void Die()
        {
            if (selectionRing != null) selectionRing.SetActive(false);
            Collider body = GetComponent<Collider>();
            if (body != null) body.enabled = false;
            if (barRoot != null) barRoot.gameObject.SetActive(false);
            if (animator != null)
            {
                animator.SetBool("Moving", false);
                animator.SetTrigger("Death");
                Destroy(gameObject, 1.6f);
            }
            else if (rig != null) rig.Collapse(gameObject);
            else Destroy(gameObject);
        }
    }
}
