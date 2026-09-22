using System;
using Unity.Mathematics.Geometry;
using UnityEngine;
using Random = UnityEngine.Random;

namespace NKY.Scripts.UI
{
    public class ManuDinoMover : MonoBehaviour
    {
        [SerializeField] private float destroyPadding = 1f;
        [SerializeField] private Transform standingPoint;
        
        private ManuDinoDataSo data;
        private SpriteRenderer spriteRenderer;

        private float speed;
        private Vector2 direction;
        private Vector2 _initialDirection;

        private float directionChangeTimer;
        
        private Camera cam;

        private void Awake()
        {
            cam = Camera.main;
        }

        public void Initialize(
            ManuDinoDataSo dinoData,
            Vector2 initialDirection)
        {
            data = dinoData;

            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            
            float scale = Random.Range(data.minScale, data.maxScale);
            transform.localScale = new Vector3(scale, scale, 1f);

            // 스프라이트 적용
            spriteRenderer.sprite = data.sprite;

            // 공룡마다 랜덤 속도
            speed = Random.Range(
                data.minSpeed,
                data.maxSpeed
            );

            // 생성 위치에 따른 초기 방향
            _initialDirection =  initialDirection;
            direction = initialDirection.normalized;
            UpdateDirectionChange();

            // 랜덤한 방향 전환 시간
            directionChangeTimer = Random.Range(
                data.minDirectionChangeTime,
                data.maxDirectionChangeTime
            );

            UpdateSpriteDirection();
        }

        private void Update()
        {
            Move();
            UpdateDirectionChange();
            CheckOutOfScreen();
        }

        private void Move()
        {
            transform.position +=
                (Vector3)(direction * (speed * Time.deltaTime));
        }

        private void UpdateDirectionChange()
        {
            spriteRenderer.sortingOrder = -Mathf.RoundToInt(standingPoint.position.y);
            directionChangeTimer -= Time.deltaTime;

            if (directionChangeTimer <= 0f)
            {
                float angle = Random.Range(
                    data.minTurnAngle,
                    data.maxTurnAngle
                );

                // 좌우 랜덤
                angle *= Random.value < 0.5f ? -1f : 1f;

                // 초기 방향을 기준으로 회전
                direction = RotateVector(_initialDirection, angle);

                directionChangeTimer = Random.Range(
                    data.minDirectionChangeTime,
                    data.maxDirectionChangeTime
                );

                UpdateSpriteDirection();
            }
        }

        private Vector2 RotateVector(Vector2 vector, float angle)
        {
            float radian = angle * Mathf.Deg2Rad;

            float cos = Mathf.Cos(radian);
            float sin = Mathf.Sin(radian);

            return new Vector2(
                vector.x * cos - vector.y * sin,
                vector.x * sin + vector.y * cos
            ).normalized;
        }

        private void UpdateSpriteDirection()
        {
            if (Mathf.Abs(direction.x) > 0.05f)
            {
                spriteRenderer.flipX = direction.x < 0f;
            }
        }
        
        private void CheckOutOfScreen()
        {
            if (cam == null)
                return;

            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;

            Vector3 pos = transform.position;

            if (pos.x < cam.transform.position.x - halfWidth - destroyPadding ||
                pos.x > cam.transform.position.x + halfWidth + destroyPadding ||
                pos.y < cam.transform.position.y - halfHeight - destroyPadding ||
                pos.y > cam.transform.position.y + halfHeight + destroyPadding)
            {
                Destroy(gameObject);
            }
        }
    }
}