using System;
using UnityEngine;

namespace KDH.Scripts.Objects
{
    public class KDH_Waterfall : MonoBehaviour
    {
        [SerializeField] private float force;
        private Vector2 _direction;

        private void Awake()
        {
            _direction.y = force;
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            if (collision.TryGetComponent(out Rigidbody2D rigidBody))
                rigidBody.AddForceY(_direction.y,  ForceMode2D.Impulse);
        }
    }
}
