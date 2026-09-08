using System.Collections;
using KDH.Scripts.Bullet;
using SSW;
using UnityEngine;
using UnityEngine.Events;

namespace KDH.Scripts.Upgrade.Instances.Bullets
{
    public class KDH_LightningBullet : KDH_AbstractBulletAbility
    {
        [Header("Bullet Settings")]
        [field: SerializeField] public KDH_BulletAbilityDataSO BulletAbilityData { get; private set; }
        [SerializeField] private GameObject lightningEffect;
        [SerializeField] private float damage = 50f;
        [SerializeField] private float hitWindow = 3f;
        [SerializeField] private int requiredHits = 3;
        [SerializeField] private float cooldown = 5f;
        private float timer;
        private bool _timerRunning;

        public int CountHit { get; private set; }
        private bool _skillReady;
        private bool _spawnFlag;

        private bool _onCooldown;
        private float _cooldownTimer;

        [SerializeField] private GameObject hitFlag;
        private KDH_LightningFlag lightningFlag;
        private GameObject flag;

        public Transform HitPoint { get; private set; }
        public KDH_Bullet Bullet { get; private set; }

        [SerializeField] private UnityEvent onHitPlayer;

        private void Update()
        {   
            if (_onCooldown)
            {
                _cooldownTimer += Time.deltaTime;
                if (_cooldownTimer >= cooldown)
                {
                    _onCooldown = false;
                    _cooldownTimer = 0f;
                }
            }

            if (!_timerRunning) return;

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                ResetCombo();
            }
        }

        private void ResetCombo(bool enterCooldown = false)
        {
            _timerRunning = false;
            timer = 0f;
            CountHit = 0;
            _skillReady = false;

            if (flag != null)
            {
                flag.SetActive(false);
            }
            _spawnFlag = false;

            if (enterCooldown)
            {
                _onCooldown = true;
                _cooldownTimer = 0f;
            }
        }

        public override void BulletAbility(Collider2D collision, KDH_Bullet bullet)
        {
            if (_onCooldown) return;

            HitPoint = collision.transform;
            Bullet = bullet;
            
            if (CountHit < requiredHits)
            {
                CountHit++;
            }

            if (!_spawnFlag)
            {
                _spawnFlag = true;
                flag = Instantiate(hitFlag, HitPoint);
                lightningFlag = flag.GetComponentInChildren<KDH_LightningFlag>();
            }
            else
            {
                flag.SetActive(true);
            }

            lightningFlag.Init(flag, CountHit);

            timer = hitWindow;
            _timerRunning = true;

            if (CountHit >= requiredHits)
            {
                _skillReady = true;
                StartCoroutine(GiveDamage(collision));
            }

            onHitPlayer?.Invoke();
        }

        private IEnumerator GiveDamage(Collider2D collision)
        {
            yield return new WaitForSeconds(1f);

            if (collision != null && collision.TryGetComponent(out IDamageable damageable))
            {
                GameObject effect = Instantiate(lightningEffect, collision.transform.position, Quaternion.identity);
                effect.transform.position -= new Vector3(0, 0.8f, 0);
                damageable.TakeDamage(damage);
                Destroy(effect, 0.4f);
            }

            ResetCombo(enterCooldown: true);
        }
    }
}