using System.Collections.Generic;
using UnityEngine;

namespace ShadowVeil.Summoner
{
    /// <summary>Homing projectiles and short-lived glow effects.</summary>
    public sealed class Fx
    {
        private sealed class Projectile
        {
            public GameObject root;
            public Unit source;
            public Unit target;
            public float damage;
            public float speed;
        }

        private sealed class Burst
        {
            public GameObject root;
            public Material material;
            public Color color;
            public Vector3 startScale;
            public Vector3 endScale;
            public float lifetime;
            public float age;
        }

        private readonly List<Projectile> projectiles = new List<Projectile>();
        private readonly List<Burst> bursts = new List<Burst>();

        /// <summary>Called when a projectile lands, so the battle can credit kills.</summary>
        public System.Action<Unit, Unit, float> onHit;

        public void Shoot(Unit source, Unit target, float damage, Color color)
        {
            Vector3 start = source.Position + Vector3.up * (source.IsStructure ? 3f : 1.4f) + source.transform.forward * 0.4f;
            GameObject bolt = Art.Primitive("Bolt", PrimitiveType.Sphere, start, Vector3.one * 0.22f, Art.Glow(color, 2.2f));
            Art.Primitive("Bolt tail", PrimitiveType.Capsule, start, new Vector3(0.1f, 0.3f, 0.1f), Art.Glow(color, 1.2f), bolt.transform).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            projectiles.Add(new Projectile { root = bolt, source = source, target = target, damage = damage, speed = 18f });
        }

        public void Flash(Vector3 position, Color color, float startSize, Vector3 endScale, float lifetime)
        {
            Material material = Art.UniqueGlow(color, 2f);
            GameObject obj = Art.Primitive("Burst", PrimitiveType.Sphere, position, Vector3.one * startSize, material);
            bursts.Add(new Burst { root = obj, material = material, color = color, startScale = Vector3.one * startSize, endScale = endScale, lifetime = lifetime });
        }

        public void Ring(Vector3 position, Color color, float radius, float lifetime)
        {
            Material material = Art.UniqueGlow(color, 1.4f);
            GameObject obj = Art.Primitive("Ring", PrimitiveType.Cylinder, position + Vector3.up * 0.05f, new Vector3(radius * 2f, 0.02f, radius * 2f), material);
            bursts.Add(new Burst { root = obj, material = material, color = color, startScale = obj.transform.localScale, endScale = obj.transform.localScale, lifetime = lifetime });
        }

        public void Update(float dt)
        {
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                Projectile p = projectiles[i];
                if (p.target == null || !p.target.Alive)
                {
                    Object.Destroy(p.root);
                    projectiles.RemoveAt(i);
                    continue;
                }
                Vector3 aim = p.target.AimPoint;
                Vector3 delta = aim - p.root.transform.position;
                if (delta.magnitude <= p.speed * dt + 0.15f)
                {
                    onHit?.Invoke(p.source, p.target, p.damage);
                    Flash(aim, new Color(1f, 0.6f, 0.25f), 0.15f, Vector3.one * 0.5f, 0.18f);
                    Object.Destroy(p.root);
                    projectiles.RemoveAt(i);
                    continue;
                }
                p.root.transform.position += delta.normalized * p.speed * dt;
                p.root.transform.rotation = Quaternion.LookRotation(delta.normalized);
            }

            for (int i = bursts.Count - 1; i >= 0; i--)
            {
                Burst b = bursts[i];
                b.age += dt;
                float t = Mathf.Clamp01(b.age / b.lifetime);
                b.root.transform.localScale = Vector3.Lerp(b.startScale, b.endScale, t);
                if (b.material.HasProperty("_EmissionColor")) b.material.SetColor("_EmissionColor", b.color * 2f * (1f - t));
                if (t >= 1f)
                {
                    Object.Destroy(b.root);
                    Object.Destroy(b.material);
                    bursts.RemoveAt(i);
                }
            }
        }
    }
}
