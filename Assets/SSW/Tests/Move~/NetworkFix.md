# 멀티플레이 이동·투사체 검증

2026-09-22. Unity 6000.5.2f1, NGO 2.13.2, Multiplayer Services 2.2.4, Transport 6.5.0.
기준 커밋은 Base의 `3cb0f62`. 수정 범위는 `Assets/SSW`, 작업 브랜치는 `SSW`다.

## 수정

- 상대 캐릭터의 보간 위치를 화면 프레임마다 표시한다. 기존에는 Rigidbody2D의 Transform 동기화 때문에 144FPS에서도 여러 프레임이 같은 위치를 표시했다.
- `SnapshotClock`이 수신한 위치의 지연에 맞춰 보간 시간을 조절한다. 현재 물리 간격에서 추가 버퍼의 목표는 40~200ms이며, 수신한 상태보다 화면 시간이 앞서지 않도록 제한한다. 패킷 공백 뒤의 순간적인 따라잡기를 막는다.
- `ShotContact`가 카드·물약의 로컬 예측과 수신된 투사체에 동일한 표시용 충돌 처리를 제공한다. 화면에 보이는 상대 앞에서 멈추며, 서버가 빗나감·방향 변경·대상 재배치를 확인하면 다시 진행한다. 피해 판정은 서버가 유지한다.
- 물약이 물리 프레임 사이에 얇은 지형을 통과할 때도 기존 sweep 결과로 충돌·반사를 처리한다.
- `NetCast`의 표시 갱신은 캐릭터 표시 뒤, 투사체 표시 앞에 실행된다.

## 실제 플레이어 검증

검증 빌드 GUID: `1add4da43c724f32a23fdc4c853c0ed7`.
호스트와 접속자는 같은 Windows 컴퓨터에서 별도 프로세스로 실행했다.
인위적 지연 검사는 각 프로세스의 송신에 지연 180ms, 지터 150ms, 손실 3%를 설정했다.

| 검사 | 결과 |
| --- | --- |
| `Flight22Card04`, `Flight22Potion04` | 호스트 30FPS / 접속자 144FPS, 직업 교환. 각 44개 비행 검사 통과. 네 화면 대상의 지속 이동 구간에서 정지 간격 0ms |
| `MotionQuality.py` | 상대 표시 속도 P95: 수정 전 21.89, 수정 후 카드 7.13 / 물약 6.85. 실제 이동 속도 7.0. 최종 측정 RTT 465 / 654ms |
| `Impact22Card04`, `Impact22Potion04` | 적 앞에서 멈춤, 서버 피해 10 / 20, 중복 및 잔여 투사체 없음. 수정 전에는 명중 후에도 적을 4.51 / 6.12 유닛 지나침 |
| `Special22Card01`, `Special22Potion01` | 30 / 144FPS 양방향. 귀환·지연 카드, 3갈래·반사 물약, 잔류 영역 종료 통과 |
| `Verify22Card01` | 이동·점프·밀치기·감속·순간이동·공격 취소·피해 동기화·재시작 등 27개 통과. 3.4초 100% 패킷 손실 후 복구 |
| `Turns22Mixed01` | 양쪽 이동·점프·공격 중 지연 급변. 최종 입력 적체 1틱, 클라이언트 발사 인계 8회, 거부 0회 |
| `Motion22Card01` | 이동·점프 중 발사 6회 모두 서버와 발사 위치 오차 0, 입력 틱 동일 |
| `Self22Potion01` | 자신에게 쓴 물약의 회복 5, 양쪽 체력 동일, 불필요한 반사 및 잔여 미리보기 없음 |
| `Relay22Final01`, `Relay22Potion01` | 실제 UGS 비공개 방, 별도 인증 프로필. 직업을 바꿔 각 8개 접속·발사·퇴장 검사 통과. RTT 83 / 100ms. 두 방 모두 정상 퇴장 확인 |

완료된 플레이어 로그에서 런타임 예외를 확인하지 않았다. 개발 빌드는 오류 0개, 기존 경고 9개로 성공했다. 경고는 다른 팀 폴더의 스크립트, Dashboard 연결, Player의 Pipeline 비활성 설정에 관한 것이다.

일반 Windows 빌드도 오류 0개로 생성했고, 12초 시작 검사에서 예외 없이 실행됐다. 일반 빌드의 게임 어셈블리에는 `NetProbe`, `MenuProbe`, `NetTrace`가 포함되지 않는다. 일반 빌드의 두 프로세스 플레이 검사는 수행하지 않았다.

## Unity CLI 검사

- `SnapshotChecks.cs`: 30 / 60 / 144FPS에서 패킷 지터·순서 변경·손실에 대한 화면 시간 연속성 통과.
- `SweepChecks.cs`: 실제 호스트 경기에서 얇은 벽을 가로지르는 물약의 소멸과 반사 2개 통과.
- `TargetChecks.cs`: 표시용 충돌, 오래된/새 서버 상태, 방향 변경, 서버 체력 보존 4개 통과.

`SweepChecks.cs`와 `TargetChecks.cs`는 두 플레이어가 참가한 호스트 경기에서 `eval_file`로 실행한다. `SnapshotChecks.cs`는 편집 상태에서도 실행할 수 있다.

## 재실행

프로젝트 루트에서 Development Windows 플레이어를 `Builds/Local/Game.exe`로 빌드한다. 씬은 MainMenu, SuperUltraLegendScene, Map_Basic, Map_JumpPad, Map_Teleport, Map_Vanish다. 모든 씬은 `Assets/SSW` 안에 있다.

```powershell
pwsh -File 'Assets/SSW/Tests/Move~/Flight.ps1' -Run FreshCard -HostJob Witch -ClientJob Magician -HostFps 30 -ClientFps 144 -Delay 180 -Jitter 150 -Loss 3
python 'Assets/SSW/Tests/Move~/Trajectory.py' 'Logs/Move/FreshCard'
python 'Assets/SSW/Tests/Move~/MotionQuality.py' 'Logs/Move/FreshCard'
python 'Assets/SSW/Tests/Move~/Combat.py' --run FreshImpact --scenario impact --client-job Witch --expect-pass
python 'Assets/SSW/Tests/Move~/ImpactView.py' 'Logs/Combat/FreshImpact'
pwsh -File 'Assets/SSW/Tests/Move~/Relay.ps1' -Run FreshRelay -HostJob Magician -ClientJob Witch -CreateTimeout 60
```

`Run`에는 매번 새 이름을 사용한다. 원본 프레임 추적과 빌드 보고서는 로컬의 Git 제외 경로 `Logs`에 보관한다. 이 폴더의 `~` 접미사는 검사 코드를 일반 플레이어에 포함하지 않도록 한다.

서로 다른 실제 PC·공유기 조합과 장시간 인터넷 플레이는 아직 검증하지 않았다. 통신 지연 자체가 없어지는 것은 아니며, 서버 확인을 기다리는 명중 표시 지연은 남는다.
