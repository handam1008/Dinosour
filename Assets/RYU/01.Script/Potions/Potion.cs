using System;
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

        private void Awake()
        {
            _feedBackPlayer = GetComponent<FeedBackPlayer>();
        }

        public void Init(AbstractPotion data, float radiusMultiplier)
        {
            _data = data;
            _radiusMultiplier = radiusMultiplier;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            _feedBackPlayer.PlayAllFeedBacks();
            
            float radious = splashRadious * _radiusMultiplier;
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radious);
            foreach (Collider2D hit in hits)
            {
                _data.Use(hit.gameObject);
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