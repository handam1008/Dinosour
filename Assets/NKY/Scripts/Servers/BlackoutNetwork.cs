using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace NKY.Scripts.Servers
{
    public class BlackoutNetwork : NetworkBehaviour
    {
        [Header("암전 UI 오버레이")]
        [SerializeField] private GameObject _blackoutOverlayUI; // 로컬 UI 오버레이 오브젝트

        private void Awake()
        {
            _blackoutOverlayUI.SetActive(false);
        }

        /// <summary>
        /// 스킬 시전자가 호출하는 암전 트리거
        /// </summary>
        public void TriggerBlackout(float duration)
        {
            if (IsServer)
            {
                ApplyBlackoutToOpponent(duration);
            }
            else
            {
                RequestBlackoutServerRpc(duration);
            }
        }

        [ServerRpc]
        private void RequestBlackoutServerRpc(float duration)
        {
            ApplyBlackoutToOpponent(duration);
        }

        private void ApplyBlackoutToOpponent(float duration)
        {
            // 1v1에서 내 ClientId가 아닌 상대방 ClientId 탐색
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.ClientId != OwnerClientId)
                {
                    var opponentObject = client.PlayerObject;
                    if (opponentObject != null && opponentObject.TryGetComponent<BlackoutNetwork>(out var opponentNet))
                    {
                        // 상대방 클라이언트에게만 암전 효과 연출 전송
                        opponentNet.ReceiveBlackoutClientRpc(duration);
                    }
                }
            }
        }

        [ClientRpc]
        private void ReceiveBlackoutClientRpc(float duration)
        {
            // 당하는 상대방 본인(IsOwner) 화면에서만 UI 오버레이 활성화
            if (IsOwner && _blackoutOverlayUI != null)
            {
                StartCoroutine(Co_BlackoutEffect(duration));
            }
        }

        private IEnumerator Co_BlackoutEffect(float duration)
        {
            _blackoutOverlayUI.SetActive(true);

            yield return new WaitForSeconds(duration);

            _blackoutOverlayUI.SetActive(false);
        }
    }
}