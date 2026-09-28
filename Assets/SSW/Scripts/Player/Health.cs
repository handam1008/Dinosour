using System;
using System.Collections;
using System.Collections.Generic;
using NKY.Lib.EventChannel;
using NKY.Lib.EventChannel.EventChannelAsset; // �ٲ�
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
        [SerializeField] SpriteRenderer _numberAnchor;
        [SerializeField] private DoubleFloatEventChannelSO healthChangeEvent;//�ٲ� NKY
        [SerializeField] private VoidEventChannelSO hitEvent;//�ٲ� NKY
        
        [Header("sound")]
        [SerializeField] SoundCue healingSoundCue;
        [SerializeField] SoundCue hitSoundCue;
        [SerializeField] SoundCue deadSoundCue;
        public event System.Action OnDamaged;
        public event System.Action OnDied;
        public event System.Action<float, float> OnHealthChanged;
        public event System.Action<float, bool> OnDamageDealt;
        public event System.Action<float> OnHealed;

        float current;
        IHealthAuthority _authority;
        Collider2D _shape;
        public VoidEventChannelSO HitEvent { get; private set; }
        public DoubleFloatEventChannelSO HealthChangeEvent { get; private set; }

        public float Current => current;
        public float Max => maxHealth;
        public Vector3 LabelPosition
        {
            get
            {
                if (_numberAnchor == null && _shape == null) return transform.position + Vector3.up;
                Bounds bounds = _numberAnchor != null ? _numberAnchor.bounds : _shape.bounds;
                return new Vector3(bounds.center.x, bounds.max.y + _healthBarPadding, bounds.center.z);
            }
        }

        void Awake()
        {
            _authority = GetComponent<IHealthAuthority>();
            _shape = GetComponent<Collider2D>();
            current = maxHealth;
            SpawnHealthBar();
            HitEvent = hitEvent != null ? Instantiate(hitEvent) : null;
            HealthChangeEvent = healthChangeEvent != null ? Instantiate(healthChangeEvent) : null;
            if(HealthChangeEvent != null) OnHealthChanged += HealthChangeEvent.ChangeTupleRaise; // �ٲ� NKY
            if(HitEvent != null) OnDamaged += HitEvent.Raise; //�ٲ� NKY
        }

        private void OnDestroy() // �߰��� NKY
        {
            if(HealthChangeEvent != null) OnHealthChanged -= HealthChangeEvent.ChangeTupleRaise; // �ٲ� NKY
            if(HitEvent != null) OnDamaged -= HitEvent.Raise; //�ٲ� NKY
            Destroy(HitEvent);
            Destroy(HealthChangeEvent);
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
            if (!float.IsFinite(request.Amount) || _authority != null && !_authority.CanChange) return default;
            float requestedAmount = Mathf.Max(0f, request.Amount);
            if (requestedAmount <= 0f)
                return new DamageResult(requestedAmount, 0f, false);
            if (current <= 0f)
                return new DamageResult(requestedAmount, 0f, false);

            float finalAmount = ResolveIncomingDamage(request, requestedAmount);
            if (finalAmount <= 0f)
            {
                DamageResult blocked = new DamageResult(requestedAmount, 0f, false);
                NotifyDamageReceived(request, blocked);
                return blocked;
            }

            foreach (MonoBehaviour behaviour in GetComponentsInParent<MonoBehaviour>(true))
            {
                if (!behaviour.isActiveAndEnabled || behaviour is not IDamageDelay delay || !delay.TryDefer(request, finalAmount)) continue;
                DamageResult deferred = new DamageResult(requestedAmount, 0f, false, true);
                NotifyDamageReceived(request, deferred);
                return deferred;
            }

            float previous = current;
            current = Mathf.Max(current - finalAmount, 0f);
            float appliedAmount = previous - current;
            bool wasLethal = previous > 0f && current <= 0f;

            OnDamaged?.Invoke();
            OnHealthChanged?.Invoke(current, maxHealth);
            OnDamageDealt?.Invoke(finalAmount, request.IsCritical);
            SpawnNumber(finalAmount, request.IsCritical);

            DamageResult result = new DamageResult(requestedAmount, appliedAmount, wasLethal);
            NotifyDamageReceived(request, result);
            NetGame.Current.Sounds.Play(hitSoundCue);
            if (wasLethal) Die();
            return result;
        }
        

        public void Heal(float amount)
        {
            if (!float.IsFinite(amount) || amount <= 0f) return;
            if (_authority != null && (current <= 0f || !_authority.CanChange)) return;
            if(Mathf.Approximately(Max, Current)) return;
            
            float previous = current;
            current = Mathf.Min(current + amount, maxHealth);
            float restored = current - previous;
            if (restored <= 0f) return;
            GameAudio.Current.PlaySfx(healingSoundCue);
            OnHealthChanged?.Invoke(current, maxHealth);
            ApplyNetworkHeal(restored);
        }

        internal void SetMax(float value)
        {
            float ratio = current / maxHealth;
            maxHealth = Mathf.Max(1f, value);
            current = Mathf.Clamp(ratio * maxHealth, 0f, maxHealth);
            OnHealthChanged?.Invoke(current, maxHealth);
        }

        internal void ApplyNetworkState(float value, float maximum)
        {
            float previous = current;
            float previousMax = maxHealth;
            maxHealth = maximum;
            current = Mathf.Clamp(value, 0f, maxHealth);
            if (Mathf.Approximately(previous, current) && Mathf.Approximately(previousMax, maximum)) return;

            OnHealthChanged?.Invoke(current, maxHealth);
            if (previous > 0f && current <= 0f) Die();
        }

        internal void ApplyNetworkDamage(float amount, bool critical)
        {
            OnDamaged?.Invoke();
            OnDamageDealt?.Invoke(amount, critical);
            SpawnNumber(amount, critical);
        }

        internal void ApplyNetworkHeal(float amount)
        {
            OnHealed?.Invoke(amount);
            SpawnNumber(amount, false, true);
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
            NetGame.Current.Sounds.Play(deadSoundCue);
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

            GameObject bar = Instantiate(barPrefab, LabelPosition, Quaternion.identity, transform);
            bar.transform.localScale = barPrefab.transform.localScale;
        }

        void SpawnNumber(float amount, bool isCritical, bool healed = false)
        {
            GameObject numberPrefab = Resources.Load<GameObject>(_damageNumberResourceName);
            if (numberPrefab == null) return;

            GameObject numberGo = Instantiate(numberPrefab, LabelPosition, Quaternion.identity);
            numberGo.transform.localScale = numberPrefab.transform.localScale;
            DamageNumberDisplay display = numberGo.GetComponent<DamageNumberDisplay>();
            if (display == null) return;
            if (healed) display.ShowHeal(amount);
            else display.Show(amount, isCritical);
        }

    }
}
