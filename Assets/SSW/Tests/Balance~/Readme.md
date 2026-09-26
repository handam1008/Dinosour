# 총잡이 탄환 수치 연결

통합 기준 Base `a964e846fa89b989cac78e25c03ebf8e34f2da00`. 변경은 총잡이 탄환 효과 다섯 값과 원본 자동 연동에 한정한다.

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

Player.prefab의 GunCast._balance는 출력 GUID `59aba008e14f56b48bb6206f9b17fcc3`, fileID `11400000`을 참조한다. 연결 변경은 이 한 필드이며 기존 체력·물리 재질·총구·이펙트 참조를 유지한다.

## 검증

- Unity CLI 재컴파일 오류 0.
- `BakeChecks.cs`: 46개 통과. 격리된 SSW 프리팹 사본 다섯 개를 실제 저장한 뒤 import 자동 갱신, Play/build 콜백의 오래된 출력 교체, 잘못된 원본 거부, 원본 바이트·출력·씬 보존을 확인했다. `Logs/Balance26/BakeChecks.json`.
- 앞선 두 실행은 자동 갱신 대기 실패였다. Editor update에서 처리하도록 바꾸고 재import되는 테스트 자산을 다시 읽어 최종 검사를 통과했다. 실패 기록도 Logs에 보존했다.
- `RuntimeChecks.cs`: 실제 Play에서 16사례·92개 통과. 다섯 효과의 비충전·충전, 별도 수치 4/13/0.6/3/5의 실제 소비, 피해 횟수·주기·힘·감속 만료를 확인했다. 이 중 15사례는 실제 서버 GunCast.Hit에 탄환 정보를 주입했고, 마지막 화염 사례는 Cast.Attack이 생성한 투사체의 실제 충돌로 21 피해를 확인했다. 이 실행은 최종 프리팹 연결 전 런타임 참조를 제공한 상태이며 저장된 프리팹 연결의 증거로 사용하지 않는다. `Logs/Balance26/RuntimeChecks-before-link.json`.
- 최종 저장된 프리팹으로 개발 빌드 `build_4f9766c340d2` 완료, 오류 0·경고 12. `Builds/Balance/Game.exe`, `Logs/Balance26/Build.json`.
- `Peers.ps1`: Editor 호스트와 개발 빌드 접속자, 양방향 지연 60ms·지터 10ms에서 다섯 효과를 양쪽이 각각 발사했다. 저장된 프리팹으로 새 네트워크 플레이어를 생성했으며 설정 참조를 런타임에 주입하지 않았다. 증강과 비충전 탄약만 준비하고 실제 Cast.Attack·투사체 충돌·피해 복제를 확인했다.
- 멀티 검사 총 28개 통과: `Peers2`의 화염·중력 12개와 `Peers3`의 얼음·독·수리검 16개. 화염 15+6, 독 15+16, 수리검은 거리 6에서 15+6 피해가 양쪽에 일치했다. 중력 충격 후 감속·위치 수렴, 얼음 0.3 적용 시 기본 속도 7의 70%인 4.9로 실제 이동 후 복귀, 추가 지연 피해 없음도 확인했다. `Logs/Balance26/Peers2/Checks.txt`, `Logs/Balance26/Peers3/Result.json`.
- `Peers1`은 CLI 반환 좌표 직렬화 오류로 발사되지 않았고, `Peers2`는 클라이언트의 음수 이동 방향을 속력 검사에 반영하지 않아 얼음 단계에서 멈췄다. 테스트 준비·비교를 수정해 남은 세 효과를 다시 검사했으며 실패 기록을 보존했다.
- 같은 PC의 지연 시뮬레이션이며 실제 두 PC의 UGS Relay, 접속 멈춤 원인, 모든 증강 조합을 검증한 것은 아니다. 앞선 콜백 단위 검사, 서버 명중 검사, 실제 양쪽 프로세스 검사를 구분한다.

`Setup.cs`는 최초 에셋을 연결하는 Unity CLI eval 파일이다. 기존 화염 변형의 수치를 반복 실행으로 덮지 않는다. Player.prefab은 수정하지 않고 필요한 참조 정보를 `Logs/Balance26/PlayerPatch.json`에 기록한다.

`Peers.ps1`은 연결된 Editor가 저장된 Edit 상태일 때 프로젝트 루트에서 실행한다. 실행별 새 `-Run` 이름을 지정하며 `-Effects IceBullet,PoisonBullet,ShurikenBullet`로 범위를 제한할 수 있다. 종료 시 테스트 클라이언트와 Play를 끝내고 기존 씬·직업 선택을 복원한다.
