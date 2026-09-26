using DG.Tweening;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace SSW
{
    public sealed class MapShake : NetworkBehaviour
    {
        [SerializeField] Transform _target;
        [SerializeField] Vector3 _axis = Vector3.one;
        [SerializeField] Vector2 _duration;
        [SerializeField] Vector2 _amount;
        [SerializeField] UnityEvent onShake = new UnityEvent();
        Sequence _sequence;

        public override void OnNetworkSpawn()
        {
            if (!IsServer || _duration.x <= 0f || _amount.x <= 0f) return;
            float duration = Random.Range(_duration.x, _duration.y);
            float amount = Random.Range(_amount.x, _amount.y);
            _sequence = DOTween.Sequence()
                .Append(_target.DOShakePosition(duration, _axis * amount))
                .AppendCallback(() => onShake.Invoke())
                .SetLoops(-1, LoopType.Restart)
                .SetLink(gameObject);
        }

        public override void OnNetworkDespawn()
        {
            _sequence?.Kill();
            _sequence = null;
        }

        void OnDisable()
        {
            _sequence?.Kill();
            _sequence = null;
        }
    }
}
