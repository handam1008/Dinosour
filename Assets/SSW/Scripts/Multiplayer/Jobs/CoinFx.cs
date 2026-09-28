using System;
using JJW.Script.Jackpot;
using UnityEngine;

namespace SSW
{
    public sealed class CoinFx : MonoBehaviour
    {
        [SerializeField] CoinReels _reels;
        [SerializeField] ParticleSystem _damage;
        [SerializeField] ParticleSystem _speed;
        [SerializeField] ParticleSystem _heal;
        [SerializeField] int _healCount = 8;
        [SerializeField] GameObject _shield;
        [SerializeField] ParticleSystem[] _energy;
        [SerializeField] CoinScreen _screenPrefab;
        CoinScreen _screen;
        double _damageUntil;
        double _speedUntil;
        double _shieldUntil;
        double _jackpotUntil;
        bool _damageOn;
        bool _speedOn;
        bool _jackpotOn;
        bool _fighting;

        public float SpinDuration => _reels.Duration;
        public float SpinInterval => _reels.Interval;
        public CoinReels Reels => _reels;
        public int Rolls { get; private set; }
        public int Results { get; private set; }
        public JackpotResultType Main { get; private set; }
        public JackpotResultType Old { get; private set; }
        public bool Damage => _damageOn;
        public bool Speed => _speedOn;
        public bool Shield => _shield.activeSelf;
        public bool Jackpot => _jackpotOn;
        public int HealParticles => _heal.particleCount;
        public float DeathAlpha => _screen != null ? _screen.DeathAlpha : 0f;
        public float OverlayAlpha => _screen != null ? _screen.OverlayAlpha : 0f;

        public void Open(NetPlayer player)
        {
            bool visible = player.IsClient && player.Job == PlayerJob.Gambler;
            gameObject.SetActive(visible);
            if (visible) _reels.SetView(NetGame.Current.Arena.View.transform);
            if (visible && player.IsOwner)
            {
                _screen = Instantiate(_screenPrefab);
                _screen.Open(NetGame.Current.Arena.View);
            }
        }

        public void Spin(JackpotResultType main, JackpotResultType old, uint roll, double started)
        {
            Main = main;
            Old = old;
            Rolls++;
            _reels.Play(main, roll, started);
        }

        public void Play(JackpotResultType result, double until, double now)
        {
            Results++;
            if (until <= now) return;
            switch (result)
            {
                case JackpotResultType.DamageUp: _damageUntil = Math.Max(_damageUntil, until); break;
                case JackpotResultType.SpeedUp: _speedUntil = Math.Max(_speedUntil, until); break;
                case JackpotResultType.Invincible: _shieldUntil = Math.Max(_shieldUntil, until); break;
                case JackpotResultType.Jackpot777: _jackpotUntil = Math.Max(_jackpotUntil, until); break;
                case JackpotResultType.Heal:
                    if (!_heal.isPlaying) _heal.Play(true);
                    _heal.Emit(_healCount);
                    break;
                case JackpotResultType.InstantKill:
                    if (_screen != null) _screen.Death(Mathf.Max(0f, 0.78f - (float)(until - now)));
                    break;
            }
        }

        public void Tick(double now, bool fighting)
        {
            if (!gameObject.activeSelf) return;
            if (_fighting && !fighting) Clear();
            _fighting = fighting;
            if (!fighting) return;
            _reels.Tick(now);
            Loop(_damage, ref _damageOn, now < _damageUntil);
            Loop(_speed, ref _speedOn, now < _speedUntil);
            bool shield = now < _shieldUntil;
            if (_shield.activeSelf != shield) _shield.SetActive(shield);
            bool jackpot = now < _jackpotUntil;
            if (_jackpotOn == jackpot) return;
            _jackpotOn = jackpot;
            foreach (ParticleSystem effect in _energy)
            {
                if (jackpot) effect.Play(true);
                else effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            if (_screen != null) _screen.Jackpot(jackpot);
        }

        static void Loop(ParticleSystem effect, ref bool previous, bool active)
        {
            if (previous == active) return;
            previous = active;
            if (active) effect.Play(true);
            else effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        void Clear()
        {
            _damageUntil = _speedUntil = _shieldUntil = _jackpotUntil = 0d;
            Loop(_damage, ref _damageOn, false);
            Loop(_speed, ref _speedOn, false);
            _heal.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _shield.SetActive(false);
            foreach (ParticleSystem effect in _energy) effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _jackpotOn = false;
            if (_screen != null) _screen.Jackpot(false);
            _reels.Clear();
        }

        public void Close()
        {
            Clear();
            if (_screen != null) _screen.Close();
            _screen = null;
        }

        void OnDestroy()
        {
            if (_screen != null) _screen.Close();
        }
    }
}
