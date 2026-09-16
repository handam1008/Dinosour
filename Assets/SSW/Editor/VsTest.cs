using UnityEditor;
using UnityEngine;

namespace SSW.Editor
{
    [InitializeOnLoad]
    static class VsTest
    {
        const string Pending = "Vs.Preview";
        static Vs _view;

        static VsTest()
        {
            EditorApplication.playModeStateChanged += Changed;
        }

        [MenuItem("Tools/VS 미리보기")]
        static void Open()
        {
            if (EditorApplication.isPlaying) Show();
            else
            {
                SessionState.SetBool(Pending, true);
                EditorApplication.isPlaying = true;
            }
        }

        static void Changed(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
            SessionState.SetBool(Pending, false);
            EditorApplication.delayCall += Show;
        }

        static void Show()
        {
            if (NetGame.Current != null && NetGame.Current.Connected) return;
            if (_view != null) Object.Destroy(_view.gameObject);
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            PlayerJob job = PlayerJobStorage.Load();
            PlayerJob other = job == PlayerJob.Magician ? PlayerJob.Witch : PlayerJob.Magician;
            _view = Object.Instantiate(Resources.Load<Vs>("UI/Vs"));
            _view.SetView(Camera.main);
            _view.Show(default, job, 1, default, other, 2, _view.Close);
        }
    }
}