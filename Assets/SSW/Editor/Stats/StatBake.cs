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
    public static class StatBake
    {
        sealed class Plan
        {
            public StatSources Source;
            public FighterStats[] Stats;
            public StatPotions Potions;
            public string Report;
        }

        static bool _queued;
        static bool _baking;
        static HashSet<string> _dependencies;

        static StatBake()
        {
            EditorApplication.playModeStateChanged += OnPlay;
            Queue();
        }

        public static string Bake(StatSources sources)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Play 중에는 기본 수치를 베이크하지 않습니다.");
            return Apply(Prepare(sources));
        }

        public static string BakeAll(bool required = true)
        {
            if (_baking) throw new InvalidOperationException("기본 수치 베이크가 이미 진행 중입니다.");
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Play 중에는 기본 수치를 베이크하지 않습니다.");
            _baking = true;
            try
            {
                string[] paths = AssetDatabase.FindAssets("t:StatSources", new[] { "Assets/SSW" })
                    .Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path, StringComparer.Ordinal).ToArray();
                if (required && paths.Length == 0) throw new InvalidOperationException("StatSources 원본 설정 에셋이 없습니다.");
                var plans = new List<Plan>();
                var books = new HashSet<StatsBook>();
                var stocks = new HashSet<NetStock>();
                foreach (string path in paths)
                {
                    StatSources source = AssetDatabase.LoadAssetAtPath<StatSources>(path);
                    if (!books.Add(source.Book) || !stocks.Add(source.Stock))
                        throw new InvalidOperationException(path + ": 같은 출력 에셋을 여러 StatSources가 갱신할 수 없습니다.");
                    plans.Add(Prepare(source));
                }
                _dependencies = new HashSet<string>(paths, StringComparer.Ordinal);
                foreach (string path in paths)
                    _dependencies.UnionWith(AssetDatabase.GetDependencies(path, true));
                foreach (Plan plan in plans) _dependencies.Remove(AssetDatabase.GetAssetPath(plan.Source.Book));
                return string.Join("\n", plans.Select(Apply));
            }
            finally
            {
                _baking = false;
            }
        }

        static Plan Prepare(StatSources source)
        {
            if (source == null) throw new InvalidOperationException("StatSources 참조가 없습니다.");
            Output(source.Book);
            Output(source.Stock);
            StatSources.Entry[] entries = source.Entries.OrderBy(entry => (int)entry.Job).ToArray();
            var jobs = new HashSet<PlayerJob>();
            var values = new List<FighterStats>();
            var reports = new List<string>();
            AbstractPotion[] potions = null;
            foreach (StatSources.Entry entry in entries)
            {
                if (entry.Job == PlayerJob.None || !jobs.Add(entry.Job))
                    throw new InvalidOperationException(source.name + ": 직업이 None이거나 중복됐습니다: " + entry.Job);
                StatRead read;
                try
                {
                    read = StatRead.Read(entry);
                }
                catch (Exception error)
                {
                    throw new InvalidOperationException(source.name + "/" + entry.Job + ": " + error.Message, error);
                }
                values.Add(read.Stats);
                reports.Add(read.Report);
                if (entry.Job == PlayerJob.Witch) potions = read.Potions;
            }
            foreach (PlayerJob job in Enum.GetValues(typeof(PlayerJob)))
            {
                if (job != PlayerJob.None && !jobs.Contains(job))
                    throw new InvalidOperationException(source.name + ": 원본 직업이 빠졌습니다: " + job);
            }
            var stock = new StatPotions(source.Stock, potions ?? throw new InvalidOperationException("Witch 원본 목록이 없습니다."));
            reports.Add(stock.Report);
            return new Plan { Source = source, Stats = values.ToArray(), Potions = stock, Report = string.Join("\n", reports) };
        }

        static string Apply(Plan plan)
        {
            bool changed = plan.Source.Book.Replace(plan.Stats);
            if (changed)
            {
                EditorUtility.SetDirty(plan.Source.Book);
                AssetDatabase.SaveAssetIfDirty(plan.Source.Book);
            }
            changed |= plan.Potions.Apply();
            return plan.Source.name + (changed ? ": 갱신\n" : ": 동일\n") + plan.Report;
        }

        static void Output(UnityEngine.Object asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            if (asset == null || !path.StartsWith("Assets/SSW/", StringComparison.Ordinal))
                throw new InvalidOperationException("베이크 출력 에셋은 Assets/SSW 안에 명시적으로 지정해야 합니다.");
        }

        public static void Imported(IEnumerable<string> paths)
        {
            if (_baking) return;
            if (_dependencies == null || paths.Any(path => _dependencies.Contains(path)
                || path.EndsWith("/StatSources.asset", StringComparison.Ordinal))) Queue();
        }

        public static void Queue()
        {
            if (_queued) return;
            _queued = true;
            EditorApplication.delayCall += Deferred;
        }

        static void Deferred()
        {
            _queued = false;
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                Queue();
                return;
            }
            try
            {
                BakeAll(false);
            }
            catch (Exception error)
            {
                Debug.LogError("기본 수치 자동 베이크 실패: " + error.Message);
            }
        }

        static void OnPlay(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                Queue();
                return;
            }
            if (state != PlayModeStateChange.ExitingEditMode) return;
            try
            {
                BakeAll();
            }
            catch (Exception error)
            {
                EditorApplication.isPlaying = false;
                Debug.LogError("기본 수치 베이크 실패로 Play를 중단했습니다: " + error.Message);
            }
        }
    }

    public sealed class StatBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder => -10000;

        public void OnPreprocessBuild(BuildReport report)
        {
            try
            {
                StatBake.BakeAll();
            }
            catch (Exception error)
            {
                throw new BuildFailedException("기본 수치 베이크 실패: " + error.Message);
            }
        }
    }

    public sealed class StatImports : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] previous)
            => StatBake.Imported(imported.Concat(deleted).Concat(moved).Concat(previous));
    }
}
