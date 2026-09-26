# 원본 직업 수치 연결

`Assets/SSW/Editor/StatSources.asset`에 직업별 원본을 지정한다. 프리팹과 씬 중 하나만 선택한다. 씬은 저장된 루트 경로도 필요하다. 출력은 `Resources/Network/Stats.asset`과 기존 `Potions.asset`이며 팀원 원본을 수정하지 않는다.

원본과 연결된 에셋을 저장하면 import 뒤에 갱신한다. Play 진입과 빌드 직전에도 다시 추출한다. 저장하지 않은 원본 씬, 누락 참조, 잘못된 수치는 Play·빌드를 중단하고 이유를 표시한다. 씬을 자동 저장하거나 변경을 버리지 않는다.

| 직업 | 원본 | 기본 체력 | 평타 / 간격 |
|---|---|---:|---|
| 마녀 | `Assets/RYU/02.Prefabs/Agent/Witch.prefab` | 100 | 포션 SO / 0.15초 |
| 마술사 | `Assets/SSW/Prefabs/PlayerMagician.prefab` | 80 | 5 / 1.5초 |
| 칼잡이 | `Assets/KHG/02.Prefab/Player.prefab` | 100 | 10 / 2초 |
| 암살자 | `Assets/NKY/Player (1).prefab` | 1000 | 20 / 0.7초 |
| 도박사 | `Assets/JJW/Scene/Gamblinger.unity`, `Player (1)` | 100 | 10 / 0.2초 |
| 총잡이 | `Assets/KDH/GameModules/KDH_Player Variant.prefab` | 100 | 15 / 원본 간격 제한 없음 |

위 표는 Base `111e395`에서 추출한 초기 검증 값이며 `c4e593c` 병합 후에도 같았다. 이후 `a443d6d` 병합에서 팀원이 칼잡이 피해를 15, 공격 지속을 1초, 마녀 Spread를 -1로 변경하여 StatBake가 그대로 반영했다. 원본 변경으로 생성된 Stats.asset 차이는 이 세 필드뿐이다. 암살자 체력 1000, 칼잡이 평타 간격 2초·대쉬 7초·패링 6초는 그대로다.

서버가 스폰 때 선택한 기본 수치를 NGO 변수로 전달한다. 접속자의 예측·발사·탄창 UI도 같은 값을 소비한다. 라운드 재스폰 때 기본값을 먼저 적용한 뒤 기존 증강을 복원한다. 프로토콜은 18이며 양쪽 모두 새 빌드를 사용해야 한다.

총잡이 탄창은 1~15칸이다. 원본 MaxAmmo와 AmmoPrefabs 개수가 다르면 추출을 거부한다. 원본의 미사용 AttackSpeed·ReloadSpeed 대신 실제 사용하는 ChargeSpeed를 읽는다. 마녀 포션 SO와 가중치는 원본 참조를 유지하고, 기본 목록 변경은 기존 포션 ID를 바꾸지 않는다.

기본 수치 범위는 체력·이동·점프·중력·코요테 시간, 공격 간격·피해·근접 크기, 투사체 속도·중력·수명·회전·크기, 대쉬·패링·스킬 재사용 시간, 탄창·재장전·충전이다. 모든 원본 코드와 애니메이션을 복제하는 기능은 아니다. 칼잡이 근접은 정지 자세 AABB이고 원본 애니메이션의 회전 OBB 스윕과 다르다. 패링 박스, 잭팟, 증강별 추가 효과는 기존 서버 구현을 유지한다. 원본에 직렬화된 설정이 없는 유지 상수는 `Setup.cs` 출력에 항목별로 기록한다.

## 검증

- `MathChecks.cs`: 1353개. 회전 박스·캡슐 충돌을 Unity 물리와 비교하고 지연 중력과 직렬화를 검사했다. 물리 contact skin 경계의 수치 오차 구간은 제외했다.
- `BakeChecks.cs`: 최신 808개, 패킷 172바이트. 원본·variant·연결 SO 변경, 잘못된 값, 임시 NGO 등록과 원본·열린 씬 보존을 검사했다. `Logs/Stats/BakeLatest.json`.
- `WeaponChecks.cs`: 161개. 기본·변경 프로필의 무기 계산과 15칸 탄창 표시. `GunChecks.cs`: 기존 총잡이 회귀 94개.
- `Move~/Recall.cs`: 34개. 암살자 칼의 앞·뒤 탄도, 수직 투척, 벽·45도 경사, 시간 제한과 예측 화면 유지.
- 개발 빌드 `build_0a16da8ef1dc`: 오류 0, 경고 12. `Logs/Stats/BuildLatest.json`.
- 실제 두 프로세스 검증은 `Maps~/Hazards.ps1 -StatsOnly`로 실행한다. `-StatsProfiles`는 변경된 서버 수치·재스폰·탄창만 재검사한다. 단일 PC의 에디터 호스트와 개발 빌드 접속자이며 실제 두 PC·Relay 검증과 구분한다. 지연 60ms와 지터 10ms를 각 방향에 적용한다.
- `Logs/Boundary/StatsThird`: 여섯 직업 원본 일치·물리 머티리얼·실제 이동·점프·평타·투사체 94개 통과. 후속 프로필 변경 단계는 검사 기준 문제로 중단됐으므로 이 실행 전체가 완료됐다고 보지 않는다.
- `Logs/Boundary/StatsProfiles2`: 변경된 서버 기본값 전달, 이동 8.5·점프 15, 체력 137 기반 Giant 증강, 실제 사망에 따른 세 번의 라운드 재스폰, 15칸 탄창 표시·발사·재장전 10개 통과. 임시 수치는 메모리에서만 바꾸고 종료 시 복원했다.
