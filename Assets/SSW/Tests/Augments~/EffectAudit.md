# 증강 활성 연결·효과 감사

최신 통합 기준: `origin/Base a443d6d991d01b9df94fab741182cf6be5906095`. 삭제된 RYU Pocket.asset은 복구하지 않았다. Deck ID 85를 빈 예약 슬롯으로 정리하고 NetDeck은 null 슬롯을 명시적으로 제외한다. CLI 결과 `Logs/Stats/RemovedCard.json`: 슬롯 86, 유효 SO 85, 실제 활성 후보 **84 = 공용 30 + 직업 54**다. 직업별로 마녀 9, 마술사 10, 칼잡이 4, 암살자 11, 도박사 10, 총잡이 10이다. ID 16은 이전처럼 후보에서 제외된다. 다른 ID는 이동하지 않았다.

동시에 팀원 RandomPotion이 주머니 교환의 증강 조건을 제거했으므로 SSW NetCast의 서버 수락·접속자 예측도 기본 교환을 허용하도록 맞췄다. 교환 후 투척 전 중복 교환 차단, 재고 ID·라운드 epoch 검증은 유지한다. `Logs/Boundary/PocketBase`에서 실제 호스트·접속자 교환·꺼내기·투척·중복 방지 포함 13개 검사와 재고 순서 재현 13개가 통과했다. 주머니 카드는 부여하지 않았으며 지연 100ms·지터 20ms를 양방향에 적용했다. 개발 빌드는 `build_377533348d47`이다. 아래 85개 집계는 변경 전 감사 자료다.

검증 결과: `Logs/Boundary/EffectsFirst`에서 25개 검사 통과. 개발 빌드 `build_915aba1057e5`, 프로토콜 19, 에디터 호스트 + 별도 Windows 접속자 프로세스, 양방향 지연 60ms·지터 10ms 조건이다. 실제 두 PC/UGS Relay 검증은 아니다.

- 문어의 심장: 양쪽 플레이어 최대 HP 3, 실제 칼잡이 입력으로 피격당 1 감소, 세 번째 피격 사망 후 다음 라운드 HP 3·보유 카드 복원.
- 도망: 각 소유자 실제 피격 후 속도 11.2, 이동 입력으로 해당 속도 도달, 2초 후 7로 복원, 대기 중 두 번째 피격 재발동 차단, 5초 후 재발동.
- 살기: 각 소유자 실제 투척 입력으로 6초 활성, 상대 속도 최대 30% 감소·실제 평타 피해 10→7, 종료 후 원상복귀, 대기 중 차단 및 시작 13초 이후 재발동.
- 잭팟: 실제 발사 입력으로 확정 777 시작·종료·재발동. 활성 중 서버 Roll을 양쪽 각각 100개 제어 난수로 호출해 스택 20·확률·종료 시각 보존을 검사했다. 이 200회는 실제 발사 200회가 아닌 서버 상태 주입 검사다.

재현: `Assets/SSW/Tests/Maps~/Hazards.ps1 -Run <새이름> -EffectsOnly`. 결과의 `Checks.txt`, `Aura-host.json`, `Aura-client.json`, `JackpotStress.json`, `Result.json`을 확인한다. 85개 카드의 모든 조합을 실전 검사했다는 뜻은 아니다.

## 수정 상태

2026-09-26, 원본 수치 연동 `9f88fee` 이후 확인된 E1–E4를 SSW 경로에서 수정했다.

- E1: BuffHealth 기본값과 Network Player의 `_tenLivesHealth`를 3으로 변경했다. 공격자 있는 비 Deferred 피해를 1로 받는 기존 조건은 보존했다.
- E2: KnifeTuning이 도망 SO의 직렬화 값을 증강 획득 시 한 번 읽는다. 배율 1.6을 증가량 0.6으로 바꿔 전달하며 2초 지속 후 3초 대기한다. 다른 효과의 ApplySpeed 계약은 유지한다.
- E3: CoinCast.Roll은 활성 777 동안 확정 잭팟 소비, 일반·낡은 동전의 777 추첨을 막는다. Progress 20은 다음 유효 발동까지 유지한다.
- E4: KnifeTuning이 살기 SO의 지속·범위·최대 감소량·증가 시간·대기를 읽는다. 현재 값은 6초 지속 후 7초 대기다. 전투 중 리플렉션은 사용하지 않는다.

아래 전체 연결 표와 E1–E4 원인 설명은 **수정 전 프로토콜 18의 감사 기록**이다. 최종 카탈로그 ID/GUID 확인과 총잡이 효과 5개 밸런스는 별도 담당 범위이며, 아래의 과거 총잡이 수치를 최신 결과로 인용하면 안 된다. 실제 검증 결과는 문서 마지막에 별도로 기록한다.

## 집계와 선택 경로

| 구분 | 수 | 기준 |
|---|---:|---|
| Deck 등록 | 86 | ID 0–85, 서로 다른 SO |
| 활성 공용 후보 | 30 | Deck에 있고 연결된 RealCommonAugmentPool에 포함 |
| 활성 직업 후보 | 55 | Deck에 있고 IJobRestrictedAugment.RequiredJob이 선택 직업과 일치 |
| 등록됐지만 후보 아님 | 1 | ID 16 쿨감: 공용 풀에서 제외, 직업 제한 SO 아님 |
| 조사한 증강 SO 중 Deck 밖 | 16 | 아래 비활성 목록; 다른 팀원 단독 씬에서의 사용 여부와 구분 |
| 현재 멀티에서 뽑을 수 있는 서로 다른 카드 | 85 | 공용 30 + 직업 55, 보유 ID 제외 전 |

실제 연결은 [Network Player.prefab](<D:/unity_project/Mushrooms/Assets/SSW/Resources/Network/Player.prefab:1645>)의 NetDraft._deck → [Deck.asset](<D:/unity_project/Mushrooms/Assets/SSW/Resources/Network/Deck.asset>) → [RealCommonAugmentPool.asset](<D:/unity_project/Mushrooms/Assets/AJH/03.SO/Pool/RealCommonAugmentPool.asset>)이다. 공용 풀 GUID는 `d93c3b6253d586d4caea49a125939c7d`다. 직업 후보는 별도 팀원 Pool을 조회하지 않고 NetDeck의 SO 타입과 RequiredJob으로 고른다.

