using UnityEngine;

namespace SSW
{
    [DefaultExecutionOrder(-10)]
    public sealed class BuffSkill : MonoBehaviour, IIncomingDamageModifier, IOutgoingDamageModifier, IDamageDealtListener
    {
        [SerializeField] NetPlayer _player;
        [SerializeField] NetBuff _buffs;
        [SerializeField] BuffFx _fx;
        [SerializeField] BuffArea _areas;
        [SerializeField] float _moveThreshold = 0.05f;
        [SerializeField] float _devilsDrainRatio = 0.01f;
        [SerializeField] float _devilsStopRatio = 0.10f;
        [SerializeField] float _devilsHealRate = 0.25f;
        [SerializeField] float _swiftSpeedBonus = 0.60f;
        [SerializeField] float _swiftRange = 30f;
        [SerializeField] float _nomRadius = 4f;
        [SerializeField] float _nomDamage = 3f;
        [SerializeField] float _nomHealRate = 1f;
        [SerializeField] float _nomInterval = 1f;
        [SerializeField] float _magnetRadius = 5f;
        [SerializeField] float _magnetStrength = 2f;
        [SerializeField] float _magnetInterval = 1f;
        [SerializeField] float _shrinkScale = 0.25f;
        [SerializeField] float _shrinkSpeedBonus = 0.25f;
        [SerializeField] float _versatileDamage = 0.10f;
        [SerializeField] float _versatileLifesteal = 0.05f;
        [SerializeField] float _versatileSpeed = 0.10f;
        [SerializeField] float _doubleJumpRatio = 0.9f;
        [SerializeField] float _slamMinHeight = 2f;
        [SerializeField] Vector2 _slamBoxSize = new Vector2(6f, 0.3f);
        [SerializeField] float _slamBoxOffsetY = -0.5f;
        [SerializeField] float _slamBaseDamage = 5f;
        [SerializeField] float _slamDamagePerHeight = 3f;
        [SerializeField] float _slamMaxDamage = 40f;
        [SerializeField] float _slamKnockback = 6f;
        [SerializeField] SoundCue _landingSound;
        [SerializeField] SoundCue _slamSound;
        [SerializeField] float _mineInterval = 3f;
        [SerializeField] float _slowAuraRadius = 4.5f;
        [SerializeField] float _slowAuraAmount = 0.35f;
        float _drain;
        float _nom;
        float _magnet;
        float _mine;
        bool _swift;
        bool _grounded = true;
        float _peak;
        uint _epoch;
        public int Priority => -50;
        public float Scale => Has(CommonAugmentType.ShrinkEngine) ? 1f - _shrinkScale : 1f;
        public float AirJumpRatio => _doubleJumpRatio;
        public float SpeedScale => (_swift ? 1f + _swiftSpeedBonus : 1f)
            * (Has(CommonAugmentType.ShrinkEngine) ? 1f + _shrinkSpeedBonus : 1f)
            * (Has(CommonAugmentType.Versatile) ? 1f + _versatileSpeed : 1f);
        bool Has(CommonAugmentType type) => _buffs.Has(type);

        void FixedUpdate()
        {
            if (!_player.IsSpawned || !_player.IsServer) return;
            if (!NetGame.Current.CanFight || _player.Health.Current <= 0f)
            {
                _drain = _nom = _magnet = _mine = 0f;
                _swift = false;
                _grounded = true;
                return;
            }
            TickDrain();
            TickTargets();
            TickLanding();
            if (Has(CommonAugmentType.Minefield) && Due(ref _mine, _mineInterval))
                _areas.Mine(_player.Drive.Position);
        }

        bool Due(ref float timer, float interval)
        {
            timer += Time.fixedDeltaTime;
            if (timer < interval) return false;
            timer -= interval;
            return true;
        }

        void TickDrain()
        {
            if (!Has(CommonAugmentType.DevilsDeal) || !Due(ref _drain, 1f)) return;
            float amount = Mathf.Min(_player.Health.Max * _devilsDrainRatio,
                Mathf.Max(0f, _player.Health.Current - _player.Health.Max * _devilsStopRatio));
            _player.Health.ReceiveDamage(new DamageRequest(null, amount, DamageTag.IgnoreDefense | DamageTag.DamageOverTime));
        }

