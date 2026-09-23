# 멀티플레이 이동·투사체 검증

2026-09-22. Unity 6000.5.2f1, NGO 2.13.2, Multiplayer Services 2.2.4, Transport 6.5.0.
첫 검증의 기준 커밋은 Base의 `3cb0f62`. 수정 범위는 `Assets/SSW`, 작업 브랜치는 `SSW`다. 이후 접속자 멈춤 재현과 추가 수정은 아래에 별도로 기록한다.

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

## 추가 수정: 호스트 정지 후 입력 적체

Base를 Unity 에디터에서 실행해도 접속자가 멈춘다는 보고를 받고 다시 재현했다. 기존의 `stall` 검사는 서버 시계만 확인해, 입력 적체가 계속 남는 문제를 놓쳤다.

`Hitch22Before01`은 네트워크 지연·지터·손실 없이 호스트를 900ms 정지시켰다. 호스트가 돌아온 뒤 1.5초를 기다려도 입력 31개가 처리되지 않고 남았다. 물리 간격이 20ms이므로 약 620ms가 계속 밀린 상태였다.

원인은 `MotionView`가 실제로 실행된 FixedUpdate 횟수만큼만 입력 처리 시간을 적립한 것이다. Unity가 긴 프레임의 물리 업데이트 시간을 제한하면 빠진 시간을 이후에도 적립하지 못한다. 이 동작은 [Unity의 maximumDeltaTime 설명](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Time-maximumDeltaTime.html)과 일치한다.

`InputBudget`으로 입력 처리 시간 계산을 분리하고 실제 경과 시간을 적립하도록 바꿨다. 기존의 물리 업데이트당 최대 2개 처리, 입력 기록 128개 한도, 입력 순서 검사, 순간이동 시 초기화는 유지한다. 같은 시각에 물리 업데이트가 여러 번 실행되어도 시간을 중복 적립하지 않는다.

`Combat.py --editor-client`와 `Relay.ps1 -EditorClient`는 빌드 호스트에 실제 Unity 에디터를 접속시킨다. 두 검사 도구의 `QueuePlayerLoopUpdate`를 끄므로 강제 에디터 갱신으로 멈춤을 가리지 않는다. 종료 시 에디터를 편집 모드로 되돌린다.

| 검사 | 결과 |
| --- | --- |
| `BudgetChecks.cs` | 20 / 30 / 144FPS에서 900ms 정지 4회 후 적체 1~2개. 같은 타임스탬프 중복 적립 방지, 128개 한도, 순간이동 초기화 통과 |
| `Hitch22After01` | 실제 플레이어 30 / 144FPS, 호스트 정지 4회 후 서버 적체 4개, 접속자 미확인 입력 4~5개, 재동기화 0회 |
| `Editor22Hitch02` | 에디터 접속자, 정지 4회 후 서버 적체 2~3개, 접속자 미확인 입력 3개, 재동기화 0회. 20초 추적에서 50ms 초과 프레임 0개 |
| `Editor22TurnsCard01` | 에디터 카드 접속자, 양쪽 이동·점프·공격 중 지연 20→220→20ms. 공격 인계 8회, 거부 0회, 최종 적체 2개. 30초 추적에서 50ms 초과 프레임 0개 |
| `Editor22TurnsPotion01` | 에디터 물약 접속자, 동일한 지연 변화. 공격 인계 17회, 거부 0회, 최종 적체 2개. 30초 추적에서 50ms 초과 프레임 1개, 최대 66.3ms |
| `Editor22HitchFireCard01` | 호스트가 멈춘 도중 카드 발사 3회 모두 인계, 거부 0회. 서버 적체 3~4개로 회복. 30초 추적에서 50ms 초과 프레임 1개, 최대 68.3ms |
| `Editor22RelayCard01` | 실제 UGS와 에디터 카드 접속자 검사 10개 통과. 호스트 정지 2회 후 서버 적체 2개, 접속자 미확인 입력 5~6개. RTT 99~101ms. 양쪽 방 퇴장 확인 |
| `Editor22RelayPotion01` | 최신 Base 빌드와 에디터 물약 접속자, 실제 UGS 검사 10개 통과. 정지 후 서버 적체 3~4개, 접속자 미확인 입력 7~8개. RTT 99~101ms. 양쪽 방 퇴장 확인 |
| `Verify22HitchFinal01` | 최신 Base 빌드 30 / 144FPS에서 이동·점프·밀치기·감속·순간이동·피해·재시작 등 27개 통과. 3.4초 완전 패킷 손실 후 표시와 입력 적체 회복 |

추가 검증 중 Base에 올라온 `e71529e`까지 가져왔다. 다른 팀의 프리팹·맵·물리 재질 변경을 보존했다. 최종 개발 빌드 GUID는 `66cb4aa5d36242a18b1fd5d60c6a7007`이며 오류 0개, 기존 경고 9개로 성공했다. 이전의 일반 배포 ZIP은 이번 추가 수정을 포함하지 않는다.

