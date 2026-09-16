using UnityEngine;
using UnityEngine.InputSystem;

namespace SSW
{
    public sealed class MapHost : MonoBehaviour
    {
        [SerializeField] MapLayout _placedMap;
        [SerializeField] GameObject _baseMap;
        [SerializeField] Camera _camera;
        [SerializeField] SandboxCameraFollow _follow;
        [SerializeField] Rigidbody2D _player;
        [SerializeField] Rigidbody2D _dummy;
        [SerializeField] Health _health;
        [SerializeField] Health _dummyHealth;
        [SerializeField] PlayerIdentity _identity;
        [SerializeField] Collider2D _playerShape;
        [SerializeField] Collider2D _dummyShape;

        public MapLayout Map { get; private set; }

        void Start()
        {
            MapLayout selected = MapTravel.Take();
            Map = selected != null ? Instantiate(selected) : _placedMap;
            if (Map == null)
            {
                enabled = false;
                return;
            }

            _baseMap.SetActive(false);
            _follow.enabled = false;
            _camera.backgroundColor = Map.Background;
            _identity.SetJob(PlayerJob.Magician);
            Physics2D.IgnoreCollision(_playerShape, _dummyShape, true);
            ResetMap();
        }

        void Update()
        {
            if (Time.timeScale <= 0f) return;
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                ResetMap();
            if (_player.position.y < -8f || !_player.gameObject.activeSelf)
            {
                if (Map.ResetOnFall) ResetMap();
                else Respawn(_player, _health, Map.Spawn);
            }
            if (_dummy.position.y < -8f || !_dummy.gameObject.activeSelf)
                Respawn(_dummy, _dummyHealth, Map.Target);
        }

        void LateUpdate()
        {
            _camera.transform.position = new Vector3(Map.ViewCenter.x, Map.ViewCenter.y, -10f);
            _camera.orthographicSize = Mathf.Max(Map.ViewSize.y * 0.5f, Map.ViewSize.x / (2f * _camera.aspect));
        }

        public void ResetMap()
        {
            Respawn(_player, _health, Map.Spawn);
            Respawn(_dummy, _dummyHealth, Map.Target);
            Map.ResetMap();
        }

        static void Respawn(Rigidbody2D body, Health health, Vector3 position)
        {
            health.StopAllCoroutines();
            health.Heal(health.Max);
            body.gameObject.SetActive(true);
            body.position = position;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }
    }
}
