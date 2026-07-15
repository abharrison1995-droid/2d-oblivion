using UnityEditor;
using UnityEngine;

namespace Voidovia.EditorTools
{
    /// <summary>
    /// Anything dropped into Resources/Portraits/ needs Texture Type = Sprite for
    /// Resources.Load&lt;Sprite&gt; to find it. Set that automatically on import so
    /// placeholder/final art just works without a manual Inspector step.
    /// </summary>
    public class PortraitImportSettings : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/Portraits/")) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
        }
    }
}
