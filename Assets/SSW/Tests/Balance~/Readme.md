# 총잡이 탄환 수치 연결

초기 기준 Base `a443d6d991d01b9df94fab741182cf6be5906095`. 변경은 총잡이 탄환 효과 다섯 값에 한정한다.

| 효과 | 담당 원본 프리팹 필드 | 원본 값 | 기존 서버 | 적용 값 |
|---|---|---:|---:|---:|
| FireBullet | KDH_FireBullet.dotDamage | 2 | 3 | 1 |
| GravityBullet | KDH_GravityBullet.forceAmount | 10 | 50 | 10 |
| IceBullet | KDH_IceBullet.slowAmount | 0.3 | 0.2 | 0.3 |
| Poison | KDH_PoisonBullet.dotDamage | 2 | 7 | 2 |
| Shuriken | KDH_ShurikenBullet.damage | 1 | 3 | 1 |

원본 경로는 `Assets/KDH/GameModules/Prefabs/AbilityBullets/`이다. 이 Poison과 Shuriken은 총잡이 탄환 증강이며 마녀의 독 포션, 공용 독, 암살자 투척 스킬과 별개다.

화염은 `Assets/SSW/Prefabs/Balance/FireBullet.prefab` 변형에서 `dotDamage=1`만 지정한다. 팀원 원본의 2를 수정하지 않는다. 이후 화염 피해 수정 기준은 이 SSW 변형이다. 부모의 dotDamage는 명시적인 변형 override 때문에 적용되지 않는다. 나머지 네 값은 지정한 KDH 원본을 직접 읽는다.

`Assets/SSW/Editor/GunSources.asset`이 다섯 원본 컴포넌트와 출력 `Assets/SSW/Resources/Network/GunBalance.asset`을 참조한다. GunBake는 원본/부모 변형의 저장·import, Play 진입, 빌드 전 해당 다섯 필드를 다시 읽는다. 변경이 없으면 저장하지 않는다. 출력은 SSW에만 저장하며 누락·중복 출력이나 음수/비유한 입력을 거부한다.

GunCast는 `_balance`의 값을 서버 명중 경로에서 소비한다. 화염 6회/0.5초, 독 8회/4초, 얼음 1.5초와 충전 배율, 중력 방향·Impulse 연산, 수리검 거리 곱을 유지한다. Ice 0.3은 기존 `Clamp01(1 - amount)` 연산의 입력이며 백분율로 재해석하지 않는다. 확률·기간·주기·사거리·다른 증강 값은 변경하지 않는다.

공유 Player.prefab은 루미가 연결한다. 최종 필드는 GunCast._balance이며 출력 GUID `59aba008e14f56b48bb6206f9b17fcc3`, fileID `11400000`을 지정한다. 이전 숫자 다섯 필드 패치는 이 참조로 대체한다. 이 초기 공유 커밋에는 Player.prefab 변경이 포함되지 않는다.

## 검증

- Unity CLI 재컴파일 오류 0.
- `BakeChecks.cs`: 46개 통과. 격리된 SSW 프리팹 사본 다섯 개를 실제 저장한 뒤 import 자동 갱신, Play/build 콜백의 오래된 출력 교체, 잘못된 원본 거부, 원본 바이트·출력·씬 보존을 확인했다. `Logs/Balance26/BakeChecks.json`.
- 앞선 두 실행은 자동 갱신 대기 실패였다. Editor update에서 처리하도록 바꾸고 재import되는 테스트 자산을 다시 읽어 최종 검사를 통과했다. 실패 기록도 Logs에 보존했다.
- Play/build 콜백 호출 검사는 실제 Play 명중 또는 Player 빌드 완료를 의미하지 않는다. 초기 공유 시 실제 효과 검증과 공유 프리팹 통합 검증은 진행 전이다.

`Setup.cs`는 최초 에셋을 연결하는 Unity CLI eval 파일이다. 기존 화염 변형의 수치를 반복 실행으로 덮지 않는다. Player.prefab은 수정하지 않고 필요한 참조 정보를 `Logs/Balance26/PlayerPatch.json`에 기록한다.
