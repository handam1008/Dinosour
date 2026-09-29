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
        public int rank = -1;   // 서버가 새 순위를 안 보내면 -1 (옛 Cloud Code)
    }

    public static class MatchReporter
    {
        private const string EndpointName = "ReportMatch";

        // 결과 화면에서 구독해서 +점수 표시에 쓰면 됨
        public static event Action<MatchReportResult> Reported;
        // 실패하면 결과 화면이 기다리지 않고 바로 알 수 있게
        public static event Action<Exception> Failed;

        public static async Task<MatchReportResult> ReportAsync(bool isWin, int mySets, int opponentSets, string opponentId)
        {
            var args = new Dictionary<string, object>
            {
                { "isWin", isWin },
                { "mySets", mySets },
                { "opponentSets", opponentSets },
            };

            if (!string.IsNullOrEmpty(opponentId)) args["opponentId"] = opponentId;

            try
            {
                var result = await CallAsync(args);
                Reported?.Invoke(result);
                return result;
            }
            catch (Exception e)
            {
                Failed?.Invoke(e);
                throw;
            }
        }

        // 서버가 스크립트를 아예 못 돌린 경우(503)·요청 과다(429)만 한 번 더 시도한다.
        // 스크립트가 실행됐을 수 있는 다른 오류는 점수가 두 번 들어갈 수 있으니 재시도하지 않는다
        private static async Task<MatchReportResult> CallAsync(Dictionary<string, object> args)
        {
            try
            {
                return await CloudCodeService.Instance.CallEndpointAsync<MatchReportResult>(EndpointName, args);
            }
            catch (CloudCodeException e) when (IsTransient(e))
            {
                await Task.Delay(1000);
                return await CloudCodeService.Instance.CallEndpointAsync<MatchReportResult>(EndpointName, args);
            }
        }

        private static bool IsTransient(CloudCodeException e)
        {
            string message = e.Message ?? string.Empty;
            return message.Contains("503") || message.Contains("ServiceUnavailable")
                || message.Contains("429") || message.Contains("RateLimit");
        }
    }
}
