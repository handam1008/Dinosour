using UnityEditor;
using UnityEngine;

namespace JJW.Script.Editor
{
    public class GamblerSlotTextureImporter : AssetPostprocessor
    {
        private const string SlotAssetFolder =
            "Assets/JJW/Resources/GamblerSlot/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(SlotAssetFolder))
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
            importer.maxTextureSize = 4096;
        }
    }
}