NetDraft.Awake의 BindNetwork가 로컬 풀 추첨을 비활성화한다. 서버 OfferChoices → ChooseRpc의 현재 제안 검증 → NetworkList 보유 ID → AugmentDrafter.ApplyGrant → AugmentGranted로 수신한다. 재스폰은 NetDraft.Restore를 통해 같은 ID를 다시 적용한다. JobCast 계열과 공용 수신기는 타입 집합으로 중복 적용을 막는다. 각 표의 “활성”은 후보와 효과 분기가 연결됐다는 뜻이며 모든 조합의 전투 결과가 검증됐다는 뜻은 아니다.

서버 JobCast.Apply/ServerTick, NetBolt/NetPotion의 충돌 처리와 NetHealth.CanChange가 전투 수치 변경 경로다. 소유자 예측·클라이언트 UI의 Grant 수신 자체가 추가 피해를 발생시키는 구조는 아니다. 다만 수치와 개별 효과의 정확성은 아래 불일치 및 검증 한계와 별개다.

## 확인된 활성 효과 불일치

### E1. ID 59 문어의 심장: 최대 체력 3 설명에 실제 10

- [현재 카드](<D:/unity_project/Mushrooms/Assets/AJH/03.SO/HealthAugment/Ten Lives.asset:16>)는 “체력 3 고정, 받는 피해 1 고정”이다. 원본 [HealthAugmentController](<D:/unity_project/Mushrooms/Assets/AJH/01.Scripts/Augment/HealthAugmentController.cs:41>)의 현재 코드 기본값도 3이다.
- 서버는 [Player.prefab](<D:/unity_project/Mushrooms/Assets/SSW/Resources/Network/Player.prefab:1876>)의 `_tenLivesHealth: 10`을 읽고 [BuffHealth.RefreshMax](<D:/unity_project/Mushrooms/Assets/SSW/Scripts/Multiplayer/BuffHealth.cs:101>)가 최대 HP를 10으로 고정한다. BuffHealth 기본값도 10이다. 과거 AJH 씬에도 10 직렬화 값이 남아 있어 단순 코드 기본값 변경만으로 기존 에셋은 갱신되지 않는다.
- 최소 수정안: 현재 카드의 3을 기준으로 SSW Network Player의 값과 BuffHealth 기본값을 함께 3으로 맞춘다. ID·enum은 바꾸지 않는다.
- 이후 확인 조건: 호스트/접속자 각각 ID 59만 적용해 Max=3, 공격자 있는 일반 피해=1, 재스폰 후 Max=3을 확인한다. 이번 감사에서는 실행하지 않았다.

### E2. ID 46 도망: 60% 증가가 160% 증가로 적용

- [Escape.asset](<D:/unity_project/Mushrooms/Assets/NKY/So/AssassinAugment/Escape.asset:19>)의 `speedMultiplier`는 1.6이며 설명은 2초 동안 이속 60% 증가다.
- [KnifeCast.OnDamageReceived](<D:/unity_project/Mushrooms/Assets/SSW/Scripts/Multiplayer/Jobs/KnifeCast.cs:159>) → [EscapeSO.OnHit](<D:/unity_project/Mushrooms/Assets/NKY/Scripts/Job/AugmentSO/EscapeSO.cs:22>)가 1.6을 그대로 ApplySpeed에 전달한다. [PlayerController.ApplySpeed](<D:/unity_project/Mushrooms/Assets/SSW/Scripts/Player/PlayerController.cs:123>)는 인자를 “증가량”으로 받아 `1 + amount`를 적용하므로 실제 배율은 2.6이다. 클라이언트 지연 문제가 아니라 활성 SO와 공통 인터페이스의 단위 차이다.
- 최소 수정안: 해당 증강의 SSW 설정/어댑터에서만 배율 1.6을 증가량 0.6으로 변환한다. 다른 증강·포션이 이미 증가량을 전달하므로 ApplySpeed의 전체 의미를 바꾸면 안 된다. 팀원 SO 자체는 이 감사에서 수정하지 않았다.
- 이후 확인 조건: 다른 이동 버프 없는 암살자 피격 후 Rate.Haste=1.6, 2초 후 1, 재사용 대기 적용을 확인한다.

### E3. ID 24·28·30: 이미 777인 동안 잭팟을 다시 뽑아 보상을 소모

- 원본 [JackpotDivision.Roulette](<D:/unity_project/Mushrooms/Assets/JJW/Script/Jackpot/JackpotDivision.cs:135>)는 777이 끝나야 확정 잭팟을 소모한다. 원본의 일반 777 확률과 낡은 동전 777 확률도 효과 중에는 0이다([낡은 동전](<D:/unity_project/Mushrooms/Assets/JJW/Script/Jackpot/JackpotDivision.cs:203>), [일반 확률](<D:/unity_project/Mushrooms/Assets/JJW/Script/Jackpot/JackpotDivision.cs:263>)).
- 서버 [CoinCast.Roll](<D:/unity_project/Mushrooms/Assets/SSW/Scripts/Multiplayer/Jobs/CoinCast.cs:70>)에는 이 조건이 없다. 이미 777인 상태에서 Progress=20이면 다음 룰렛에 Progress를 0으로 만들지만 [ApplyResult](<D:/unity_project/Mushrooms/Assets/SSW/Scripts/Multiplayer/Jobs/CoinCast.cs:121>)는 `if (Jackpot) break`로 새 777을 버린다. ID 28의 추가 추첨에도 같은 제외가 없으며, ID 30은 버려진 777 결과에도 확률 보너스를 올린다. ID 27로 룰렛 횟수가 늘면 이 경로에 더 쉽게 도달한다.
- 최소 수정안: Roll 시작 시 활성 777 여부를 한 번 판단해 확정 소비 조건에 `!Jackpot`을 추가하고, 효과 중 일반/낡은 동전의 777 추첨 확률을 0으로 전달한다. 활성 효과 동안 쌓인 확정 스택은 보존한다.
- 이후 확인 조건: 777 활성 + Progress=20에서 룰렛을 돌려 20이 유지되고, 종료 후 첫 룰렛에서 0으로 소비되며 새 777이 실제 시작되는지 확인한다. 활성 777 중 ID 28·30의 가짜 777/확률 누적도 차단돼야 한다.

### E4. ID 49 살기: 현재 SO의 종료 후 대기 7초가 서버에 미반영

