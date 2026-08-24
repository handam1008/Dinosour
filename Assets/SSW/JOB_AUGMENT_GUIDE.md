# SSW 직업·증강 인터페이스 가이드

## 20초 요약

1. 다른 직업의 클래스를 직접 참조하지 않습니다.
2. 피해는 항상 `CombatDamage.Deal(...)`로 보냅니다.
3. 회복·둔화·가속·밀치기 등은 대상의 능력 인터페이스를 찾아 호출합니다.
4. 직업 스킬은 `JobModuleBehaviour`, 직업 증강 수신기는 `AugmentReceiverBehaviour`를 상속합니다.
5. 카드 무늬, 잭팟, 포션 순환 같은 직업 규칙은 공용 인터페이스로 만들지 않습니다.

```csharp
using SSW;
```

다른 팀 폴더에서는 위 네임스페이스만 추가하면 SSW 계약을 사용할 수 있습니다.

## 어떤 인터페이스를 쓰면 되나

| 하고 싶은 것 | 사용할 계약 | 현재 구현체 |
| --- | --- | --- |
| 피해 주기 | `IDamageable` + `CombatDamage` | `Health` |
| 피해 종류·결과·방어 처리 | `IDamageReceiver`, `DamageRequest`, `DamageResult` | `Health` |
| 회복하기 | `IHealable` | `Health` |
| 둔화하기 | `ISlowable` | `PlayerController` |
| 이동속도 증가 | `ISpeedable` | `PlayerController` |
| 공격력 감소 | `IWeakenable` | `PlayerController` |
| 밀기·당기기 | `IForceReceiver` | `PlayerController`, `Rigidbody2DForceReceiver` |
| 공격자의 최종 피해 변경 | `IOutgoingDamageModifier` | `PlayerController` |
| 방어·무적·받는 피해 변경 | `IIncomingDamageModifier` | 효과별 컴포넌트가 구현 |
| 공격 성공·흡혈·적중 퀘스트 | `IDamageDealtListener` | 효과별 컴포넌트가 구현 |
| 피격 후 가속·반격 | `IDamageReceivedListener` | 효과별 컴포넌트가 구현 |
| 직업 기능 활성화 | `IJobModule` / `JobModuleBehaviour` | 각 직업 스킬 |
| 증강 발행 | `IAugmentSource` | `AugmentDrafter` |
| 증강 수신 | `IAugmentReceiver` / `AugmentReceiverBehaviour` | 각 직업 증강 컨트롤러 |
| 직업 전용 증강 제한 | `IJobRestrictedAugment` | 각 직업 증강 에셋 |

`IDamageable` 안의 `Heal`은 기존 코드 호환용입니다. 새 회복 코드는 역할이 분리된 `IHealable`을 사용합니다.

## 피해 주기

```csharp
IDamageable target = hit.GetComponentInParent<IDamageable>();
if (target == null) return;

DamageResult result = CombatDamage.Deal(
    this,
    target,
    damage,
    DamageTag.BasicAttack | DamageTag.Projectile);

if (result.WasApplied)
{
    // 실제 피해 성공
}
```

`CombatDamage`를 거치면 다음 순서가 자동으로 적용됩니다.

1. 공격자 부모의 모든 `IOutgoingDamageModifier` (`Priority`가 낮은 순서)
2. 대상 부모의 `IIncomingDamageModifier` (`Priority`가 낮은 순서)
3. 실제 체력 감소
4. 대상의 `IDamageReceivedListener`
5. 공격자의 `IDamageDealtListener`

직접 `target.TakeDamage(...)`를 호출하면 기존 피해는 들어가지만 공격자 정보, 피해 태그, 공격 성공 리스너가 빠집니다.

### DamageTag

| 태그 | 언제 붙이나 |
| --- | --- |
| `BasicAttack` | 기본 공격 |
| `JobSkill` | 직업 스킬로 발생한 피해 |
| `Projectile` | 카드·총알·동전·포션 같은 발사체 |
| `DamageOverTime` | 독·화상·출혈 같은 지속 피해 |
| `Environment` | 장애물·바닥·맵 피해 |
| `IgnoreDefense` | 방어 무시 공격이라는 표시 |

