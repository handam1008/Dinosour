using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SSW
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkPlayerOwner : NetworkBehaviour
    {
        readonly NetworkVariable<bool> _facingLeft = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

        Transform _visual;

        public override void OnNetworkSpawn()
        {
            Animator animator = GetComponentInChildren<Animator>(true);
            _visual = animator != null ? animator.transform : null;

            _facingLeft.OnValueChanged += HandleFacingChanged;
            SetLocalControl(IsOwner);
            ApplyFacing(_facingLeft.Value);
            StartCoroutine(RefreshCameraNextFrame());
        }

        void Update()
        {
            if (!IsSpawned || !IsOwner || _visual == null) return;

            bool facingLeft = _visual.localScale.x < 0f;
            if (_facingLeft.Value != facingLeft)
                _facingLeft.Value = facingLeft;
        }

        void SetLocalControl(bool isLocal)
        {
            PlayerInput input = GetComponent<PlayerInput>();
            if (input != null) input.enabled = isLocal;

            SetEnabled(GetComponent<PlayerController>(), isLocal);
            SetEnabled(GetComponent<NumberRoller>(), isLocal);
            SetEnabled(GetComponent<SuitSelector>(), isLocal);
            SetEnabled(GetComponent<MagicianAugmentController>(), isLocal);
            SetEnabled(GetComponent<LocalPlayerLoadoutApplier>(), isLocal);
        }

        static void SetEnabled(Behaviour behaviour, bool value)
        {
            if (behaviour != null) behaviour.enabled = value;
        }

        void HandleFacingChanged(bool previous, bool current)
        {
            ApplyFacing(current);
        }

        void ApplyFacing(bool facingLeft)
        {
            if (_visual == null) return;
            Vector3 scale = _visual.localScale;
            float width = Mathf.Max(0.0001f, Mathf.Abs(scale.x));
            scale.x = facingLeft ? -width : width;
            _visual.localScale = scale;
        }

        IEnumerator RefreshCameraNextFrame()
        {
            yield return null;
            yield return null;
            RefreshCameraTargets();
        }

        static void RefreshCameraTargets()
        {
            NetworkPlayerOwner[] players = FindObjectsByType<NetworkPlayerOwner>();
            NetworkPlayerOwner local = null;
            NetworkPlayerOwner opponent = null;

            foreach (NetworkPlayerOwner player in players)
            {
                if (!player.IsSpawned) continue;
                if (player.IsOwner) local = player;
                else if (opponent == null) opponent = player;
            }

            if (local == null) return;
            SandboxCameraFollow cameraFollow = FindAnyObjectByType<SandboxCameraFollow>();
            if (cameraFollow != null)
                cameraFollow.SetTargets(local.transform, opponent != null ? opponent.transform : null);
        }

        public override void OnNetworkDespawn()
        {
            _facingLeft.OnValueChanged -= HandleFacingChanged;
            RefreshCameraTargets();
        }
    }
}