- [KillingIntent.asset](<D:/unity_project/Mushrooms/Assets/NKY/So/AssassinAugment/KillingIntent.asset:23>)은 지속 6초, [auraCoolTime](<D:/unity_project/Mushrooms/Assets/NKY/So/AssassinAugment/KillingIntent.asset:33>)은 7초다. 원본 [KillingIntentSO](<D:/unity_project/Mushrooms/Assets/NKY/Scripts/Job/AugmentSO/KillingIntentSO.cs:90>)는 아우라가 끝난 뒤 7초 대기를 건다.
- 서버 [KnifeCast.Execute](<D:/unity_project/Mushrooms/Assets/SSW/Scripts/Multiplayer/Jobs/KnifeCast.cs:130>)는 지속 6초와 시작 후 12초 재사용을 하드코딩했다. 현재 SO 기준 시작 후 약 13초보다 1초 빠르다.
- 최소 수정안: SSW에서 현재 SO의 지속시간과 종료 후 대기를 읽거나 베이크한 값을 사용해 `ready = start + duration + cooldown`으로 설정한다. 당장 수치만 맞추면 13초이나, 12를 다른 상수로만 바꾸면 다음 원본 조정 때 재발한다.
- 이후 확인 조건: 첫 발동 12.5초 뒤 능력 사용은 살기 재발동 불가, 13초 이후 재발동 가능을 확인한다.

이 네 항목은 현재 후보에서 도달 가능한 코드/설정 불일치다. 실제 재현·수정 후 검증은 부모 작업의 별도 범위다. 아래 표의 다른 효과를 무결하다고 보증하지 않는다.

## 중복 타입 우선 대조

| 활성 항목 | 같은 타입의 레거시 항목 | 현재 연결 판정 |
|---|---|---|
| ID 72 SwiftApproach, 17 | ID 16 CooldownReduction.asset(쿨감), 저장 타입 17 | 쿨감은 Deck 등록만 남고 공용 풀 제외. 현재 후보에서 충돌 없음. enum CooldownReduction 자체는 16 |
| ID 59 TenLives, 20 | Poison.asset(독), 20 | Poison은 Deck 밖. 활성 독 효과 연결 오류가 아니라 비활성 별칭. ID 59의 HP 문제는 E1 |
| ID 60 Multiscale, 21 | Sniper.asset(저격), 21 | Sniper는 Deck 밖 |
| ID 61 Regeneration, 22 | Ricochet.asset(각도기), 22 | Ricochet은 Deck 밖 |
| ID 68 BestOffense, 10 | Microwave.asset(전자레인지), 10 | Microwave는 Deck 밖 |
| ID 70 ConterAttack.asset → CounterAttack, 29 | WhereIsIt.asset(ㅇㄷ/관통), 29 | WhereIsIt은 Deck 밖. 파일 철자 ConterAttack이 실제 enum 매핑을 바꾸지 않음 |

현재 활성 공용 30개 사이에는 타입 중복이 없다. 이 레거시 SO를 지금 풀에 추가하면 이름과 효과가 어긋날 수 있지만 현재 후보 연결 오류로 집계하지 않았다. ID 81 LeapBomb은 타입 100이며, BuffGuard의 명시적 SO 참조와 Deck SO GUID `1997664b41a1a7b4ca96cd8d8868eb08`가 일치한다.

## 활성 공용 30개

SO 클래스는 전부 CommonAugment, 타입은 CommonAugmentType이다. 각 이름 링크는 현재 Deck이 참조하는 정확한 원본이다.

