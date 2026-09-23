using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudCode;

namespace RYU._01.Script.Leaderboard
{
    [Serializable]
    public class MatchReportResult
    {
        public int delta;
        public double score;
    }

    public static class MatchReporter
    {
        private const string EndpointName = "ReportMatch";

        // 결과 화면에서 구독해서 +점수 표시에 쓰면 됨
        public static event Action<MatchReportResult> Reported;

        public static async Task<MatchReportResult> ReportAsync(bool isWin, int mySets, int opponentSets, string opponentId)
        {
            var args = new Dictionary<string, object>
            {
                { "isWin", isWin },
                { "mySets", mySets },
                { "opponentSets", opponentSets },
            };
            
            if (!string.IsNullOrEmpty(opponentId)) args["opponentId"] = opponentId;

            var result = await CloudCodeService.Instance.CallEndpointAsync<MatchReportResult>(EndpointName, args);
            Reported?.Invoke(result);
            return result;
        }
    }
}
