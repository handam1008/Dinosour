# 리더보드 조회

2026-09-26 Base `87786ba`에서 확인했다. 실제 UGS 프로젝트 `6dd30f94-b424-49c0-976d-0360a9d2f7b5`의 기존 `Ranking` 조회는 6개 기록을 정상 반환했다. 서버 전체 장애는 재현되지 않았다.

MainMenu의 비활성 `LeaderboardManager`에는 `titleManager`가 없었다. 조회 결과에 현재 로그인한 플레이어가 포함되면 `LoadAllScore` 84행의 칭호 변경에서 `NullReferenceException`이 발생했다. 서버 쓰기 없이 로컬 응답 대역에 자기 기록을 넣어 기존 메서드에서 재현했다. 이는 조회 성공 이후 표시 단계의 오류다. 실제 검사 계정은 순위에 없었으므로 고객 계정에서 발생한 조건까지 확인한 것은 아니다.

MainMenu는 동일한 UGS 초기화·인증·Ranking 조회를 사용하는 `RankSource`와 기존 RankRow 프리팹을 표시하는 `RankList`에 연결했다. 다른 팀원 코드와 서버의 기록·설정은 변경하지 않았다. 칭호 갱신과 순위 조회를 분리하고, 이름 없는 기록은 실제 플레이어 식별자로 표시한다. 비어 있는 목록, 인증·네트워크·서비스·설정·요청 제한 오류를 구분한다. 요청에는 실제 시간 기준 15초 제한이 있고, 닫은 화면의 응답은 새 화면을 덮지 않는다.

## 검증

- 실제 서비스: 6개 순위·점수·이름이 응답과 일치한다. MainMenu 직접 진입과 재열기 3회, 재조회가 성공했다.
- 실제 StartMenu의 저장된 세션 로그인 후 MainMenu 자동 전환과 리더보드 버튼 조회가 성공했다. 프로젝트 ID는 동일했고 6개 행이 표시됐다. 다른 사용자의 외부 Unity 계정 로그인은 실행하지 않았다.
- 로컬 응답 대역: 자기 기록, 누락 이름, 빈 목록, 네트워크·인증·서비스·설정·요청 제한 실패, 일시정지 중 시간 초과, 닫기/재열기 이후 늦은 응답 무시와 실제 서비스 복귀를 확인했다.
- `RankChecks` 36개 검사가 통과했다. 오류 0개로 스크립트 컴파일을 확인했다. 별도 배포 빌드와 다른 PC 서비스 조회는 이번 검사에 포함하지 않았다.

MainMenu Play에서 `unity command eval_file --file "Assets/SSW/Tests/Menu~/RankRun.cs" --caller plugin --skill unity-cli --project-path "<project>" --format json`을 실행한다. 결과는 `Logs/Leaderboard26/checks.json`에 기록한다. 검사 종료 시 실제 조회 소스로 복귀한다. 원본 재현과 StartMenu 결과는 같은 로그 폴더에 보존한다.

기존 UGS 조회 형식은 [Unity Get scores](https://docs.unity.com/en-us/leaderboards/tutorials/unity-sdk/get-score)를 참고했다.
