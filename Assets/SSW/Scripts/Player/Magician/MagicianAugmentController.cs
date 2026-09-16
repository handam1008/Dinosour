using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace SSW
{
    [DisallowMultipleComponent]
    public class MagicianAugmentController : AugmentReceiverBehaviour, IAugmentCooldownProvider
    {
        [SerializeField] NumberRoller _numberRoller;
        [SerializeField] float _quickShuffleTickMultiplier = 1.15f;
        [SerializeField] float _quickShuffleFireDelay = 0.1f;
        [SerializeField] float _sharpCardBonusDamage = 3f;
        [SerializeField] float _sharpCardDelay = 1f;
        [SerializeField] float _emergencyHealBonus = 3f;
        [SerializeField] float _emergencyCooldown = 3f;
        [SerializeField] float _diamondRadiusMultiplier = 1.15f;
        [SerializeField] float _diamondSlowAmount = 0.2f;
        [SerializeField] float _diamondSlowDuration = 0.5f;
        [SerializeField] float _cloverWeakenAmount = 0.1f;
        [SerializeField] float _cloverWeakenDuration = 0.5f;
        [SerializeField] float _returnDelay = 0.5f;
        [SerializeField] float _returnDamageMultiplier = 0.4f;
        [SerializeField] int _chainRequiredHits = 2;
        [SerializeField] float _chainBonusMultiplier = 1.5f;
        [SerializeField] float _mirrorDelay = 0.3f;
        [SerializeField] float _mirrorEffectMultiplier = 0.35f;
        [SerializeField] float _mirrorCooldown = 8f;
        [SerializeField] float _jokerInterval = 15f;

        readonly HashSet<MagicianAugmentType> _acquired = new HashSet<MagicianAugmentType>();
        float _emergencyReadyTime;
        float _mirrorReadyTime;
        float _jokerTimer;
        bool _jokerArmed;
        Suit _lastHitSuit;
        int _chainCount;
        bool _chainStarted;
        JobAugmentHUD _augmentHud;

        public override PlayerJob Job => PlayerJob.Magician;

        public float SharpCardBonusDamage => _sharpCardBonusDamage;
        public float SharpCardDelay => _sharpCardDelay;
        public float DiamondRadiusMultiplier => Has(MagicianAugmentType.SparklingDiamond) ? _diamondRadiusMultiplier : 1f;
        public float DiamondSlowAmount => _diamondSlowAmount;
        public float DiamondSlowDuration => _diamondSlowDuration;
        public float CloverWeakenAmount => _cloverWeakenAmount;
        public float CloverWeakenDuration => _cloverWeakenDuration;
        public float ReturnDelay => _returnDelay;
        public float ReturnDamageMultiplier => _returnDamageMultiplier;
        public float TickIntervalMultiplier => Has(MagicianAugmentType.QuickShuffle) ? _quickShuffleTickMultiplier : 1f;
        public float FireDelay => Has(MagicianAugmentType.QuickShuffle) ? _quickShuffleFireDelay : 0f;

        public override bool TryReceive(Augment augment)
        {
            if (augment is not MagicianAugment magicianAugment)
            {
                return false;
            }

            NetPlayer player = GetComponent<NetPlayer>();
            if (_acquired.Add(magicianAugment.type) && (player == null || player.IsOwner))
            {
                EnsureAugmentHud();
                _augmentHud.AddAugment(magicianAugment);
            }

            return true;
        }

        void Update()
        {
            if (!IsJobActive) return;
            if (!Has(MagicianAugmentType.JokerCard) || _jokerArmed) return;
            _jokerTimer += Time.deltaTime;
            if (_jokerTimer >= _jokerInterval) _jokerArmed = true;
        }

        public bool Has(MagicianAugmentType type)
        {
            return IsJobActive && _acquired.Contains(type);
        }

        public bool TryGetCooldown(Augment augment, out float remaining, out float duration)
        {
            remaining = 0f;
            duration = 0f;

            if (augment is not MagicianAugment magicianAugment
                || !_acquired.Contains(magicianAugment.type))
            {
                return false;
            }

            switch (magicianAugment.type)
            {
                case MagicianAugmentType.EmergencyMagic:
                    duration = _emergencyCooldown;
                    remaining = Mathf.Max(_emergencyReadyTime - Time.time, 0f);
                    return true;

                case MagicianAugmentType.MirrorCard:
                    duration = _mirrorCooldown;
                    remaining = Mathf.Max(_mirrorReadyTime - Time.time, 0f);
                    return true;

                case MagicianAugmentType.JokerCard:
                    duration = _jokerInterval;
                    remaining = _jokerArmed ? 0f : Mathf.Max(_jokerInterval - _jokerTimer, 0f);
                    return true;

                default:
                    return false;
            }
        }

        public float ConsumeEmergencyHealBonus()
        {
            if (!Has(MagicianAugmentType.EmergencyMagic)) return 0f;
            if (Time.time < _emergencyReadyTime) return 0f;
            _emergencyReadyTime = Time.time + _emergencyCooldown;
            return _emergencyHealBonus;
        }

        public bool ConsumeJoker()
        {
            if (!IsJobActive) return false;
            if (!_jokerArmed) return false;
            _jokerArmed = false;
            _jokerTimer = 0f;
            return true;
        }

        public float GetChainMultiplier(Suit suit)
        {
            if (!Has(MagicianAugmentType.CardChain)) return 1f;
            if (_chainStarted && _chainCount >= _chainRequiredHits && suit == _lastHitSuit) return _chainBonusMultiplier;
            return 1f;
        }

        public void RegisterHit(Suit suit)
        {
            if (!IsJobActive) return;
            if (_chainStarted && suit == _lastHitSuit)
            {
                _chainCount++;
            }
            else
            {
                _chainStarted = true;
                _lastHitSuit = suit;
                _chainCount = 1;
            }
        }

        public void RegisterMiss()
        {
            if (!IsJobActive) return;
            _chainStarted = false;
            _chainCount = 0;
        }

        public void TryQueueMirror(Suit suit, int number, Vector2 direction)
        {
            if (!Has(MagicianAugmentType.MirrorCard)) return;
            if (Time.time < _mirrorReadyTime) return;
            _mirrorReadyTime = Time.time + _mirrorCooldown;

            DOVirtual.DelayedCall(_mirrorDelay, () =>
            {
                if (_numberRoller != null) _numberRoller.SpawnMirrorCard(suit, number - 1, direction, _mirrorEffectMultiplier);
            }, false);
        }

        protected override void OnJobDeactivated()
        {
            _chainStarted = false;
            _chainCount = 0;
            _jokerArmed = false;
            _jokerTimer = 0f;
            if (_augmentHud != null) _augmentHud.SetJobVisible(false);
        }

        protected override void OnJobActivated()
        {
            if (_augmentHud != null) _augmentHud.SetJobVisible(true);
        }

        void EnsureAugmentHud()
        {
            if (_augmentHud == null)
            {
                PlayerIdentity owner = GetComponentInParent<PlayerIdentity>();
                GameObject hudOwner = owner != null ? owner.gameObject : gameObject;
                _augmentHud = hudOwner.GetComponent<JobAugmentHUD>();
                if (_augmentHud == null)
                    _augmentHud = hudOwner.AddComponent<JobAugmentHUD>();
            }

            _augmentHud.Configure(this);
            _augmentHud.SetJobVisible(IsJobActive);
        }
    }
}