| ID | 표시 이름 / SO | 타입 | 수신 컨트롤러 | 현재 서버 효과 경로 |
|---:|---|---|---|---|
| 0 | [거인](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Common/Giant.asset>) | Giant (0) | NetBuff / BuffHealth | RefreshMax·Scale → 최대 HP ×1.55, 크기 ×1.10 |
| 1 | [뱀파이어](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Common/Vampire.asset>) | Vampire (1) | NetBuff / BuffHealth | OnDamageDealt → 실제 가한 피해의 55% Heal |
| 2 | [광전사](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Common/Berserker.asset>) | Berserker (2) | NetBuff / BuffHealth | Berserker·SpeedScale·ModifyOutgoingDamage → HP 75% 이하에서 속도 +45%, 피해 +60% |
| 3 | [자신감](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Common/Confidence.asset>) | Confidence (3) | NetBuff / BuffHealth | OnDamageDealt → 2초 동안 이동속도 +30% |
| 4 | [유리 대포](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Common/GlassCannon.asset>) | GlassCannon (4) | NetBuff / BuffHealth | RefreshMax·Scale·ModifyOutgoingDamage → 최대 HP ×0.7, 크기 ×0.9, 피해 ×1.8 |
| 57 | [불사조](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Common/Phoenix.asset>) | Phoenix (5) | NetBuff / BuffHealth | RefreshMax·Scale → 최대 HP ×0.75, 크기 ×0.92; ModifyIncomingDamage에서 생애 1회 치명타 방지·전부 회복·무적 2.5초·정지 1초 |
| 58 | [죽음의 무도](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Common/DeathWaltz.asset>) | DeathWaltz (6) | NetBuff / BuffHealth | RefreshMax HP ×1.3; TryDefer·TickDamage → 피해를 5초로 분산, Deferred 태그로 재분산 차단 |
| 59 | [문어의 심장](<D:/unity_project/Mushrooms/Assets/AJH/03.SO/HealthAugment/Ten Lives.asset>) | TenLives (20) | NetBuff / BuffHealth | RefreshMax → 현재 최대 HP 10 고정; ModifyIncomingDamage → 공격자 있는 비 Deferred 피해 1; HP 차이 E1 |
| 60 | [멀티스케일](<D:/unity_project/Mushrooms/Assets/AJH/03.SO/HealthAugment/Multiscale.asset>) | Multiscale (21) | NetBuff / BuffHealth | ModifyIncomingDamage → 최대 HP에서 방어무시 이외 피해 50% 감소 |
| 61 | [띠끌모아태산](<D:/unity_project/Mushrooms/Assets/AJH/03.SO/HealthAugment/Regeneration.asset>) | Regeneration (22) | NetBuff / BuffHealth | TickRegen → 전투 중 1초마다 1 Heal |
| 62 | [방어 숙련](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Common/GuardMastery.asset>) | GuardMastery (14) | NetBuff / BuffGuard | Cooldown → 방어 재사용 시간 ×0.7 |
| 63 | [무적](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Common/Invincible.asset>) | Invincible (12) | NetBuff / BuffGuard | Duration → 방어 시간 +1초 |
| 64 | [재충전](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Common/Recharge.asset>) | Recharge (13) | NetBuff / BuffGuard | StartGuard·Update → 방어 종료 0.1초 후 추가 방어 1회, 재사용 시간 ×1.4 |
| 65 | [힐 필드](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Common/HealField.asset>) | HealField (9) | NetBuff / BuffGuard / BuffArea | StartGuard → Heal 장판, 1초 후 범위 1.3 내 각 대상 최대 HP의 20% 회복 |
| 66 | [점멸](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Common/Blink.asset>) | Blink (7) | NetBuff / BuffGuard | StartGuard → Drive.Burst(조준 방향 4, 0.15초), 방어 재사용 시간 ×1.2 |
| 67 | [아이스 에이지](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Common/IceAge.asset>) | IceAge (8) | NetBuff / BuffGuard | StartGuard·Ice·Update → 반경 5 빙결 0.5초 후 65% 둔화 2.5초, 방어 재사용 시간 ×1.15 |
| 68 | [최고의 방어는 공격](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Common/BestOffense.asset>) | BestOffense (10) | NetBuff / BuffGuard | StartGuard·ModifyOutgoingDamage → 5초 내 첫 피해 ×3, OnDamageDealt로 소모/방어 대기 초기화, 방어 재사용 시간 ×1.5 |
| 69 | [뉴클리어](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Common/Nuclear.asset>) | Nuclear (11) | NetBuff / BuffGuard | StartGuard·Update·Nuclear → 0.8초 후 현재 위치 기준 반경 5, 벽 차단, 대상 최대 HP의 20% 피해, 재사용 시간 ×2.5 |
| 70 | [카운터](<D:/unity_project/Mushrooms/Assets/AJH/03.SO/DefanceAugment/ConterAttack.asset>) | CounterAttack (29) | NetBuff / BuffGuard | OnDamageReceived → 방어한 직접 공격의 60%를 공격자에게 반사 |
| 71 | [악마와의 거래](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Common/DevilsDeal.asset>) | DevilsDeal (15) | NetBuff / BuffSkill | TickDrain → 초당 최대 HP 1% 자가 감소(10%에서 중단); 환경 피해 면역, OnDamageDealt 흡혈 25% |
| 72 | [쾌속 접근](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Common/SwiftApproach.asset>) | SwiftApproach (17) | NetBuff / BuffSkill | TickTargets·SpeedScale → 시야가 열린 30m 내 상대를 향해 이동 시 속도 +60% |
| 73 | [냠냠](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Common/NomNom.asset>) | NomNom (18) | NetBuff / BuffSkill | TickTargets → 반경 4 내 상대에게 1초마다 3 Drain 피해, OnDamageDealt 실제 피해만큼 회복 |
| 74 | [자석](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Common/Magnet.asset>) | Magnet (19) | NetBuff / BuffSkill | TickTargets → 반경 5 내 상대에 1초마다 수평 인력 2 |
| 75 | [축소 엔진](<D:/unity_project/Mushrooms/Assets/AJH/03.SO/SkillAugment/ShrinkENgine.asset>) | ShrinkEngine (24) | NetBuff / BuffSkill | Scale·SpeedScale → 크기 ×0.75, 이동속도 ×1.25 |
| 76 | [다재다능](<D:/unity_project/Mushrooms/Assets/AJH/03.SO/SkillAugment/Versatile.asset>) | Versatile (25) | NetBuff / BuffHealth / BuffSkill / BuffGuard | 최대 HP·피해·이속 +10%, 흡혈 5%, 방어 재사용 시간 -10% |
| 77 | [더블점프](<D:/unity_project/Mushrooms/Assets/AJH/03.SO/SkillAugment/DoubleJump.asset>) | DoubleJump (26) | NetBuff / BuffSkill / MotionView | MotionView.AirJumps·AirJumpRatio → MotionMotor 공중 점프 1회, 점프 속도 ×0.9 |
| 78 | [쿵](<D:/unity_project/Mushrooms/Assets/AJH/03.SO/SkillAugment/Slam.asset>) | Slam (23) | NetBuff / BuffSkill | TickLanding·Slam → 낙하 높이 ≥2, 바닥 상자 판정·높이 비례 피해(최대 40)·반발 |
| 79 | [지뢰밭](<D:/unity_project/Mushrooms/Assets/AJH/03.SO/SkillAugment/Minefield.asset>) | Minefield (27) | NetBuff / BuffSkill / BuffArea | FixedUpdate → 3초마다 Mine, 진입 폭발 피해 10 / 반경 2 |
| 80 | [느려저라](<D:/unity_project/Mushrooms/Assets/AJH/03.SO/SkillAugment/SlowAura.asset>) | SlowAura (28) | NetBuff / BuffSkill | TickTargets → 반경 4.5 내 상대 35% 둔화, 0.15초씩 갱신 |
| 81 | [폭탄 도약](<D:/unity_project/Mushrooms/Assets/AJH/03.SO/DefanceAugment/LeapBomb.asset>) | LeapBomb (100) | NetBuff / BuffGuard / BuffArea | 정확한 _leapBombAugment SO 소유 확인 → 방어 시 폭탄 3개·Drive.Leap(16), 방어 재사용 시간 ×1.2 |

## 활성 직업 55개

JobCast 계열은 AugmentGranted를 직접 구독한다. 마술사/마녀는 AugmentReceiverBehaviour를 통해 같은 Drafter 이벤트를 받고 NetCast가 컨트롤러 상태를 소비한다. 팀원 원본 효과 컨트롤러를 네트워크 플레이어에 중복 추가하는 방식이 아니다.

### 마술사 10개

MagicianAugment / MagicianAugmentType.

