using System;
using System.Collections;
using System.Collections.Generic;
using NKY.Lib.EventChannel;
using NKY.Lib.EventChannel.EventChannelAsset; // ¹Ù²Þ
using UnityEngine;

namespace SSW
{
    public class Health : MonoBehaviour, IDamageable, IHealable, IDamageReceiver
    {
        public float maxHealth = 100f;
        [SerializeField] bool _disableOnDeath = true;
        [SerializeField] float _deathDisableDelay;
        [SerializeField] string _healthBarResourceName = "HealthBarUI";
        [SerializeField] string _damageNumberResourceName = "DamageNumberUI";
        [SerializeField] float _healthBarPadding = 0.15f;
        [SerializeField] private DoubleFloatEventChannelSO healthChangeEvent;//¹Ù²Þ NKY
        [SerializeField] private VoidEventChannelSO hitEvent;//¹Ù²Þ NKY
        public event System.Action OnDamaged;
        public event System.Action OnDied;
        public event System.Action<float, float> OnHealthChanged;
        public event System.Action<float, bool> OnDamageDealt;

        float current;

        public float Current => current;
        public float Max => maxHealth;

        void Awake()
        {
            current = maxHealth;
            SpawnHealthBar();
            if(healthChangeEvent != null) OnHealthChanged += healthChangeEvent.ChangeTupleRaise; // ¹Ù²Þ NKY
            if(hitEvent != null) OnDamaged += hitEvent.Raise; //¹Ù²Þ NKY
        }

        private void OnDestroy() // Ãß°¡ÇÔ NKY
        {
            if(healthChangeEvent != null) OnHealthChanged -= healthChangeEvent.ChangeTupleRaise; // ¹Ù²Þ NKY
            if(hitEvent != null) OnDamaged -= hitEvent.Raise; //¹Ù²Þ NKY
        }

        public void TakeDamage(float amount)
        {
            TakeDamage(amount, false);
        }

        public void TakeDamage(float amount, bool isCritical)
        {
            ReceiveDamage(new DamageRequest(null, amount, DamageTag.None, isCritical));
        }

        public DamageResult ReceiveDamage(DamageRequest request)
        {
            float requestedAmount = Mathf.Max(0f, request.Amount);
            if (requestedAmount <= 0f)
                return new DamageResult(requestedAmount, 0f, false);
            if (current <= 0f)
                return new DamageResult(requestedAmount, 0f, false);

            IHealthNetworkBridge networkBridge = GetComponent<IHealthNetworkBridge>();
            if (networkBridge != null && networkBridge.TryForwardDamage(request, out DamageResult pendingResult))
                return pendingResult;

            float finalAmount = ResolveIncomingDamage(request, requestedAmount);
            if (finalAmount <= 0f)
            {
                DamageResult blocked = new DamageResult(requestedAmount, 0f, false);
                NotifyDamageReceived(request, blocked);
                return blocked;
            }

            float previous = current;
            current = Mathf.Max(current - finalAmount, 0f);
            float appliedAmount = previous - current;
            bool wasLethal = previous > 0f && current <= 0f;

            OnDamaged?.Invoke();
            OnHealthChanged?.Invoke(current, maxHealth);
            OnDamageDealt?.Invoke(finalAmount, request.IsCritical);
            SpawnDamageNumber(finalAmount, request.IsCritical);

            DamageResult result = new DamageResult(requestedAmount, appliedAmount, wasLethal);
            NotifyDamageReceived(request, result);
            if (wasLethal) Die();
            return result;
        }
        

        public void Heal(float amount)
        {
            if (amount <= 0f) return;

            IHealthNetworkBridge networkBridge = GetComponent<IHealthNetworkBridge>();
            if (networkBridge != null && networkBridge.TryForwardHeal(amount)) return;

            current = Mathf.Min(current + amount, maxHealth);
            OnHealthChanged?.Invoke(current, maxHealth);
        }

        internal void ApplyNetworkState(float value)
        {
            float previous = current;
            current = Mathf.Clamp(value, 0f, maxHealth);
            if (Mathf.Approximately(previous, current)) return;

            if (current < previous)
            {
                float damage = previous - current;
                OnDamaged?.Invoke();
                OnDamageDealt?.Invoke(damage, false);
                SpawnDamageNumber(damage, false);
            }

            OnHealthChanged?.Invoke(current, maxHealth);
            if (previous > 0f && current <= 0f) Die();
        }

        float ResolveIncomingDamage(DamageRequest request, float amount)
        {
            MonoBehaviour[] behaviours = GetComponentsInParent<MonoBehaviour>(true);
            List<IIncomingDamageModifier> modifiers = new List<IIncomingDamageModifier>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour.isActiveAndEnabled && behaviour is IIncomingDamageModifier modifier)
                    modifiers.Add(modifier);
            }

            modifiers.Sort(CompareModifiers);
            foreach (IIncomingDamageModifier modifier in modifiers)
                amount = Mathf.Max(0f, modifier.ModifyIncomingDamage(request, amount));

            return amount;
        }

        static int CompareModifiers(IIncomingDamageModifier left, IIncomingDamageModifier right)
        {
            int priority = left.Priority.CompareTo(right.Priority);
            return priority != 0
                ? priority
                : string.Compare(left.GetType().FullName, right.GetType().FullName, StringComparison.Ordinal);
        }

        void NotifyDamageReceived(DamageRequest request, DamageResult result)
        {
            MonoBehaviour[] behaviours = GetComponentsInParent<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour.isActiveAndEnabled && behaviour is IDamageReceivedListener listener)
                    listener.OnDamageReceived(request, result);
            }
        }

        void Die()
        {
            OnDied?.Invoke();
            if (!_disableOnDeath) return;
            if (_deathDisableDelay <= 0f)
            {
                gameObject.SetActive(false);
                return;
            }

            StartCoroutine(DisableAfterDeath());
        }

        IEnumerator DisableAfterDeath()
        {
            yield return new WaitForSeconds(_deathDisableDelay);
            gameObject.SetActive(false);
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