```powershell
unity command eval_file --file 'Assets/SSW/Tests/Move~/BudgetChecks.cs' --project-path . --caller plugin --skill unity-cli --format json
python 'Assets/SSW/Tests/Move~/Combat.py' --run FreshHitch --scenario hitches --repeat 4 --host-fps 30 --client-fps 144 --delay 0 --jitter 0 --loss 0 --expect-pass
python 'Assets/SSW/Tests/Move~/Combat.py' --run FreshEditorHitch --scenario hitches --repeat 3 --host-fps 30 --client-fps 144 --delay 0 --jitter 0 --loss 0 --editor-client --fire-during-hitch --expect-pass
python 'Assets/SSW/Tests/Move~/Combat.py' --run FreshEditorTurns --scenario turns --host-job Magician --client-job Witch --host-fps 30 --client-fps 144 --editor-client --expect-pass
pwsh -File 'Assets/SSW/Tests/Move~/Relay.ps1' -Run FreshEditorRelay -HostJob Magician -ClientJob Witch -CreateTimeout 60 -EditorClient
```

이번 수정은 재현한 호스트 정지 뒤의 지속적인 입력 적체를 해결한다. 호스트 자체가 멈춘 동안의 서버 응답이나 실제 인터넷 지연을 없애지는 않는다. 서로 다른 두 PC의 장시간 사용은 아직 검증하지 않았다.

## 실제 두 PC 검증과 반사 표시 수정 (2026-09-23)

Base `698a6c6`을 양쪽에 맞추고 서로 다른 Windows PC를 비공개 UGS Relay 방으로 연결했다. 호스트는 새 Development 플레이어 `505742b38e03484b85a4a2f081908722`, 접속자는 Unity 6000.5.2f1 에디터다. 전 과정은 Unity CLI와 기존 MenuProbe/NetProbe로 진행했고 지연·손실·호스트 정지 주입은 없었다.

기존 로컬 개발/일반 실행 파일은 9월 18일 게임 DLL이라 이후의 SnapshotClock/InputBudget 수정이 들어 있지 않았다. 소스 동기화와 함께 두 실행 파일을 다시 빌드했다. 사용자가 증상을 봤던 실행 파일은 특정하지 못했으므로 이것을 최초 현상의 단독 원인으로 단정하지 않는다.

실제 교전에서 물약 반사가 서버에서 확정된 뒤에도 접속자의 표시 오프셋이 이전 하강 궤적을 보존했다. 바닥 충돌을 다시 감지하면서 샷 `(53,3,0)`, `(53,6,0)`, `(53,7,0)`이 각각 51.7/43.6/49.3ms 멈췄다. `ShotSync`는 방향 변경 시 직전 표시 위치에서 새 속도로 이어가도록 수정했다. 서버 물리·피해 판정·전송 형식은 바꾸지 않았다.

라운드 재생성 첫 프레임에는 접속자 몸체가 `(6,-2.8)`인데 표시가 프리팹 위치 `(-5.74,-0.69)`에 남고 다음 서버 상태에서 11.928만큼 보정됐다. `MotionView` 초기 위치를 네트워크 스폰 Transform에서 가져와 몸체와 함께 맞췄다.

| 검사 | 결과 |
| --- | --- |
| `Peer23/client.trace.1/2` | 수정 전 최신 Base, 정지 발사 8회와 이동·점프 발사 8회 인계. 비행 중 정지·동일 방향 역행·중복·표시 공백 0. RTT 약 97~119ms |
| `Peer23/client.trace.3` | 서로 이동·공격하는 교전과 세트 전환. 반사 후 재블록 3건, 잘못된 재스폰 첫 표시 재현 |
| `Peer23/After/client.trace.4` | 동일한 두 PC, 호스트 물약 12발 모두 반사. 확정 반사 후 blocked 프레임 0, 비행 중 정지·역행·중복·공백 0. 접속자 프레임 p95 18.4ms/max22.8ms |
| 재스폰 | trace4 serverTime269.3648부터 첫 6프레임의 접속자 몸체·표시 위치 차이 0. 테스트용 명시적 warp의 위치 보정은 별도 구분 |
| `Peer23/After/client.trace.5` | ReturnCard 6발 모두 서버 투사체로 인계. 비행 중 정지·동일 turn 역행·위치 점프·중복·공백 0, 이동 보정 0. 발판에 닿아 약 0.5초 기다린 뒤 복귀하는 게임 동작은 정지 오류로 세지 않음 |
| `TurnChecks.cs` | 30/60/144FPS에서 반사와 복귀 방향 6개 검사 통과. 서버에서 확정된 새 방향으로 이동하고 한 프레임 이동 거리를 넘지 않음 |

호스트 trace201의 발사 전 구간에는 인위 주입 없이 811.5ms 프레임 지연 1회가 있었다. 그동안 접속자의 미확인 입력이 47틱까지 늘었지만 이후 7틱으로 회복했다. 해당 구간에는 활성 투사체가 없었다. 호스트·에디터 로그에서 원인을 확정할 GC/셰이더/예외 증거는 찾지 못했다. 반사 수정으로 이 호스트 정지까지 해결됐다고 판단하지 않는다. 프로파일러를 연결한 재현이 추가로 필요하다.

원본은 양쪽 프로젝트의 `Logs/Peer23`에 보존한다. 이 검증은 서로 다른 실제 PC에서 수행했으며 장시간 플레이나 모든 네트워크 환경에 대한 무지연 보장은 아니다.

```powershell
unity command eval_file --file 'Assets/SSW/Tests/Move~/TurnChecks.cs' --project-path . --caller plugin --skill unity-cli --format json
```