| ID | 표시 이름 / SO | 타입 | 수신 컨트롤러 | 현재 서버 효과 경로 |
|---:|---|---|---|---|
| 5 | [카드 연계](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Magician/CardChain.asset>) | CardChain (7) | MagicianAugmentController | FlyingCard.HitTarget → GetChainMultiplier·RegisterHit, 같은 문양 연속 2회 후 ×1.5 |
| 6 | [더블 드로우](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Magician/DoubleDraw.asset>) | DoubleDraw (6) | MagicianAugmentController | NetCast.DrawRank → 서로 다른 두 난수 표본 중 큰 숫자 선택 |
| 7 | [응급 마술](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Magician/EmergencyMagic.asset>) | EmergencyMagic (2) | MagicianAugmentController | FlyingCard.ApplySuitEffect(Heart) → ConsumeEmergencyHealBonus, 회복 +3 / 대기 3초 |
| 8 | [조커 카드](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Magician/JokerCard.asset>) | JokerCard (9) | MagicianAugmentController | Update·ConsumeJoker → NetCast.Fire·NetCard → FlyingCard 추가 문양 효과 |
| 9 | [행운의 클로버](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Magician/LuckyClover.asset>) | LuckyClover (4) | MagicianAugmentController | FlyingCard.ApplySuitEffect(Clover) → 지연 공격력 약화 10%, 0.5초 |
| 10 | [미러 카드](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Magician/MirrorCard.asset>) | MirrorCard (8) | MagicianAugmentController | FlyingCard → TryQueueMirror → NumberRoller.SpawnMirrorCard → NetCast.Mirror·NetCard; 0.3초 후 효과 ×0.35, 대기 8초 |
| 11 | [빠른 셔플](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Magician/QuickShuffle.asset>) | QuickShuffle (0) | MagicianAugmentController | NetCast 숫자 갱신 간격 ×1.15, FireDelay 0.1초 |
| 12 | [리턴 카드](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Magician/ReturnCard.asset>) | ReturnCard (5) | MagicianAugmentController | FlyingCard.BeginReturn → 0.5초 후 귀환, 귀환 피해 ×0.4 |
| 13 | [날카로운 카드](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Magician/SharpCard.asset>) | SharpCard (1) | MagicianAugmentController | FlyingCard.QueueSharpCardBonus → 1초 후 추가 피해 3 |
| 14 | [반짝이는 다이아](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Magician/SparklingDiamond.asset>) | SparklingDiamond (3) | MagicianAugmentController | FlyingCard 다이아 범위 ×1.15, 20% 둔화 0.5초 |

### 마녀 10개

WitchAugment / WitchAugmentType.

| ID | 표시 이름 / SO | 타입 | 수신 컨트롤러 | 현재 서버 효과 경로 |
|---:|---|---|---|---|
| 15 | [마녀의 흡정](<D:/unity_project/Mushrooms/Assets/RYU/3.SO/Witch/LifeSteal.asset>) | Lifesteal (0) | WitchAugmentController / WitchLifesteal | CombatDamage의 OnDamageDealt → 실제 가한 피해의 15% Heal; Network Player에 수신기 직렬화됨 |
| 17 | [넓은 살포](<D:/unity_project/Mushrooms/Assets/RYU/3.SO/Witch/WideSpray.asset>) | WideSpray (2) | WitchAugmentController | BuildModifiers.Splash ×1.5 → NetPotion.Contact·Splash |
| 18 | [진한 농도](<D:/unity_project/Mushrooms/Assets/RYU/3.SO/Witch/ThickBrew.asset>) | ThickBrew (1) | WitchAugmentController | BuildModifiers.Duration ×1.25 → NetPotion·NetZone → 원본 Potion.Use |
| 19 | [정밀 조제](<D:/unity_project/Mushrooms/Assets/RYU/3.SO/Witch/PrecisionDispensing.asset>) | PrecisionDispensing (6) | WitchAugmentController | ProjectileCount 3, Power ×0.7 → NetCast.Throw·NetPotion.Init |
| 20 | [잔류형 포션](<D:/unity_project/Mushrooms/Assets/RYU/3.SO/Witch/Remaining Potion.asset>) | RemainingPotion (5) | WitchAugmentController | BuildModifiers.LeaveZone → NetPotion.Contact → NetZone, 잔류 구역 반복 Potion.Use |
| 21 | [깨진 유리병](<D:/unity_project/Mushrooms/Assets/RYU/3.SO/Witch/brokenGlass.asset>) | brokenGlass (7) | WitchAugmentController | BuildModifiers.Bounces 1 → NetPotion 충돌 반사 / 추가 파열 |
| 82 | [독의 해금](<D:/unity_project/Mushrooms/Assets/RYU/3.SO/Witch/PoisonUnlock.asset>) | PoisonUnlock (3) | WitchAugmentController | TryReceive의 Unlocked → NetCast 서버 Stock.Pick → 독 포션 원본 Use |
| 83 | [재생의 해금](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Witch/Regeneration.asset>) | reproductionUnlock (4) | WitchAugmentController | 수정된 SSW SO의 reproductionUnlock(4) → Unlocked → NetCast.Stock.Pick → 재생 포션 Use |
| 84 | [여러 포션을 해금합니다](<D:/unity_project/Mushrooms/Assets/RYU/3.SO/Witch/AllUnlock.asset>) | AllUnlock (8) | WitchAugmentController | 독·재생 동시 Unlocked + CycleInterval ×0.6 → NetCast 제조 주기 |
| 85 | [주머니](<D:/unity_project/Mushrooms/Assets/RYU/3.SO/Witch/Pocket.asset>) | Pocket (9) | WitchAugmentController / NetCast | Pocket → 서버 CastStock.Swap / Held·Pocket·Epoch·소모 검증, 성공 투척 후 1회 교환 제한 해제 |

### 도박사 10개

GamblerAugment / GamblerAugmentType.

