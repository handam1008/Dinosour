using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SSW
{
    [InitializeOnLoad]
    public static class MapBake
    {
        const string ProfilePath = "Assets/SSW/Editor/Maps/MapSources.asset";
        const string GamePath = "Assets/SSW/Resources/Network/NetGame.prefab";
        const string Version = "4";
        static readonly int[] Numbers = { 1, 2, 3, 6, 7, 8, 9, 12, 13, 14, 17 };
        static readonly int[] Protected = { 4, 5, 10, 15, 16 };
        static bool _baking;

        static MapBake() => EditorApplication.playModeStateChanged += OnPlay;

        public static string Setup()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Play 중에는 맵을 갱신하지 않습니다.");
            MapSources profile = AssetDatabase.LoadAssetAtPath<MapSources>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<MapSources>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            var entries = profile.Entries.ToList();
            foreach (int number in Numbers)
            {
                if (entries.Any(item => item.Number == number)) continue;
                string sourcePath = "Assets/MapPrefab/KDH_Map " + number + ".prefab";
                GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
                if (source == null) throw new InvalidOperationException("원본 맵이 없습니다: " + sourcePath);
                entries.Add(new MapSources.Entry
                {
                    Number = number,
                    Source = source,
                    Map = AssetDatabase.LoadAssetAtPath<BattleMap>("Assets/SSW/Maps/Battle/Map" + number.ToString("00") + ".prefab")
                });
            }
            profile.Replace(entries.ToArray());
            MapRead.Edit(profile, target =>
            {
                if (target.FindProperty("_game").objectReferenceValue == null)
                    target.FindProperty("_game").objectReferenceValue = AssetDatabase.LoadAssetAtPath<NetGame>(GamePath);
            });
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssetIfDirty(profile);
            return BakeAll();
        }

        public static string BakeAll()
        {
            if (_baking) throw new InvalidOperationException("맵 갱신이 이미 진행 중입니다.");
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Play 중에는 맵을 갱신하지 않습니다.");
            _baking = true;
            try
            {
                string[] paths = AssetDatabase.FindAssets("t:MapSources", new[] { "Assets/SSW" })
                    .Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path, StringComparer.Ordinal).ToArray();
                var reports = new List<string>();
                var failures = new List<Exception>();
                var outputs = new HashSet<string>(StringComparer.Ordinal);
                foreach (string path in paths)
                {
                    MapSources sources = AssetDatabase.LoadAssetAtPath<MapSources>(path);
                    var entries = sources.Entries;
                    var numbers = new HashSet<int>();
                    bool changed = false;
                    for (int i = 0; i < entries.Length; i++)
                    {
                        var entry = entries[i];
                        if (!Numbers.Contains(entry.Number)) continue;
                        try
                        {
                            Validate(entry);
                            if (!numbers.Add(entry.Number)) throw new InvalidOperationException(path + ": 맵 번호가 중복됐습니다: " + entry.Number);
                            string output = entry.Map != null ? AssetDatabase.GetAssetPath(entry.Map)
                                : "Assets/SSW/Maps/Battle/Map" + entry.Number.ToString("00") + ".prefab";
                            if (!outputs.Add(output)) throw new InvalidOperationException("같은 맵을 여러 원본 설정이 갱신합니다: " + output);
                            string sourcePath = AssetDatabase.GetAssetPath(entry.Source);
                            string hash = Version + ":" + AssetDatabase.AssetPathToGUID(sourcePath) + ":" + AssetDatabase.GetAssetDependencyHash(sourcePath) + ":" + output;
                            if (entry.Map != null && entry.Hash == hash) continue;
                            entry.Map = MapRefresh.Apply(entry);
                            entry.Hash = hash;
                            entries[i] = entry;
                            changed = true;
                            reports.Add(entry.Number + "번 갱신");
                        }
                        catch (Exception error)
                        {
                            failures.Add(new InvalidOperationException(entry.Number + "번 갱신 실패: " + error.Message, error));
                        }
                    }
                    if (changed)
                    {
                        sources.Replace(entries);
                        EditorUtility.SetDirty(sources);
                        AssetDatabase.SaveAssetIfDirty(sources);
                    }
                    try { Register(sources, entries, reports); }
                    catch (Exception error) { failures.Add(error); }
                }
                if (failures.Count > 0) throw new AggregateException(string.Join("\n", reports.Concat(failures.Select(error => error.Message))), failures);
                return reports.Count == 0 ? "맵 원본 변경 없음" : string.Join("\n", reports);
            }
            finally { _baking = false; }
        }

        static void Validate(MapSources.Entry entry)
        {
            if (entry.Source == null || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(entry.Source)))
                throw new InvalidOperationException(entry.Number + "번 원본 프리팹이 없습니다.");
            var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.scene.isDirty && (stage.assetPath == AssetDatabase.GetAssetPath(entry.Source)
                || entry.Map != null && stage.assetPath == AssetDatabase.GetAssetPath(entry.Map)))
                throw new InvalidOperationException("프리팹 편집 내용을 먼저 처리해야 합니다: " + stage.assetPath);
            if (entry.Map == null) return;
            string output = AssetDatabase.GetAssetPath(entry.Map);
            if (!output.StartsWith("Assets/SSW/", StringComparison.Ordinal)
                || Protected.Any(number => output == "Assets/SSW/Maps/Battle/Map" + number.ToString("00") + ".prefab"))
                throw new InvalidOperationException("이번 갱신에서 수정할 수 없는 맵입니다: " + output);
        }

        static void Register(MapSources sources, MapSources.Entry[] entries, List<string> report)
        {
            string path = AssetDatabase.GetAssetPath(sources.Game);
            if (sources.Game == null || !path.StartsWith("Assets/SSW/", StringComparison.Ordinal))
                throw new InvalidOperationException("MapSources에 SSW NetGame 프리팹을 연결해야 합니다.");
            var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.assetPath == path && stage.scene.isDirty)
                throw new InvalidOperationException("NetGame 프리팹 편집 내용을 먼저 처리해야 합니다.");
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                NetGame game = root.GetComponent<NetGame>();
                var rotation = (MapRotation)new SerializedObject(game).FindProperty("_maps").objectReferenceValue;
                var settings = new SerializedObject(rotation);
                var array = settings.FindProperty("_maps");
                var maps = new List<BattleMap>();
                for (int i = 0; i < array.arraySize; i++)
                {
                    var map = (BattleMap)array.GetArrayElementAtIndex(i).objectReferenceValue;
                    if (map == null) throw new InvalidOperationException("기존 맵 로테이션에 누락 참조가 있습니다.");
                    maps.Add(map);
                }
                var fingerprints = new HashSet<string>(maps.Select(SourceOf).Where(item => item != null).Select(Fingerprint), StringComparer.Ordinal);
                bool changed = false;
                foreach (var entry in entries)
                {
                    if (!Numbers.Contains(entry.Number) || entry.Map == null || maps.Contains(entry.Map)) continue;
                    string fingerprint = Fingerprint(entry.Source);
                    if (!fingerprints.Add(fingerprint))
                    {
                        report.Add(entry.Number + "번 등록 보류: 기존 맵과 원본 내용 중복");
                        continue;
                    }
                    maps.Add(entry.Map);
                    changed = true;
                    report.Add(entry.Number + "번 로테이션 추가");
                }
                if (!changed) return;
                array.arraySize = maps.Count;
                for (int i = 0; i < maps.Count; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = maps[i];
                settings.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
                if (!success) throw new InvalidOperationException("NetGame 로테이션 저장 실패");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static GameObject SourceOf(BattleMap map)
        {
            for (int i = 0; i < map.transform.childCount; i++)
            {
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(map.transform.GetChild(i).gameObject);
                string path = AssetDatabase.GetAssetPath(source);
                if (path.StartsWith("Assets/MapPrefab/", StringComparison.Ordinal)
                    || path.StartsWith("Assets/KDH/GameModules/Maps/", StringComparison.Ordinal)) return source;
            }
            return null;
        }

        static string Fingerprint(GameObject source)
        {
            string path = AssetDatabase.GetAssetPath(source);
            string text = File.ReadAllText(path).Replace("\r\n", "\n");
            text = text.Replace("  m_Name: " + source.name + "\n", "  m_Name: <Map>\n");
            return Hash128.Compute(text).ToString();
        }

        static void OnPlay(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;
            try { BakeAll(); }
            catch (Exception error)
            {
                EditorApplication.isPlaying = false;
                Debug.LogError("맵 원본 갱신 실패로 Play를 중단했습니다: " + error.Message);
            }
        }
    }

    public sealed class MapBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder => -9000;
        public void OnPreprocessBuild(BuildReport report)
        {
            try { MapBake.BakeAll(); }
            catch (Exception error) { throw new BuildFailedException("맵 원본 갱신 실패: " + error.Message); }
        }
    }
}
