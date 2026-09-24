using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace NKY.Scripts.Servers
{
    public class StealthNetwork : NetworkBehaviour
    {
        private SpriteRenderer[] _renderers;
        private Image[] _images; // UI Image 컴포넌트 수집용

        private readonly NetworkVariable<bool> _isStealthed = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private void Start()
        {
            _renderers = GetComponentsInChildren<SpriteRenderer>();
            _images = GetComponentsInChildren<Image>(); // 자식의 모든 Image 컴포넌트 수집
        }

        public override void OnNetworkSpawn()
        {
            _isStealthed.OnValueChanged += OnStealthStateChanged;
            UpdateVisuals(_isStealthed.Value);
        }

        public override void OnNetworkDespawn()
        {
            _isStealthed.OnValueChanged -= OnStealthStateChanged;
        }

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
                // [은신 해제] 스프라이트 및 UI 이미지 100% 표시
                SetRenderersState(true, 1.0f);
                SetImagesState(true, 1.0f);
                return;
            }

            if (IsOwner)
            {
                // [내 화면] 캐릭터와 UI 이미지 모두 반투명(0.5f) 처리
                // 내 체력바는 계속 선명하게(1.0f) 보고 싶다면 SetImagesState(true, 1.0f)로 변경
                SetRenderersState(true, 0.5f);
                SetImagesState(true, 0.5f);
            }
            else
            {
                // [상대 화면] 캐릭터 및 UI 이미지 완전히 비활성화
                SetRenderersState(false, 0.0f);
                SetImagesState(false, 0.0f);
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

        private void SetImagesState(bool isEnabled, float alpha)
        {
            foreach (var img in _images)
            {
                if (img == null) continue;
                img.enabled = isEnabled;

                Color color = img.color;
                color.a = alpha;
                img.color = color;
            }
        }
    }
}