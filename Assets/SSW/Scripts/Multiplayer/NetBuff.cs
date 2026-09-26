using System.Collections.Generic;
using UnityEngine;

namespace SSW
{
    public sealed class NetBuff : MonoBehaviour
    {
        [SerializeField] AugmentDrafter _source;
        [SerializeField] NetPlayer _player;
        [SerializeField] PlayerController _motion;
        [SerializeField] BuffHealth _health;
        [SerializeField] BuffSkill _skill;
        [SerializeField, Range(0.1f, 1f)] float _cooldownScale = 0.8f;
        readonly HashSet<CommonAugmentType> _owned = new HashSet<CommonAugmentType>();
        readonly HashSet<Augment> _assets = new HashSet<Augment>();
        public float MaxScale { get => _health.MaxScale; set => _health.MaxScale = value; }
        public float Scale => _health.Scale * _skill.Scale;
        public float AirJumpRatio => _skill.AirJumpRatio;
        public bool Has(CommonAugmentType type) => _owned.Contains(type);
        public bool Owns(Augment augment) => _assets.Contains(augment);
        public void SetBaseHealth(float value) => _health.SetBase(value);

        void Awake() => _source.AugmentGranted += Granted;

        void Granted(Augment augment)
        {
            _assets.Add(augment);
            if (augment is not CommonAugment common || !_owned.Add(common.type)) return;
            if (common.type == CommonAugmentType.CooldownReduction) _player.Cast.CooldownScale = _cooldownScale;
        }

        void FixedUpdate()
        {
            if (_player.IsSpawned && _player.IsServer)
                _motion.SpeedFactor = _health.SpeedScale * _skill.SpeedScale;
        }

        void OnDestroy() => _source.AugmentGranted -= Granted;
    }
}
