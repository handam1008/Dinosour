using System.Collections.Generic;
using UnityEngine;

namespace SSW
{
    public sealed class NetBuff : MonoBehaviour, IOutgoingDamageModifier, IDamageDealtListener
    {
        [SerializeField] AugmentDrafter _source;
        [SerializeField] NetPlayer _player;
        [SerializeField] PlayerController _motion;
        readonly HashSet<CommonAugmentType> _owned = new HashSet<CommonAugmentType>();
        float _baseMax;
        float _confidenceUntil;
        bool _confident;
        public int Priority => 20;

        void Awake()
        {
            _baseMax = _player.Health.Max;
            _source.AugmentGranted += Granted;
        }

        void Granted(Augment augment)
        {
            if (augment is not CommonAugment common || !_owned.Add(common.type)) return;
            if (_player.IsServer) RefreshMax();
        }

        bool Has(CommonAugmentType type) => _owned.Contains(type);
        float Missing => 1f - _player.Health.Current / _player.Health.Max;

        void Update()
        {
            if (!_player.IsServer) return;
            _motion.SpeedFactor = Has(CommonAugmentType.Berserker) ? 1f + Missing * 0.4f : 1f;
            if (_confident && Time.time >= _confidenceUntil)
            {
                _confident = false;
                RefreshMax();
            }
        }

        void RefreshMax()
        {
            float value = _baseMax;
            if (Has(CommonAugmentType.Giant)) value *= 1.8f;
            if (Has(CommonAugmentType.GlassCannon)) value *= 0.7f;
            if (_confident) value *= 1.2f;
            _player.Health.SetMax(value);
        }

        public float ModifyOutgoingDamage(float amount)
        {
            if (Has(CommonAugmentType.GlassCannon)) amount *= 1.35f;
            if (Has(CommonAugmentType.Berserker)) amount *= 1f + Missing * 0.5f;
            return amount;
        }

        public void OnDamageDealt(DamageRequest request, DamageResult result)
        {
            if (!_player.IsServer || !result.WasApplied) return;
            if (Has(CommonAugmentType.Vampire)) _player.Health.Heal(result.AppliedAmount * 0.3f);
            if (!Has(CommonAugmentType.Confidence)) return;
            _confidenceUntil = Time.time + 3f;
            if (_confident) return;
            _confident = true;
            RefreshMax();
        }

        void OnDestroy()
        {
            _source.AugmentGranted -= Granted;
        }
    }
}