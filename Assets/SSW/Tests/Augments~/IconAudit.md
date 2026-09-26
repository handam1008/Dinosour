# 획득 아이콘 원본 대조

2026-09-26, 최신 공유 Base `0f641844fab7b1c42468835ed3622b4e3e228d63` 기준. 대상은 `MissingIcons.json`에 기록된 40개 경로다.

## 결론

40개 모두 현재도 `icon: {fileID: 0}`이고 Unity에서 읽은 Sprite 참조도 null이다. 이미 연결되어 해결된 항목 0개, 기존 정식 아이콘의 연결만 빠졌다고 확정한 항목 0개다. 저장소에서 대상별 정식 아이콘 또는 그것을 지시하는 기존 매핑을 확인하지 못한 항목은 SSW 20, AJH 11, JJW 9개로 총 40개다.

`JobAugmentHUD.cs:154-155`는 `augment.icon`을 그대로 Image에 연결하고 null일 때 그림만 비활성화한다. 다른 아이콘을 골라 덮어쓰거나 원본 참조를 버리는 소비 경로 문제는 확인되지 않았다. 빈 칸의 실제 이름·설명 툴팁은 기존 처리대로 유지된다. 소스·씬·프리팹·원본 SO·등록표·아이콘 이미지는 수정하지 않았다.

## 확인 범위와 근거

- Unity CLI의 AssetDatabase로 `Assets` 전체에서 Augment 자산 101개, Texture2D를 보유한 경로 1,248개와 Sprite 하위 자산 3,508개를 조회했다. 별도 Sprite 검색 1,095개 경로는 전부 같은 텍스처 조사 범위 안이었다. 101은 중복·비활성 자산을 포함한 검색 수이며 최종 등록/후보/효과 집계가 아니다.
- 각 대상의 실제 GUID, fileID, 현재 Deck 인덱스, 표시 이름, 직렬화된 enum, icon 참조를 확인했다. 안정된 식별자는 에셋 GUID이며 숫자 네트워크 ID는 위 Base에서의 값이다. 40개 전부 비어 있는 참조이고, 유실된 비영(非零) Sprite GUID 참조는 없었다.
- 같은 클래스와 enum 값을 가진 다른 SO도 조사했지만 연결된 아이콘은 없었다. 일부 레거시 에셋은 이름과 enum이 다르므로 이 비교는 검색 단서로만 썼고 동일한 효과라고 단정하지 않았다.
- 프로젝트 C#의 icon 대입과 Sprite 매핑을 조사했다. 이 40개를 다른 정식 아이콘에 연결하는 경로는 확인되지 않았다. 관련 폴더의 확보된 Git 이력에서 발견한 비어 있지 않은 아이콘 연결은 대상 밖의 도박사 `Luck`이었다.
- AJH의 `MagnetField`, `NomField`, `SlowField`는 실제 오라 프리팹에서, `MineBody`는 지뢰·폭탄 프리팹에서 사용하는 그림이다. 파티클용 `HealPlus`, `HealRing`, `IceBlock` 등은 이펙트 머티리얼 참조다. 증강 아이콘으로 지정된 근거가 없어 재사용하지 않았다.
- JJW의 `CoinsV2_outline_18x18res.png`는 동전 투사체·회전 애니메이션 참조다. 최근 `exec-093fc393…`, `exec-12edde05…`, `exec-c43b0116…` 그림도 각각 광선·발광·고리이며 대상 SO와 연결된 근거가 없다. 모자, 음표, 숫자 7, 룰렛 그림도 대상 아이콘으로 지정하지 않았다.
- `LuckSprite.png`는 기존 `GamblerAugment_Luck.asset`의 행운 아이콘이고, KDH의 `augmentation-icon-sheet.png`는 총잡이 10개 SO에 이미 연결돼 있다. 다른 증강에 대신 붙이지 않았으며 현재 연결도 그대로 보존했다.

그림의 존재 자체와 특정 증강의 정식 아이콘이라는 근거를 구분했다. 다른 PC의 미커밋 아트, 저장소 밖의 원본, 담당자만 아는 미기록 매핑은 이 조사로 확인할 수 없다. 따라서 분류는 “현재 저장소에서 정식 원본/매핑 미확인”이며 외부에도 그림이 절대로 없다는 뜻은 아니다.

## 최신 요구와 처리 완료

2026-09-26 지휘실이 전달한 사용자 답변은 “몰라 없는데 그냥 빈그림으로 해두면 될듯.”이다. 아래 40개는 기존 빈 아이콘 칸과 실제 이름·설명 호버를 유지하는 것으로 승인됐다. 원본 Sprite 지정 대기는 해제하며 새 아트, 대체 그림, 원본 SO 연결 변경은 요구되지 않는다. 기존 획득 UI가 이미 이 동작이므로 게임 코드는 수정하지 않았고 기존 240개 검사도 반복하지 않았다. 현재 `acquiredUi` 요구는 충족된 것으로 기록한다.

아래 목록은 원본 대조 당시의 미확인 아이콘 기록으로 보존하며 미완료 작업 목록으로 취급하지 않는다. 전체 GUID·직렬화 값·유사 원본·후보 그림의 기존 사용처와 최신 승인 상태는 `IconAudit.json`에 기록했다.

