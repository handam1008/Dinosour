using SSW;
using UnityEngine;

namespace KDH.Scripts.Objects
{
    public class KDH_TouchScreenOutline : MonoBehaviour
    {
        [SerializeField] private float damage;
        [SerializeField] private float knockbackForce;
        [SerializeField] private Vector2 direction;

        [SerializeField] private SoundCue hitSound;
        
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.TryGetComponent(out IDamageable damageable))
                ApplyDamage(damageable, damage);

            if (collision.gameObject.TryGetComponent(out IForceReceiver forceReceiver))
                ApplyForce(forceReceiver, direction * knockbackForce, ForceMode2D.Impulse);

            if (NetGame.Current != null && hitSound != null)
                NetGame.Current.Sounds.Play(hitSound); // 사운드
        }
        
        private void ApplyDamage(IDamageable damageable, float applyDamage)
        {
            damageable.TakeDamage(applyDamage);
        }

        private void ApplyForce(IForceReceiver forceReceiver, Vector2 applyDirection, ForceMode2D forceMode)
        {
            forceReceiver.ApplyForce(applyDirection, forceMode);
        }
    }
}
