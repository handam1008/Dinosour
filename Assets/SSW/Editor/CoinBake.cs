using System;
using System.IO;
using System.Linq;
using JJW.Script.Jackpot;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SSW
{
    public static class CoinBake
    {
        const string Folder = "Assets/SSW/Presentation/Coin";
        const string PlayerPath = "Assets/SSW/Resources/Network/Player.prefab";

        public static string Setup()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before baking coin effects");
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            Scene previous = SceneManager.GetActiveScene();
            if (SceneManager.GetSceneByPath("Assets/JJW/Scene/Gamblinger.unity").isLoaded)
                throw new InvalidOperationException("Close Gamblinger before baking coin effects");
            Scene source = EditorSceneManager.OpenScene("Assets/JJW/Scene/Gamblinger.unity", OpenSceneMode.Additive);
            GameObject player = null;
            GameObject screen = null;
            try
            {
                SceneManager.SetActiveScene(source);
                T Find<T>() where T : Component => source.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<T>(true)).Single();
                var original = Find<GamblerJackpotVFX>();
                var values = new SerializedObject(original);
                var jackpot = new SerializedObject(Find<GamblerJackpotEffect>());
                var roulette = new SerializedObject(Find<GamblerSlotRoulette>());
                screen = BuildScreen(values, jackpot);
                GameObject screenPrefab = PrefabUtility.SaveAsPrefabAsset(screen, Folder + "/Screen.prefab");
                if (screenPrefab == null) throw new InvalidOperationException("Coin screen prefab could not be saved");
                player = PrefabUtility.LoadPrefabContents(PlayerPath);
                Transform view = player.GetComponent<NetPlayer>().View;
                Transform old = view.Find("CoinEffects");
                if (old != null) Object.DestroyImmediate(old.gameObject);
                var root = new GameObject("CoinEffects");
                root.transform.SetParent(view, false);
                var fx = root.AddComponent<CoinFx>();
                ParticleSystem damage = Rising(root.transform, "DamageUpVFX", "DamageArrow", values, "damageArrow", false);
                ParticleSystem speed = Rising(root.transform, "SpeedUpVFX", "SpeedArrow", values, "speedArrow", false);
                ParticleSystem heal = Rising(root.transform, "HealVFX", "HealPlus", values, "healPlus", true);
                var shield = Object.Instantiate((GameObject)values.FindProperty("invincibleShield").objectReferenceValue, root.transform, false);
                shield.name = "InvincibleShieldEffect";
                shield.SetActive(false);
                ParticleSystem[] energy = jackpot.FindProperty("jackpotParticles").Elements()
                    .Select(x => (ParticleSystem)x.objectReferenceValue).Where(x => x.transform.IsChildOf(original.transform))
                    .Select(x => CopyParticle(x, root.transform)).ToArray();
                CoinReels reels = BuildReels(root.transform, roulette);
                Edit(fx, data =>
                {
                    Ref(data, "_reels", reels);
                    Ref(data, "_damage", damage);
                    Ref(data, "_speed", speed);
                    Ref(data, "_heal", heal);
                    data.FindProperty("_healCount").intValue = values.FindProperty("healPlusCount").intValue;
                    Ref(data, "_shield", shield);
                    Refs(data, "_energy", energy);
                    Ref(data, "_screenPrefab", screenPrefab.GetComponent<CoinScreen>());
                });
                Edit(player.GetComponent<CoinCast>(), data => Ref(data, "_effects", fx));
                PrefabUtility.SaveAsPrefabAsset(player, PlayerPath);
                AssetDatabase.SaveAssets();
                return "Coin effects, original sprites, shield, roulette, 444 and 777 presentation bound";
            }
            finally
            {
                if (player != null) PrefabUtility.UnloadPrefabContents(player);
                if (screen != null) Object.DestroyImmediate(screen);
                EditorSceneManager.CloseScene(source, true);
                SceneManager.SetActiveScene(previous);
            }
        }

        static GameObject BuildScreen(SerializedObject values, SerializedObject jackpot)
        {
            var root = new GameObject("CoinScreen");
            var controller = root.AddComponent<CoinScreen>();
            RectTransform canvasRoot = Rect("Canvas", root.transform, new Vector2(1920f, 1080f));
            var canvas = canvasRoot.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            var scaler = canvasRoot.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var originalOverlay = (Image)jackpot.FindProperty("jackpotOverlay").objectReferenceValue;
            Image overlay = Object.Instantiate(originalOverlay, canvasRoot, false);
            overlay.name = "JackOverlay";
            overlay.raycastTarget = false;
            overlay.gameObject.SetActive(false);
            var digits = Object.Instantiate((JackpotUI)jackpot.FindProperty("jackpotUI").objectReferenceValue, canvasRoot, false);
            digits.name = "JackpotDigits";
            foreach (MonoBehaviour component in digits.GetComponents<MonoBehaviour>())
                if (component != digits && !(component is UnityEngine.EventSystems.UIBehaviour)) Object.DestroyImmediate(component);
            digits.GetComponent<CanvasGroup>().alpha = 0f;
            digits.gameObject.SetActive(false);
            Image death = Image("DeathMark444", canvasRoot, Vector2.one * values.FindProperty("deathMarkSize").floatValue,
                Sprite("Death", Resources.Load<Texture2D>("JackpotVFX/DeathMark444")));
            var deathGroup = death.gameObject.AddComponent<CanvasGroup>();
            deathGroup.alpha = 0f;
            deathGroup.interactable = false;
            deathGroup.blocksRaycasts = false;
            var rig = new GameObject("ScreenParticles");
            rig.transform.SetParent(root.transform, false);
            var money = CopyParticle((ParticleSystem)jackpot.FindProperty("jackpotParticles").GetArrayElementAtIndex(0).objectReferenceValue, rig.transform);
            var objects = jackpot.FindProperty("jackpotObjects").Elements().Select(x =>
            {
                var value = Object.Instantiate((GameObject)x.objectReferenceValue, rig.transform, false);
                value.SetActive(false);
                return value;
            }).ToArray();
            Edit(controller, data =>
            {
                Ref(data, "_death", deathGroup);
                CopyFloat(data, "_deathScale", values, "deathMarkDisplayScale");
                CopyFloat(data, "_deathAlpha", values, "deathMarkMaxAlpha");
                CopyFloat(data, "_deathIn", values, "deathMarkFadeIn");
                CopyFloat(data, "_deathHold", values, "deathMarkHold");
                CopyFloat(data, "_deathOut", values, "deathMarkFadeOut");
                Ref(data, "_overlay", overlay);
                CopyFloat(data, "_overlayLow", jackpot, "minOverlayAlpha");
                CopyFloat(data, "_overlayHigh", jackpot, "maxOverlayAlpha");
                CopyFloat(data, "_overlayPeriod", jackpot, "overlayDuration");
                Ref(data, "_digits", digits);
                Ref(data, "_rig", rig.transform);
                Ref(data, "_money", money);
                Refs(data, "_objects", objects);
            });
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(item.gameObject);
                foreach (MonoBehaviour component in item.GetComponents<MonoBehaviour>())
                    if (!(component is CoinScreen) && !(component is JackpotUI) && !(component is UnityEngine.EventSystems.UIBehaviour)) Object.DestroyImmediate(component);
            }
            return root;
        }

        static CoinReels BuildReels(Transform parent, SerializedObject source)
        {
            var root = new GameObject("Roulette");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = ((Component)source.targetObject).transform.localPosition;
            var controller = root.AddComponent<CoinReels>();
            RectTransform canvasRoot = Rect("SlotCanvas", root.transform, new Vector2(720f, 250f));
            canvasRoot.localPosition = source.FindProperty("visualLocalPosition").vector3Value;
            canvasRoot.localScale = Vector3.one * source.FindProperty("visualScale").floatValue;
            var canvas = canvasRoot.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = source.FindProperty("sortingOrder").intValue;
            var group = canvasRoot.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            Image background = Image("Background", canvasRoot, new Vector2(665f, 205f), null);
            background.color = new Color(0.015f, 0.035f, 0.025f, 0.96f);
            Texture2D sheet = Resources.Load<Texture2D>("GamblerSlot/SlotSymbols");
            Sprite[] symbols = Enumerable.Range(0, 6).Select(i => Sprite("Symbol" + i, sheet, new Rect(i * sheet.width / 6f, 0f, sheet.width / 6f, sheet.height))).ToArray();
            var current = new Image[3];
            var next = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                RectTransform mask = Rect("Reel" + (i + 1), canvasRoot, Vector2.one * 170f);
                mask.anchoredPosition = new Vector2((i - 1) * 196f, 0f);
                mask.gameObject.AddComponent<RectMask2D>();
                current[i] = Image("Current", mask, Vector2.one * 145f, symbols[0]);
                next[i] = Image("Next", mask, Vector2.one * 145f, symbols[1]);
                next[i].rectTransform.anchoredPosition = new Vector2(0f, 170f);
            }
            Image("Frame", canvasRoot, new Vector2(700f, 245f), Sprite("Frame", Resources.Load<Texture2D>("GamblerSlot/SlotFrame")));
            Edit(controller, data =>
            {
                Ref(data, "_group", group);
                Refs(data, "_symbols", symbols);
                CopyFloat(data, "_step", source, "symbolStepDuration");
                CopyFloat(data, "_hold", source, "resultHoldDuration");
                CopyFloat(data, "_fade", source, "fadeDuration");
                CopyFloat(data, "_volume", source, "reelStopVolume");
                Ref(data, "_stop", source.FindProperty("reelStopSound").objectReferenceValue ?? ReelSound());
                SerializedProperty reels = data.FindProperty("_reels");
                reels.arraySize = 3;
                string[] stops = { "leftStopTime", "middleStopTime", "rightStopTime" };
                for (int i = 0; i < 3; i++)
                {
                    SerializedProperty reel = reels.GetArrayElementAtIndex(i);
                    reel.FindPropertyRelative("Current").objectReferenceValue = current[i];
                    reel.FindPropertyRelative("Next").objectReferenceValue = next[i];
                    reel.FindPropertyRelative("Stop").floatValue = source.FindProperty(stops[i]).floatValue;
                }
            });
            return controller;
        }

        static ParticleSystem Rising(Transform parent, string name, string texture, SerializedObject source, string prefix, bool heal)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0f, heal ? -0.15f : -0.35f, 0f);
            var effect = root.AddComponent<ParticleSystem>();
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = effect.main;
            main.playOnAwake = false;
            main.loop = !heal;
            main.duration = heal ? 0.7f : 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.startLifetime = heal ? new ParticleSystem.MinMaxCurve(0.55f, 0.8f) : new ParticleSystem.MinMaxCurve(0.85f);
            main.startSpeed = 0f;
            float size = source.FindProperty(prefix + "Size").floatValue;
            main.startSize = heal ? new ParticleSystem.MinMaxCurve(size * 0.8f, size * 1.2f) : new ParticleSystem.MinMaxCurve(size);
            main.maxParticles = 32;
            var emission = effect.emission;
            emission.rateOverTime = heal ? 0f : source.FindProperty(prefix + "Rate").floatValue;
            emission.SetBursts(Array.Empty<ParticleSystem.Burst>());
            var shape = effect.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(heal ? 0.8f : 0.9f, heal ? 0.15f : 0.08f, 0f);
            float rise = source.FindProperty(prefix + "RiseSpeed").floatValue;
            var velocity = effect.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(heal ? -0.2f : -0.12f, heal ? 0.2f : 0.12f);
            velocity.y = new ParticleSystem.MinMaxCurve(rise * (heal ? 0.85f : 0.95f), rise * (heal ? 1.15f : 1.05f));
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0.85f, 0.65f), new GradientAlphaKey(0f, 1f) });
            var color = effect.colorOverLifetime;
            color.enabled = true;
            color.color = fade;
            string path = Folder + "/" + texture + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Sprites/Default"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.mainTexture = Resources.Load<Texture2D>("JackpotVFX/" + texture);
            EditorUtility.SetDirty(material);
            var renderer = effect.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortingOrder = source.FindProperty("particleSortingOrder").intValue;
            return effect;
        }

        static ParticleSystem CopyParticle(ParticleSystem source, Transform parent)
        {
            ParticleSystem copy = Object.Instantiate(source, parent, false);
            copy.name = source.name;
            var main = copy.main;
            main.playOnAwake = false;
            copy.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            copy.gameObject.SetActive(true);
            return copy;
        }

        static Sprite Sprite(string name, Texture2D texture, Rect? region = null)
        {
            string path = Folder + "/" + name + ".asset";
            Sprite saved = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (saved != null) return saved;
            var result = UnityEngine.Sprite.Create(texture, region ?? new Rect(0f, 0f, texture.width, texture.height), Vector2.one * 0.5f, 100f, 0, SpriteMeshType.FullRect);
            result.name = name;
            AssetDatabase.CreateAsset(result, path);
            return result;
        }

        static AudioClip ReelSound()
        {
            string path = Folder + "/ReelStop.wav";
            if (!File.Exists(path))
            {
                const int rate = 22050;
                int count = Mathf.CeilToInt(rate * 0.13f);
                using var output = new BinaryWriter(File.Create(path));
                output.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); output.Write(36 + count * 2);
                output.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); output.Write(16);
                output.Write((short)1); output.Write((short)1); output.Write(rate); output.Write(rate * 2); output.Write((short)2); output.Write((short)16);
                output.Write(System.Text.Encoding.ASCII.GetBytes("data")); output.Write(count * 2);
                var noise = new System.Random(777);
                for (int i = 0; i < count; i++)
                {
                    float time = i / (float)rate;
                    float sample = (Mathf.Sin(2f * Mathf.PI * 105f * time) * 0.72f + Mathf.Sin(2f * Mathf.PI * 210f * time) * 0.22f
                        + ((float)noise.NextDouble() * 2f - 1f) * 0.12f) * Mathf.Exp(-26f * time);
                    output.Write((short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue));
                }
            }
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        static RectTransform Rect(string name, Transform parent, Vector2 size)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = size;
            return rect;
        }

        static Image Image(string name, Transform parent, Vector2 size, Sprite sprite)
        {
            RectTransform rect = Rect(name, parent, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        static void Edit(Object target, Action<SerializedObject> edit)
        {
            using var data = new SerializedObject(target);
            edit(data);
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Ref(SerializedObject data, string field, Object value) => data.FindProperty(field).objectReferenceValue = value;
        static void CopyFloat(SerializedObject to, string field, SerializedObject from, string source) => to.FindProperty(field).floatValue = from.FindProperty(source).floatValue;

        static void Refs(SerializedObject data, string field, Object[] values)
        {
            SerializedProperty array = data.FindProperty(field);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        static System.Collections.Generic.IEnumerable<SerializedProperty> Elements(this SerializedProperty array)
        {
            for (int i = 0; i < array.arraySize; i++) yield return array.GetArrayElementAtIndex(i);
        }
    }
}
