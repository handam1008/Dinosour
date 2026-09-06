using UnityEngine;

public class KHG_Swordglow : MonoBehaviour
{
    [Header("크기 조절 설정")]
    [SerializeField] private float growthPerSecond = 0.1f;
    [SerializeField] private float maxScale = 3.0f;

    void FixedUpdate()
    {
        if (transform.localScale.x < maxScale)
        {
            transform.localScale += Vector3.one * growthPerSecond * Time.fixedDeltaTime;

            Physics2D.SyncTransforms();

        }
    }
}