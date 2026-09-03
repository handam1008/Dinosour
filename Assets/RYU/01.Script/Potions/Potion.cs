using System.Collections.Generic;
using RYU._01.Script.FeedBack;
using SSW;
using UnityEngine;

namespace RYU._01.Script.Potions
{
    public class Potion : MonoBehaviour
    {
        [SerializeField] private float splashRadious = 1.5f;
        [SerializeField] private GameObject _zonePrefab; // 잔류형 장판 (비어 있으면 장판 없음)

        private FeedBackPlayer _feedBackPlayer;
        private AbstractPotion _data;
        private PotionModifiers _mods = PotionModifiers.None;
        private Component _owner;

        private Rigidbody2D _rb;
        private Collider2D _collider;

        private bool _exploded;
        private int _bounceLeft;

        private readonly HashSet<Transform> _appliedRoots = new HashSet<Transform>();

        // 들고 있는 동안 시전자 몸에서 터지지 않도록 무시하다가, 몸에서 벗어나면 해제한다.
        private Collider2D _ownerCollider;
        private bool _thrown;

        // 부채꼴로 복제할 때 어떤 포션인지 알아야 해서 열어둔다
        public AbstractPotion Data => _data;

        private float Radius => splashRadious * _mods.Splash;

        private void Awake()
        {
            _feedBackPlayer = GetComponent<FeedBackPlayer>();
            _collider = GetComponent<Collider2D>();
            _rb = GetComponent<Rigidbody2D>();
        }

        public void Init(AbstractPotion data, PotionModifiers mods, Component owner)
        {
            _data = data;
            _mods = mods;
            _owner = owner;
            _bounceLeft = mods.Bounce;

            if (transform.parent != null)
                _ownerCollider = transform.parent.GetComponentInParent<Collider2D>();

            if (_ownerCollider != null && _collider != null)
                Physics2D.IgnoreCollision(_collider, _ownerCollider, true);
        }

        public void Release()
        {
            _thrown = true;
        }

        private void FixedUpdate()
        {
            if (!_thrown || _ownerCollider == null) return;
            if (_collider.Distance(_ownerCollider).isOverlapped) return;

            Physics2D.IgnoreCollision(_collider, _ownerCollider, false);
            _ownerCollider = null;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (_exploded) return;
            _exploded = true;

            // 깨진 유리병: 스플래시로라도 적을 맞췄으면 그대로 깨지고,
            // 아무도 못 맞췄을 때만 벽·바닥에 튕긴다
            bool hitTarget = Explode();

            if (_bounceLeft > 0 && !hitTarget)
            {
                _bounceLeft--;
                Bounce(collision);

                _exploded = false;
                _appliedRoots.Clear(); // 다음 폭발에서는 같은 대상도 다시 맞을 수 있다
                return;
            }

            Destroy(gameObject);
        }

        // 스플래시 범위 안의 대상들에게 효과를 적용한다.
        // 반환값: 시전자 포함, '체력 있는 대상'을 하나라도 맞췄는지
        private bool Explode()
        {
            if (_feedBackPlayer != null) _feedBackPlayer.PlayAllFeedBacks(_mods.Splash);
            if (_data == null) return false;

            bool hitTarget = false;

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, Radius);
            foreach (Collider2D hit in hits)
            {
                // 대상의 자식 콜라이더가 여러 개 잡혀도 한 번만 적용
                if (!_appliedRoots.Add(hit.transform.root)) continue;

                // 누구든 체력 있는 대상을 맞췄으면 더 튕기지 않는다 (자기 자신 포함)
                if (hit.GetComponentInParent<IDamageable>() != null) hitTarget = true;

                _data.Use(hit.gameObject, _owner, _mods);
            }

            if (_mods.LeaveZone) SpawnZone();
            return hitTarget;
        }

        // 잔류형 포션: 터진 자리에 장판을 남긴다
        private void SpawnZone()
        {
            if (_zonePrefab == null) return;

            GameObject go = Instantiate(_zonePrefab, transform.position, Quaternion.identity);
            PotionZone zone = go.GetComponent<PotionZone>();
            if (zone != null) zone.Init(_data, _mods, _owner, Radius);
        }

        // 트리거라서 물리로는 안 튕긴다. 벽 방향을 구해 속도를 직접 반사시킨다.
        // 튕기는 방향이 반대로 보이면 -normal 의 부호를 뒤집으면 된다.
        private void Bounce(Collider2D wall)
        {
            if (_rb == null || _collider == null) return;

            Vector2 normal = _collider.Distance(wall).normal;
            _rb.linearVelocity = Vector2.Reflect(_rb.linearVelocity, -normal);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, Radius);
        }
    }
}
