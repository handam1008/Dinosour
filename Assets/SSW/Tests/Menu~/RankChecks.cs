using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RYU._01.Script.Leaderboard;
using SSW;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards.Exceptions;
using Unity.Services.Leaderboards.Models;
using UnityEngine;
using UnityEngine.UI;

public static class RankChecks
{
    sealed class Source : IRankSource
    {
        readonly Func<Task<IReadOnlyList<LeaderboardEntry>>> _load;
        public Source(Func<Task<IReadOnlyList<LeaderboardEntry>>> load) { _load = load; }
        public Task<IReadOnlyList<LeaderboardEntry>> LoadAsync(CancellationToken cancellation) => _load();
    }

    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, Fields).GetValue(target);
    static void Set(object target, string field, object value) => target.GetType().GetField(field, Fields).SetValue(target, value);
    static IRankSource Data(params LeaderboardEntry[] entries) => new Source(() => Task.FromResult<IReadOnlyList<LeaderboardEntry>>(entries));

    public static async Task RunAsync()
    {
        var checks = new List<string>();
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException(name);
            checks.Add(name);
        }
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        var canvas = roots.Single(x => x.name == "Canvas").transform;
        var menu = roots.Single(x => x.name == "MainMenuController").GetComponent<MainMenuController>();
        var panel = canvas.Find("LeaderboardPanel").GetComponent<LeaderboardPanel>();
        var list = panel.GetComponent<RankList>();
        var status = Get<TMP_Text>(panel, "_status");
        var button = Get<Button>(panel, "_refresh");
        var scroll = Get<ScrollRect>(panel, "_scroll");
        var source = Get<IRankSource>(panel, "_source");
        float timeout = Get<float>(panel, "_timeout");
        float timeScale = Time.timeScale;
        string failure = null;
        int liveCount = 0;
        try
        {
            var live = await source.LoadAsync(CancellationToken.None);
            liveCount = live.Count;
            menu.ShowLeaderboard();
            while (Get<CancellationTokenSource>(panel, "_request") != null) await Task.Delay(20);
            Check(list.Count == live.Count, "live service row count");
            Check(status.text == (live.Count == 0 ? "아직 기록이 없습니다" : string.Empty), "live success status");
            var rows = scroll.content.GetComponentsInChildren<RankUserInfo>(true);
            for (int i = 0; i < rows.Length; i++)
            {
                Check(Get<TextMeshProUGUI>(rows[i], "_rank").text == (live[i].Rank + 1).ToString(), "live rank " + i);
                Check(Get<TextMeshProUGUI>(rows[i], "_score").text == live[i].Score.ToString(), "live score " + i);
                string name = live[i].PlayerName?.Split('#')[0];
                if (string.IsNullOrWhiteSpace(name)) name = live[i].PlayerId;
                Check(Get<TextMeshProUGUI>(rows[i], "_playerName").text == name, "live player " + i);
            }
            for (int i = 0; i < 3; i++)
            {
                menu.ShowMain();
                await Task.Delay(300);
                canvas.Find("MainPanel/Button_Leaderboard").GetComponent<Button>().onClick.Invoke();
                while (Get<CancellationTokenSource>(panel, "_request") != null) await Task.Delay(20);
                Check(list.Count == live.Count && scroll.content.childCount == live.Count, "reopen without duplicate " + i);
            }

            var own = new LeaderboardEntry(AuthenticationService.Instance.PlayerId, "OwnFixture#1234", 0, 75);
            Set(panel, "_source", Data(own));
            await panel.RefreshAsync();
            Check(list.Count == 1 && status.text == string.Empty, "own entry without title manager");

            Set(panel, "_source", Data(new LeaderboardEntry("fixture-player", null, 4, 2.5)));
            await panel.RefreshAsync();
            var unnamed = scroll.content.GetComponentInChildren<RankUserInfo>(true);
            Check(Get<TextMeshProUGUI>(unnamed, "_playerName").text == "fixture-player", "missing name uses player identifier");

            Set(panel, "_source", Data());
            await panel.RefreshAsync();
            Check(list.Count == 0 && scroll.content.childCount == 0 && status.text == "아직 기록이 없습니다", "empty after populated list");

            var reasons = new[] { LeaderboardsExceptionReason.NoInternetConnection, LeaderboardsExceptionReason.Unauthorized, LeaderboardsExceptionReason.ServiceUnavailable, LeaderboardsExceptionReason.LeaderboardNotFound, LeaderboardsExceptionReason.TooManyRequests };
            var expected = new[] { "네트워크", "로그인", "서비스", "설정", "요청이 많습니다" };
            for (int i = 0; i < reasons.Length; i++)
            {
                var error = (Exception)Activator.CreateInstance(typeof(LeaderboardsException), BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { reasons[i], 0, "Local failure fixture", null }, null);
                Set(panel, "_source", new Source(() => Task.FromException<IReadOnlyList<LeaderboardEntry>>(error)));
                await panel.RefreshAsync();
                Check(status.text.Contains(expected[i]) && button.interactable && !scroll.content.gameObject.activeSelf, "failure state " + reasons[i]);
            }

            var pending = new TaskCompletionSource<IReadOnlyList<LeaderboardEntry>>();
            Set(panel, "_source", new Source(() => pending.Task));
            Set(panel, "_timeout", 0.05f);
            Time.timeScale = 0f;
            await panel.RefreshAsync();
            Check(status.text.Contains("응답이 늦습니다") && button.interactable, "timeout while paused");
            pending.SetResult(new[] { own });
            await Task.Delay(100);
            Check(status.text.Contains("응답이 늦습니다") && list.Count == 0, "late response after timeout ignored");
            Time.timeScale = timeScale;
            Set(panel, "_timeout", timeout);

            var previous = new TaskCompletionSource<IReadOnlyList<LeaderboardEntry>>();
            Set(panel, "_source", new Source(() => previous.Task));
            Task abandoned = panel.RefreshAsync();
            panel.gameObject.SetActive(false);
            await abandoned;
            Check(button.interactable, "close cancels loading");
            panel.gameObject.SetActive(true);
            Set(panel, "_source", Data());
            await panel.RefreshAsync();
            previous.SetResult(new[] { own });
            await Task.Delay(100);
            Check(list.Count == 0 && status.text == "아직 기록이 없습니다", "old response cannot overwrite reopened panel");

            Set(panel, "_source", source);
            await panel.RefreshAsync();
            Check(list.Count == live.Count && button.interactable, "retry restores live records");
        }
        catch (Exception error)
        {
            failure = error.ToString();
        }
        finally
        {
            Time.timeScale = timeScale;
            Set(panel, "_source", source);
            Set(panel, "_timeout", timeout);
            panel.gameObject.SetActive(true);
            await panel.RefreshAsync();
            File.WriteAllText("Logs/Leaderboard26/checks.json", JsonConvert.SerializeObject(new { passed = failure == null, liveCount, checks, failure }, Formatting.Indented));
        }
    }
}
