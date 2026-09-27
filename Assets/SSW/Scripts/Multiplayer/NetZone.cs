using System.Collections.Generic;
using RYU._01.Script.Potions;
using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    public sealed class NetZone : NetworkBehaviour
    {
        [SerializeField] NetStock _stock;
        [SerializeField] ParticleSystem[] _particles;
        readonly NetworkVariable<int> _kind = new NetworkVariable<int>();
        readonly NetworkVariable<float> _radius = new NetworkVariable<float>();
        NetPlayer _owner;
        PotionModifiers _mods;
        int _startKind;
        float _startRadius;
        float _life;
        float _tick;

        public void Init(NetPlayer owner, int kind, float radius, PotionModifiers mods)
        {
            _owner = owner;
            _startKind = kind;
            _startRadius = radius;
            _mods = mods;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _kind.Value = _startKind;
                _radius.Value = _startRadius;
            }
            foreach (ParticleSystem particle in _particles)
            {
                ParticleSystem.MainModule main = particle.main;
                main.startColor = _stock.At(_kind.Value).potionColor;
                ParticleSystem.ShapeModule shape = particle.shape;
                shape.radius = _radius.Value;
            }
        }

        void Update()
        {
            if (!IsServer || !IsSpawned) return;
            _life += Time.deltaTime;
            if (_life >= 3f || !NetGame.Current.CanFight)
            {
                NetworkObject.Despawn();
                return;
            }
            _tick += Time.deltaTime;
            if (_tick < 0.5f) return;
            _tick -= 0.5f;
            HashSet<Health> applied = new HashSet<Health>();
            foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, _radius.Value))
            {
                Health health = hit.GetComponentInParent<Health>();
                if (health != null && applied.Add(health))
                    PotionUse.Apply(_stock.At(_kind.Value), health, _owner, _mods);
            }
        }
    }
}
