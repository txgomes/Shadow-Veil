using System.Collections.Generic;
using UnityEngine;

namespace ShadowVeil.Summoner
{
    /// <summary>Runtime materials and primitives. Everything made here is destroyed with the battle.</summary>
    public static class Art
    {
        private static readonly List<Object> owned = new List<Object>();
        private static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        public static Material Flat(Color color)
        {
            string key = "flat" + color;
            if (cache.TryGetValue(key, out Material cached) && cached != null) return cached;
            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            Material material = new Material(shader) { color = color };
            owned.Add(material);
            cache[key] = material;
            return material;
        }

        public static Material Metal(Color color, float metallic)
        {
            string key = "metal" + color + metallic;
            if (cache.TryGetValue(key, out Material cached) && cached != null) return cached;
            Material material = new Material(Flat(color));
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.6f);
            owned.Add(material);
            cache[key] = material;
            return material;
        }

        public static Material Glow(Color color, float intensity)
        {
            string key = "glow" + color + intensity;
            if (cache.TryGetValue(key, out Material cached) && cached != null) return cached;
            Material material = UniqueGlow(color, intensity);
            cache[key] = material;
            return material;
        }

        /// <summary>A glow material that is not shared, for effects that fade their own emission.</summary>
        public static Material UniqueGlow(Color color, float intensity)
        {
            Material material = new Material(Flat(color));
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * intensity);
            }
            owned.Add(material);
            return material;
        }

        public static GameObject Primitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, Transform parent = null)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.position = position;
            obj.transform.localScale = scale;
            if (parent != null) obj.transform.SetParent(parent, true);
            obj.GetComponent<Renderer>().sharedMaterial = material;
            Collider collider = obj.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);
            return obj;
        }

        public static Transform Pivot(string name, Vector3 position, Transform parent)
        {
            GameObject pivot = new GameObject(name);
            pivot.transform.SetParent(parent, true);
            pivot.transform.position = position;
            return pivot.transform;
        }

        public static Texture2D NoiseTexture(string name, Color tint, float contrast)
        {
            const int size = 256;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGB24, false);
            texture.name = name;
            texture.wrapMode = TextureWrapMode.Repeat;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float broad = Mathf.PerlinNoise(x * 0.03f, y * 0.03f);
                float fine = Mathf.PerlinNoise(x * 0.17f + 17f, y * 0.17f + 31f);
                float cracks = Mathf.Abs(Mathf.PerlinNoise(x * 0.06f + 5f, y * 0.06f + 9f) - 0.5f) < 0.02f ? -0.25f : 0f;
                float value = Mathf.Clamp01(0.7f + (broad - 0.5f) * contrast + (fine - 0.5f) * contrast * 0.5f + cracks);
                texture.SetPixel(x, y, tint * value);
            }
            texture.Apply(false, true);
            owned.Add(texture);
            return texture;
        }

        public static void Own(Object obj) => owned.Add(obj);

        public static void ReleaseAll()
        {
            foreach (Object obj in owned) if (obj != null) Object.Destroy(obj);
            owned.Clear();
            cache.Clear();
        }
    }
}
