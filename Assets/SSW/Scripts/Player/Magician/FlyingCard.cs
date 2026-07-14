using UnityEngine;
using DG.Tweening;

namespace SSW
{
    public class FlyingCard : MonoBehaviour
    {
        [SerializeField] float _baseDamage = 5f;
        [SerializeField] float _knockbackForce = 4f;

        [SerializeField] float _spadeDamagePerNumber = 1f;

        [SerializeField] float _heartHealPerNumber = 1f;

        [SerializeField] float _diamondDamagePerNumber = 1f;
        [SerializeField] float _diamondRadius = 1.5f;

        [SerializeField] float _cloverSlowPerNumber = 0.05f;
        [SerializeField] float _cloverSlowDuration = 1.5f;

        Suit _suit;
        int _number;
        Health _casterHealth;
        MagicianAugmentController _augments;
        Transform _caster;
        Rigidbody2D _rb;
        float _effectMultiplier = 1f;
        float _lifetime = 3f;
        float _age;
        bool _isJoker;
        bool _isMirror;
        bool _returning;
        bool _consumed;

        public void Configure(Suit suit, int number, Health casterHealth)
        {
            _suit = suit;
            _number = number;
            _casterHealth = casterHealth;
        }

        public void SetAugments(MagicianAugmentController augments, Transform caster)
        {
            _augments = augments;
            _caster = caster;
        }

        public void SetLifetime(float lifetime)
        {
            _lifetime = lifetime;
        }

        public void SetEffectMultiplier(float multiplier)
        {
            _effectMultiplier = multiplier;
        }

        public void MarkJoker()
        {
            _isJoker = true;
        }

        public void MarkMirror()
        {
            _isMirror = true;
        }

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        void Update()
        {
            if (_consumed) return;

            _age += Time.deltaTime;

            if (_returning && _caster != null && Vector2.Distance(transform.position, _caster.position) < 0.35f)
            {
                _consumed = true;
                Destroy(gameObject);
                return;
            }

            if (_age >= _lifetime) HandleMiss();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_consumed) return;

            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable == null)
            {
                if (_returning) return;
                HandleMiss();
                return;
            }

            if ((Object)damageable == _casterHealth) return;

