using System;
using RYU._01.Script.FeedBack;
using UnityEngine;

namespace RYU._01.Script.Potion
{
    public class Potion : MonoBehaviour
    {
         private FeedBackPlayer _feedBackPlayer;

        private void Awake()
        {
            _feedBackPlayer = GetComponent<FeedBackPlayer>();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            Transform hit = collision.transform;
            _feedBackPlayer.PlayAllFeedBacks();
            Destroy(gameObject);
        }
    }
}