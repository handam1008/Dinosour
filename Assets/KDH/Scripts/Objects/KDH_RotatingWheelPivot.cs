using UnityEngine;

namespace KDH.Scripts.Objects
{
    public class KDH_RotatingWheelPivot : MonoBehaviour
    {
        [SerializeField] private Transform[] parts;       // 흰 발판 4개
        [SerializeField] private float rotationSpeed = 30f;
        [SerializeField] private bool keepPartsLevel = true; // 발판을 항상 수평으로 유지

        private float[] radii;
        private float[] angles;

        void Awake()
        {
            radii = new float[parts.Length];
            angles = new float[parts.Length];

            for (int i = 0; i < parts.Length; i++)
            {
                Vector3 offset = parts[i].position - transform.position; // pivot 기준 오프셋
                radii[i] = offset.magnitude;
                angles[i] = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
            }
        }

        void FixedUpdate()
        {
            float delta = rotationSpeed * Time.fixedDeltaTime;

            for (int i = 0; i < parts.Length; i++)
            {
                angles[i] += delta;
                float rad = angles[i] * Mathf.Deg2Rad;
                Vector3 newPos = transform.position + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * radii[i];
                parts[i].position = newPos;

                if (!keepPartsLevel)
                    parts[i].Rotate(0f, 0f, delta); // 체크 해제하면 발판도 같이 기울며 돎
            }
        }
    }
}