        void TickTargets()
        {
            bool nom = Has(CommonAugmentType.NomNom) && Due(ref _nom, _nomInterval);
            bool magnet = Has(CommonAugmentType.Magnet) && Due(ref _magnet, _magnetInterval);
            _swift = false;
            foreach (NetPlayer target in NetGame.Current.Players)
            {
                if (target == _player || target.Health.Current <= 0f) continue;
                Vector2 offset = target.Drive.Position - _player.Drive.Position;
                float distance = offset.sqrMagnitude;
                if (Has(CommonAugmentType.SwiftApproach) && distance <= _swiftRange * _swiftRange
                    && Mathf.Abs(_player.Velocity.x) > _moveThreshold && _player.Velocity.x * offset.x > 0f
                    && !Physics2D.Linecast(_player.Collider.bounds.center, target.Collider.bounds.center, _player.GroundMask))
                    _swift = true;
                if (nom && distance <= _nomRadius * _nomRadius)
                {
                    CombatDamage.Deal(this, target.Health, _nomDamage, DamageTag.JobSkill | DamageTag.Drain);
                }
                if (magnet && distance <= _magnetRadius * _magnetRadius)
                    target.Drive.ApplyForce(new Vector2(-Mathf.Sign(offset.x) * _magnetStrength, 0f), ForceMode2D.Impulse);
                if (Has(CommonAugmentType.SlowAura) && distance <= _slowAuraRadius * _slowAuraRadius)
                    target.Motion.ApplySlow(_slowAuraAmount, 0.15f);
            }
        }

        void TickLanding()
        {
            bool grounded = _player.Drive.Grounded;
            float y = _player.Drive.Position.y;
            if (_epoch != _player.Epoch)
            {
                _epoch = _player.Epoch;
                _grounded = grounded;
                _peak = y;
            }
            if (!grounded)
            {
                _peak = _grounded ? y : Mathf.Max(_peak, y);
            }
            else if (!_grounded && _peak - y >= _slamMinHeight)
            {
                if (Has(CommonAugmentType.Slam))
                    Slam(_peak - y);
                else
                    NetGame.Current.Sounds.Play(_landingSound); 
            }

            _grounded = grounded;
        }

        void Slam(float height)
        {
            Vector2 center = _player.Drive.Position + Vector2.up * _slamBoxOffsetY;
            _fx.Play(BuffEffect.Slam, center);
            NetGame.Current.Sounds.Play(_slamSound);
            float damage = Mathf.Min(_slamBaseDamage + _slamDamagePerHeight * height, _slamMaxDamage);
            Bounds area = new Bounds(center, _slamBoxSize);
            foreach (NetPlayer target in NetGame.Current.Players)
            {
                if (target == _player || target.Health.Current <= 0f) continue;
                Bounds bounds = target.Collider.bounds;
                if (bounds.max.x < area.min.x || bounds.min.x > area.max.x || bounds.max.y < area.min.y || bounds.min.y > area.max.y) continue;
                CombatDamage.Deal(this, target.Health, damage, DamageTag.JobSkill);
                float direction = Mathf.Sign(target.Drive.Position.x - center.x);
                target.Drive.ApplyForce(new Vector2(direction, 0.6f) * (_slamKnockback * (1f + height * 0.1f)), ForceMode2D.Impulse);
            }
        }

        public float ModifyOutgoingDamage(float amount) => Has(CommonAugmentType.Versatile) ? amount * (1f + _versatileDamage) : amount;

        public float ModifyIncomingDamage(DamageRequest request, float amount) =>
            Has(CommonAugmentType.DevilsDeal) && request.HasTag(DamageTag.Environment) && !request.HasTag(DamageTag.IgnoreDefense) ? 0f : amount;

        public void OnDamageDealt(DamageRequest request, DamageResult result)
        {
            if (!_player.IsServer || !result.WasApplied) return;
            float rate = (Has(CommonAugmentType.DevilsDeal) ? _devilsHealRate : 0f)
                + (Has(CommonAugmentType.Versatile) ? _versatileLifesteal : 0f);
            if (request.Source == this && request.HasTag(DamageTag.Drain)) rate += _nomHealRate;
            if (rate > 0f) _player.Health.Heal(result.AppliedAmount * rate);
        }
    }
}
