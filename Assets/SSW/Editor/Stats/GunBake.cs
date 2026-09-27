using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SSW
{
    [InitializeOnLoad]
    public static class GunBake
    {
        static bool _queued;
        static bool _baking;
        static HashSet<string> _dependencies;

        static GunBake()
        {
            EditorApplication.playModeStateChanged += OnPlay;
            Queue();
        }

        public static bool Bake(GunSources source)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Play 중에는 탄환 수치를 베이크하지 않습니다.");
            if (source == null || source.Balance == null) throw new InvalidOperationException("탄환 원본 또는 출력 참조가 없습니다.");
            string path = AssetDatabase.GetAssetPath(source.Balance);
            if (!path.StartsWith("Assets/SSW/", StringComparison.Ordinal))
                throw new InvalidOperationException("탄환 수치 출력은 Assets/SSW 안에 지정해야 합니다.");
            using var fire = new StatValue(source.Fire);
            using var gravity = new StatValue(source.Gravity);
            using var ice = new StatValue(source.Ice);
            using var poison = new StatValue(source.Poison);
            using var shuriken = new StatValue(source.Shuriken);
            using var air = new StatValue(source.Air);
            float fireDamage = fire.Integer("dotDamage");
            float gravityForce = gravity.Number("forceAmount");
            float iceSlow = ice.Number("slowAmount");
            float poisonDamage = poison.Number("dotDamage");
            float shurikenDamage = shuriken.Number("damage");
            float airForce = air.Number("flyPower");
            foreach (float value in new[] { fireDamage, gravityForce, iceSlow, poisonDamage, shurikenDamage, airForce })
                if (!float.IsFinite(value) || value < 0f) throw new InvalidOperationException(source.name + ": 탄환 수치는 유한한 0 이상의 값이어야 합니다.");
            bool changed = source.Balance.Replace(fireDamage, gravityForce, iceSlow, poisonDamage, shurikenDamage, airForce);
            if (changed)
            {
                EditorUtility.SetDirty(source.Balance);
                AssetDatabase.SaveAssetIfDirty(source.Balance);
            }
            return changed;
        }

        public static void BakeAll(bool required = true)
        {
            if (_baking) throw new InvalidOperationException("탄환 수치 베이크가 이미 진행 중입니다.");
            _baking = true;
            try
            {
                string[] paths = AssetDatabase.FindAssets("t:GunSources", new[] { "Assets/SSW" })
                    .Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path, StringComparer.Ordinal).ToArray();
                if (required && paths.Length == 0) throw new InvalidOperationException("GunSources 원본 설정이 없습니다.");
                var outputs = new HashSet<GunBalance>();
                var sources = paths.Select(AssetDatabase.LoadAssetAtPath<GunSources>).ToArray();
                foreach (GunSources source in sources)
                    if (source == null || !outputs.Add(source.Balance)) throw new InvalidOperationException("탄환 수치 출력 참조가 없거나 중복됐습니다.");
                _dependencies = new HashSet<string>(paths, StringComparer.Ordinal);
                foreach (string path in paths) _dependencies.UnionWith(AssetDatabase.GetDependencies(path, true));
                foreach (GunSources source in sources)
                {
                    _dependencies.Remove(AssetDatabase.GetAssetPath(source.Balance));
                    Bake(source);
                }
            }
            finally { _baking = false; }
        }

        public static void Imported(IEnumerable<string> paths)
        {
            if (_baking) return;
            if (_dependencies == null || paths.Any(path => _dependencies.Contains(path)
                || path.EndsWith("/GunSources.asset", StringComparison.Ordinal))) Queue();
        }

        public static void Queue()
        {
            if (_queued) return;
            _queued = true;
            EditorApplication.update += Deferred;
        }

        static void Deferred()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            EditorApplication.update -= Deferred;
            _queued = false;
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer) return;
            try { BakeAll(false); }
            catch (Exception error) { Debug.LogError("탄환 수치 자동 베이크 실패: " + error.Message); }
        }

        static void OnPlay(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode) { Queue(); return; }
            if (state != PlayModeStateChange.ExitingEditMode) return;
            try { BakeAll(); }
            catch (Exception error)
            {
                EditorApplication.isPlaying = false;
                Debug.LogError("탄환 수치 베이크 실패로 Play를 중단했습니다: " + error.Message);
            }
        }
    }

    public sealed class GunBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder => -9900;
        public void OnPreprocessBuild(BuildReport report)
        {
            try { GunBake.BakeAll(); }
            catch (Exception error) { throw new BuildFailedException("탄환 수치 베이크 실패: " + error.Message); }
        }
    }

    public sealed class GunImports : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] previous)
            => GunBake.Imported(imported.Concat(deleted).Concat(moved).Concat(previous));
    }
}
