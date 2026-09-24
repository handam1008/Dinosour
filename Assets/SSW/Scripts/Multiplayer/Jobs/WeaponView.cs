using UnityEngine;

namespace SSW
{
    public sealed class WeaponView : MonoBehaviour
    {
        [SerializeField] NetPlayer _player;
        [SerializeField] PlayerJob _job;
        [SerializeField] SpriteRenderer _sprite;
        [SerializeField] bool _melee;
        [SerializeField] float _offset = 0.48f;
        float _until;
        Vector2 _direction = Vector2.right;

        public void Play(CastKind kind, Vector2 direction)
        {
            if (kind != CastKind.Press && kind != CastKind.Parry) return;
            _direction = direction;
            _until = Time.time + 0.24f;
        }

        void LateUpdate()
        {
            bool active = _player.IsSpawned && _player.Job == _job && _player.CanAct;
            _sprite.enabled = active;
            if (!active) return;
            float phase = Mathf.Clamp01((_until - Time.time) / 0.24f);
            Vector2 direction = phase > 0f ? _direction : _player.IsOwner ? _player.Aim : new Vector2(_player.Motion.FacingSign, 0f);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            if (_melee) angle += (_job == PlayerJob.Assassin ? -90f : 0f) + (phase > 0f ? Mathf.Lerp(-65f, 70f, phase) * Mathf.Sign(direction.x) : 0f);
            transform.position = (Vector2)_player.View.position + direction * (_offset - (_melee ? 0f : phase * 0.15f));
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            _sprite.flipY = direction.x < 0f;
        }
    }
}