| ID | 표시 이름 / SO | 타입 | 수신 컨트롤러 | 현재 서버 효과 경로 |
|---:|---|---|---|---|
| 22 | [코인 업그레이드!](<D:/unity_project/Mushrooms/Assets/JJW/Cards/GamblerAugment_CoinUpgrade.asset>) | CoinUpgrade (6) | CoinCast | Hit → 이동속도 잭팟 중 둔화, 피해 잭팟 중 지속 피해, 회복 잭팟 후 Drain / OnDamageDealt 20% 흡혈 |
| 23 | [신난다](<D:/unity_project/Mushrooms/Assets/JJW/Cards/GamblerAugment_Excited.asset>) | Excited (0) | CoinCast | ApplyResult(Jackpot777) → 777 동안 Motion.ApplySpeed, 속도 ×1.5 |
| 24 | [확실한 잭팟](<D:/unity_project/Mushrooms/Assets/JJW/Cards/GamblerAugment_GuaranteedJackpot.asset>) | GuaranteedJackpot (4) | CoinCast | Roll → 성공 결과 누적 20회 후 다음 룰렛 777; 활성 777 중 소모 오류 E3 |
| 25 | [잭팟!!](<D:/unity_project/Mushrooms/Assets/JJW/Cards/GamblerAugment_JackpotBoost.asset>) | JackpotBoost (5) | CoinCast | DamageScale·ApplyResult → 777 동안 피해 ×1.3, 속도 ×1.2 |
| 26 | [행운](<D:/unity_project/Mushrooms/Assets/JJW/Cards/GamblerAugment_Luck.asset>) | Luck (1) | CoinCast | Probability·Roll → 각 결과 확률 +0.5%p |
| 27 | [더 많은 기회](<D:/unity_project/Mushrooms/Assets/JJW/Cards/GamblerAugment_MoreChances.asset>) | MoreChances (9) | CoinCast | Plan → 모든 동전 Style 2 룰렛; RollTable 일반 5%, 777 3%로 변경 |
| 28 | [낡은 동전](<D:/unity_project/Mushrooms/Assets/JJW/Cards/GamblerAugment_OldCoin.asset>) | OldCoin (8) | CoinCast | Roll → 별도 추가 추첨, 동일 DamageUp/Heal은 중복 적용; 활성 777 제외 누락 E3 |
| 29 | [넘치는 힘](<D:/unity_project/Mushrooms/Assets/JJW/Cards/GamblerAugment_OverflowingPower.asset>) | OverflowingPower (3) | CoinCast | DamageScale → 777 동안 피해 ×1.5 |
| 30 | [확률 변동](<D:/unity_project/Mushrooms/Assets/JJW/Cards/GamblerAugment_ProbabilityShift.asset>) | ProbabilityShift (2) | CoinCast | Roll·Probability → 777 결과마다 +1%p, 표시/적용 상한 10%; 활성 777 중 가짜 누적 E3 |
| 31 | [판돈 올리기](<D:/unity_project/Mushrooms/Assets/JJW/Cards/GamblerAugment_RaiseTheStakes.asset>) | RaiseTheStakes (7) | CoinCast | Roll·DamageScale → 연속 성공 1~3스택, 스택당 피해 +10%, 완전 실패 시 0 |

### 총잡이 10개

GunnerArgument / GunnerAugmentType.

| ID | 표시 이름 / SO | 타입 | 수신 컨트롤러 | 현재 서버 효과 경로 |
|---:|---|---|---|---|
| 32 | [공기탄](<D:/unity_project/Mushrooms/Assets/KDH/GameModules/SOs/Arguments/KDH_Augment_AirBullet.asset>) | AirBullet (0) | GunCast | Hit → 공중 대상 Drive.Launch(40 × 강화 배율) |
| 33 | [화려한 발걸음](<D:/unity_project/Mushrooms/Assets/KDH/GameModules/SOs/Arguments/KDH_Augment_BeautifulFootStepAbility.asset>) | BeautifulFootStepAbility (1) | GunCast | Hit → Motion.ApplySpeed(+0.5, 1초), 대기 3초 |
| 34 | [화염탄](<D:/unity_project/Mushrooms/Assets/KDH/GameModules/SOs/Arguments/KDH_Augment_FireBullet.asset>) | FireBullet (2) | GunCast | Hit → DamageOverTime, 0.5초 간격 6회 × (3 × 강화 배율) |
| 35 | [중력탄](<D:/unity_project/Mushrooms/Assets/KDH/GameModules/SOs/Arguments/KDH_Augment_GravityBullet.asset>) | GravityBullet (3) | GunCast | Hit → 대상 Drive.ApplyForce, 발사자 반대 방향 50 × 강화 배율 |
| 36 | [얼음탄](<D:/unity_project/Mushrooms/Assets/KDH/GameModules/SOs/Arguments/KDH_Augment_IceBullet.asset>) | IceBullet (4) | GunCast | Hit → Motion.ApplySlow(20% × 강화 배율, 1.5초 × 강화 배율) |
| 37 | [번개탄](<D:/unity_project/Mushrooms/Assets/KDH/GameModules/SOs/Arguments/KDH_Augment_LightningBullet.asset>) | LightningBullet (5) | GunCast | Hit → 같은 대상 3초 안 3회 표식, Lightning에서 1초 후 피해 50, 이후 대기 5초 |
| 38 | [독극탄](<D:/unity_project/Mushrooms/Assets/KDH/GameModules/SOs/Arguments/KDH_Augment_PoisonBullet.asset>) | PoisonBullet (6) | GunCast | Hit·ApplyPoison·ServerTick → 4초 / 8틱, 틱당 7 × 강화 배율; 재적중 잔여 횟수 갱신 |
| 39 | [퀘스트: 진화](<D:/unity_project/Mushrooms/Assets/KDH/GameModules/SOs/Arguments/KDH_Augment_Quest_EvolutionAbility.asset>) | Quest_EvolutionAbility (7) | GunCast | Hit → 강화탄 2회 적중 Progress, Advance·ProgressChanged의 한 발 보충 시간 ×0.5 |
| 40 | [축소 장치](<D:/unity_project/Mushrooms/Assets/KDH/GameModules/SOs/Arguments/KDH_Augment_ShrinkingDeviceAbility.asset>) | ShrinkingDeviceAbility (8) | GunCast | Hit → PlayerFx.Shrink 2초, 대기 5초 |
| 41 | [수리검탄](<D:/unity_project/Mushrooms/Assets/KDH/GameModules/SOs/Arguments/KDH_Augment_ShurikenBullet.asset>) | ShurikenBullet (9) | GunCast | Hit → 두 플레이어 거리 ×3 × 강화 배율의 추가 피해 |

### 암살자 11개

AbstractAssassinAugmentSO의 개별 파생 SO / AssassinAugmentType.

