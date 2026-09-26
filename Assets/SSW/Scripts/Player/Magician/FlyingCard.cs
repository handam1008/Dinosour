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
        MagicianCardFeedback _feedback;
        float _effectMultiplier = 1f;
        float _lifetime = 3f;
        float _age;
        bool _isJoker;
        bool _isMirror;
        bool _returning;
        bool _consumed;
        IShotLife _life;
        bool _swept;

        public void UseSweep() => _swept = true;

        public bool Returning => _returning;

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

        public void SetDamage(float damage) => _baseDamage = damage;

        public void SetEffectMultiplier(float multiplier)
        {
            _effectMultiplier = multiplier;
            if (_feedback != null) _feedback.SetIntensityMultiplier(multiplier);
        }

        public void MarkJoker()
        {
            _isJoker = true;
            if (_feedback != null) _feedback.MarkJoker();
        }

        public void MarkMirror()
        {
            _isMirror = true;
        }

        public void SetLife(IShotLife life)
        {
            _life = life;
        }

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _feedback = GetComponent<MagicianCardFeedback>();
        }

        void Update()
        {
            if (_consumed) return;

            _age += Time.deltaTime;

            if (_returning && _caster != null && Vector2.Distance(transform.position, _caster.position) < 0.35f)
            {
                _consumed = true;
                DestroyCard();
                return;
            }

            if (_age >= _lifetime) HandleMiss();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!_swept) Contact(other, other.ClosestPoint(transform.position));
        }

        public void Contact(Collider2D other, Vector2 impactPoint)
        {
            if (_consumed) return;

            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable == null)
            {
                if (_returning || other.isTrigger) return;

                if (_feedback != null)
                {
                    _feedback.PlayEnvironmentImpact(impactPoint);
                    _life?.Impact(impactPoint, 0f, _isMirror || _caster == null || _augments == null || !_augments.Has(MagicianAugmentType.ReturnCard));
                }

                HandleMiss();
                return;
            }

            if (IsCaster(damageable)) return;

            HitTarget(other, damageable, impactPoint);
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
            DestroyCard();
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

        void HitTarget(Collider2D other, IDamageable damageable, Vector2 impactPoint)
        {
            _consumed = true;
            Component targetComponent = damageable as Component;
            if (_feedback != null) _feedback.PlayTargetFlash(targetComponent);

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

            Suit? jokerBonusSuit = null;
            if (_isJoker)
            {
                jokerBonusSuit = RandomOtherSuit(_suit);
                ApplySuitEffect(jokerBonusSuit.Value, other, damageable, effectMul, damageScale);
            }

            if (_augments != null && !_isMirror)
            {
                _augments.RegisterHit(_suit);
                if (!_returning && _rb != null)
                    _augments.TryQueueMirror(_suit, _number, _rb.linearVelocity.normalized);
            }

            IForceReceiver forceReceiver = other.GetComponentInParent<IForceReceiver>();
            if (forceReceiver != null && _rb != null)
                forceReceiver.ApplyForce(_rb.linearVelocity.normalized * _knockbackForce, ForceMode2D.Impulse);

            if (_feedback != null)
            {
                bool hasDiamondExplosion = _suit == Suit.Diamond || jokerBonusSuit == Suit.Diamond;
                _feedback.PlayImpact(impactPoint, hasDiamondExplosion ? GetDiamondRadius() : 0f);
                _life?.Impact(impactPoint, hasDiamondExplosion ? GetDiamondRadius() : 0f);
            }

            DestroyCard();
        }

        void DestroyCard()
        {
            if (_feedback != null) _feedback.ReleaseTrail();
            if (_life != null) _life.Finish();
            else Destroy(gameObject);
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
                    float radius = GetDiamondRadius();
                    float damage = _number * _diamondDamagePerNumber * effectMul * damageScale;
                    Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
                    HashSet<IDamageable> damagedTargets = new HashSet<IDamageable> { damageable };
                    DealDamage(damageable, damage);
                    if (_augments != null && _augments.Has(MagicianAugmentType.SparklingDiamond))
                    {
                        ISlowable direct = target.GetComponentInParent<ISlowable>();
                        if (direct != null) direct.ApplySlow(_augments.DiamondSlowAmount, _augments.DiamondSlowDuration);
                    }
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

        float GetDiamondRadius()
        {
            return _diamondRadius * (_augments != null ? _augments.DiamondRadiusMultiplier : 1f);
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
