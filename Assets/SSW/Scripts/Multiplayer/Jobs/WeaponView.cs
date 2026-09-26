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
        [SerializeField] float _angle;
        [SerializeField] GunView _gun;
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
            if (_gun != null) { _gun.Render(active); return; }
            if (!active) return;
            float phase = Mathf.Clamp01((_until - Time.time) / 0.24f);
            Vector2 direction = phase > 0f ? _direction : _player.IsOwner ? _player.Aim : new Vector2(_player.Motion.FacingSign, 0f);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + _angle;
            if (_melee && phase > 0f) angle += Mathf.Lerp(-65f, 70f, phase) * Mathf.Sign(direction.x);
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            if(_job != PlayerJob.Assassin)
                _sprite.flipY = direction.x < 0f;
            else
                _sprite.flipX = direction.x < 0f;
            Vector2 center = _sprite.sprite.bounds.center;
            if (_sprite.flipY) center.y = -center.y;
            transform.position = (Vector2)_player.View.position
                + direction * ((_offset - (_melee ? 0f : phase * 0.15f)) * _player.Drive.Scale)
                - (Vector2)transform.TransformVector(center);
        }
    }
}