| ID | 표시 이름 / SO | 타입 | 수신 컨트롤러 | 현재 서버 효과 경로 |
|---:|---|---|---|---|
| 42 | [매복](<D:/unity_project/Mushrooms/Assets/NKY/So/AssassinAugment/Ambush.asset>) | Ambush (10) | KnifeCast | Strike 성공 → PlayerFx.Hide 0.8초, 대기 5초 |
| 43 | [백스탭](<D:/unity_project/Mushrooms/Assets/NKY/So/AssassinAugment/BackStab.asset>) | BackStab (7) | KnifeCast / BackStabSO | Strike → OnBasicAttackHit·ModifyDamage → 적 뒤쪽 판정에서 피해 ×1.4 |
| 44 | [암전](<D:/unity_project/Mushrooms/Assets/NKY/So/AssassinAugment/Blackout.asset>) | Blackout (8) | KnifeCast | Execute(Cycle) → 상대 PlayerFx.Blind 2.5초, 시작 기준 17.5초 간격 |
| 45 | [암기](<D:/unity_project/Mushrooms/Assets/NKY/So/AssassinAugment/ConcealedWeapon.asset>) | ConcealedWeapon (1) | KnifeCast | Plan → 투척 BoltSpec.Bounce=1 → NetBolt 반사 |
| 46 | [도망](<D:/unity_project/Mushrooms/Assets/NKY/So/AssassinAugment/Escape.asset>) | Escape (2) | KnifeCast / EscapeSO | OnDamageReceived → EscapeSO.OnHit → PlayerController.ApplySpeed; +60% 의도와 +160% 실제 차이 E2 |
| 47 | [약점 잡기](<D:/unity_project/Mushrooms/Assets/NKY/So/AssassinAugment/ExploitWeakness.asset>) | ExploitWeakness (11) | KnifeCast / ExploitWeaknessSO | Strike → DamageInfo.IgnoreDefense + 피해 ×1.2, 대기 8초 |
| 48 | [확실한 처리](<D:/unity_project/Mushrooms/Assets/NKY/So/AssassinAugment/FinishingBlow.asset>) | FinishingBlow (0) | KnifeCast / FinishingBlowSO | Execute(Cycle) → OnSkillUsed 플래그, 다음 Strike·ModifyDamage 피해 ×1.3 후 소모 |
| 49 | [살기](<D:/unity_project/Mushrooms/Assets/NKY/So/AssassinAugment/KillingIntent.asset>) | KillingIntent (9) | KnifeCast | Execute·ServerTick → 6초 동안 반경 3, 3초에 걸쳐 속도/공격력 -30%; 재사용 시각 차이 E4 |
| 50 | [방심 유도](<D:/unity_project/Mushrooms/Assets/NKY/So/AssassinAugment/Lure.asset>) | Lure (3) | KnifeCast / LureSO | Strike → 상대 최대 HP일 때 다음 ModifyDamage ×1.2 |
| 51 | [탈출 불가](<D:/unity_project/Mushrooms/Assets/NKY/So/AssassinAugment/NoEscape.asset>) | NoEscape (4) | KnifeCast / NoEscapeSO | Strike → OnBasicAttackHit, 70% 둔화 0.5초, 효과 종료 후 대기 4초 |
| 52 | [작전 변경](<D:/unity_project/Mushrooms/Assets/NKY/So/AssassinAugment/TacticalShift.asset>) | TacticalShift (5) | KnifeCast | OnDamageReceived → 생애 1회 HP ≤30%에서 주변 4m 상대 50% 둔화 7초, 자신 70% 둔화·피해 ×1.5 10초 |

### 칼잡이 4개

SwordAugment / SwordPerk.

| ID | 표시 이름 / SO | 타입 | 수신 컨트롤러 | 현재 서버 효과 경로 |
|---:|---|---|---|---|
| 53 | [받아내기](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Sword/ParryHeal.asset>) | ParryHeal (0) | SwordCast | ParrySuccess·Recover → 0.2초마다 2씩 5회 Heal |
| 54 | [질주](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Sword/DashSpeed.asset>) | DashSpeed (1) | SwordCast | Execute(Dash) → 돌진 시간 + 이후 3초 동안 이동속도 +50% |
| 55 | [긴 돌진](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Sword/DashRange.asset>) | DashRange (2) | SwordCast | PredictAction·Execute(Dash) → 돌진 지속시간 ×1.5, 서버/예측 공통 |
| 56 | [반격 돌진](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Sword/DashPower.asset>) | DashPower (3) | SwordCast | ParrySuccess → _boosted 1회, 다음 Execute(Dash)의 피해 ×2 후 소모 |

## 비활성 항목

여기서 비활성은 현재 Network Deck/후보 기준이다. Assets/SSW/Augments/CommonAugmentPool.asset 등 과거 단독 테스트 풀, 팀원 단독 씬에서의 참조까지 제거됐다는 뜻은 아니다.

| Deck ID | 이름 / SO | 저장 타입 | 구분 |
|---:|---|---|---|
| 16 | [쿨감](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Common/CooldownReduction.asset>) | CommonAugmentType.SwiftApproach (17) | 등록됨, 후보 아님 |
| — | [ㅇㄷ](<D:/unity_project/Mushrooms/Assets/AJH/03.SO/DefanceAugment/WhereIsIt.asset>) | CounterAttack (29) | Deck 밖, 현재 멀티 후보 아님 |
| — | [표시 이름 없음](<D:/unity_project/Mushrooms/Assets/KHG/01.Script/KHG_Am/jeonggang/System/SwordArgument.asset>) | SwordAugmentType.DashSpeed (1) | Deck 밖, 현재 멀티 후보 아님 |
| — | [추진기](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Common/Thruster.asset>) | Versatile (25) | Deck 밖, 현재 멀티 후보 아님 |
| — | [가만히 있으면 반은 간다](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Common/StayStill.asset>) | CooldownReduction (16) | Deck 밖, 현재 멀티 후보 아님 |
| — | [스프레이](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Common/Spray.asset>) | ShrinkEngine (24) | Deck 밖, 현재 멀티 후보 아님 |
| — | [저격](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Common/Sniper.asset>) | Multiscale (21) | Deck 밖, 현재 멀티 후보 아님 |
| — | [각도기](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Common/Ricochet.asset>) | Regeneration (22) | Deck 밖, 현재 멀티 후보 아님 |
| — | [연타](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Common/RapidStrike.asset>) | Minefield (27) | Deck 밖, 현재 멀티 후보 아님 |
| — | [독](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Common/Poison.asset>) | TenLives (20) | Deck 밖, 현재 멀티 후보 아님 |
| — | [어디 있게?](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Common/PhaseShift.asset>) | 정의 없음 (32) | Deck 밖, 현재 멀티 후보 아님 |
| — | [전자레인지](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Common/Microwave.asset>) | BestOffense (10) | Deck 밖, 현재 멀티 후보 아님 |
| — | [드릴](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Common/Drill.asset>) | Slam (23) | Deck 밖, 현재 멀티 후보 아님 |
| — | [총알 복제](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Common/BulletRefund.asset>) | DoubleJump (26) | Deck 밖, 현재 멀티 후보 아님 |
| — | [ㅈㄴ 큰 무기 (근접)](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Common/BigWeaponMelee.asset>) | 정의 없음 (31) | Deck 밖, 현재 멀티 후보 아님 |
| — | [ㅈㄴ 큰 무기 (BF)](<D:/unity_project/Mushrooms/Assets/SSW/Augments/Common/BigWeapon.asset>) | Magnet (19) | Deck 밖, 현재 멀티 후보 아님 |
| — | [재생의 해금](<D:/unity_project/Mushrooms/Assets/RYU/3.SO/Witch/reproductionUnlock.asset>) | PoisonUnlock (3) | Deck 밖, 현재 멀티 후보 아님 |

