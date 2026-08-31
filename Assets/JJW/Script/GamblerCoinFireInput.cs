using UnityEngine;
using UnityEngine.InputSystem;

namespace JJW.Script
{
    public class GamblerCoinFireInput : MonoBehaviour
    {
        [SerializeField]
        private GamblerCoinShooter coinShooter;

        private void Awake()
        {
            if (coinShooter == null)
            {
                coinShooter =
                    GetComponent<GamblerCoinShooter>();
            }
        }

        private void Update()
        {
            if (coinShooter == null ||
                Mouse.current == null)
            {
                return;
            }

            if (Mouse.current
                .leftButton
                .wasPressedThisFrame)
            {
                coinShooter.TryFire();
            }
        }
    }
}