| Base의 ID | 원본 폴더 | 현재 이름 | SO 경로 |
|---:|---|---|---|
| 0 | SSW | 거인 | `Assets/SSW/Augments/Common/Giant.asset` |
| 1 | SSW | 뱀파이어 | `Assets/SSW/Augments/Common/Vampire.asset` |
| 2 | SSW | 광전사 | `Assets/SSW/Augments/Common/Berserker.asset` |
| 3 | SSW | 자신감 | `Assets/SSW/Augments/Common/Confidence.asset` |
| 4 | SSW | 유리 대포 | `Assets/SSW/Augments/Common/GlassCannon.asset` |
| 16 | SSW | 쿨감 | `Assets/SSW/Augments/Common/CooldownReduction.asset` |
| 22 | JJW | 코인 업그레이드! | `Assets/JJW/Cards/GamblerAugment_CoinUpgrade.asset` |
| 23 | JJW | 신난다 | `Assets/JJW/Cards/GamblerAugment_Excited.asset` |
| 24 | JJW | 확실한 잭팟 | `Assets/JJW/Cards/GamblerAugment_GuaranteedJackpot.asset` |
| 25 | JJW | 잭팟!! | `Assets/JJW/Cards/GamblerAugment_JackpotBoost.asset` |
| 27 | JJW | 더 많은 기회 | `Assets/JJW/Cards/GamblerAugment_MoreChances.asset` |
| 28 | JJW | 낡은 동전 | `Assets/JJW/Cards/GamblerAugment_OldCoin.asset` |
| 29 | JJW | 넘치는 힘 | `Assets/JJW/Cards/GamblerAugment_OverflowingPower.asset` |
| 30 | JJW | 확률 변동 | `Assets/JJW/Cards/GamblerAugment_ProbabilityShift.asset` |
| 31 | JJW | 판돈 올리기 | `Assets/JJW/Cards/GamblerAugment_RaiseTheStakes.asset` |
| 57 | SSW | 불사조 | `Assets/SSW/Augments/Common/Phoenix.asset` |
| 58 | SSW | 죽음의 무도 | `Assets/SSW/Augments/Common/DeathWaltz.asset` |
| 59 | AJH | 10개의 목숨 | `Assets/AJH/03.SO/HealthAugment/Ten Lives.asset` |
| 60 | AJH | 멀티스케일 | `Assets/AJH/03.SO/HealthAugment/Multiscale.asset` |
| 61 | AJH | 띠끌모아태산 | `Assets/AJH/03.SO/HealthAugment/Regeneration.asset` |
| 62 | SSW | 방어 숙련 | `Assets/SSW/Augments/Common/GuardMastery.asset` |
| 63 | SSW | 무적 | `Assets/SSW/Augments/Common/Invincible.asset` |
| 64 | SSW | 재충전 | `Assets/SSW/Augments/Common/Recharge.asset` |
| 65 | SSW | 힐 필드 | `Assets/SSW/Augments/Common/HealField.asset` |
| 66 | SSW | 점멸 | `Assets/SSW/Augments/Common/Blink.asset` |
| 67 | SSW | 아이스 에이지 | `Assets/SSW/Augments/Common/IceAge.asset` |
| 68 | SSW | 최고의 방어는 공격 | `Assets/SSW/Augments/Common/BestOffense.asset` |
| 69 | SSW | 뉴클리어 | `Assets/SSW/Augments/Common/Nuclear.asset` |
| 70 | AJH | 카운터 | `Assets/AJH/03.SO/DefanceAugment/ConterAttack.asset` |
| 71 | SSW | 악마와의 거래 | `Assets/SSW/Augments/Common/DevilsDeal.asset` |
| 72 | SSW | 쾌속 접근 | `Assets/SSW/Augments/Common/SwiftApproach.asset` |
| 73 | SSW | 냠냠 | `Assets/SSW/Augments/Common/NomNom.asset` |
| 74 | SSW | 자석 | `Assets/SSW/Augments/Common/Magnet.asset` |
| 75 | AJH | 축소 엔진 | `Assets/AJH/03.SO/SkillAugment/ShrinkENgine.asset` |
| 76 | AJH | 다재다능 | `Assets/AJH/03.SO/SkillAugment/Versatile.asset` |
| 77 | AJH | 더블점프 | `Assets/AJH/03.SO/SkillAugment/DoubleJump.asset` |
| 78 | AJH | 쿵 | `Assets/AJH/03.SO/SkillAugment/Slam.asset` |
| 79 | AJH | 지뢰밭 | `Assets/AJH/03.SO/SkillAugment/Minefield.asset` |
| 80 | AJH | 느려저라 | `Assets/AJH/03.SO/SkillAugment/SlowAura.asset` |
| 81 | AJH | 폭탄 도약 | `Assets/AJH/03.SO/DefanceAugment/LeapBomb.asset` |

## 검증과 보존

새 기능 코드는 없으므로 기존 240개 검사를 반복하지 않았다. 이번 근거는 최신 저장 자산의 실제 Editor 참조 조회와 소비 코드 대조다. 원본 목록 `MissingIcons.json`은 당시 기록으로 보존했다. 조사 전후 사용자 `.idea` 변경, DOTween 설정, EditorBuildSettings 파일의 SHA-256이 일치했다. Unity는 MainMenu, Edit 모드, 저장되지 않은 씬 변경 없음으로 유지했다.

원시 조사 자료: `Logs/Icons26/assets.json`, `references.json`, `candidates.json`. 최초 전체 조회의 CLI 응답은 5초 제한으로 종료됐지만 에디터 작업은 완료되어 전체 JSON 결과가 저장된 것을 확인했다. 이후 별도 Sprite 경로 조회도 정상 응답했다. 새로운 Player 빌드나 대전 검증은 수행하지 않았다.
