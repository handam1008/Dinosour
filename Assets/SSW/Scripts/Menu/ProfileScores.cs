using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RYU._01.Script.Leaderboard;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using UnityEngine;

namespace SSW
{
    public static class ProfileScores
    {
        static readonly Dictionary<string, ProfileScore> Scores = new Dictionary<string, ProfileScore>();
        public static event Action<string> Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            MatchReporter.Reported -= Reported;
            Scores.Clear();
            Changed = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Listen()
        {
            MatchReporter.Reported += Reported;
        }

        public static bool TryGet(string player, out double value)
        {
            if (Scores.TryGetValue(player, out ProfileScore score) && score.HasValue)
            {
                value = score.Value;
                return true;
            }
            value = 0d;
            return false;
        }

        public static async Task RefreshAsync(string player)
        {
            ProfileScore score = Get(player);
            int revision = score.BeginRead();
            var entry = await UnityServices.Instance.GetLeaderboardsService().GetPlayerScoreAsync("Ranking");
            if (score.CompleteRead(revision, entry.Score, entry.Rank)) Changed?.Invoke(player);
        }

        public static bool TryGetRank(string player, out int rank)
        {
            if (Scores.TryGetValue(player, out ProfileScore score) && score.Rank.HasValue && score.Rank.Value >= 0)
            {
                rank = score.Rank.Value;
                return true;
            }
            rank = -1;
            return false;
        }

        static void Reported(MatchReportResult result)
        {
            if (UnityServices.State != ServicesInitializationState.Initialized || !AuthenticationService.Instance.IsSignedIn) return;
            string player = AuthenticationService.Instance.PlayerId;
            Get(player).Report(result.score);
            Changed?.Invoke(player);
        }

        static ProfileScore Get(string player)
        {
            if (!Scores.TryGetValue(player, out ProfileScore score))
            {
                score = new ProfileScore();
                Scores.Add(player, score);
            }
            return score;
        }
    }
}