재생의 해금은 원본 RYU SO가 type 3(PoisonUnlock)이지만 현재 Deck ID 83은 SSW 사본의 type 4(reproductionUnlock)를 가리킨다. 따라서 원본의 타입 오기 자체를 현재 활성 독·재생 충돌로 세지 않는다.

## 오판하지 않은 차이와 검증 한계

- 총잡이 ID 39 설명은 강화탄 5회지만 현재 원본 퀘스트 프리팹의 questClearCount도 2이며 서버도 2다. ID 40 설명은 대기 3초지만 원본 코드와 서버 모두 5초다. 이는 현재 설명과 구현의 차이이며 서버 효과 연결 누락으로 분류하지 않았다.
- 총잡이 원본의 ChargeSpeed는 한 발 보충 시간이다. 퀘스트가 이것을 절반으로 줄이는 동작은 서버 Stats.Reload 절반 처리와 대응된다. Stats.ChargeTime은 이미 채운 탄의 강화 대기이므로 이름만 보고 둘을 바꾸면 안 된다.
- ID 44 암전은 원본도 실명 2.5초가 끝난 후 15초 대기하므로 서버의 시작 후 17.5초는 일치한다. ID 69 뉴클리어도 원본이 지연 후 현재 위치를 읽으므로 그 점만으로 오류로 보지 않았다.
- ID 47 약점 잡기의 DamageInfo는 참조형이고 원본 SO가 바꾼 IgnoreDefense 태그가 Strike의 CombatDamage.Deal에 전달된다. ID 10 미러 카드는 NumberRoller가 네트워크 바인딩을 확인해 NetCast.Mirror로 넘기는 경로가 있다.
- [Audit.md](<D:/unity_project/Mushrooms/Assets/SSW/Tests/Augments~/Audit.md>)와 [Choices4/Checks.txt](<D:/unity_project/Mushrooms/Logs/Augments/Choices4/Checks.txt>)의 PASS 616개는 선택 UI·후보 ID·수신·리스폰 유지 증거다. 실제 피해량, 상태 효과 기간, 모든 증강 조합·원격 PC 동작 616건을 의미하지 않는다.
- Pocket/Stock/Weights 및 동일 PC 지연 주입 결과는 기존 보고서에 한정된 증거다. 이번에는 재실행하지 않았다. 호스트·접속자 전투 수치, 복합 증강 상호작용, 실제 외부망 결과는 새로 검증하지 않았다.
- 기존 [IconAudit.md](<D:/unity_project/Mushrooms/Assets/SSW/Tests/Augments~/IconAudit.md>)의 그림 40개 미확인 기록은 유지한다. 사용자가 이후 확정한 최종 정책은 **빈칸 + 이름/설명 호버**다. 과거 Audit의 대체 그림 유지 문구나 IconAudit의 원본 아트 요청은 현재 작업의 미완료 조건이 아니며, 새 아트·대체 아이콘 작업은 필요 없다.

## 코드 경로 색인

- [NetDeck](<D:/unity_project/Mushrooms/Assets/SSW/Scripts/Multiplayer/NetDeck.cs>)
- [NetDraft](<D:/unity_project/Mushrooms/Assets/SSW/Scripts/Multiplayer/NetDraft.cs>)
- [AugmentDrafter](<D:/unity_project/Mushrooms/Assets/SSW/Scripts/Augments/AugmentDrafter.cs>)
- [NetBuff](<D:/unity_project/Mushrooms/Assets/SSW/Scripts/Multiplayer/NetBuff.cs>)
- [BuffHealth](<D:/unity_project/Mushrooms/Assets/SSW/Scripts/Multiplayer/BuffHealth.cs>)
- [BuffSkill](<D:/unity_project/Mushrooms/Assets/SSW/Scripts/Multiplayer/BuffSkill.cs>)
- [BuffGuard](<D:/unity_project/Mushrooms/Assets/SSW/Scripts/Multiplayer/BuffGuard.cs>)
- [BuffArea](<D:/unity_project/Mushrooms/Assets/SSW/Scripts/Multiplayer/BuffArea.cs>)
- [NetCast](<D:/unity_project/Mushrooms/Assets/SSW/Scripts/Multiplayer/NetCast.cs>)
- [JobCast](<D:/unity_project/Mushrooms/Assets/SSW/Scripts/Multiplayer/Jobs/JobCast.cs>)
- [GunCast](<D:/unity_project/Mushrooms/Assets/SSW/Scripts/Multiplayer/Jobs/GunCast.cs>)
- [CoinCast](<D:/unity_project/Mushrooms/Assets/SSW/Scripts/Multiplayer/Jobs/CoinCast.cs>)
- [KnifeCast](<D:/unity_project/Mushrooms/Assets/SSW/Scripts/Multiplayer/Jobs/KnifeCast.cs>)
- [SwordCast](<D:/unity_project/Mushrooms/Assets/SSW/Scripts/Multiplayer/Jobs/SwordCast.cs>)
- [MagicianAugmentController](<D:/unity_project/Mushrooms/Assets/SSW/Scripts/Player/Magician/MagicianAugmentController.cs>)
- [FlyingCard](<D:/unity_project/Mushrooms/Assets/SSW/Scripts/Player/Magician/FlyingCard.cs>)
- [WitchAugmentController](<D:/unity_project/Mushrooms/Assets/RYU/01.Script/Argument/WitchAugmentController.cs>)
- [WitchLifesteal](<D:/unity_project/Mushrooms/Assets/RYU/01.Script/Argument/WitchLifesteal.cs>)
- [NetPotion](<D:/unity_project/Mushrooms/Assets/SSW/Scripts/Multiplayer/NetPotion.cs>)
- [NetZone](<D:/unity_project/Mushrooms/Assets/SSW/Scripts/Multiplayer/NetZone.cs>)