태그는 `|`로 함께 붙입니다. `IgnoreDefense` 자체가 자동으로 모든 방어를 뚫는 것은 아닙니다. 방어 구현체가 해당 태그를 보고 자기 방어만 건너뛰어야 합니다.

```csharp
DamageTag tags = DamageTag.JobSkill | DamageTag.DamageOverTime;
CombatDamage.Deal(this, target, tickDamage, tags);
```

### DamageResult

- `RequestedAmount`: 공격력 보정까지 끝나고 대상에게 요청된 피해
- `AppliedAmount`: 방어와 남은 체력을 반영한 실제 피해
- `WasApplied`: 실제 피해가 0보다 큰지
- `WasBlocked`: 피해 요청은 있었지만 실제 피해가 0인지
- `WasLethal`: 이번 피해로 체력이 0이 됐는지

흡혈은 반드시 `AppliedAmount`를 기준으로 계산합니다.

## 회복·상태·힘 적용

```csharp
hit.GetComponentInParent<IHealable>()?.Heal(10f);

// 20% 감소, 1.5초
hit.GetComponentInParent<ISlowable>()?.ApplySlow(0.2f, 1.5f);

// 20% 증가, 3초
hit.GetComponentInParent<ISpeedable>()?.ApplySpeed(0.2f, 3f);

// 공격력 10% 감소, 0.5초
hit.GetComponentInParent<IWeakenable>()?.ApplyAttackWeaken(0.1f, 0.5f);

// 오른쪽으로 순간 힘 4
hit.GetComponentInParent<IForceReceiver>()?.ApplyForce(
    Vector2.right * 4f,
    ForceMode2D.Impulse);
```

`Rigidbody2D`만 있는 대상에는 `Rigidbody2DForceReceiver`를 붙이면 됩니다. SSW 더미 프리팹에는 이미 붙어 있습니다.

현재 `PlayerController`의 같은 종류 속도 효과는 마지막 호출이 이전 값을 덮어씁니다. 중첩·영구 효과 규칙이 확정되기 전까지 합산된다고 가정하면 안 됩니다. 둔화와 가속은 서로 곱해집니다.

## 방어·무적·죽음의 무도 만들기

받는 피해를 바꾸는 컴포넌트만 `IIncomingDamageModifier`를 구현합니다. `Health`를 고치지 않습니다.

```csharp
public sealed class GuardDamageModifier
    : MonoBehaviour, IIncomingDamageModifier
{
    public int Priority => 100;
    public bool IsGuarding { get; set; }

    public float ModifyIncomingDamage(
        DamageRequest request,
        float currentAmount)
    {
        if (!IsGuarding) return currentAmount;
        if (request.HasTag(DamageTag.IgnoreDefense)) return currentAmount;
        return 0f;
    }
}
```

- 무적: 활성 중 `0f` 반환
- 피해 감소: `currentAmount * 0.8f`처럼 반환
- 죽음의 무도: 일부만 즉시 반환하고 나머지를 `DamageOverTime` 태그로 나눠 전달
- 처리 순서가 중요하면 `Priority`를 사용

공격/방어 보정 모두 낮은 `Priority`부터 실행됩니다. 순서에 따라 결과가 달라지는 보정끼리는 서로 다른 값을 써야 합니다(예: 고정 가감 `100`, 비율 보정 `200`). 같은 값이면 타입 이름 순으로 처리됩니다.

## 흡혈·공격 성공·피격 반응 만들기

```csharp
public sealed class VampireRuntime
    : MonoBehaviour, IDamageDealtListener
{
    [SerializeField] float _rate = 0.3f;
    IHealable _owner;

    void Awake()
    {
        _owner = GetComponent<IHealable>();
    }

    public void OnDamageDealt(
        DamageRequest request,
        DamageResult result)
    {
        if (!result.WasApplied) return;
        if (request.HasTag(DamageTag.DamageOverTime)) return;
        _owner?.Heal(result.AppliedAmount * _rate);
    }
}
```

