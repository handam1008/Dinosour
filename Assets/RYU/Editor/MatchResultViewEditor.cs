using RYU._01.Script.Leaderboard;
using UnityEditor;
using UnityEngine;

namespace RYU.Editor
{
    // MatchResultView 인스펙터 아래에 결과 화면 테스트 버튼을 붙인다
    [CustomEditor(typeof(MatchResultView))]
    public class MatchResultViewEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("결과 화면 테스트 (가짜 데이터, 점수 저장 안 함)", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Play 모드에서 누를 수 있어요.", MessageType.Info);
                return;
            }

            var view = (MatchResultView)target;

            if (GUILayout.Button("승리 + 승급 (메테오 → 빙하기)")) view.TestPromote();
            if (GUILayout.Button("승리 + 첫 경기 (알급 → 원숭이)")) view.TestFirstMatch();
            if (GUILayout.Button("승리 (티어 유지)")) view.TestSameTier();
            if (GUILayout.Button("패배 + 강등 (메테오 → 마그마)")) view.TestDemote();

            EditorGUILayout.Space(4);
            if (GUILayout.Button("로딩 → 성공")) view.TestLoading(true);
            if (GUILayout.Button("로딩 → 실패 (서버 오류)")) view.TestLoading(false);
        }
    }
}
