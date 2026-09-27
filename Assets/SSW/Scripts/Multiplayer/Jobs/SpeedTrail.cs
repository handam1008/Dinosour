using System.Collections;
using System.Reflection;
using RYU._01.Script.FeedBack;
using UnityEngine;

namespace SSW
{
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class SpeedTrail : MonoBehaviour
    {
        [SerializeField] NetPlayer _player;
        [SerializeField] SpeedAfterimage _effect;
        [SerializeField] SpriteRenderer[] _sprites;
        [SerializeField] float _fadeDuration;
        static readonly FieldInfo Until = typeof(SpeedAfterimage).GetField("_until", BindingFlags.Instance | BindingFlags.NonPublic);
        SpriteRenderer[] _visible;
        float _until;
        bool _released;
        public bool Active => !_released && _effect.enabled && Time.time < _until;
        public int Sources { get; private set; }
        public float Remaining => Mathf.Max(0f, (float)Until.GetValue(_effect) - Time.time);

        void Start()
        {
            _visible = new SpriteRenderer[_sprites.Length];
            typeof(SpeedAfterimage).GetField("_renderers", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_effect, _visible);
        }

        public void Track(float duration) => _until = Mathf.Max(_until, Time.time + duration);

        public void Play(float duration)
        {
            Track(duration);
            _effect.Play(duration);
        }

        void Update()
        {
            if (_released) return;
            _effect.enabled = _player.CanAct && (!_player.Effects.Hidden || _player.IsOwner);
            Sources = 0;
            for (int i = 0; i < _sprites.Length; i++)
            {
                SpriteRenderer sprite = _sprites[i];
                bool visible = sprite.enabled && sprite.gameObject.activeInHierarchy && sprite.sprite != null && sprite.color.a > 0f;
                _visible[i] = visible ? sprite : null;
                if (visible) Sources++;
            }
        }

        public void Release()
        {
            if (_released) return;
            _released = true;
            _effect.enabled = false;
            transform.SetParent(null, true);
            DontDestroyOnLoad(gameObject);
            StartCoroutine(Finish());
        }

        IEnumerator Finish()
        {
            yield return new WaitForSeconds(_fadeDuration);
            yield return null;
            Destroy(gameObject);
        }
    }
}
