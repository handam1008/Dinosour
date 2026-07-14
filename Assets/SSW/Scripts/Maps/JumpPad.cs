using UnityEngine;
using DG.Tweening;

namespace SSW
{
    public class JumpPad : MonoBehaviour
    {
        [SerializeField] float _launchVelocity = 16f;

        void OnTriggerEnter2D(Collider2D other)
        {
            Rigidbody2D rb = other.attachedRigidbody;
            if (rb == null) return;

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, _launchVelocity);

            transform.DOKill(true);
            transform.DOPunchScale(new Vector3(0.15f, -0.4f, 0f), 0.25f, 6, 0.6f);
        }
    }
}
