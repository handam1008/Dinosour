using SSW;
using UnityEngine;

namespace KDH.Scripts.Objects
{
    public class KDH_Volcano : MonoBehaviour
    {
        [SerializeField] private float damage;
        [SerializeField] private SoundCue volcanoSound;

        private ParticleSystem ps;
        private float _timer;

        void OnEnable()
        {
            ps = GetComponent<ParticleSystem>();
            _timer = 0f;
        }

        void LateUpdate()
        {
            float rate = ps.emission.rateOverTimeMultiplier;
            _timer += rate * Time.deltaTime;

            while (_timer >= 1f)
            {
                _timer -= 1f;
                if (NetGame.Current != null && volcanoSound != null)
                {
                    NetGame.Current.Sounds.Play(volcanoSound); // 사운드
                }
            }
        }

        void OnParticleCollision(GameObject other)
        {
            if (other.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(damage);
            }
        }
    }
}