            HitTarget(other, damageable);
        }

        void HandleMiss()
        {
            if (_consumed) return;

            if (!_returning && !_isMirror && _caster != null && _augments != null && _augments.Has(MagicianAugmentType.ReturnCard))
            {
                BeginReturn();
                return;
            }

            _consumed = true;
            if (_augments != null && !_isMirror) _augments.RegisterMiss();
            Destroy(gameObject);
        }

        void BeginReturn()
        {
            _returning = true;
            _age = 0f;

            float speed = 8f;
            if (_rb != null)
            {
                speed = Mathf.Max(_rb.linearVelocity.magnitude, speed);
                _rb.linearVelocity = Vector2.zero;
                _rb.gravityScale = 0f;
                _rb.angularVelocity = 240f;
            }

            FlyingCard self = this;
            DOVirtual.DelayedCall(_augments.ReturnDelay, () =>
            {
                if (self == null || self._consumed || self._caster == null) return;
                Vector2 dir = ((Vector2)self._caster.position - (Vector2)self.transform.position).normalized;
                if (self._rb != null) self._rb.linearVelocity = dir * speed;
            }, false);
        }

        void HitTarget(Collider2D other, IDamageable damageable)
        {
            _consumed = true;

            float chainMultiplier = _augments != null ? _augments.GetChainMultiplier(_suit) : 1f;
            float effectMul = _effectMultiplier * chainMultiplier;
            float damageScale = _returning && _augments != null ? _augments.ReturnDamageMultiplier : 1f;

            float damage = _baseDamage * _effectMultiplier * damageScale;
            if (_suit == Suit.Spade)
            {
                damage += _number * _spadeDamagePerNumber * effectMul * damageScale;
                QueueSharpCardBonus(damageable);
            }
            damageable.TakeDamage(damage);

            if (_suit != Suit.Spade) ApplySuitEffect(_suit, other, damageable, effectMul, damageScale);
            if (_isJoker) ApplySuitEffect(RandomOtherSuit(_suit), other, damageable, effectMul, damageScale);

            if (_augments != null && !_isMirror)
            {
                _augments.RegisterHit(_suit);
                if (!_returning && _rb != null)
                    _augments.TryQueueMirror(_suit, _number, _rb.linearVelocity.normalized);
            }

            Rigidbody2D targetRb = other.attachedRigidbody;
            if (targetRb != null && _rb != null)
                targetRb.AddForce(_rb.linearVelocity.normalized * _knockbackForce, ForceMode2D.Impulse);

            Destroy(gameObject);
        }

        void ApplySuitEffect(Suit suit, Collider2D target, IDamageable damageable, float effectMul, float damageScale)
        {
            switch (suit)
            {
                case Suit.Spade:
                    damageable.TakeDamage(_number * _spadeDamagePerNumber * effectMul * damageScale);
                    QueueSharpCardBonus(damageable);
                    break;

                case Suit.Heart:
                    if (_casterHealth != null)
                    {
                        float heal = _number * _heartHealPerNumber * effectMul;
                        if (_augments != null) heal += _augments.ConsumeEmergencyHealBonus();
                        _casterHealth.Heal(heal);
                    }
                    break;

                case Suit.Diamond:
                {
                    float radius = _diamondRadius * (_augments != null ? _augments.DiamondRadiusMultiplier : 1f);
                    float damage = _number * _diamondDamagePerNumber * effectMul * damageScale;
                    Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
                    foreach (Collider2D hit in hits)
                    {
                        IDamageable hitDamageable = hit.GetComponent<IDamageable>();
                        if (hitDamageable == null || (Object)hitDamageable == _casterHealth) continue;

                        hitDamageable.TakeDamage(damage);

                        if (_augments != null && _augments.Has(MagicianAugmentType.SparklingDiamond))
                        {
                            ISlowable hitSlowable = hit.GetComponent<ISlowable>();
                            if (hitSlowable != null) hitSlowable.ApplySlow(_augments.DiamondSlowAmount, _augments.DiamondSlowDuration);
                        }
                    }
                    break;
                }

                case Suit.Clover:
                    ISlowable slowable = target.GetComponent<ISlowable>();
                    if (slowable != null) slowable.ApplySlow(_number * _cloverSlowPerNumber * effectMul, _cloverSlowDuration);

                    if (_augments != null && _augments.Has(MagicianAugmentType.LuckyClover))
                    {
                        Component targetComp = target;
                        float weakenAmount = _augments.CloverWeakenAmount;
                        float weakenDuration = _augments.CloverWeakenDuration;
                        DOVirtual.DelayedCall(_cloverSlowDuration, () =>
                        {
                            if (targetComp == null) return;
                            IWeakenable weakenable = targetComp.GetComponent<IWeakenable>();
                            if (weakenable != null) weakenable.ApplyAttackWeaken(weakenAmount, weakenDuration);
                        }, false);
                    }
                    break;
            }
        }

        void QueueSharpCardBonus(IDamageable damageable)
        {
            if (_augments == null || !_augments.Has(MagicianAugmentType.SharpCard)) return;

            Component targetComp = damageable as Component;
            float bonusDamage = _augments.SharpCardBonusDamage;
            DOVirtual.DelayedCall(_augments.SharpCardDelay, () =>
            {
                if (targetComp == null) return;
                IDamageable late = targetComp as IDamageable;
                if (late != null) late.TakeDamage(bonusDamage);
            }, false);
        }

        static Suit RandomOtherSuit(Suit current)
        {
            Suit result = current;
            while (result == current) result = (Suit)Random.Range(0, 4);
            return result;
        }
    }
}
