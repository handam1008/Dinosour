using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
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
        const string AnimationRoot = "Assets/SSW/Dinosaurs/Animations";
        const string ControllerPath = AnimationRoot + "/Dinosaur.controller";
        const string PlayerPrefab = "Assets/SSW/Prefabs/Player.prefab";
        const int TileSize = 24;
        const float FramesPerSecond = 12f;

        static readonly string[] States = { "Idle", "Move", "Hurt", "Dead" };
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
            CreateAnimations();
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

        static void CreateAnimations()
        {
            EnsureFolder(AnimationRoot);

            Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>
            {
                ["Idle"] = CreateResolverClip("Idle", "Idle", GetCommonFrameCount("Idle"), GetCommonFrameCount("Idle") / FramesPerSecond, true, false),
                ["Move"] = CreateResolverClip("Move", "Move", GetCommonFrameCount("Move"), GetCommonFrameCount("Move") / FramesPerSecond, true, false),
                ["Hurt"] = CreateResolverClip("Hurt", "Hurt", GetCommonFrameCount("Hurt"), 0.35f, false, false),
                ["Dead"] = CreateResolverClip("Dead", "Dead", GetCommonFrameCount("Dead"), 0.8f, false, false),
                ["AirborneIdle"] = CreateResolverClip("AirborneIdle", "Idle", 1, 1f, false, false),
                ["EggMove"] = CreateResolverClip("EggMove", "EggMove", GetCommonFrameCount("EggMove"), 1.2f, false, true),
                ["EggCrack"] = CreateResolverClip("EggCrack", "EggCrack", GetCommonFrameCount("EggCrack"), 0.8f, false, false),
                ["EggHatch"] = CreateResolverClip("EggHatch", "EggHatch", GetCommonFrameCount("EggHatch"), 1f, false, false)
            };

            AddEggMotion(clips["EggMove"], 1.2f);
            CreateAnimatorController(clips);
        }

        static AnimationClip CreateResolverClip(
            string clipName,
            string category,
            int frameCount,
            float duration,
            bool loop,
            bool repeatFrames)
        {
            string path = AnimationRoot + "/" + clipName + ".anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip { name = clipName };
                AssetDatabase.CreateAsset(clip, path);
            }

            clip.ClearCurves();
            clip.frameRate = FramesPerSecond;

            int sampleCount = repeatFrames
                ? Mathf.Max(frameCount, Mathf.CeilToInt(duration * FramesPerSecond))
                : frameCount;
            float step = repeatFrames ? 1f / FramesPerSecond : duration / Mathf.Max(1, frameCount);
            Keyframe[] keys = new Keyframe[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                int frame = repeatFrames ? i % frameCount : Mathf.Min(i, frameCount - 1);
                int hash = Animator.StringToHash(category + "_" + frame) & 0x3FFFFFFF;
                keys[i] = new Keyframe(
                    i * step,
                    BitConverter.ToSingle(BitConverter.GetBytes(hash), 0),
                    float.PositiveInfinity,
                    float.PositiveInfinity);
            }

            EditorCurveBinding spriteBinding = EditorCurveBinding.DiscreteCurve(
                string.Empty,
                typeof(SpriteResolver),
                "m_SpriteHash");
            AnimationUtility.SetEditorCurve(clip, spriteBinding, new AnimationCurve(keys));
            SetRestTransform(clip, duration);
            SetClipSettings(clip, duration, loop);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        static void SetRestTransform(AnimationClip clip, float duration)
        {
            AnimationCurve position = AnimationCurve.Linear(0f, 0f, duration, 0f);
            AnimationCurve rotation = AnimationCurve.Linear(0f, 0f, duration, 0f);
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "m_LocalPosition.y"),
                position);
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.z"),
                rotation);
        }

        static void AddEggMotion(AnimationClip clip, float duration)
        {
            const int keyCount = 25;
            Keyframe[] positionKeys = new Keyframe[keyCount];
            Keyframe[] rotationKeys = new Keyframe[keyCount];

            for (int i = 0; i < keyCount; i++)
            {
                float normalized = i / (keyCount - 1f);
                float time = duration * normalized;
                float wave = Mathf.Sin(normalized * Mathf.PI * 6f);
                positionKeys[i] = new Keyframe(time, Mathf.Abs(wave) * 0.08f);
                rotationKeys[i] = new Keyframe(time, wave * 7f);
            }

            AnimationCurve position = new AnimationCurve(positionKeys);
            AnimationCurve rotation = new AnimationCurve(rotationKeys);
            for (int i = 0; i < keyCount; i++)
            {
                position.SmoothTangents(i, 0f);
                rotation.SmoothTangents(i, 0f);
            }

            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "m_LocalPosition.y"),
                position);
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "localEulerAnglesRaw.z"),
                rotation);
            EditorUtility.SetDirty(clip);
        }

        static void SetClipSettings(AnimationClip clip, float duration, bool loop)
        {
            SerializedObject serializedClip = new SerializedObject(clip);
            SerializedProperty settings = serializedClip.FindProperty("m_AnimationClipSettings");
            if (settings != null)
            {
                SerializedProperty loopTime = settings.FindPropertyRelative("m_LoopTime");
                SerializedProperty startTime = settings.FindPropertyRelative("m_StartTime");
                SerializedProperty stopTime = settings.FindPropertyRelative("m_StopTime");
                if (loopTime != null) loopTime.boolValue = loop;
                if (startTime != null) startTime.floatValue = 0f;
                if (stopTime != null) stopTime.floatValue = duration;
            }

            serializedClip.ApplyModifiedPropertiesWithoutUndo();
        }

        static int GetCommonFrameCount(string category)
        {
            SpriteLibraryAsset[] libraries = AssetDatabase.FindAssets("t:SpriteLibraryAsset", new[] { LibraryRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<SpriteLibraryAsset>)
                .Where(asset => asset != null)
                .ToArray();

            int count = libraries
                .Select(asset => asset.GetCategoryLabelNames(category).Count())
                .DefaultIfEmpty(0)
                .Min();
            if (count <= 0)
                throw new InvalidOperationException("Sprite Library category is empty: " + category);
            return count;
        }

        static void CreateAnimatorController(IReadOnlyDictionary<string, AnimationClip> clips)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in stateMachine.states.ToArray())
                stateMachine.RemoveState(child.state);
            foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions.ToArray())
                stateMachine.RemoveAnyStateTransition(transition);

            AnimatorState idle = AddState(stateMachine, "Idle", clips["Idle"], 360f, 220f);
            AddState(stateMachine, "Move", clips["Move"], 560f, 220f);
            AddState(stateMachine, "Hurt", clips["Hurt"], 560f, 80f);
            AddState(stateMachine, "Dead", clips["Dead"], 760f, 80f);
            AddState(stateMachine, "AirborneIdle", clips["AirborneIdle"], 360f, 80f);
            AnimatorState eggMove = AddState(stateMachine, "EggMove", clips["EggMove"], 0f, 360f);
            AnimatorState eggCrack = AddState(stateMachine, "EggCrack", clips["EggCrack"], 220f, 360f);
            AnimatorState eggHatch = AddState(stateMachine, "EggHatch", clips["EggHatch"], 440f, 360f);

            AddExitTransition(eggMove, eggCrack);
            AddExitTransition(eggCrack, eggHatch);
            AddExitTransition(eggHatch, idle);
            stateMachine.defaultState = idle;
            EditorUtility.SetDirty(controller);
        }

        static AnimatorState AddState(
            AnimatorStateMachine stateMachine,
            string stateName,
            AnimationClip clip,
            float x,
            float y)
        {
            AnimatorState state = stateMachine.AddState(stateName, new Vector3(x, y));
            state.motion = clip;
            state.writeDefaultValues = true;
            return state;
        }

        static void AddExitTransition(AnimatorState from, AnimatorState to)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = true;
            transition.exitTime = 1f;
            transition.hasFixedDuration = true;
            transition.duration = 0f;
            transition.canTransitionToSelf = false;
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
                if (animator == null) animator = visual.gameObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
                animator.enabled = true;

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
