using System;
using UnityEngine;
using UnityEngine.UI;

namespace SSW
{
    public sealed class RoomPlayerUI : MonoBehaviour
    {
        [SerializeField] Text _name;
        [SerializeField] Text _job;
        [SerializeField] Button _kick;
        string _playerId;
        Action<string> _remove;

        void Awake() => _kick.onClick.AddListener(Kick);

        public void Bind(RoomPlayer player, JobCatalog jobs, bool canKick, Action<string> remove)
        {
            _playerId = player.Id;
            _remove = remove;
            _name.supportRichText = false;
            _name.text = player.Name + (player.IsHost ? "  (방장)" : "");
            _job.text = jobs.TryGet(player.Job, out JobDefinition job) ? job.KoreanName : "직업 확인 중...";
            _kick.gameObject.SetActive(!player.IsHost && canKick);
            gameObject.SetActive(true);
        }

        void Kick() => _remove?.Invoke(_playerId);
    }
}
