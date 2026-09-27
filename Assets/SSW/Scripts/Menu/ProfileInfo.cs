using System;
using System.Collections;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace SSW
{
    public sealed class ProfileInfo : MonoBehaviour
    {
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _score;
        IAuthenticationService _auth;
        string _player;
        int _revision;

        void OnEnable()
        {
            ProfileScores.Changed += Changed;
            _name.SetText("플레이어");
            _score.SetText("-");
            StartCoroutine(Connect());
        }

        void OnDisable()
        {
            ProfileScores.Changed -= Changed;
            StopAllCoroutines();
            if (_auth != null)
            {
                _auth.SignedIn -= Refresh;
                _auth.SignedOut -= Refresh;
                _auth.Expired -= Refresh;
            }
            _auth = null;
            _player = null;
            ++_revision;
        }

        IEnumerator Connect()
        {
            while (UnityServices.State != ServicesInitializationState.Initialized) yield return null;
            _auth = AuthenticationService.Instance;
            _auth.SignedIn += Refresh;
            _auth.SignedOut += Refresh;
            _auth.Expired += Refresh;
            Refresh();
        }

        void OnApplicationFocus(bool focused)
        {
            if (focused && isActiveAndEnabled && _auth != null) Refresh();
        }

        public async void Refresh()
        {
            int revision = ++_revision;
            if (_auth == null || !_auth.IsSignedIn)
            {
                _player = null;
                _name.SetText("플레이어");
                _score.SetText("-");
                return;
            }
            _player = _auth.PlayerId;
            string player = _player;
            string name = _auth.PlayerName;
            _name.SetText(string.IsNullOrWhiteSpace(name) ? "플레이어" : name.Split('#')[0]);
            ShowScore();
            try
            {
                await ProfileScores.RefreshAsync(player);
            }
            catch (Exception)
            {
                if (this != null && isActiveAndEnabled && revision == _revision && _player == player) ShowScore();
            }
        }

        void Changed(string player)
        {
            if (player == _player) ShowScore();
        }

        void ShowScore() => _score.SetText(ProfileScores.TryGet(_player, out double value) ? value.ToString("0.##") + "점" : "-");
    }
}
