using System.Collections.Generic;
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
        IHealable _casterHealth;
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

        public void Configure(Suit suit, int number, IHealable casterHealth)
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

            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable == null)
            {
                if (_returning) return;
                HandleMiss();
                return;
            }

            if (IsCaster(damageable)) return;

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

            float damage = _baseDamage * effectMul * damageScale;
            if (_suit == Suit.Spade)
            {
                damage += _number * _spadeDamagePerNumber * effectMul * damageScale;
                QueueSharpCardBonus(damageable);
            }
            DealDamage(damageable, damage);

            if (_suit != Suit.Spade) ApplySuitEffect(_suit, other, damageable, effectMul, damageScale);
            if (_isJoker) ApplySuitEffect(RandomOtherSuit(_suit), other, damageable, effectMul, damageScale);

            if (_augments != null && !_isMirror)
            {
                _augments.RegisterHit(_suit);
                if (!_returning && _rb != null)
                    _augments.TryQueueMirror(_suit, _number, _rb.linearVelocity.normalized);
            }

            IForceReceiver forceReceiver = other.GetComponentInParent<IForceReceiver>();
            if (forceReceiver != null && _rb != null)
                forceReceiver.ApplyForce(_rb.linearVelocity.normalized * _knockbackForce, ForceMode2D.Impulse);

            Destroy(gameObject);
        }

        void ApplySuitEffect(Suit suit, Collider2D target, IDamageable damageable, float effectMul, float damageScale)
        {
            switch (suit)
            {
                case Suit.Spade:
                    DealDamage(damageable, _number * _spadeDamagePerNumber * effectMul * damageScale);
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
                    HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();
                    foreach (Collider2D hit in hits)
                    {
                        IDamageable hitDamageable = hit.GetComponentInParent<IDamageable>();
                        if (hitDamageable == null || IsCaster(hitDamageable) || !damagedTargets.Add(hitDamageable))
                            continue;

                        DealDamage(hitDamageable, damage);

                        if (_augments != null && _augments.Has(MagicianAugmentType.SparklingDiamond))
                        {
                            ISlowable hitSlowable = hit.GetComponentInParent<ISlowable>();
                            if (hitSlowable != null) hitSlowable.ApplySlow(_augments.DiamondSlowAmount, _augments.DiamondSlowDuration);
                        }
                    }
                    break;
                }

                case Suit.Clover:
                    ISlowable slowable = target.GetComponentInParent<ISlowable>();
                    if (slowable != null) slowable.ApplySlow(_number * _cloverSlowPerNumber * effectMul, _cloverSlowDuration);

                    if (_augments != null && _augments.Has(MagicianAugmentType.LuckyClover))
                    {
                        Component targetComp = damageable as Component;
                        float weakenAmount = _augments.CloverWeakenAmount;
                        float weakenDuration = _augments.CloverWeakenDuration;
                        DOVirtual.DelayedCall(_cloverSlowDuration, () =>
                        {
                            if (targetComp == null) return;
                            IWeakenable weakenable = targetComp.GetComponentInParent<IWeakenable>();
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
            Transform damageSource = _caster;
            DOVirtual.DelayedCall(_augments.SharpCardDelay, () =>
            {
                if (targetComp == null || damageSource == null) return;
                IDamageable late = targetComp as IDamageable;
                if (late != null)
                    CombatDamage.Deal(damageSource, late, bonusDamage, DamageTag.JobSkill | DamageTag.Projectile);
            }, false);
        }

        void DealDamage(IDamageable target, float amount)
        {
            CombatDamage.Deal(_caster, target, amount, DamageTag.JobSkill | DamageTag.Projectile);
        }

        bool IsCaster(IDamageable target)
        {
            Component targetComponent = target as Component;
            return targetComponent != null
                && _caster != null
                && targetComponent.transform.root == _caster.root;
        }

        static Suit RandomOtherSuit(Suit current)
        {
            Suit result = current;
            while (result == current) result = (Suit)Random.Range(0, 4);
            return result;
        }
    }
}
