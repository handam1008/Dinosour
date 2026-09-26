using System.Collections.Generic;
using UnityEngine;

namespace SSW
{
    [DisallowMultipleComponent]
    public sealed class BuffHealth : MonoBehaviour, IIncomingDamageModifier, IOutgoingDamageModifier, IDamageDealtListener, IDamageDelay
    {
        [SerializeField] NetPlayer _player;
        [SerializeField] AugmentDrafter _source;
        [SerializeField] BuffFx _fx;
        [SerializeField] float _giantHpBonus = 0.55f;
        [SerializeField] float _giantScaleBonus = 0.1f;
        [SerializeField] float _glassHpPenalty = 0.3f;
        [SerializeField] float _glassDamage = 1.8f;
        [SerializeField] float _glassScalePenalty = 0.1f;
        [SerializeField] float _phoenixHpPenalty = 0.25f;
        [SerializeField] float _phoenixImmune = 2.5f;
        [SerializeField] float _phoenixLock = 1f;
        [SerializeField] float _phoenixScalePenalty = 0.08f;
        [SerializeField] float _vampireHeal = 0.55f;
        [SerializeField] float _confidenceSpeed = 0.3f;
        [SerializeField] float _confidenceDuration = 2f;
        [SerializeField] float _berserkerRatio = 0.75f;
        [SerializeField] float _berserkerSpeed = 0.45f;
        [SerializeField] float _berserkerDamage = 0.6f;
        [SerializeField] float _waltzDuration = 5f;
        [SerializeField] float _waltzHpBonus = 0.3f;
        [SerializeField] float _tenLivesHealth = 3f;
        [SerializeField] float _tenLivesDamage = 1f;
        [SerializeField] float _multiscaleReduction = 0.5f;
        [SerializeField] float _regenAmount = 1f;
        [SerializeField] float _regenInterval = 1f;
        [SerializeField] float _versatileHpBonus = 0.1f;
        readonly HashSet<CommonAugmentType> _owned = new HashSet<CommonAugmentType>();
        readonly List<Pending> _pending = new List<Pending>();
        float _baseMax;
        float _maxScale = 1f;
        float _regenTime;
        float _confidenceUntil;
        float _immuneUntil;
        bool _phoenixUsed;
        bool _spawned;

        struct Pending
        {
            public DamageRequest Request;
            public float Amount;
            public float Next;
            public int Ticks;
        }

        public int Priority => 0;
        public bool Berserker => Has(CommonAugmentType.Berserker)
            && _player.Health.Current > 0f && _player.Health.Current <= _player.Health.Max * _berserkerRatio;
        public float SpeedScale => (Time.time < _confidenceUntil ? 1f + _confidenceSpeed : 1f)
            * (Berserker ? 1f + _berserkerSpeed : 1f);
        public float Scale => (Has(CommonAugmentType.Giant) ? 1f + _giantScaleBonus : 1f)
            * (Has(CommonAugmentType.GlassCannon) ? 1f - _glassScalePenalty : 1f)
            * (Has(CommonAugmentType.Phoenix) ? 1f - _phoenixScalePenalty : 1f);
        public float MaxScale
        {
            get => _maxScale;
            set
            {
                _maxScale = value;
                RefreshMax();
            }
        }

        void Awake()
        {
            _baseMax = _player.Health.Max;
            _source.AugmentGranted += Granted;
            foreach (Augment augment in _source.Owned) Granted(augment);
        }

        bool Has(CommonAugmentType type) => _owned.Contains(type);

        public void SetBase(float value)
        {
            _baseMax = value;
            RefreshMax();
        }

        void Granted(Augment augment)
        {
            if (augment is not CommonAugment common || !_owned.Add(common.type)) return;
            RefreshMax();
        }

        void RefreshMax()
        {
            if (!_player.IsServer) return;
            float value = _baseMax * _maxScale;
            if (Has(CommonAugmentType.Giant)) value *= 1f + _giantHpBonus;
            if (Has(CommonAugmentType.GlassCannon)) value *= 1f - _glassHpPenalty;
            if (Has(CommonAugmentType.Phoenix)) value *= 1f - _phoenixHpPenalty;
            if (Has(CommonAugmentType.DeathWaltz)) value *= 1f + _waltzHpBonus;
            if (Has(CommonAugmentType.Versatile)) value *= 1f + _versatileHpBonus;
            if (Has(CommonAugmentType.TenLives)) value = _tenLivesHealth;
            if (!Mathf.Approximately(value, _player.Health.Max)) _player.Health.SetMax(value);
        }

