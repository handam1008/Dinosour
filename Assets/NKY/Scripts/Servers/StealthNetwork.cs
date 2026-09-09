using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace NKY.Scripts.Servers
{
    public class StealthNetwork : NetworkBehaviour
    {
        private SpriteRenderer[] _renderers;

        // 모든 클라이언트가 읽을 수 있고, 서버만 수정 가능한 은신 상태 변수
        private readonly NetworkVariable<bool> _isStealthed = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private void Awake()
        {
            _renderers = GetComponentsInChildren<SpriteRenderer>();
        }

        public override void OnNetworkSpawn()
        {
            // 은신 상태가 변할 때마다 각 클라이언트에서 시각 효과 갱신
            _isStealthed.OnValueChanged += OnStealthStateChanged;
            UpdateVisuals(_isStealthed.Value);
        }

        public override void OnNetworkDespawn()
        {
            _isStealthed.OnValueChanged -= OnStealthStateChanged;
        }

        /// <summary>
        /// 은신 발동 함수
        /// </summary>
        public void TriggerStealth(float duration)
        {
            if (IsServer)
            {
                StartCoroutine(Co_StealthTimer(duration));
            }
            else
            {
                RequestStealthServerRpc(duration);
            }
        }

        [ServerRpc]
        private void RequestStealthServerRpc(float duration)
        {
            StartCoroutine(Co_StealthTimer(duration));
        }

        private IEnumerator Co_StealthTimer(float duration)
        {
            _isStealthed.Value = true;
            yield return new WaitForSeconds(duration);
            _isStealthed.Value = false;
        }

        private void OnStealthStateChanged(bool previousValue, bool newValue)
        {
            UpdateVisuals(newValue);
        }

        private void UpdateVisuals(bool isStealthed)
        {
            if (!isStealthed)
            {
                // 은신 해제: 모두에게 100% 선명하게 표시
                SetRenderersState(true, 1.0f);
                return;
            }

            // 은신 발동 시: 화면 소유권(IsOwner)에 따라 다르게 렌더링
            if (IsOwner)
            {
                // [내 화면] 스프라이트를 켜두고 알파만 0.5 (반투명)
                SetRenderersState(true, 0.5f);
            }
            else
            {
                // [상대 화면] 스프라이트만 완전히 끔 (Collider는 100% 작동)
                SetRenderersState(false, 0.0f);
            }
        }

        private void SetRenderersState(bool isEnabled, float alpha)
        {
            foreach (var sr in _renderers)
            {
                if (sr == null) continue;
                
                sr.enabled = isEnabled;

                Color color = sr.color;
                color.a = alpha;
                sr.color = color;
            }
        }
    }
}