using UnityEngine;

namespace SSW
{
    public class SandboxCollisionSetup : MonoBehaviour
    {
        [SerializeField] Collider2D _player;
        [SerializeField] Collider2D _dummy;
        [SerializeField] Collider2D[] _walls;

        PhysicsMaterial2D _slippery;

        public void Configure(Collider2D player, Collider2D dummy, Collider2D[] walls)
        {
            _player = player;
            _dummy = dummy;
            _walls = walls;
            Apply();
        }

        void Awake()
        {
            Apply();
        }

        void Start()
        {
            Apply();
        }

        void FixedUpdate()
        {
            if (_player != null && _dummy != null && !Physics2D.GetIgnoreCollision(_player, _dummy))
                Physics2D.IgnoreCollision(_player, _dummy, true);
        }

        void Apply()
        {
            if (_slippery == null)
            {
                _slippery = new PhysicsMaterial2D("Sandbox Slippery")
                {
                    friction = 0f,
                    bounciness = 0f
                };
            }

            if (_player != null)
            {
                _player.sharedMaterial = _slippery;
                Rigidbody2D body = _player.attachedRigidbody;
                if (body != null)
                {
                    body.freezeRotation = true;
                    body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                }
            }

            IgnorePlayerDummyCollisions();
            if (_walls == null) return;
            foreach (Collider2D wall in _walls)
            {
                if (wall != null) wall.sharedMaterial = _slippery;
            }
        }

        void IgnorePlayerDummyCollisions()
        {
            if (_player == null || _dummy == null) return;

            Collider2D[] playerColliders = _player.transform.root.GetComponentsInChildren<Collider2D>(true);
            Collider2D[] dummyColliders = _dummy.transform.root.GetComponentsInChildren<Collider2D>(true);
            foreach (Collider2D playerCollider in playerColliders)
            {
                foreach (Collider2D dummyCollider in dummyColliders)
                    Physics2D.IgnoreCollision(playerCollider, dummyCollider, true);
            }
        }
    }
}
