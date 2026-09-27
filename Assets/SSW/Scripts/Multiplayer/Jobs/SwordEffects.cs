using System.Collections;
using UnityEngine;

namespace SSW
{
    sealed class SwordEffects
    {
        readonly SwordCast _cast;
        readonly NetPlayer _player;
        readonly SwordTuning _tuning;
        float _rootAt;
        float _bonusAt;
        float _bonusUntil;
        float _standing;
        float _healing;
        public float Growth { get; private set; } = 1f;
        public float DashScale => Time.time >= _bonusAt && Time.time < _bonusUntil ? 1f + _tuning.DashBonus : 1f;

        public SwordEffects(SwordCast cast, NetPlayer player, SwordTuning tuning)
        {
            _cast = cast;
            _player = player;
            _tuning = tuning;
        }

        public void DashHit(NetPlayer target, float end, float damage)
        {
            if (_cast.Has(SwordPerk.DashRecovery))
            {
                _player.Health.Heal(damage * _tuning.Recovery);
                _bonusAt = end;
                _bonusUntil = end + _tuning.BonusDuration;
            }
            if (_cast.Has(SwordPerk.DashRoot) && Time.time >= _rootAt)
            {
                _rootAt = Time.time + _tuning.RootCooldown;
                target.Motion.ApplySlow(1f, _tuning.RootDuration);
            }
            if (_cast.Has(SwordPerk.DashBleed)) _cast.StartCoroutine(Bleed(target));
        }

        IEnumerator Bleed(NetPlayer target)
        {
            for (int i = 0; i < _tuning.BleedTicks; i++)
            {
                yield return new WaitForSeconds(_tuning.BleedInterval);
                if (!_player.CanAct || target == null || !target.CanAct) yield break;
                CombatDamage.Deal(_cast, target.Health, _tuning.BleedDamage, DamageTag.JobSkill | DamageTag.DamageOverTime);
            }
        }

        public void Tick(float delta)
        {
            if (_cast.Has(SwordPerk.SwordGrowth))
                Growth = Mathf.Min(_tuning.GrowthLimit, Growth + _tuning.GrowthRate * delta);
            if (!_cast.Has(SwordPerk.StandingHeal) || _player.Velocity.sqrMagnitude > 0.01f)
            {
                Rest();
                return;
            }
            float previous = _standing;
            _standing += delta;
            _healing += Mathf.Max(0f, _standing - _tuning.StandingDelay) - Mathf.Max(0f, previous - _tuning.StandingDelay);
            while (_healing >= _tuning.HealInterval)
            {
                _healing -= _tuning.HealInterval;
                _player.Health.Heal(_tuning.HealAmount);
            }
        }

        public void Rest()
        {
            _standing = 0f;
            _healing = 0f;
        }
    }
}
