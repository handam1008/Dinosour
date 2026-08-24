# SSW 직업·증강 확장 가이드

## 구조

- `PlayerIdentity`는 현재 직업만 보관하고 `JobChanged` 이벤트를 발행합니다.
- 직업 기능은 `JobModuleBehaviour`를 상속합니다. 공통 기반이 활성 직업 여부를 관리하므로 각 직업 코드는 다른 직업을 확인할 필요가 없습니다.
- `AugmentDrafter`는 `IAugmentSource`만 담당합니다. 구체적인 직업 컨트롤러를 참조하지 않습니다.
- 직업별 증강 컨트롤러는 `AugmentReceiverBehaviour`를 상속합니다. 공통 기반이 증강 이벤트 구독·해제, 기존 보유 증강 전달, 같은 에셋의 중복 적용 방지를 처리합니다.
- 직업 전용 증강 에셋은 `IJobRestrictedAugment`를 구현하여 잘못된 직업의 드래프트에 섞이지 않게 합니다.
- 공격 대상은 `IDamageable`, `ISlowable`, `IWeakenable` 같은 능력 인터페이스로만 찾습니다.
- 피해는 `CombatDamage.TryDeal(공격자컴포넌트, 대상, 기본피해)`로 전달합니다. 그래야 공격력 감소 같은 공통 상태가 모든 직업에 동일하게 적용됩니다.

## 새 직업 스킬 추가

```csharp
using UnityEngine;

namespace SSW
{
    public sealed class SwordsmanDash : JobModuleBehaviour
    {
        public override PlayerJob Job => PlayerJob.Swordsman;

        void Update()
        {
            if (!IsJobActive) return;
            // 이 파일 안에는 칼잽이 기능만 작성합니다.
        }

        protected override void OnJobDeactivated()
        {
            // 진행 중인 코루틴, 이펙트, 일시 상태를 정리합니다.
        }
    }
}
```

1. 자기 직업 폴더에 `JobModuleBehaviour` 파생 컴포넌트를 만듭니다.
2. `Job` 프로퍼티에 담당 직업을 반환합니다.
3. 입력이나 `Update` 시작에서 `IsJobActive`만 검사합니다.
4. 자기 직업 프리팹에 컴포넌트를 붙입니다. 다른 직업 스크립트는 참조하지 않습니다.

## 새 직업 증강 추가

```csharp
using UnityEngine;

namespace SSW
{
    [CreateAssetMenu(menuName = "SSW/Swordsman Augment")]
    public sealed class SwordsmanAugment : Augment, IJobRestrictedAugment
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
            if (augment is not SwordsmanAugment ownAugment) return false;
            // ownAugment.type을 저장하고 자기 기능에만 반영합니다.
            return true;
        }
    }
}
```

1. 직업별 `Augment` 파생 타입과 식별 enum을 자기 폴더에 둡니다.
2. `IJobRestrictedAugment.RequiredJob`을 구현합니다.
3. 직업별 컨트롤러가 `AugmentReceiverBehaviour.TryReceive`에서 자기 타입만 받게 합니다.
4. 만든 증강 에셋을 자기 `AugmentPool`에 넣고 직업 프리팹의 `AugmentDrafter._jobPool`에 연결합니다.

## 마술사 조작과 구현 범위

- `CycleSuit`을 누르는 동안 무늬를 섞고, 놓으면 확정합니다.
- `Attack`을 누르는 동안 숫자를 섞고, 놓으면 카드를 발사합니다.
- 스페이드: 숫자 비례 추가 피해
- 하트: 숫자 비례 자가 회복
- 다이아: 숫자 비례 범위 피해
- 클로버: 숫자 비례 1.5초 둔화
- 마술사 전용 증강 10종은 `MagicianAugmentController`와 `FlyingCard`에 연결되어 있습니다.
- 테스트용 드래프트 키는 `P`(공용 3택)와 `O`(직업 보상 + 공용 3택)입니다.

## 팀 규칙

- 다른 직업의 구체 클래스나 enum을 직접 참조하지 않습니다.
- 대상 기능은 새 능력 인터페이스로 표현합니다. 예: 피해 대상은 `IDamageable`.
- 공격 코드에서 `target.TakeDamage(damage)`를 직접 호출하지 말고 `CombatDamage.TryDeal(this, target, damage)`를 사용합니다.
- 공통 시스템에 직업별 `if`/`switch`를 추가하지 않습니다.
- ScriptableObject에는 설정 데이터를, MonoBehaviour에는 런타임 상태를 둡니다.
- 이벤트를 구독했다면 `OnDisable` 또는 `OnDestroy`에서 반드시 해제합니다.
