using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SSW
{
    [DisallowMultipleComponent]
    public sealed class MultiplayerRoomRowUI : MonoBehaviour
    {
        [SerializeField] Text _roomNameText;
        [SerializeField] Text _playerCountText;
        [SerializeField] Button _joinButton;

        MultiplayerRoomInfo _room;
        Action<MultiplayerRoomInfo> _join;

        void Awake()
        {
            _joinButton.onClick.AddListener(Join);
        }

        public void Bind(MultiplayerRoomInfo room, Action<MultiplayerRoomInfo> join)
        {
            _room = room;
            _join = join;
            _roomNameText.text = room.Name + (room.HasPassword ? "  [잠금]" : string.Empty);
            _playerCountText.text = $"{room.PlayerCount}/2";

            transform.DOKill();
            transform.localScale = Vector3.zero;
            transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
        }

        void Join()
        {
            _join?.Invoke(_room);
        }

        public void SetBusy(bool busy)
        {
            _joinButton.interactable = !busy;
        }

        void OnDestroy()
        {
            transform.DOKill();
        }
    }
}
