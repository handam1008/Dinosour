using NKY.Lib.EventChannel;
using SSW;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NKY.Scripts.Skill
{
    public class PlayerSkillModule : MonoBehaviour
    {
        [SerializeField] private AbstractPlayerSkillSo _skillData;
        [SerializeField] private VoidEventChannelSO _onSkillUsedEvent;
        
        private PlayerController _player;
        
        // 런타임 가변 상태는 SO가 아닌 인스턴스 컴포넌트에서 보유
        private float _lastUsedTime = -999f;
        private bool _isReUseReady = false;
        private GameObject _activeSkillInstance;

        private void Awake()
        {
            _player = GetComponentInParent<PlayerController>();
        }

        private void Update()
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                TryUseSkill();
            }
        }

        public void TryUseSkill()
        {

            if (_skillData == null)
                return;

            if (Time.time - _lastUsedTime >= _skillData.SkillCooldown)
            {
                Vector3 aimDir = GetAimDirection();

                UseSkillServerRpc(aimDir);

                _lastUsedTime = Time.time;
            }
        }
        
        private void UseSkillServerRpc(Vector3 aimDirection)
        {
            GameObject skill = _skillData.ExecuteSkill(
                _player,
                aimDirection
            );

            if (skill == null)
                return;
            
            _onSkillUsedEvent?.Raise();
        }

        private Vector3 GetAimDirection()
        {
            if (Camera.main == null) return transform.up;
            
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            mouseWorldPos.z = 0f;
            Vector3 dir = (mouseWorldPos - _player.transform.position).normalized;
            return dir.sqrMagnitude > 0.001f ? dir : transform.up;
        }
    }
}