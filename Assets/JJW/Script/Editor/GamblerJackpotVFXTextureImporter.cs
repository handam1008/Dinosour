using UnityEditor;
using UnityEngine;

namespace JJW.Script.Editor
{
    public class GamblerJackpotVFXTextureImporter : AssetPostprocessor
    {
        private const string VfxAssetFolder =
            "Assets/JJW/Resources/JackpotVFX/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(VfxAssetFolder))
            {
                return;
            }

            TextureImporter importer =
                (TextureImporter)assetImporter;

            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
        }
    }
}
