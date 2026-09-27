if (!UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Play required");
string path = System.IO.Path.GetFullPath("Logs/AugmentLink27/RoomScore.json");
if (System.IO.File.Exists(path)) throw new System.InvalidOperationException("Preserve previous run");
async System.Threading.Tasks.Task Run()
{
    System.IO.File.WriteAllText("Logs/AugmentLink27/RoomStage.txt", "started");
    await System.Threading.Tasks.Task.Yield();
    var game = SSW.NetGame.GetOrCreate();
    var sessions = SSW.MultiplayerSessionManager.GetOrCreate();
    if (sessions.IsInSession || game.Connected) throw new System.InvalidOperationException("Existing session");
    string profile = null;
    bool signedIn = false;
    int ended = 0;
    int reported = 0;
    System.Action<SSW.MatchState> onEnd = _ => ended++;
    System.Action<RYU._01.Script.Leaderboard.MatchReportResult> onReport = _ => reported++;
    game.Ended += onEnd;
    RYU._01.Script.Leaderboard.MatchReporter.Reported += onReport;
    var checks = new System.Collections.Generic.List<string>();
    string error = null;
    string before = null;
    string after = null;
    async System.Threading.Tasks.Task<string> Score()
    {
        try
        {
            var value = await Unity.Services.Leaderboards.LeaderboardsService.Instance.GetPlayerScoreAsync("Ranking");
            return value.Score.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (Unity.Services.Leaderboards.Exceptions.LeaderboardsException exception)
        {
            if (exception.Reason == Unity.Services.Leaderboards.Exceptions.LeaderboardsExceptionReason.EntryNotFound) return "no entry";
            throw;
        }
    }
    try
    {
        if (Unity.Services.Core.UnityServices.State != Unity.Services.Core.ServicesInitializationState.Initialized)
            await Unity.Services.Core.UnityServices.InitializeAsync();
        System.IO.File.WriteAllText("Logs/AugmentLink27/RoomStage.txt", "services initialized");
        var auth = Unity.Services.Authentication.AuthenticationService.Instance;
        profile = auth.Profile;
        signedIn = auth.IsSignedIn;
        if (signedIn) auth.SignOut();
        auth.SwitchProfile("fixes27-room-score");
        await auth.SignInAnonymouslyAsync();
        System.IO.File.WriteAllText("Logs/AugmentLink27/RoomStage.txt", "test profile signed in");
        before = await Score();
        for (int i = 0; i < 2; i++)
        {
            await sessions.CreateRoomAsync(new SSW.MultiplayerRoomRequest("Codex room score " + i, "", true));
            await System.Threading.Tasks.Task.Delay(3000);
            if (!sessions.IsInSession || sessions.PlayerCount != 1) throw new System.InvalidOperationException("Solo room not created");
            checks.Add("solo room " + i + " created");
            System.IO.File.WriteAllText("Logs/AugmentLink27/RoomStage.txt", "room " + i + " created");
            await sessions.LeaveRoomAsync();
            await System.Threading.Tasks.Task.Delay(1000);
        }
        after = await Score();
        if (ended != 0 || reported != 0 || before != after) throw new System.InvalidOperationException("Room lifecycle changed score");
        checks.Add("no Ended or Reported event");
        checks.Add("UGS score unchanged");
        var invalid = new SSW.MatchState { Phase = SSW.MatchPhase.Finished, Reason = SSW.MatchEnd.Knockout, Winner = 0, First = 0, Second = 0, FirstSets = 4 };
        if (SSW.Rounds.Reportable(invalid)) throw new System.InvalidOperationException("Solo result accepted");
        invalid.Second = 1;
        invalid.FirstSets = 0;
        if (SSW.Rounds.Reportable(invalid)) throw new System.InvalidOperationException("Unplayed result accepted");
        invalid.FirstSets = 4;
        if (!SSW.Rounds.Reportable(invalid)) throw new System.InvalidOperationException("Completed result rejected");
        checks.Add("solo, incomplete, completed result eligibility");
    }
    catch (System.Exception exception) { error = exception.ToString(); }
    finally
    {
        try
        {
            if (sessions.IsInSession) await sessions.LeaveRoomAsync();
            if (profile != null)
            {
                var auth = Unity.Services.Authentication.AuthenticationService.Instance;
                if (auth.IsSignedIn) auth.SignOut();
                auth.SwitchProfile(profile);
                if (signedIn) await auth.SignInAnonymouslyAsync();
            }
        }
        catch (System.Exception exception) { error = (error ?? "") + "\nCleanup: " + exception; }
        game.Ended -= onEnd;
        RYU._01.Script.Leaderboard.MatchReporter.Reported -= onReport;
        System.IO.File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(new { error, before, after, ended, reported, checks, joined = sessions.IsInSession }, Newtonsoft.Json.Formatting.Indented));
    }
}
_ = Run();
return new { running = true, path };
