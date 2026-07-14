using UnityEngine;

namespace SSW
{
    public class Health : MonoBehaviour, IDamageable
    {
        public float maxHealth = 100f;
        [SerializeField] bool _disableOnDeath = true;
        [SerializeField] string _healthBarResourceName = "HealthBarUI";
        [SerializeField] string _damageNumberResourceName = "DamageNumberUI";
        [SerializeField] float _healthBarPadding = 0.15f;
        public event System.Action OnDamaged;
        public event System.Action<float, float> OnHealthChanged;
        public event System.Action<float, bool> OnDamageDealt;

        float current;

        public float Current => current;
        public float Max => maxHealth;

        void Awake()
        {
            current = maxHealth;
            SpawnHealthBar();
        }

        public void TakeDamage(float amount)
        {
            TakeDamage(amount, false);
        }

        public void TakeDamage(float amount, bool isCritical)
        {
            if (amount <= 0f) return;
            current = Mathf.Max(current - amount, 0f);
            OnDamaged?.Invoke();
            OnHealthChanged?.Invoke(current, maxHealth);
            OnDamageDealt?.Invoke(amount, isCritical);
            SpawnDamageNumber(amount, isCritical);
            if (current <= 0f) Die();
        }

        public void Heal(float amount)
        {
            if (amount <= 0f) return;
            current = Mathf.Min(current + amount, maxHealth);
            OnHealthChanged?.Invoke(current, maxHealth);
        }

        void Die()
        {
            if (_disableOnDeath) gameObject.SetActive(false);
        }

        void SpawnHealthBar()
        {
            GameObject barPrefab = Resources.Load<GameObject>(_healthBarResourceName);
            if (barPrefab == null) return;

            Vector3 localPos = ComputeTopCenter();
            GameObject bar = Instantiate(barPrefab, transform);
            bar.transform.localPosition = localPos;
        }

        void SpawnDamageNumber(float amount, bool isCritical)
        {
            GameObject numberPrefab = Resources.Load<GameObject>(_damageNumberResourceName);
            if (numberPrefab == null) return;

            Vector3 spawnPos = transform.TransformPoint(ComputeTopCenter());
            GameObject numberGo = Instantiate(numberPrefab, spawnPos, Quaternion.identity);
            numberGo.transform.localScale = numberPrefab.transform.localScale;
            DamageNumberDisplay display = numberGo.GetComponent<DamageNumberDisplay>();
            if (display != null) display.Show(amount, isCritical);
        }

        Vector3 ComputeTopCenter()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Vector3(0f, 1f, 0f);

            Bounds combined = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) combined.Encapsulate(renderers[i].bounds);

            Vector3 worldTopCenter = new Vector3(combined.center.x, combined.max.y + _healthBarPadding, combined.center.z);
            return transform.InverseTransformPoint(worldTopCenter);
        }
    }
}
