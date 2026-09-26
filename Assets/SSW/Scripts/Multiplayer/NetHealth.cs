using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    [DisallowMultipleComponent]
    public sealed class NetHealth : NetworkBehaviour, IHealthAuthority
    {
        [SerializeField] Health _health;
        readonly NetworkVariable<Vector2> _state = new NetworkVariable<Vector2>();

        public bool CanChange => IsSpawned && IsServer && NetGame.Current.CanFight;

        public override void OnNetworkSpawn()
        {
            _state.OnValueChanged += Sync;
            _health.OnHealthChanged += Publish;
            _health.OnDamageDealt += PublishDamage;
            _health.OnHealed += PublishHeal;
            if (IsServer) Publish(_health.Current, _health.Max);
            else Sync(default, _state.Value);
        }

        void Publish(float current, float maximum)
        {
            if (IsServer) _state.Value = new Vector2(current, maximum);
        }

        void PublishDamage(float amount, bool critical)
        {
            if (IsServer) DamageRpc(amount, critical);
        }

        void PublishHeal(float amount)
        {
            if (IsServer) HealRpc(amount);
        }

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        void HealRpc(float amount) => _health.ApplyNetworkHeal(amount);

        [Rpc(SendTo.NotServer, InvokePermission = RpcInvokePermission.Server)]
        void DamageRpc(float amount, bool critical)
        {
            _health.ApplyNetworkDamage(amount, critical);
        }

        void Sync(Vector2 previous, Vector2 current)
        {
            if (!IsServer) _health.ApplyNetworkState(current.x, current.y);
        }

        public override void OnNetworkDespawn()
        {
            _state.OnValueChanged -= Sync;
            _health.OnHealthChanged -= Publish;
            _health.OnDamageDealt -= PublishDamage;
            _health.OnHealed -= PublishHeal;
        }
    }
}
