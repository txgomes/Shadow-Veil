using UnityEditor;

namespace ShadowVeil.Summoner.EditorTools
{
    /// <summary>Imports the extracted First Summoner PNGs under Summoner/ as UI sprites.</summary>
    public sealed class SummonerSpriteImporter : AssetPostprocessor
    {
        private const string Root = "Assets/ShadowVeil/Summoner/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root) || !assetPath.EndsWith(".png")) return;
            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
        }
    }
}
