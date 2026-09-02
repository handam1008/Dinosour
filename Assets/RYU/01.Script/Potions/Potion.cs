using System;
using System.Collections.Generic;
using RYU._01.Script.FeedBack;
using SSW;
using UnityEngine;

namespace RYU._01.Script.Potions
{
    public class Potion : MonoBehaviour
    {
         private FeedBackPlayer _feedBackPlayer;
         private AbstractPotion _data;
         [SerializeField] private float splashRadious = 1.5f;

         private float _radiusMultiplier;
         
         private Component _owner;
         private bool _exploded;

         private readonly HashSet<Transform> _appliedRoots = new HashSet<Transform>();

         private Collider2D _collider;
         // 들고 있는 동안 시전자 몸에서 터지지 않도록 무시하다가, 몸에서 벗어나면 해제한다.
         private Collider2D _ownerCollider;
         private bool _thrown;

        private void Awake()
        {
            _feedBackPlayer = GetComponent<FeedBackPlayer>();
            _collider = GetComponent<Collider2D>();
        }

        public void Init(AbstractPotion data, float radiusMultiplier, Component owner)
        {
            _data = data;
            _radiusMultiplier = radiusMultiplier;
            _owner = owner;

            if (transform.parent != null)
                _ownerCollider = transform.parent.GetComponentInParent<Collider2D>();

            if (_ownerCollider != null)
                Physics2D.IgnoreCollision(_collider, _ownerCollider, true);
        }

        /// <summary>던진 시점부터 시전자와의 충돌 무시를 풀 수 있게 한다.</summary>
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

            _feedBackPlayer.PlayAllFeedBacks();
            
            float radious = splashRadious * _radiusMultiplier;
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radious);
            foreach (Collider2D hit in hits)
            {
                // 손에 든 포션처럼 대상의 자식 콜라이더가 함께 잡히면
                // Use 안의 GetComponentInParent가 같은 대상을 또 찾아 중복 적용된다.
                if (!_appliedRoots.Add(hit.transform.root)) continue;
                _data.Use(hit.gameObject, _owner);
            }
            
            Destroy(gameObject);
        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, splashRadious * _radiusMultiplier);
        }
    }
}