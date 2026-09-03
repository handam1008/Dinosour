using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject), typeof(Health))]
    public sealed class NetworkHealthBridge : NetworkBehaviour, IHealthNetworkBridge
    {
        readonly NetworkVariable<float> _healthValue = new NetworkVariable<float>();
        Health _health;

        void Awake()
        {
            _health = GetComponent<Health>();
        }

        public override void OnNetworkSpawn()
        {
            _healthValue.OnValueChanged += HandleNetworkHealthChanged;
            _health.OnHealthChanged += HandleLocalHealthChanged;

            if (IsServer)
                _healthValue.Value = _health.Current;
            else
                _health.ApplyNetworkState(_healthValue.Value);
        }

        public bool TryForwardDamage(DamageRequest request, out DamageResult pendingResult)
        {
            pendingResult = default;
            if (!IsSpawned || IsServer) return false;

            RequestDamageRpc(request.Amount, (int)request.Tags, request.IsCritical);
            pendingResult = new DamageResult(request.Amount, 0f, false);
            return true;
        }

        public bool TryForwardHeal(float amount)
        {
            if (!IsSpawned || IsServer) return false;
            RequestHealRpc(amount);
            return true;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void RequestDamageRpc(float amount, int tags, bool isCritical)
        {
            if (_health == null || amount <= 0f) return;
            _health.ReceiveDamage(new DamageRequest(null, amount, (DamageTag)tags, isCritical));
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void RequestHealRpc(float amount)
        {
            if (_health == null || amount <= 0f) return;
            _health.Heal(amount);
        }

        void HandleLocalHealthChanged(float current, float maximum)
        {
            if (IsServer && !Mathf.Approximately(_healthValue.Value, current))
                _healthValue.Value = current;
        }

        void HandleNetworkHealthChanged(float previous, float current)
        {
            if (_health != null && !Mathf.Approximately(_health.Current, current))
                _health.ApplyNetworkState(current);
        }

        public override void OnNetworkDespawn()
        {
            _healthValue.OnValueChanged -= HandleNetworkHealthChanged;
            if (_health != null) _health.OnHealthChanged -= HandleLocalHealthChanged;
        }
    }
}
