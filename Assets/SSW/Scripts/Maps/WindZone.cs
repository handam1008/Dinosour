using UnityEngine;

namespace SSW
{
    public sealed class WindZone : MonoBehaviour, IMapAction, IMapReset
    {
        [SerializeField] Transform[] _arrows;
        [SerializeField] SpriteRenderer _field;
        [SerializeField] SpriteRenderer _lamp;
        [SerializeField] Color _onColor;
        [SerializeField] Color _offColor;
        [SerializeField] float _height = 8f;
        [SerializeField] float _riseSpeed = 8f;
        [SerializeField] float _acceleration = 65f;

        public bool IsOn { get; private set; } = true;

        void Update()
        {
            for (int i = 0; i < _arrows.Length; i++)
            {
                float phase = (Time.time * (IsOn ? 0.7f : 0f) + (float)i / _arrows.Length) % 1f;
                _arrows[i].localPosition = new Vector3(0f, (phase - 0.5f) * _height, 0f);
                _arrows[i].gameObject.SetActive(IsOn);
            }

            _field.color = IsOn ? _onColor : _offColor;
            _lamp.color = IsOn ? new Color(0.72f, 1f, 0.87f) : new Color(0.35f, 0.32f, 0.46f);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (!IsOn) return;
            Rigidbody2D body = other.attachedRigidbody;
            if (body == null || !body.TryGetComponent<IForceReceiver>(out _)) return;

            Vector2 velocity = body.linearVelocity;
            velocity.y = Mathf.MoveTowards(velocity.y, _riseSpeed, _acceleration * Time.fixedDeltaTime);
            body.linearVelocity = velocity;
        }

        public void Use()
        {
            IsOn = !IsOn;
        }

        public void ResetMap()
        {
            IsOn = true;
        }
    }
}
