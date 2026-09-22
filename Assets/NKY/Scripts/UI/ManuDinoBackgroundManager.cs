using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace NKY.Scripts.UI
{
    public class ManuDinoBackgroundManager : MonoBehaviour
    {
        [Header("공룡 프리팹")]
        [SerializeField] private ManuDinoMover dinoPrefab;

        [Header("공룡 데이터")]
        [SerializeField] private List<ManuDinoDataSo> dinoDatas = new();

        [Header("생성 설정")]
        [SerializeField] private float minDinoSpawnDelay = 0.8f;
        [SerializeField] private float maxDinoSpawnDelay = 3f;

        [Header("이동 범위")]
        [SerializeField] private Vector2 minBounds = new Vector2(-8f, -4.5f);
        [SerializeField] private Vector2 maxBounds = new Vector2(8f, 4.5f);

        [Header("화면 바깥 생성 거리")]
        [SerializeField] private float spawnPadding = 1f;

        private readonly List<ManuDinoMover> spawnedDinos = new();
        
        private float currentSpawnDelay;
        private float lastSpawnTime = -99f;

        private void Start()
        {
            currentSpawnDelay = Random.Range(minDinoSpawnDelay, maxDinoSpawnDelay);
        }

        private void Update()
        {
            if (lastSpawnTime + currentSpawnDelay < Time.time)
            {
                SpawnDinos();
                
                lastSpawnTime = Time.time;
                currentSpawnDelay = Random.Range(minDinoSpawnDelay, maxDinoSpawnDelay);
            }
        }

        private void SpawnDinos()
        {
            if (dinoPrefab == null)
            {
                Debug.LogError("Dino Prefab이 지정되지 않았습니다.");
                return;
            }

            if (dinoDatas == null || dinoDatas.Count == 0)
            {
                Debug.LogError("Dino Data가 하나도 등록되지 않았습니다.");
                return;
            }
            
            SpawnDino();
        }

        private void SpawnDino()
        {
            // 공룡 데이터 랜덤 선택
            ManuDinoDataSo randomData =
                dinoDatas[Random.Range(0, dinoDatas.Count)];

            // 테두리 위치 + 화면 안쪽으로 향하는 방향
            Vector2 spawnPosition;
            Vector2 moveDirection;

            GetBorderSpawnPosition(
                out spawnPosition,
                out moveDirection
            );

            // 공룡 생성
            ManuDinoMover dino = Instantiate(
                dinoPrefab,
                spawnPosition,
                Quaternion.identity,
                transform
            );

            // 데이터 + 시작 방향 전달
            dino.Initialize(
                randomData,
                moveDirection
            );

            spawnedDinos.Add(dino);
        }

        /// <summary>
        /// 화면 테두리 중 한 곳에서 공룡을 생성한다.
        /// 공룡은 화면 안쪽을 향하는 방향을 받는다.
        /// </summary>
        private void GetBorderSpawnPosition(
            out Vector2 spawnPosition,
            out Vector2 moveDirection)
        {
            int border = Random.Range(0, 4);

            switch (border)
            {
                // 왼쪽
                case 0:
                    spawnPosition = new Vector2(
                        minBounds.x - spawnPadding,
                        Random.Range(
                            minBounds.y,
                            maxBounds.y
                        )
                    );

                    moveDirection = Vector2.right;
                    break;


                // 오른쪽
                case 1:
                    spawnPosition = new Vector2(
                        maxBounds.x + spawnPadding,
                        Random.Range(
                            minBounds.y,
                            maxBounds.y
                        )
                    );

                    moveDirection = Vector2.left;
                    break;


                // 아래쪽
                case 2:
                    spawnPosition = new Vector2(
                        Random.Range(
                            minBounds.x,
                            maxBounds.x
                        ),
                        minBounds.y - spawnPadding
                    );

                    moveDirection = Vector2.up;
                    break;


                // 위쪽
                case 3:
                    spawnPosition = new Vector2(
                        Random.Range(
                            minBounds.x,
                            maxBounds.x
                        ),
                        maxBounds.y + spawnPadding
                    );

                    moveDirection = Vector2.down;
                    break;


                default:
                    spawnPosition = Vector2.zero;
                    moveDirection = Vector2.right;
                    break;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;

            // 범위의 중앙
            Vector2 center =
                (minBounds + maxBounds) * 0.5f;

            // 범위의 크기
            Vector2 size =
                maxBounds - minBounds;

            Gizmos.DrawWireCube(
                center,
                size
            );
        }
    }
}