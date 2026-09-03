using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Animation;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace SSW.Editor
{
    public static class DinosaurAssetSetup
    {
        const string DownloadRoot = "Assets/0.Asset/download";
        const string OriginalRoot = "Assets/0.Asset/dinoCharactersVersion1.1/sheets";
        const string LibraryRoot = "Assets/SSW/Dinosaurs/Libraries";
        const string PlayerPrefab = "Assets/SSW/Prefabs/Player.prefab";
        const int TileSize = 24;

        static readonly string[] States = { "Idle", "Move", "Hurt", "Dead", "Jump" };
        static readonly string[] EggStates = { "EggMove", "EggCrack", "EggHatch" };
        static readonly HashSet<string> OriginalMales = new HashSet<string>
        {
            "doux", "mort", "tard", "vita"
        };

        [MenuItem("Tools/Dinosaurs/Rebuild Sprite Libraries")]
        public static void Rebuild()
        {
            ConfigureTextures();
            CreateLibraries();
            ConfigurePlayerPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void ConfigureTextures()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { DownloadRoot });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;
                ConfigureTexture(path);
            }
        }

        static void ConfigureTexture(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (importer == null || texture == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 1024;

            SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            Dictionary<string, GUID> existingIds = provider.GetSpriteRects()
                .GroupBy(rect => rect.name)
                .ToDictionary(group => group.Key, group => group.First().spriteID);
            List<SpriteRect> rects = new List<SpriteRect>();
            int columns = texture.width / TileSize;
            int rows = texture.height / TileSize;
            string baseName = Path.GetFileNameWithoutExtension(path);

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    int index = row * columns + column;
                    string spriteName = baseName + "_" + index;
                    rects.Add(new SpriteRect
                    {
                        name = spriteName,
                        rect = new Rect(column * TileSize, (rows - row - 1) * TileSize, TileSize, TileSize),
                        alignment = SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                        spriteID = existingIds.TryGetValue(spriteName, out GUID id) ? id : GUID.Generate()
                    });
                }
            }

            provider.SetSpriteRects(rects.ToArray());
            ISpriteNameFileIdDataProvider names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (names != null)
            {
                SpriteNameFileIdPair[] pairs = rects
                    .Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID))
                    .ToArray();
                names.SetNameFileIdPairs(pairs);
            }

            provider.Apply();
            importer.SaveAndReimport();
        }

        static void CreateLibraries()
        {
            EnsureFolder(LibraryRoot);
            foreach (string genderPath in GetDirectories(DownloadRoot))
            {
                string gender = Path.GetFileName(genderPath);
                string outputFolder = LibraryRoot + "/" + ToTitle(gender);
                EnsureFolder(outputFolder);

                foreach (string variantPath in GetDirectories(genderPath))
                {
                    string variant = Path.GetFileName(variantPath);
                    List<ISpriteLibraryCategory> categories = BuildCategories(gender, variant, variantPath);
                    string outputPath = outputFolder + "/" + ToTitle(variant) + ".spriteLib";
                    SpriteLibrarySourceAssetFactory.Create(outputPath, categories);
                    AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);
                }
            }
        }

        static List<ISpriteLibraryCategory> BuildCategories(string gender, string variant, string variantPath)
        {
            List<ISpriteLibraryCategory> categories = new List<ISpriteLibraryCategory>();
            foreach (string state in States)
            {
                string sourceName = state.ToLowerInvariant();
                Sprite[] frames = LoadFrames(variantPath + "/base/" + sourceName + ".png");
                if (frames.Length == 0 && gender == "male" && OriginalMales.Contains(variant))
                    frames = LoadOriginalFrames(variant, state);
                AddCategory(categories, state, frames);
            }

            foreach (string state in EggStates)
            {
                string sourceName = state.Substring(3).ToLowerInvariant();
                AddCategory(categories, state, LoadFrames(variantPath + "/egg/" + sourceName + ".png"));
            }

            return categories;
        }

        static Sprite[] LoadOriginalFrames(string variant, string state)
        {
            Sprite[] fullTiles = LoadFrames(OriginalRoot + "/DinoSprites - " + variant + ".png")
                .GroupBy(sprite => Mathf.FloorToInt(sprite.rect.center.x / TileSize))
                .OrderBy(group => group.Key)
                .Select(group => group.OrderByDescending(sprite => sprite.rect.width * sprite.rect.height).First())
                .ToArray();

            if (state == "Idle") return fullTiles.Skip(0).Take(4).ToArray();
            if (state == "Move") return fullTiles.Skip(4).Take(6).ToArray();
            if (state == "Hurt") return fullTiles.Skip(13).Take(4).ToArray();
            return Array.Empty<Sprite>();
        }

        static Sprite[] LoadFrames(string path)
        {
            if (!File.Exists(ToFullPath(path))) return Array.Empty<Sprite>();
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderByDescending(sprite => sprite.rect.y)
                .ThenBy(sprite => sprite.rect.x)
                .ToArray();
        }

        static void AddCategory(List<ISpriteLibraryCategory> categories, string name, Sprite[] frames)
        {
            if (frames.Length == 0) return;
            SpriteLibraryLabel[] labels = frames
                .Select((sprite, index) => new SpriteLibraryLabel(index.ToString(), sprite))
                .ToArray();
            categories.Add(new SpriteLibraryCategory(name, labels));
        }

        static void ConfigurePlayerPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefab);
            try
            {
                Transform visual = root.transform.Find("Visual");
                if (visual == null) throw new InvalidOperationException("Player prefab has no Visual child.");

                SpriteLibrary library = visual.GetComponent<SpriteLibrary>();
                if (library == null) library = visual.gameObject.AddComponent<SpriteLibrary>();
                library.spriteLibraryAsset = AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>(
                    LibraryRoot + "/Male/Doux.spriteLib");

                SpriteResolver resolver = visual.GetComponent<SpriteResolver>();
                if (resolver == null) resolver = visual.gameObject.AddComponent<SpriteResolver>();
                resolver.SetCategoryAndLabel("Idle", "0");

                DinosaurVisualController controller = visual.GetComponent<DinosaurVisualController>();
                if (controller == null) visual.gameObject.AddComponent<DinosaurVisualController>();

                Animator animator = visual.GetComponent<Animator>();
                if (animator != null) animator.enabled = false;

                Health health = root.GetComponent<Health>();
                if (health != null)
                {
                    SerializedObject serializedHealth = new SerializedObject(health);
                    serializedHealth.FindProperty("_deathDisableDelay").floatValue = 0.8f;
                    serializedHealth.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static IEnumerable<string> GetDirectories(string assetPath)
        {
            string fullPath = ToFullPath(assetPath);
            if (!Directory.Exists(fullPath)) return Array.Empty<string>();
            return Directory.GetDirectories(fullPath)
                .Select(path => path.Replace('\\', '/').Replace(Application.dataPath, "Assets"));
        }

        static string ToFullPath(string assetPath)
        {
            return assetPath.Replace("Assets", Application.dataPath).Replace('/', Path.DirectorySeparatorChar);
        }

        static string ToTitle(string value)
        {
            return char.ToUpperInvariant(value[0]) + value.Substring(1).ToLowerInvariant();
        }

        static void EnsureFolder(string assetPath)
        {
            string[] parts = assetPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
