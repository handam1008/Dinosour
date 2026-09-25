using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    public sealed class BattleMap : NetworkBehaviour
    {
        [SerializeField] string _title;
        [SerializeField] SpawnPoints _spawns;
        [SerializeField] Bounds _bounds;
        [SerializeField] float _fallY = -14f;
        [SerializeField] Behaviour[] _serverOnly;
        readonly HashSet<ulong> _ready = new HashSet<ulong>();

        public string Title => _title;
        public Bounds Bounds => _bounds;
        public float FallY => _fallY;
        public bool Ready => IsServer && _ready.Count == NetworkManager.ConnectedClientsIds.Count;
        public Vector3 Spawn(int slot) => _spawns.At(slot);

        public override void OnNetworkSpawn()
        {
            NetGame.Current.Arena.UseMap(this);
            if (IsServer) _ready.Add(NetworkManager.ServerClientId);
            else
            {
                foreach (Behaviour item in _serverOnly) Destroy(item);
                Physics2D.SyncTransforms();
                LoadedRpc();
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void LoadedRpc(RpcParams rpc = default)
        {
            if (NetworkManager.ConnectedClients.ContainsKey(rpc.Receive.SenderClientId))
                _ready.Add(rpc.Receive.SenderClientId);
        }

        public override void OnNetworkDespawn()
        {
            foreach (Transform item in GetComponentsInChildren<Transform>(true))
                DG.Tweening.DOTween.Kill(item);
            foreach (SpriteRenderer item in GetComponentsInChildren<SpriteRenderer>(true))
                DG.Tweening.DOTween.Kill(item);
        }
    }
}
