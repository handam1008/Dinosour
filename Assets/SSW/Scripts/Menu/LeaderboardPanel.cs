using System;
using RYU._01.Script.Leaderboard;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace SSW
{
    public sealed class LeaderboardPanel : MonoBehaviour
    {
        [SerializeField] LeaderboardManager _scores;
        [SerializeField] UnityEngine.UI.Button _refresh;
        [SerializeField] UnityEngine.UI.ScrollRect _scroll;
        [SerializeField] TMP_Text _status;
        bool _loading;

        public async void Refresh()
        {
            if (_loading) return;
            _loading = true;
            _refresh.interactable = false;
            _scroll.content.gameObject.SetActive(false);
            ShowStatus("불러오는 중...");
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                    await UnityServices.InitializeAsync(new InitializationOptions().SetProfile(NetLaunch.Profile));
                if (!AuthenticationService.Instance.IsSignedIn)
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                if (this == null) return;
                await _scores.LoadAllScore();
                if (this == null) return;
                _scroll.content.gameObject.SetActive(true);
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_scroll.content);
                _scroll.verticalNormalizedPosition = 1f;
                ShowStatus(_scroll.content.childCount == 0 ? "아직 기록이 없습니다" : string.Empty);
            }
            catch (Exception exception)
            {
                if (this == null) return;
                ShowStatus("기록을 불러오지 못했습니다. 다시 시도해 주세요");
                Debug.LogWarning($"리더보드 조회 실패: {exception.Message}", this);
            }
            finally
            {
                _loading = false;
                if (this != null) _refresh.interactable = true;
            }
        }

        void ShowStatus(string value)
        {
            _status.text = value;
            _status.gameObject.SetActive(value.Length > 0);
        }
    }
}
