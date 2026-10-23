using UnityEngine;

namespace ShadowVeil.Summoner
{
    /// <summary>The single dungeon lane: your pedestal at the bottom, the blue gem halfway, the enemy pedestal at the top.</summary>
    public static class Arena
    {
        public const float HalfWidth = 4.6f;
        public const float PlayerPedestalZ = 0f;
        public const float GemZ = 46f;
        public const float EnemyPedestalZ = 92f;
        public const float LaneStart = -8f;
        public const float LaneEnd = 100f;

        public static Vector3 Clamp(Vector3 point)
        {
            return new Vector3(Mathf.Clamp(point.x, -HalfWidth, HalfWidth), 0f, Mathf.Clamp(point.z, LaneStart + 2f, LaneEnd - 2f));
        }

        public static void Build()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.20f, 0.17f, 0.20f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.06f, 0.035f, 0.035f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 30f;
            RenderSettings.fogEndDistance = 70f;

            Light moon = new GameObject("Key light").AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.color = new Color(0.85f, 0.70f, 0.62f);
            moon.intensity = 1.1f;
            moon.shadows = LightShadows.Soft;
            moon.transform.rotation = Quaternion.Euler(50f, -25f, 0f);

            float length = LaneEnd - LaneStart;
            float mid = (LaneEnd + LaneStart) * 0.5f;
            Material floor = new Material(Art.Flat(new Color(0.42f, 0.38f, 0.36f)));
            floor.mainTexture = Art.NoiseTexture("Dungeon flagstones", new Color(0.62f, 0.56f, 0.52f), 0.5f);
            floor.mainTextureScale = new Vector2(2f, length / 5f);
            Art.Own(floor);
            Art.Primitive("Lane floor", PrimitiveType.Cube, new Vector3(0f, -0.25f, mid), new Vector3(HalfWidth * 2f + 1.2f, 0.5f, length), floor);

            Material lava = Art.Glow(new Color(1f, 0.32f, 0.06f), 2.4f);
            Material rock = Art.Flat(new Color(0.10f, 0.08f, 0.09f));
            Material wall = Art.Flat(new Color(0.18f, 0.15f, 0.16f));
            foreach (float side in new[] { -1f, 1f })
            {
                Art.Primitive("Lava river", PrimitiveType.Cube, new Vector3(side * (HalfWidth + 2.2f), -0.55f, mid), new Vector3(3f, 0.2f, length), lava);
                Art.Primitive("Outer rock", PrimitiveType.Cube, new Vector3(side * (HalfWidth + 9f), 0.4f, mid), new Vector3(10f, 2f, length), rock);
                Art.Primitive("Lane curb", PrimitiveType.Cube, new Vector3(side * (HalfWidth + 0.7f), 0.05f, mid), new Vector3(0.35f, 0.4f, length), wall);
                for (float z = LaneStart + 4f; z < LaneEnd; z += 9f)
                {
                    float x = side * (HalfWidth + 4.4f);
                    Art.Primitive("Pillar", PrimitiveType.Cube, new Vector3(x, 1.6f, z), new Vector3(1.1f, 3.6f, 1.1f), wall);
                    Art.Primitive("Brazier", PrimitiveType.Cylinder, new Vector3(x, 3.6f, z), new Vector3(0.9f, 0.2f, 0.9f), Art.Metal(new Color(0.3f, 0.25f, 0.2f), 0.6f));
                    Art.Primitive("Brazier fire", PrimitiveType.Sphere, new Vector3(x, 3.95f, z), new Vector3(0.6f, 0.75f, 0.6f), lava);
                    if (Mathf.RoundToInt(z) % 18 < 9)
                    {
                        Light fire = new GameObject("Brazier light").AddComponent<Light>();
                        fire.type = LightType.Point;
                        fire.transform.position = new Vector3(x * 0.8f, 4f, z);
                        fire.color = new Color(1f, 0.45f, 0.18f);
                        fire.range = 11f;
                        fire.intensity = 2f;
                    }
                }
            }

            // Scattered bones and rubble break up the floor the way the original dungeon maps did.
            Material bone = Art.Flat(new Color(0.75f, 0.70f, 0.60f));
            Random.State saved = Random.state;
            Random.InitState(1907);
            for (int i = 0; i < 60; i++)
            {
                Vector3 at = new Vector3(Random.Range(-HalfWidth, HalfWidth), 0.02f, Random.Range(LaneStart, LaneEnd));
                bool isBone = Random.value < 0.4f;
                GameObject bit = Art.Primitive(isBone ? "Bone" : "Rubble", isBone ? PrimitiveType.Capsule : PrimitiveType.Cube, at,
                    isBone ? new Vector3(0.08f, 0.25f, 0.08f) : Vector3.one * Random.Range(0.15f, 0.4f), isBone ? bone : rock);
                bit.transform.rotation = Quaternion.Euler(isBone ? 90f : Random.Range(0f, 40f), Random.Range(0f, 360f), 0f);
            }
            Random.state = saved;
        }
    }
}
