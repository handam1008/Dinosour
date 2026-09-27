using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace SSW
{
    [DefaultExecutionOrder(100)]
    public sealed class ProfileTitle : MonoBehaviour
    {
        [SerializeField] KHG_TitleManager _source;
        string _player = "";

        public string Current
        {
            get
            {
                Refresh();
                return _source.GetCurrentTitle();
            }
        }

        void OnEnable() => ProfileScores.Changed += Changed;
        void OnDisable() => ProfileScores.Changed -= Changed;
        void Start() => Refresh();

        void Changed(string player)
        {
            if (UnityServices.State == ServicesInitializationState.Initialized
                && AuthenticationService.Instance.IsSignedIn && AuthenticationService.Instance.PlayerId == player) Refresh();
        }

        void Refresh()
        {
            string player = UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn
                ? AuthenticationService.Instance.PlayerId : "";
            if (_player != player)
            {
                _player = player;
                _source.equippedTitle = _source.eggTitle;
            }
            if (player.Length > 0 && ProfileScores.TryGetRank(player, out int rank)) _source.ChangeTierTitle(RankTier.Key(rank));
        }

        public async Task RefreshAsync()
        {
            Refresh();
            if (_player.Length == 0 || ProfileScores.TryGetRank(_player, out _)) return;
            try { await ProfileScores.RefreshAsync(_player); }
            catch (Exception) { }
            Refresh();
        }
    }
}
