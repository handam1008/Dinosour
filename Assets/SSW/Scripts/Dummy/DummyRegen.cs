using UnityEngine;
using DG.Tweening;

namespace SSW
{
    public class DummyRegen : MonoBehaviour
    {
        [SerializeField] float _regenDelay = 2f;
        [SerializeField] Animator _animator;

        Health _health;
        int _gen;

        void Awake()
        {
            _health = GetComponent<Health>();
            _health.OnDamaged += HandleDamaged;
        }

        void HandleDamaged()
        {
            if (_animator != null) _animator.SetTrigger("Hit");

            _gen++;
            int gen = _gen;
            DOVirtual.DelayedCall(_regenDelay, () =>
            {
                if (gen != _gen) return;
                _health.Heal(_health.Max);
            }).SetLink(gameObject).SetUpdate(false);
        }

        void OnDestroy()
        {
            _health.OnDamaged -= HandleDamaged;
        }
    }
}
