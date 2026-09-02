using Unity.Netcode;
using UnityEngine;

namespace SSW
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject), typeof(NumberRoller))]
    public sealed class NetworkCardRelay : NetworkBehaviour
    {
        NumberRoller _numberRoller;

        void Awake()
        {
            _numberRoller = GetComponent<NumberRoller>();
        }

        public void ShareCard(
            Suit suit,
            int number,
            Vector2 position,
            Vector2 direction,
            float scale,
            float effectMultiplier,
            bool isJoker,
            bool isMirror)
        {
            if (!IsSpawned || !IsOwner) return;
            ShowCardRpc((int)suit, number, position, direction, scale, effectMultiplier, isJoker, isMirror);
        }

        [Rpc(SendTo.NotOwner, InvokePermission = RpcInvokePermission.Owner)]
        void ShowCardRpc(
            int suit,
            int number,
            Vector2 position,
            Vector2 direction,
            float scale,
            float effectMultiplier,
            bool isJoker,
            bool isMirror)
        {
            if (_numberRoller == null) return;
            _numberRoller.SpawnNetworkCard(
                (Suit)suit,
                number,
                position,
                direction,
                scale,
                effectMultiplier,
                isJoker,
                isMirror);
        }
    }
}
