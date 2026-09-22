using UnityEngine;

namespace NKY.Scripts.UI
{
    [CreateAssetMenu(fileName = "ManuDinoDataSo", menuName = "SO/UI/MainManuDino", order = 0)]
    public class ManuDinoDataSo : ScriptableObject
    {
        [Header("기본 정보")]
        public string dinoName;

        [Header("그래픽")]
        public Sprite sprite;

        [Header("이동 속도")]
        [Min(0f)]
        public float minSpeed = 0.5f;

        [Min(0f)]
        public float maxSpeed = 1.5f;
        
        [Header("크기")]
        [Min(0f)]
        public float minScale = 0.5f;

        [Min(0f)]
        public float maxScale = 1.5f;

        [Header("방향 전환")]
        [Min(0.1f)]
        public float minDirectionChangeTime = 2f;

        [Min(0.1f)]
        public float maxDirectionChangeTime = 5f;

        [Range(0f, 360f)]
        public float minTurnAngle = 0f;
        
        [Range(0f, 360f)]
        public float maxTurnAngle = 90f;
    }
}