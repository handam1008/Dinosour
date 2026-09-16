using System;
using SSW;
using UnityEngine;
using UnityEngine.Events;

namespace KDH.Scripts.Objects
{
    public class KDH_AutoAddForce : MonoBehaviour
    {
        [SerializeField] private Vector2 direction;
        [SerializeField] private float force;
        
        [SerializeField] private float tickTime = 2f;
        private PlayerController[] players;
        private float _timer;

        [SerializeField] private UnityEvent onForce;
        
        private void Start()
        {
            players = FindObjectsByType<PlayerController>();
        }
        
        private void Update()
        {
            _timer +=  Time.deltaTime;

            if (_timer > tickTime)
            {
                AddForceBRO();
                onForce?.Invoke();
                _timer = 0;
            }
        }

        private void AddForceBRO()
        {
            foreach (var player in players)
            {
                if (player.TryGetComponent(out IForceReceiver receiver))
                    receiver.ApplyForce(direction.normalized * force, ForceMode2D.Impulse);
            }
        }
    }
}
