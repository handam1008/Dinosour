using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;

namespace SSW
{
    public interface IRankSource
    {
        Task<IReadOnlyList<LeaderboardEntry>> LoadAsync(CancellationToken cancellation);
    }

    public sealed class RankSource : IRankSource
    {
        static Task _signIn;

        public async Task<IReadOnlyList<LeaderboardEntry>> LoadAsync(CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync(new InitializationOptions().SetProfile(NetLaunch.Profile));
            cancellation.ThrowIfCancellationRequested();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                if (_signIn == null || _signIn.IsCompleted)
                    _signIn = AuthenticationService.Instance.SignInAnonymouslyAsync();
                await _signIn;
            }
            cancellation.ThrowIfCancellationRequested();
            var page = await UnityServices.Instance.GetLeaderboardsService()
                .GetScoresAsync("Ranking", new GetScoresOptions { Limit = 1000 });
            cancellation.ThrowIfCancellationRequested();
            return page.Results;
        }
    }
}
