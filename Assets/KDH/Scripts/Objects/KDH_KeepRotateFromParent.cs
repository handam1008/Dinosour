using UnityEngine;

namespace KDH.Scripts.Objects
{
    public class KDH_KeepRotateFromParent : MonoBehaviour
    {
        [SerializeField] private float startPos;
        public float speed = 2f;    // 회전 속도
        public float radius = 5f;   // 원점과의 거리 (반지름)
        private float angle = 0f;   // 각도

        void Update()
        {
            angle += speed * Time.deltaTime;

            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            transform.position = new Vector3(x, z, transform.position.z);
        }
    }
}
