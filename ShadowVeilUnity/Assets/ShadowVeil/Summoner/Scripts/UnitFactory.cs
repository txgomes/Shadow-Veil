using UnityEngine;

namespace ShadowVeil.Summoner
{
    /// <summary>Procedural limbs for units that have no imported model yet.</summary>
    public sealed class UnitRig
    {
        public Transform body;
        public Transform leftArm;
        public Transform rightArm;
        public Transform leftLeg;
        public Transform rightLeg;
        public Transform wings;
        public float scale = 1f;
        public float hover;
        private float phase = Random.value * 5f;

        public void Animate(float dt, bool moving, float attackTime, float speedScale)
        {
            phase += dt * (moving ? 11f : 3f) * speedScale;
            float stride = moving ? Mathf.Sin(phase) * 34f : 0f;
            float swing = attackTime > 0f ? Mathf.Sin((1f - attackTime / 0.36f) * Mathf.PI) : 0f;
            float breath = Mathf.Sin(phase) * 1.4f;
            if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(stride, 0f, 0f);
            if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(-stride, 0f, 0f);
            if (leftArm != null) leftArm.localRotation = Quaternion.Euler(-stride * 0.55f - breath, 0f, swing * 10f);
            if (rightArm != null) rightArm.localRotation = Quaternion.Euler(stride * 0.55f - swing * 112f - breath, 0f, -swing * 8f);
            if (body != null) body.localRotation = Quaternion.Euler(-swing * 9f, 0f, moving ? Mathf.Sin(phase) * 2f : 0f);
            if (body != null && hover > 0f) body.localPosition = Vector3.up * (hover + Mathf.Sin(phase * 0.7f) * 0.18f);
            if (wings != null) wings.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase * 2.4f) * 22f);
        }

        public void Collapse(GameObject root)
        {
            root.AddComponent<Sinker>();
        }
    }

    /// <summary>Topples and sinks a procedural corpse, then removes it.</summary>
    public sealed class Sinker : MonoBehaviour
    {
        private float age;

        private void Update()
        {
            age += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(80f, transform.eulerAngles.y, 0f), Time.deltaTime * 6f);
            if (age > 0.6f) transform.position += Vector3.down * Time.deltaTime * 1.4f;
            if (age > 1.6f) Destroy(gameObject);
        }
    }

    public static class UnitFactory
    {
        public static Unit CreateHero(Vector3 position)
        {
            Unit unit = CreateRoot("Rachel", Team.Player, UnitKind.Hero, position, 1f);
            unit.role = UnitRole.Ranged;
            unit.Init(2400f);
            unit.damage = 95f;
            unit.attackRate = 0.75f;
            unit.range = 9f;
            unit.speed = 6f;
            unit.hitsAir = true;
            if (!TryAttachModel(unit, "rachel", 1f))
                unit.rig = BuildHumanoid(unit.transform, new Color(0.55f, 0.18f, 0.16f), new Color(0.95f, 0.76f, 0.64f), 1f, HumanoidStyle.Ranger);
            unit.BuildHealthBar(1.4f, 2.7f, new Color(0.35f, 0.9f, 0.45f));
            return unit;
        }

        public static Unit CreateSummon(CardDefinition card, Team team, Vector3 position, float powerScale)
        {
            float scale = card.role == UnitRole.Tank ? 1.35f : card.role == UnitRole.King ? 1.3f : card.role == UnitRole.Flyer ? 0.85f : 0.9f;
            Unit unit = CreateRoot(card.title, team, UnitKind.Summon, position, scale);
            unit.card = card;
            unit.role = card.role;
            unit.Init(card.health * card.LevelScale * powerScale);
            unit.damage = card.damage * card.LevelScale * powerScale;
            unit.attackRate = card.attackRate;
            unit.range = card.range;
            unit.speed = card.speed;
            unit.airborne = card.airborne;
            unit.hitsAir = card.hitsAir;
            Color skin = card.tint;
            Color cloth = team == Team.Player ? new Color(0.20f, 0.30f, 0.42f) : new Color(0.38f, 0.14f, 0.16f);
            if (!TryAttachModel(unit, card.modelId, scale))
            {
                HumanoidStyle style = card.role == UnitRole.Flyer ? HumanoidStyle.Winged : card.role == UnitRole.Healer ? HumanoidStyle.Staff
                    : card.role == UnitRole.Ranged ? HumanoidStyle.Ranger : HumanoidStyle.Warrior;
                unit.rig = BuildHumanoid(unit.transform, cloth, skin, scale, style);
                if (card.airborne) unit.rig.hover = 1.4f;
                if (card.role == UnitRole.King)
                    Art.Primitive("Crown", PrimitiveType.Cylinder, position + Vector3.up * 2.45f * scale, new Vector3(0.36f, 0.08f, 0.36f) * scale, Art.Glow(new Color(0.95f, 0.75f, 0.25f), 1.5f), unit.rig.body);
            }
            Color bar = team == Team.Player ? new Color(0.35f, 0.75f, 0.95f) : new Color(0.95f, 0.25f, 0.22f);
            unit.BuildHealthBar(0.9f * scale, (card.airborne ? 3.9f : 2.5f) * scale, bar);
            if (team == Team.Enemy) AddSelectionRing(unit, scale);
            // Parts are placed facing +Z, so turn enemies around only after building them.
            if (team == Team.Enemy) unit.transform.rotation = Quaternion.LookRotation(Vector3.back);
            return unit;
        }

        public static Unit CreatePedestal(Team team, Vector3 position, float health)
        {
            Unit unit = CreateRoot(team == Team.Player ? "Summoner's Pedestal" : "Enemy Pedestal", team, UnitKind.Structure, position, 1f);
            unit.Init(health);
            unit.radius = 2f;
            unit.damage = team == Team.Enemy ? 70f : 50f;
            unit.attackRate = 1.4f;
            unit.range = 7.5f;
            unit.hitsAir = true;
            Color stone = new Color(0.22f, 0.21f, 0.24f);
            Color glow = team == Team.Player ? new Color(0.30f, 0.65f, 1f) : new Color(1f, 0.20f, 0.12f);
            Transform root = unit.transform;
            Art.Primitive("Plinth", PrimitiveType.Cylinder, position + Vector3.up * 0.3f, new Vector3(3.6f, 0.3f, 3.6f), Art.Flat(stone), root);
            Art.Primitive("Column", PrimitiveType.Cylinder, position + Vector3.up * 1.3f, new Vector3(1.4f, 1f, 1.4f), Art.Metal(stone * 1.3f, 0.3f), root);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 1.55f;
                Art.Primitive("Spike", PrimitiveType.Cube, position + offset + Vector3.up * 1.1f, new Vector3(0.28f, 2.2f, 0.28f), Art.Flat(stone * 0.8f), root).transform.rotation = Quaternion.Euler(0f, i * 90f + 45f, 0f);
            }
            GameObject orb = Art.Primitive("Soul orb", PrimitiveType.Sphere, position + Vector3.up * 2.9f, Vector3.one * 1.1f, Art.Glow(glow, 2.5f), root);
            orb.AddComponent<Bobber>();
            Light light = new GameObject("Pedestal light").AddComponent<Light>();
            light.transform.SetParent(root, false);
            light.transform.localPosition = Vector3.up * 3f;
            light.type = LightType.Point;
            light.color = glow;
            light.range = 9f;
            light.intensity = 2.5f;
            AddCollider(unit, 1.6f, 3.6f);
            unit.BuildHealthBar(3f, 4.4f, team == Team.Player ? new Color(0.35f, 0.75f, 0.95f) : new Color(0.95f, 0.25f, 0.22f));
            if (team == Team.Enemy) AddSelectionRing(unit, 2.4f);
            return unit;
        }

        public static Unit CreateManaGem(Vector3 position, float health)
        {
            Unit unit = CreateRoot("Blue Gem", Team.Enemy, UnitKind.Structure, position, 1f);
            unit.Init(health);
            unit.radius = 1f;
            unit.hitsAir = false;
            Art.Primitive("Gem base", PrimitiveType.Cylinder, position + Vector3.up * 0.15f, new Vector3(1.8f, 0.15f, 1.8f), Art.Flat(new Color(0.18f, 0.18f, 0.22f)), unit.transform);
            GameObject gem = Art.Primitive("Gem crystal", PrimitiveType.Cube, position + Vector3.up * 1.4f, new Vector3(0.9f, 1.5f, 0.9f), Art.Glow(new Color(0.25f, 0.55f, 1f), 2.2f), unit.transform);
            gem.transform.rotation = Quaternion.Euler(0f, 45f, 35f);
            gem.AddComponent<Bobber>().spin = 60f;
            Light light = new GameObject("Gem light").AddComponent<Light>();
            light.transform.SetParent(unit.transform, false);
            light.transform.localPosition = Vector3.up * 1.6f;
            light.type = LightType.Point;
            light.color = new Color(0.3f, 0.55f, 1f);
            light.range = 7f;
            light.intensity = 2.5f;
            AddCollider(unit, 1f, 2.6f);
            unit.BuildHealthBar(1.6f, 3f, new Color(0.35f, 0.6f, 1f));
            AddSelectionRing(unit, 1.6f);
            return unit;
        }

        private static Unit CreateRoot(string name, Team team, UnitKind kind, Vector3 position, float scale)
        {
            GameObject root = new GameObject(name);
            root.transform.position = position;
            Unit unit = root.AddComponent<Unit>();
            unit.team = team;
            unit.kind = kind;
            unit.displayName = name;
            unit.radius = 0.5f * scale;
            if (kind != UnitKind.Structure) AddCollider(unit, 0.55f * scale, 2.1f * scale);
            return unit;
        }

        private static void AddCollider(Unit unit, float radius, float height)
        {
            CapsuleCollider collider = unit.gameObject.AddComponent<CapsuleCollider>();
            collider.radius = radius;
            collider.height = height;
            collider.center = Vector3.up * height * 0.5f;
        }

        private static void AddSelectionRing(Unit unit, float scale)
        {
            unit.selectionRing = Art.Primitive("Target ring", PrimitiveType.Cylinder, unit.Position + Vector3.up * 0.04f, new Vector3(1.5f * scale, 0.01f, 1.5f * scale), Art.Glow(new Color(1f, 0.2f, 0.15f), 1.4f), unit.transform);
            unit.selectionRing.SetActive(false);
        }

        /// <summary>
        /// Uses an imported character when a prefab exists at Resources/Characters/&lt;modelId&gt;.
        /// The Animator is driven with a "Moving" bool and "Attack"/"Death" triggers.
        /// </summary>
        private static bool TryAttachModel(Unit unit, string modelId, float scale)
        {
            if (string.IsNullOrEmpty(modelId)) return false;
            GameObject prefab = Resources.Load<GameObject>("Characters/" + modelId);
            if (prefab == null) return false;
            GameObject visual = Object.Instantiate(prefab, unit.transform, false);
            visual.transform.localPosition = unit.airborne ? Vector3.up * 1.4f : Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one * scale;
            unit.animator = visual.GetComponentInChildren<Animator>();
            return true;
        }

        private enum HumanoidStyle { Warrior, Ranger, Staff, Winged }

        private static UnitRig BuildHumanoid(Transform root, Color cloth, Color skin, float scale, HumanoidStyle style)
        {
            Vector3 p = root.position;
            Material outfit = Art.Flat(cloth);
            Material skinMat = Art.Flat(skin);
            Material leather = Art.Flat(new Color(0.12f, 0.10f, 0.12f));
            Material armor = Art.Metal(new Color(0.31f, 0.34f, 0.39f), 0.7f);
            UnitRig rig = new UnitRig { scale = scale };
            rig.body = Art.Pivot("Body", p, root);
            Art.Primitive("Torso", PrimitiveType.Capsule, p + Vector3.up * 1.05f * scale, new Vector3(0.5f, 0.58f, 0.38f) * scale, outfit, rig.body);
            Art.Primitive("Chest", PrimitiveType.Cube, p + new Vector3(0f, 1.15f, 0.08f) * scale, new Vector3(0.4f, 0.42f, 0.12f) * scale, armor, rig.body);
            Art.Primitive("Head", PrimitiveType.Sphere, p + Vector3.up * 1.88f * scale, new Vector3(0.36f, 0.4f, 0.36f) * scale, skinMat, rig.body);
            if (style == HumanoidStyle.Ranger && cloth.r > 0.5f)
            {
                Material hair = Art.Flat(new Color(0.45f, 0.10f, 0.07f));
                Art.Primitive("Hair", PrimitiveType.Sphere, p + new Vector3(0f, 1.98f, -0.08f) * scale, new Vector3(0.42f, 0.42f, 0.4f) * scale, hair, rig.body);
                Art.Primitive("Hair tail", PrimitiveType.Capsule, p + new Vector3(0f, 1.45f, -0.2f) * scale, new Vector3(0.2f, 0.5f, 0.14f) * scale, hair, rig.body);
            }
            rig.leftArm = Art.Pivot("Left shoulder", p + new Vector3(-0.4f, 1.42f, 0f) * scale, rig.body);
            rig.rightArm = Art.Pivot("Right shoulder", p + new Vector3(0.4f, 1.42f, 0f) * scale, rig.body);
            Art.Primitive("Left arm", PrimitiveType.Capsule, p + new Vector3(-0.4f, 1.12f, 0f) * scale, new Vector3(0.17f, 0.43f, 0.17f) * scale, skinMat, rig.leftArm);
            Art.Primitive("Right arm", PrimitiveType.Capsule, p + new Vector3(0.4f, 1.12f, 0f) * scale, new Vector3(0.17f, 0.43f, 0.17f) * scale, skinMat, rig.rightArm);
            rig.leftLeg = Art.Pivot("Left hip", p + new Vector3(-0.16f, 0.72f, 0f) * scale, rig.body);
            rig.rightLeg = Art.Pivot("Right hip", p + new Vector3(0.16f, 0.72f, 0f) * scale, rig.body);
            Art.Primitive("Left leg", PrimitiveType.Capsule, p + new Vector3(-0.16f, 0.38f, 0f) * scale, new Vector3(0.19f, 0.4f, 0.2f) * scale, leather, rig.leftLeg);
            Art.Primitive("Right leg", PrimitiveType.Capsule, p + new Vector3(0.16f, 0.38f, 0f) * scale, new Vector3(0.19f, 0.4f, 0.2f) * scale, leather, rig.rightLeg);
            switch (style)
            {
                case HumanoidStyle.Warrior:
                    Art.Primitive("Blade", PrimitiveType.Cube, p + new Vector3(0.55f, 1.0f, 0.3f) * scale, new Vector3(0.1f, 0.1f, 0.9f) * scale, armor, rig.rightArm);
                    break;
                case HumanoidStyle.Ranger:
                    Art.Primitive("Bow", PrimitiveType.Cylinder, p + new Vector3(-0.5f, 1.12f, 0.15f) * scale, new Vector3(0.05f, 0.62f, 0.05f) * scale, Art.Flat(new Color(0.42f, 0.24f, 0.12f)), rig.leftArm);
                    break;
                case HumanoidStyle.Staff:
                    Art.Primitive("Staff", PrimitiveType.Cylinder, p + new Vector3(0.6f, 1.15f, 0.1f) * scale, new Vector3(0.06f, 0.9f, 0.06f) * scale, leather, rig.rightArm);
                    Art.Primitive("Staff crystal", PrimitiveType.Sphere, p + new Vector3(0.6f, 2.05f, 0.1f) * scale, Vector3.one * 0.22f * scale, Art.Glow(new Color(0.3f, 0.95f, 0.55f), 2.5f), rig.rightArm);
                    break;
                case HumanoidStyle.Winged:
                    rig.wings = Art.Pivot("Wings", p + new Vector3(0f, 1.4f, -0.2f) * scale, rig.body);
                    Material wing = Art.Flat(skin * 0.6f);
                    Art.Primitive("Left wing", PrimitiveType.Cube, p + new Vector3(-0.75f, 1.6f, -0.25f) * scale, new Vector3(1.1f, 0.6f, 0.05f) * scale, wing, rig.wings).transform.rotation = Quaternion.Euler(0f, 0f, 20f);
                    Art.Primitive("Right wing", PrimitiveType.Cube, p + new Vector3(0.75f, 1.6f, -0.25f) * scale, new Vector3(1.1f, 0.6f, 0.05f) * scale, wing, rig.wings).transform.rotation = Quaternion.Euler(0f, 0f, -20f);
                    break;
            }
            Art.Primitive("Shadow", PrimitiveType.Cylinder, p + Vector3.up * 0.02f, new Vector3(0.75f, 0.01f, 0.5f) * scale, Art.Flat(new Color(0.03f, 0.03f, 0.04f)), root);
            return rig;
        }
    }

    /// <summary>Gentle float and spin for orbs and gems.</summary>
    public sealed class Bobber : MonoBehaviour
    {
        public float spin = 25f;
        private Vector3 origin;
        private float phase;

        private void Start() => origin = transform.localPosition;

        private void Update()
        {
            phase += Time.deltaTime;
            transform.localPosition = origin + Vector3.up * Mathf.Sin(phase * 1.6f) * 0.15f;
            transform.Rotate(Vector3.up, spin * Time.deltaTime, Space.World);
        }
    }
}