- 흡혈·명중 퀘스트·표식: 공격자 쪽 `IDamageDealtListener`
- 피격 시 가속·반격: 대상 쪽 `IDamageReceivedListener`
- 리스너는 `CombatDamage.Deal`에 넘긴 공격자 컴포넌트의 부모에서 검색됩니다.

## 새 직업 스킬 추가

```csharp
public sealed class SwordsmanDash : JobModuleBehaviour
{
    public override PlayerJob Job => PlayerJob.Swordsman;

    void Update()
    {
        if (!IsJobActive) return;
        // 칼잽이 기능만 작성
    }

    protected override void OnJobDeactivated()
    {
        // 코루틴, 이펙트, 임시 상태 정리
    }
}
```

1. 자기 직업 폴더에 `JobModuleBehaviour` 파생 컴포넌트를 만듭니다.
2. `Job`에 담당 직업만 반환합니다.
3. 입력·`Update` 시작에서 `IsJobActive`를 검사합니다.
4. 다른 직업의 컨트롤러나 증강 enum을 참조하지 않습니다.

## 새 직업 증강 추가

```csharp
[CreateAssetMenu(menuName = "Game/Swordsman Augment")]
public sealed class SwordsmanAugment
    : Augment, IJobRestrictedAugment
{
    public SwordsmanAugmentType type;
    public PlayerJob RequiredJob => PlayerJob.Swordsman;
}

public sealed class SwordsmanAugmentController
    : AugmentReceiverBehaviour
{
    public override PlayerJob Job => PlayerJob.Swordsman;

    public override bool TryReceive(Augment augment)
    {
        if (augment is not SwordsmanAugment own) return false;
        // own.type을 자기 런타임 상태에 반영
        return true;
    }
}
```

1. 직업별 `Augment` 타입과 enum은 자기 폴더에 둡니다.
2. 직업 전용 에셋은 `IJobRestrictedAugment.RequiredJob`을 구현합니다.
3. 수신기는 `AugmentReceiverBehaviour`를 상속하고 자기 증강 타입만 받습니다.
4. 에셋을 자기 `AugmentPool`에 넣고 `AugmentDrafter`의 직업 풀에 연결합니다.

공통 증강은 특정 직업 컨트롤러를 찾지 말고 위 능력 인터페이스·피해 리스너를 조합합니다.

## 기획서 기능 매핑

| 기획 기능 | 구현 방향 |
| --- | --- |
| 직접 피해·범위 피해 | `CombatDamage` + 알맞은 `DamageTag` |
| 독·화상·출혈 | 틱마다 `DamageOverTime` 태그 피해. 상태 중첩 규칙은 별도 시스템에서 관리 |
| 회복·힐 필드·재생 | `IHealable` |
| 흡혈·공격 성공·적중 퀘스트 | `IDamageDealtListener` + `DamageResult.AppliedAmount` |
| 피격 시 도망·반격 | `IDamageReceivedListener` |
| 무적·피해 감소·죽음의 무도 | `IIncomingDamageModifier` |
| 공격력 증가·감소 | `IOutgoingDamageModifier` / `IWeakenable` |
| 둔화·신속 | `ISlowable` / `ISpeedable` |
| 추진기·자석·넉백 | `IForceReceiver` (`Force`는 끌기, `Impulse`는 순간 밀기) |
| 카드 무늬·연계·조커 | 마술사 런타임 상태 + `MagicianAugmentController` |
| 잭팟·룰렛 확률 | 도박사 런타임 상태 + 이벤트 + ScriptableObject 데이터 |
| 포션 풀·교체·칵테일 | 마녀 런타임 상태 + 이벤트 + ScriptableObject 데이터 |
| 수치·설명·등급·아이콘 | ScriptableObject |

## 아직 만들지 않은 공통 계약

아래는 기획에는 필요하지만 규칙이 확정되지 않아 이름만 먼저 만들지 않았습니다.

