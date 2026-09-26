using System;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards.Exceptions;

namespace SSW
{
    public static class RankStatus
    {
        public static string Message(Exception exception)
        {
            if (exception is TimeoutException)
                return "응답이 늦습니다. 잠시 후 다시 시도해 주세요";
            if (exception is AuthenticationException)
                return "로그인 상태를 확인할 수 없습니다. 다시 시도해 주세요";
            if (exception is LeaderboardsException scores)
            {
                switch (scores.Reason)
                {
                    case LeaderboardsExceptionReason.NoInternetConnection:
                        return "네트워크 연결을 확인해 주세요";
                    case LeaderboardsExceptionReason.Unauthorized:
                    case LeaderboardsExceptionReason.AccessTokenMissing:
                    case LeaderboardsExceptionReason.PlayerIdMissing:
                        return "로그인 상태를 확인할 수 없습니다. 다시 시도해 주세요";
                    case LeaderboardsExceptionReason.ProjectIdMissing:
                    case LeaderboardsExceptionReason.LeaderboardNotFound:
                        return "리더보드 설정을 확인할 수 없습니다";
                    case LeaderboardsExceptionReason.TooManyRequests:
                        return "요청이 많습니다. 잠시 후 다시 시도해 주세요";
                    default:
                        return "리더보드 서비스에 연결할 수 없습니다. 다시 시도해 주세요";
                }
            }
            if (exception is ServicesInitializationException || exception is RequestFailedException)
                return "서비스 연결을 확인할 수 없습니다. 다시 시도해 주세요";
            return "기록을 표시하지 못했습니다. 다시 시도해 주세요";
        }
    }
}
