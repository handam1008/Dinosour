using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    public sealed class MapBreath : NetworkBehaviour, IMapPhase
    {
        [SerializeField] SpriteRenderer _first;
        [SerializeField] SpriteRenderer _second;
        [SerializeField] ParticleSystem _effect;
        [SerializeField] Color _signal;
        [SerializeField] Color _ready;
        [SerializeField] Color _base;
        [SerializeField, Min(4f)] float _delay = 5f;

        [Header("sound")] 
        [SerializeField] private SoundCue breathSoundCue;
        
        readonly NetworkVariable<double> _startedAt = new NetworkVariable<double>(-1d);
        Color _firstColor;
        Color _secondColor;
        int _played = -1;
        bool _emitting;
        float Duration => _effect.main.duration;
        double Age => _startedAt.Value < 0d ? -1d : System.Math.Max(0d, NetworkManager.ServerTime.Time - _startedAt.Value);
        public bool Active => IsSpawned && Age >= 0d && Age % (_delay + Duration) >= _delay;

        public override void OnNetworkSpawn()
        {
            _firstColor = _first.color;
            _secondColor = _second.color;
            _effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _effect.useAutoRandomSeed = false;
            _effect.randomSeed = unchecked((uint)NetworkObjectId * 747796405u + 2891336453u);
        }

        void Update()
        {
            if (!IsSpawned) return;
            if (IsServer && _startedAt.Value < 0d && NetGame.Current.CanFight)
                _startedAt.Value = NetworkManager.ServerTime.Time;
            if (Age < 0d) return;
            double age = Age;
            float period = _delay + Duration;
            int cycle = (int)(age / period);
            float phase = (float)(age % period);
            if (phase >= _delay && _played != cycle)
            {
                if (IsServer && NetGame.Current.CanFight) NetGame.Current.Sounds.Play(breathSoundCue);
                _played = cycle;
                _effect.Simulate(phase - _delay, true, true, true);
                _effect.Play(true);
                _emitting = true;
            }
            else if (phase < _delay && _emitting)
            {
                _effect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                _emitting = false;
            }
            if (phase < 4f)
            {
                _first.color = cycle == 0 ? _firstColor : _base;
                _second.color = cycle == 0 ? _secondColor : Color.Lerp(_signal, _ready, Ease(phase));
            }
            else
            {
                float t = Ease(phase - 4f);
                _first.color = Color.Lerp(_signal, _base, t);
                _second.color = Color.Lerp(_ready, _signal, t);
            }
        }

        static float Ease(float value)
        {
            float t = Mathf.Clamp01(value);
            return 1f - (1f - t) * (1f - t);
        }

        public override void OnNetworkDespawn()
        {
            _effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