| 후보 | 먼저 정할 규칙 |
| --- | --- |
| 상태효과 시스템 | 합산/최강값/갱신, 상태 해제, 라운드 종료 정리 |
| 스탯 수정 시스템 | 가산·곱연산 순서, 최대 체력 변경 시 현재 체력 처리 |
| 쿨타임 계약 | 기본 지속시간 감소와 현재 남은 시간 감소를 분리할지 |
| 방어/패링 이벤트 | 방어 시작, 방어 성공, 패링 성공을 같은 시스템으로 볼지 |
| 투사체 계약 | 카드·총알·동전·포션 중 공용 원거리 증강 적용 대상 |
| 탄약 계약 | 현재/최대 탄약, 한 발 회수, 강탈, 재장전 책임 위치 |
| 부활 계약 | 사망 이벤트 순서, 1회 소모 시점, 비활성화 처리 |
| 이동 불가 계약 | 수평 입력만 막는지, 점프·중력까지 멈추는지 |

담당 기능이 실제 구현될 때 소비자와 구현체가 최소 2개 이상이면 공통 인터페이스로 올립니다. 그 전에는 직업 내부 이벤트나 데이터로 둡니다.

## 다른 팀이 해야 할 통합 작업

SSW 밖 파일은 이번 작업에서 수정하지 않았습니다. 아래 공격 코드는 담당자가 `CombatDamage.Deal`로 바꿔야 공격력 감소, 흡혈, 피해 태그가 모두 연결됩니다.

- `Assets/NKY/Scripts/AssassinNormalSkill.cs`
- `Assets/NKY/Scripts/AssassinMeleeAttack.cs`
- `Assets/KDH/Scripts/Bullet/KDH_BulletDamageModule.cs`
- `Assets/RYU/3.SO/PotionSO/DamagePotion.cs`

담당자별 권장 태그:

- 암살자 기본 공격: `BasicAttack`
- 암살자 단검 스킬: `JobSkill | Projectile`
- 총알: `BasicAttack | Projectile`
- 공격 포션: `JobSkill | Projectile`
- 독·화상 틱: 기존 태그에 `DamageOverTime` 추가

칼잽이 담당자는 현재 `KHG_Paring.OnParrySuccess`를 공용 방어 이벤트로 노출하기 전에 방어 시작/성공/패링 성공의 차이를 팀에서 확정해야 합니다. 총 담당자는 탄약의 소비·회수·강탈 규칙을 확정한 뒤 공용 탄약 계약을 추가해야 합니다.

## 팀 규칙 체크리스트

- [ ] 다른 직업의 구체 클래스나 enum을 참조하지 않았다.
- [ ] 피해는 `CombatDamage.Deal`로 보냈고 태그를 붙였다.
- [ ] 회복은 `IHealable`, 힘은 `IForceReceiver`를 사용했다.
- [ ] ScriptableObject에는 설정값, MonoBehaviour에는 런타임 상태를 뒀다.
- [ ] 공용 시스템에 직업별 `if`/`switch`를 추가하지 않았다.
- [ ] 이벤트를 구독했다면 `OnDisable`/`OnDestroy`에서 해제했다.
- [ ] 새 인터페이스는 실제 소비자와 구현체가 있는지 확인했다.
- [ ] 자기 폴더 밖 수정이 필요하면 먼저 해당 담당자에게 전달했다.

## 현재 마술사 연결 상태

- 스페이드·다이아 피해는 `JobSkill | Projectile` 태그로 전달됩니다.
- 하트 회복은 `IHealable`을 사용합니다.
- 클로버 둔화/공격력 감소는 `ISlowable`/`IWeakenable`을 사용합니다.
- 카드 넉백은 `IForceReceiver`를 사용합니다.
- 마술사 전용 증강 10종은 `MagicianAugmentController`와 `FlyingCard`에 연결되어 있습니다.
- 테스트용 드래프트 키는 `P`(공용 3택), `O`(직업 보상 + 공용 3택)입니다.
