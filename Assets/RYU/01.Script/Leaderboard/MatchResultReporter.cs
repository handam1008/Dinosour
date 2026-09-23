using System;
using SSW;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace RYU._01.Script.Leaderboard
{
    
    public class MatchResultReporter : MonoBehaviour
    {
        private NetGame _game;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            var go = new GameObject(nameof(MatchResultReporter));
            DontDestroyOnLoad(go);
            go.AddComponent<MatchResultReporter>();
        }

        private void Update()
        {
            if (NetGame.Current == _game) return;

            if (_game != null) _game.Ended -= OnMatchEnded;
            _game = NetGame.Current;
            if (_game != null) _game.Ended += OnMatchEnded;
        }

        private void OnDestroy()
        {
            if (_game != null) _game.Ended -= OnMatchEnded;
        }

        private async void OnMatchEnded(MatchState state)
        {
            if (state.Reason != MatchEnd.Knockout) return;
            if (UnityServices.State != ServicesInitializationState.Initialized) return;
            if (!AuthenticationService.Instance.IsSignedIn) return;

            bool isFirst = _game.LocalId == state.First;
            int mySets = isFirst ? state.FirstSets : state.SecondSets;
            int opponentSets = isFirst ? state.SecondSets : state.FirstSets;
            bool isWin = state.Winner == _game.LocalId;
            string opponentId = MultiplayerSessionManager.Current != null ? MultiplayerSessionManager.Current.OpponentPlayerId : null;

            try
            {
                var result = await MatchReporter.ReportAsync(isWin, mySets, opponentSets, opponentId);
                Debug.Log($"[랭킹] {(isWin ? "승리" : "패배")} {mySets}:{opponentSets} → {result.delta:+#;-#;0}점, 현재 {result.score}점");
            }
            catch (Exception e)
            {
                Debug.LogError($"[랭킹] 점수 보고 실패: {e}");
            }
        }
    }
}
