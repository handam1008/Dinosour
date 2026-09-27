using System;
using RYU._01.Script.Leaderboard;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using UnityEngine;

namespace SSW
{
    public sealed class ProfileInfo : MonoBehaviour
    {
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _score;
        int _revision;

        void OnEnable()
        {
            MatchReporter.Reported += Reported;
            Refresh();
        }

        void OnDisable()
        {
            MatchReporter.Reported -= Reported;
            _revision++;
        }

        async void Refresh()
        {
            int revision = ++_revision;
            if (UnityServices.State != ServicesInitializationState.Initialized || !AuthenticationService.Instance.IsSignedIn)
            {
                _name.SetText("플레이어");
                _score.SetText("-");
                return;
            }
            string player = AuthenticationService.Instance.PlayerId;
            string name = AuthenticationService.Instance.PlayerName;
            _name.SetText(string.IsNullOrWhiteSpace(name) ? "플레이어" : name.Split('#')[0]);
            try
            {
                var entry = await UnityServices.Instance.GetLeaderboardsService().GetPlayerScoreAsync("Ranking");
                if (this != null && isActiveAndEnabled && revision == _revision
                    && AuthenticationService.Instance.IsSignedIn && AuthenticationService.Instance.PlayerId == player)
                    _score.SetText(entry.Score.ToString("0.##") + "점");
            }
            catch (Exception)
            {
                if (this != null && revision == _revision) _score.SetText("-");
            }
        }

        void Reported(MatchReportResult result)
        {
            _revision++;
            _score.SetText(result.score.ToString("0.##") + "점");
        }
    }
}
