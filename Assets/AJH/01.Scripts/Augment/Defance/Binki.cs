using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SSW
{
    public class Blink : MonoBehaviour
    {
        [SerializeField] float _blinkDistance = 5f;
        [SerializeField] float _blinkDuration = 0.2f;

        AugmentDrafter _drafter;
        Defance _defance;
        bool _unlocked;
        bool _isBlinking;

        void Awake()
        {
            _drafter = GetComponent<AugmentDrafter>();
            _defance = GetComponent<Defance>();

            _drafter.OnAugmentSelected += HandleSelected;
            _defance.OnBarrierUsed += DoBlink;
        }

        void OnDestroy()
        {
            if (_drafter != null) _drafter.OnAugmentSelected -= HandleSelected;
            if (_defance != null) _defance.OnBarrierUsed -= DoBlink;
        }

        void HandleSelected(Augment augment)
        {
            if (augment is not CommonAugment common) return;
            if (common.type != CommonAugmentType.Blink) return;

            _unlocked = true;
        }

        void DoBlink()
        {
            if (!_unlocked || _isBlinking) return;

            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            mouseWorld.z = 0f;

            Vector3 dir = (mouseWorld - transform.position).normalized;
            float dist = Vector3.Distance(transform.position, mouseWorld);
            float moveDist = Mathf.Min(dist, _blinkDistance);

            Vector3 targetPos = transform.position + dir * moveDist;
            StartCoroutine(BlinkMove(targetPos));
        }

        IEnumerator BlinkMove(Vector3 targetPos)
        {
            _isBlinking = true;

            Vector3 startPos = transform.position;
            float elapsed = 0f;

            while (elapsed < _blinkDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _blinkDuration;
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            transform.position = targetPos;
            _isBlinking = false;
        }
    }
}