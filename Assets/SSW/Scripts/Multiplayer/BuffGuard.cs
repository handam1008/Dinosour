using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    public sealed class BuffGuard : NetworkBehaviour, IIncomingDamageModifier, IDamageReceivedListener, IOutgoingDamageModifier, IDamageDealtListener
    {
        struct Chill
        {
            public NetPlayer Target;
            public double At;
        }

        [SerializeField] NetPlayer _player;
        [SerializeField] NetBuff _buffs;
        [SerializeField] BuffFx _fx;
        [SerializeField] BuffArea _areas;
        [SerializeField] float _coolDown = 3f;
        [SerializeField] float _barrierContinue = 0.5f;
        [SerializeField] float _guardMasteryReduction = 0.3f;
        [SerializeField] float _invincibleBonusTime = 1f;
        [SerializeField] float _rechargeCoolPenalty = 0.4f;
        [SerializeField] float _rechargeGap = 0.1f;
        [SerializeField] float _blinkDistance = 4f;
        [SerializeField] float _blinkDuration = 0.15f;
        [SerializeField] float _blinkCoolPenalty = 0.2f;
        [SerializeField] float _iceAgeRadius = 5f;
        [SerializeField] float _iceAgeFreezeTime = 0.5f;
        [SerializeField] float _iceAgeSlowAmount = 0.65f;
        [SerializeField] float _iceAgeSlowTime = 2.5f;
        [SerializeField] float _iceAgeCoolPenalty = 0.15f;
        [SerializeField] float _bestOffenseBonus = 2f;
        [SerializeField] float _bestOffenseWindow = 5f;
        [SerializeField] float _bestOffenseCoolPenalty = 0.5f;
        [SerializeField] float _nuclearRadius = 5f;
        [SerializeField] float _nuclearDamageRatio = 0.2f;
        [SerializeField] float _nuclearCoolPenalty = 1.5f;
        [SerializeField] LayerMask _nuclearWallMask;
        [SerializeField] float _nuclearChargeTime = 0.8f;
        [SerializeField] float _counterRatio = 0.6f;
        [SerializeField] Augment _leapBombAugment;
        [SerializeField] int _leapBombCount = 3;
        [SerializeField] float _leapSpeed = 16f;
        [SerializeField] float _leapBombSpread = 3f;
        [SerializeField] float _leapBombCoolPenalty = 0.2f;
        [SerializeField] float _versatileGuardCool = 0.1f;
        
        [Header("sound")]
        [SerializeField] SoundCue _guardStartSound;
        [SerializeField] SoundCue _guardSuccessSound;
        
        [SerializeField] SoundCue _blinkSound;
        [SerializeField] SoundCue _blinkHitSound;
        [SerializeField] SoundCue _iceAgeSound;
        [SerializeField] SoundCue _nuclearChargeSound;
        [SerializeField] SoundCue _nuclearBlastSound;
        
        readonly NetworkVariable<double> _guardUntil = new NetworkVariable<double>();
        readonly NetworkVariable<double> _readyAt = new NetworkVariable<double>();
        readonly NetworkVariable<double> _empowerUntil = new NetworkVariable<double>();
        readonly List<double> _nuclear = new List<double>();
        readonly List<Chill> _chill = new List<Chill>();
        double _rechargeAt;
        bool _active;
        bool _pending;
        uint _request;
        double _previewUntil;
        double _previewReady;
        
        bool _guardBlocked;

        int IIncomingDamageModifier.Priority => -100;
        int IOutgoingDamageModifier.Priority => 200;
        public bool Guarding => IsSpawned && _player.CanAct && (Now < _guardUntil.Value || IsOwner && !IsServer && Now < _previewUntil);
        public bool Ready => IsSpawned && _player.CanAttack && Now >= _readyAt.Value
            && (IsServer || !_pending && Now >= _previewReady);
        public bool Empowered => IsSpawned && _player.CanAct && Has(CommonAugmentType.BestOffense) && Now < _empowerUntil.Value;
        public double ReadyAt => _readyAt.Value;
        public double GuardUntil => _guardUntil.Value;
        public double NuclearAt => _nuclear.Count > 0 ? _nuclear[0] : 0d;
        public float Duration => _barrierContinue + (Has(CommonAugmentType.Invincible) ? _invincibleBonusTime : 0f);
        double Now => NetGame.Current.ServerTime;
        bool LeapBomb => _buffs.Owns(_leapBombAugment);
        bool Has(CommonAugmentType type) => _buffs.Has(type);

        public float Cooldown
        {
            get
            {
                float value = _coolDown;
                if (Has(CommonAugmentType.GuardMastery)) value *= 1f - _guardMasteryReduction;
                if (Has(CommonAugmentType.Recharge)) value *= 1f + _rechargeCoolPenalty;
                if (Has(CommonAugmentType.Blink)) value *= 1f + _blinkCoolPenalty;
                if (Has(CommonAugmentType.IceAge)) value *= 1f + _iceAgeCoolPenalty;
                if (Has(CommonAugmentType.BestOffense)) value *= 1f + _bestOffenseCoolPenalty;
                if (Has(CommonAugmentType.Nuclear)) value *= 1f + _nuclearCoolPenalty;
                if (Has(CommonAugmentType.Versatile)) value *= 1f - _versatileGuardCool;
                if (LeapBomb) value *= 1f + _leapBombCoolPenalty;
                return Mathf.Max(0f, value);
            }
        }

        public override void OnNetworkSpawn()
        {
            _guardUntil.OnValueChanged += GuardChanged;
            _readyAt.OnValueChanged += ReadyChanged;
        }

        void GuardChanged(double previous, double current)
        {
            if (!_pending) _previewUntil = 0d;
            _previewReady = 0d;
        }

        void ReadyChanged(double previous, double current) => _previewReady = 0d;

        public void Guard()
        {
            if (!IsOwner || !Ready) return;
            
            NetGame.Current.Sounds.Play(_guardStartSound);
            
            uint request = ++_request;
            if (!IsServer)
            {
                _pending = true;
                _previewUntil = Now + Duration;
            }
            GuardRpc(request);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        void GuardRpc(uint request, RpcParams rpc = default)
        {
            if (rpc.Receive.SenderClientId != OwnerClientId) return;
            bool accepted = _player.CanAttack && Now >= _readyAt.Value;
            if (accepted) StartGuard(false);
            GuardResultRpc(request, accepted, _guardUntil.Value, _readyAt.Value);
        }

        [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
        void GuardResultRpc(uint request, bool accepted, double until, double ready)
        {
            if (request != _request || IsServer) return;
            _pending = false;
            _previewUntil = accepted ? until : 0d;
            _previewReady = accepted && _guardUntil.Value < until ? ready : 0d;
        }

        void StartGuard(bool recharge)
        {
            double now = Now;
            _active = true;
            _guardUntil.Value = now + Duration;
            _readyAt.Value = _guardUntil.Value + Cooldown;
            Vector2 center = _player.Body.position;
            if (Has(CommonAugmentType.HealField)) _areas.Heal(center);
            if (Has(CommonAugmentType.IceAge)) Ice(center, now);
            if (Has(CommonAugmentType.Nuclear))
            {
                _nuclear.Add(now + _nuclearChargeTime);
                _fx.Play(BuffEffect.NuclearCharge, center, 1f, _nuclearChargeTime);
                NetGame.Current.Sounds.Play(_nuclearChargeSound);
            }
            if (Has(CommonAugmentType.BestOffense)) _empowerUntil.Value = now + _bestOffenseWindow;
            if (Has(CommonAugmentType.Blink))
            {
                _player.Drive.Burst(_player.Aim, _blinkDistance, _blinkDuration);
                NetGame.Current.Sounds.Play(_blinkSound);
                NetGame.Current.Sounds.Play(_blinkHitSound);
            }
            if (LeapBomb)
            {
                for (int i = 0; i < _leapBombCount; i++)
                {
                    float spread = _leapBombCount > 1 ? i / (_leapBombCount - 1f) * 2f - 1f : 0f;
                    _areas.Bomb(center, new Vector2(spread * _leapBombSpread, 0f));
                }
                _player.Drive.Leap(_leapSpeed);
            }
            if (!recharge && Has(CommonAugmentType.Recharge)) _rechargeAt = _guardUntil.Value + _rechargeGap;
        }

        void Update()
        {
            if (!IsSpawned || !IsServer) return;
            if (!_player.CanAct)
            {
                if (_active) ResetGuard();
                return;
            }
            double now = Now;
            if (_rechargeAt > 0d && now >= _rechargeAt)
            {
                _rechargeAt = 0d;
                StartGuard(true);
            }
            for (int i = _chill.Count - 1; i >= 0; i--)
            {
                Chill chill = _chill[i];
                if (now < chill.At) continue;
                _chill.RemoveAt(i);
                if (chill.Target != null && chill.Target.CanAct) chill.Target.Motion.ApplySlow(_iceAgeSlowAmount, _iceAgeSlowTime);
            }
            for (int i = _nuclear.Count - 1; i >= 0; i--)
            {
                if (now < _nuclear[i]) continue;
                _nuclear.RemoveAt(i);
                Nuclear(_player.Body.position);
            }
        }

        void Ice(Vector2 center, double now)
        {
            _fx.Play(BuffEffect.IceBlast, center, _iceAgeRadius);
            NetGame.Current.Sounds.Play(_iceAgeSound);
            foreach (NetPlayer target in NetGame.Current.Players)
            {
                if (target == _player || !target.CanAct || !BuffArea.InRange(target, center, _iceAgeRadius)) continue;
                target.Drive.Freeze(_iceAgeFreezeTime);
                _fx.Play(BuffEffect.Freeze, target.Body.position, 1f, _iceAgeFreezeTime);
                _chill.Add(new Chill { Target = target, At = now + _iceAgeFreezeTime });
            }
        }

        void Nuclear(Vector2 center)
        {
            _fx.Play(BuffEffect.NuclearBlast, center, _nuclearRadius);
            NetGame.Current.Sounds.Play(_nuclearBlastSound);
            int walls = _nuclearWallMask.value;
            foreach (NetPlayer target in NetGame.Current.Players)
            {
                if (target == _player || !target.CanAct || !BuffArea.InRange(target, center, _nuclearRadius)) continue;
                Vector2 point = target.Collider.ClosestPoint(center);
                if (ShotQuery.Ground(center, point, 0.001f, walls, out _)) continue;
                CombatDamage.Deal(null, target.Health, target.Health.Max * _nuclearDamageRatio, DamageTag.JobSkill);
            }
        }

        public float ModifyIncomingDamage(DamageRequest request, float amount)
        {
            bool blocked =
                IsServer &&
                IsSpawned &&
                _player.CanAct &&
                Now < _guardUntil.Value &&
                !request.HasTag(DamageTag.IgnoreDefense);

            if (blocked)
            {
                _guardBlocked = true;
                return 0f;
            }

            return amount;
        }

        public void OnDamageReceived(DamageRequest request, DamageResult result)
        {
            if (!IsServer) return;
            if (_guardBlocked)
            {
                _guardBlocked = false;
                NetGame.Current.Sounds.Play(_guardSuccessSound);
            }
            if(!_player.CanAct || !Has(CommonAugmentType.CounterAttack) || !result.WasBlocked
                || request.HasTag(DamageTag.DamageOverTime) || request.HasTag(DamageTag.IgnoreDefense) || request.Source == null) return;
            NetPlayer source = request.Source.GetComponentInParent<NetPlayer>();
            if (source == null || source == _player || !source.CanAct) return;
            CombatDamage.Deal(null, source.Health, request.Amount * _counterRatio, DamageTag.JobSkill);
        }

        public float ModifyOutgoingDamage(float amount) => IsServer && Empowered ? amount * (1f + _bestOffenseBonus) : amount;

        public void OnDamageDealt(DamageRequest request, DamageResult result)
        {
            if (!IsServer || !Empowered || request.HasTag(DamageTag.Deferred)) return;
            _empowerUntil.Value = 0d;
            if (result.WasAccepted) _readyAt.Value = Now;
        }

        void ResetGuard()
        {
            _active = false;
            _rechargeAt = 0d;
            _guardUntil.Value = 0d;
            _readyAt.Value = 0d;
            _empowerUntil.Value = 0d;
            _nuclear.Clear();
            _chill.Clear();
        }

        public override void OnNetworkDespawn()
        {
            _guardUntil.OnValueChanged -= GuardChanged;
            _readyAt.OnValueChanged -= ReadyChanged;
            _pending = false;
            _previewUntil = _previewReady = 0d;
            _nuclear.Clear();
            _chill.Clear();
            _rechargeAt = 0d;
            _active = false;
        }
    }
}
