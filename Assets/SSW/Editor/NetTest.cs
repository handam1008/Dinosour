using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SSW.Editor
{
    public sealed class NetTest : EditorWindow
    {
        [SerializeField] bool _pending;
        [SerializeField] int _clientId;
        string _error;
        static string Exe => Path.GetFullPath("Builds/Local/Game.exe");
        public int ClientId => _clientId;

        [MenuItem("Tools/대전 테스트")]
        static void Open()
        {
            GetWindow<NetTest>("대전 테스트");
        }

        void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlay;
        }

        void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlay;
        }

        void OnGUI()
        {
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || ClientOpen))
                if (GUILayout.Button("테스트 빌드")) Build();
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                if (GUILayout.Button("Play")) EditorApplication.isPlaying = true;
            using (new EditorGUI.DisabledScope(_pending || ClientOpen || !File.Exists(Exe) || EditorApplication.isCompiling))
                if (GUILayout.Button("클라이언트 열기")) OpenClient();
            if (_pending) EditorGUILayout.LabelField("Play 준비 중...");
            if (!string.IsNullOrEmpty(_error)) EditorGUILayout.HelpBox(_error, MessageType.Error);
        }

        bool ClientOpen
        {
            get
            {
                if (_clientId == 0) return false;
                try
                {
                    using Process process = Process.GetProcessById(_clientId);
                    return !process.HasExited && process.ProcessName == "Game";
                }
                catch (ArgumentException)
                {
                    _clientId = 0;
                    return false;
                }
            }
        }

        void Run(Action action)
        {
            _error = null;
            try { action(); }
            catch (Exception exception) { _error = exception.Message; }
            Repaint();
        }

        void OnPlay(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && _pending)
            {
                _pending = false;
                EditorApplication.delayCall += () => Run(Launch);
            }
            if (state == PlayModeStateChange.EnteredEditMode) _pending = false;
        }

        public void OpenClient()
        {
            Run(() =>
            {
                if (_pending || ClientOpen) return;
                if (!File.Exists(Exe)) throw new FileNotFoundException("먼저 테스트 빌드를 만들어 주세요.");
                if (!EditorApplication.isPlaying)
                {
                    _pending = true;
                    EditorApplication.isPlaying = true;
                    return;
                }
                Launch();
            });
        }

        void Launch()
        {
            if (!EditorApplication.isPlaying) return;
            Application.runInBackground = true;
            using Process process = Process.Start(new ProcessStartInfo
            {
                FileName = Exe,
                WorkingDirectory = Path.GetDirectoryName(Exe),
                Arguments = "--net-profile test-client -screen-fullscreen 0 -screen-width 960 -screen-height 600",
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Normal
            });
            _clientId = process.Id;
        }

        public static void Build()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Exe));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled && scene.path.StartsWith("Assets/SSW/") && File.Exists(scene.path)).Select(scene => scene.path).ToArray(),
                locationPathName = Exe,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report == null) throw new InvalidOperationException("Unity가 빌드를 시작하지 못했습니다.");
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("테스트 빌드 실패: " + report.summary.result);
            UnityEngine.Debug.Log("테스트 빌드: " + Exe);
        }
    }
}