        void Update()
        {
            if (!_player.IsSpawned)
            {
                if (_spawned) ResetLife();
                return;
            }
            if (!_spawned)
            {
                _spawned = true;
                RefreshMax();
            }
            if (!_player.IsServer) return;
            if (!NetGame.Current.CanFight || _player.Health.Current <= 0f)
            {
                ClearTimed();
                return;
            }
            TickDamage();
            TickRegen();
        }

        void TickRegen()
        {
            if (!Has(CommonAugmentType.Regeneration) || _player.Health.Current <= 0f) return;
            _regenTime += Time.deltaTime;
            if (_regenTime < _regenInterval) return;
            _regenTime %= Mathf.Max(0.01f, _regenInterval);
            if (_player.Health.Current < _player.Health.Max) _player.Health.Heal(_regenAmount);
        }

        public float ModifyOutgoingDamage(float amount)
        {
            if (Has(CommonAugmentType.GlassCannon)) amount *= _glassDamage;
            if (Berserker) amount *= 1f + _berserkerDamage;
            return amount;
        }

        public void OnDamageDealt(DamageRequest request, DamageResult result)
        {
            if (!_player.IsServer || !_player.IsSpawned || !NetGame.Current.CanFight || !result.WasApplied) return;
            if (Has(CommonAugmentType.Vampire)) _player.Health.Heal(result.AppliedAmount * _vampireHeal);
            if (Has(CommonAugmentType.Confidence)) _confidenceUntil = Time.time + _confidenceDuration;
        }

        public float ModifyIncomingDamage(DamageRequest request, float amount)
        {
            if (!_player.IsServer || amount <= 0f) return amount;
            if (Time.time < _immuneUntil) return 0f;
            bool direct = !request.HasTag(DamageTag.IgnoreDefense);
            if (direct && Has(CommonAugmentType.Multiscale)
                && _player.Health.Current >= _player.Health.Max - 0.001f)
                amount *= 1f - _multiscaleReduction;
            bool attacked = request.Source != null && !request.HasTag(DamageTag.Deferred);
            if (attacked && Has(CommonAugmentType.TenLives)) amount = _tenLivesDamage;
            if (Has(CommonAugmentType.Phoenix) && !_phoenixUsed && amount >= _player.Health.Current)
            {
                _phoenixUsed = true;
                _immuneUntil = Time.time + _phoenixImmune;
                _pending.Clear();
                _player.Health.Heal(_player.Health.Max);
                _player.Drive.Freeze(_phoenixLock);
                _fx.Revive();
                return 0f;
            }
            return amount;
        }

        public bool TryDefer(DamageRequest request, float amount)
        {
            if (!_player.IsServer || amount <= 0f || request.HasTag(DamageTag.IgnoreDefense) || !Has(CommonAugmentType.DeathWaltz)) return false;
            int ticks = Mathf.Max(1, Mathf.RoundToInt(_waltzDuration));
            _pending.Add(new Pending { Request = request, Amount = amount / ticks, Ticks = ticks, Next = Time.time + 1f });
            return true;
        }

        void TickDamage()
        {
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                Pending pending = _pending[i];
                if (Time.time < pending.Next) continue;
                pending.Ticks--;
                pending.Next += 1f;
                if (pending.Ticks == 0) _pending.RemoveAt(i);
                else _pending[i] = pending;
                DamageRequest request = new DamageRequest(pending.Request.Source, pending.Amount,
                    pending.Request.Tags | DamageTag.IgnoreDefense | DamageTag.DamageOverTime | DamageTag.Deferred, pending.Request.IsCritical);
                DamageResult result = _player.Health.ReceiveDamage(request);
                NotifySource(request, result);
                if (_pending.Count == 0 || _player.Health.Current <= 0f) break;
            }
        }

        static void NotifySource(DamageRequest request, DamageResult result)
        {
            if (request.Source == null || !result.WasApplied) return;
            MonoBehaviour[] behaviours = request.Source.GetComponentsInParent<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
                if (behaviour.isActiveAndEnabled && behaviour is IDamageDealtListener listener)
                    listener.OnDamageDealt(request, result);
        }

        void ClearTimed()
        {
            _pending.Clear();
            _regenTime = 0f;
            _confidenceUntil = 0f;
            _immuneUntil = 0f;
        }

        void ResetLife()
        {
            ClearTimed();
            _phoenixUsed = false;
            _spawned = false;
        }

        void OnDisable() => ResetLife();

        void OnDestroy()
        {
            _source.AugmentGranted -= Granted;
        }
    }
}
