using UnityEngine;

namespace SSW
{
    public class GravityCycler : MonoBehaviour
    {
        [SerializeField] float _normalDuration = 6f;
        [SerializeField] float _lowGravityDuration = 3f;
        [SerializeField] float _lowGravityScale = 0.12f;

        Vector2 _baseGravity;
        float _timer;
        bool _low;

        void Awake()
        {
            _baseGravity = Physics2D.gravity;
            _timer = _normalDuration;
        }

        void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            _low = !_low;
            _timer = _low ? _lowGravityDuration : _normalDuration;
            Physics2D.gravity = _low ? _baseGravity * _lowGravityScale : _baseGravity;
        }

        void OnDestroy()
        {
            Physics2D.gravity = _baseGravity;
        }
    }
}
