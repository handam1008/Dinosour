# 사운드 연결

공통 재생기는 `Assets/SSW/Resources/Audio/GameAudio.prefab`이다. 메뉴와 맵에서 자동으로 준비되며, 전체·배경음·효과음 음량을 각각 저장한다. 효과음은 24개 소스를 재사용하고 2D로 재생하므로 카메라 거리 때문에 상대 소리가 작아지지 않는다.

## 음악 넣기

- `Assets/SSW/Audio/Cues/MenuMusic.asset`: 메뉴 배경음
- `Assets/SSW/Audio/Cues/BattleMusic.asset`: 대전·샌드박스 배경음

각 에셋의 `Clip`에 음악을 넣으면 된다. `Music`, `Is Loop`가 이미 설정되어 있다. 현재 저장소에는 BGM 음원이 없어 두 슬롯은 비워 두었다. 메뉴↔맵 전환 시 자동으로 교체되고, 같은 음악 요청은 처음부터 재생하지 않는다.

## 효과음 넣기

1. Project에서 `Create → Sound → Cue`를 만든다.
2. `Clip`에 음원을 넣고 `Audio Type = Sfx`, 음량·피치를 정한다. `End Time = 0`은 끝까지 재생한다.
3. 버튼이나 오브젝트에 `SoundEmitter`를 붙이고 `Cue`를 연결한다.
4. UnityEvent나 애니메이션 이벤트에서 `SoundEmitter.Play()`를 호출한다. 필요하면 `Play On Enable`을 켠다.

일반 UI는 `Shared`를 끈다. 같은 컴포넌트의 `Play()`는 이전 소리를 교체하며 `Stop()`으로 중지할 수 있다. 여러 소리를 겹치려면 아래 공통 서비스의 `PlaySfx()`를 사용한다. 반복 효과음은 소유 컴포넌트가 사라지기 전에 `Stop()`을 호출한다.

## 양쪽에 들리는 효과음

`Assets/SSW/Audio/Sounds.asset`의 `Shared` 목록에 Cue를 넣고 `SoundEmitter.Shared`를 켠다. 서버가 실제 행동을 확정하는 지점에서 `Play()`를 호출한다. 접속자의 호출은 서버로 임의 전송하지 않는다. 이미 RPC로 양쪽에서 실행되는 UI·연출에는 `Shared`를 끈다.

코드에서 서버 확정 효과음을 재생할 때:

```csharp
[SerializeField] SoundCue _hit;

void PlayHit()
{
    NetGame.Current.Sounds.Play(_hit);
}
```

기본 6개 직업의 공격·타격·구현된 스킬은 이미 연결했다. `Sounds.asset`의 `Jobs`에서 음원을 바꾸면 된다. 소유자의 발사음은 로컬에서 즉시 재생하고 서버 응답의 중복을 제거한다. 지연 발사 카드는 실제 발사 시점에 재생한다. 충돌음은 서버 판정으로 전달한다. 네트워크로 음원 파일이나 연속 스트림을 보내지 않는다.

`Shared`는 반복하지 않는 효과음 전용이다. BGM은 위 메뉴·전투 슬롯을 사용한다. 새 공유 Cue를 추가한 뒤에는 양쪽 모두 같은 버전으로 실행한다. Cue ID는 Unity 에셋 GUID에서 자동으로 만들어지므로 직접 입력할 필요가 없다.

## 기존 코드에서 사용하기

`SoundCue`는 기존 `SoundClipSO`를 상속하고, `GameAudio`는 기존 `IAudioService`를 구현한다. 기존 서비스 호출도 그대로 사용할 수 있다. 아래 호출은 해당 컴퓨터에서만 재생한다.

```csharp
ServiceLocator.Get<IAudioService>().PlaySfx(cue);
ServiceLocator.Get<IAudioService>().PlayBgm(music);
ServiceLocator.Get<IAudioService>().StopBgm();
```

효과음 `channel`이 0이면 중첩 재생한다. 양수 채널을 지정하면 같은 채널의 이전 소리를 교체하고 `StopSfx(channel)`로 멈춘다. 효과음 슬라이더는 전투·카드·화면 전환 소리에 함께 적용된다. 팀원 폴더의 기존 소스·믹서는 변경하지 않았다.

## 검증

`Assets/SSW/Tests/Audio~/Smoke.cs`는 Unity CLI 플레이 모드에서 BGM·SFX 출력, 음소거 분리, 풀 회수를 검사한다. 테스트용 톤은 실행 중에만 생성하며 음악 에셋에 저장하지 않는다. `Relay.ps1`은 최신 개발 빌드 두 개로 실제 UGS Relay 방에 접속하고 발사·충돌 횟수와 오디오 출력 신호를 비교한다. 검증 프로브는 릴리스 빌드에서 제외된다.
