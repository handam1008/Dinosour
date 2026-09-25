# 맵 전환 검증

최대 7세트 동안 맵 14개를 중복 없이 사용한다. 세트가 끝나면 패자의 증강 선택 전에 다음 맵을 생성하며, 같은 세트의 다음 라운드는 같은 맵을 초기화한다. 두 클라이언트가 맵을 불러온 뒤 전투를 시작한다.

원본은 Assets/MapPrefab, 멀티플레이용 참조 프리팹은 Assets/SSW/Maps/Battle에 있다. 원본 배치는 유지하고 스폰 두 곳과 맵 기믹 동기화를 연결했다. Map02만 원본에 없던 스폰 마커 두 개를 추가했다. Map16·Map17·SwingMap의 줄 23개는 초기 배치의 실제 길이로 맞춰 생성 직후 발판이 밀리는 현상을 방지했다.

프로젝트 루트에서 Unity CLI에 연결된 에디터를 사용한다. 시작 전에 MainMenu 씬의 편집 내용을 저장하고 플레이 모드를 종료한다.

```powershell
unity command eval_file --project-path (Get-Location).Path --caller plugin --skill unity-cli --format json --file ((Get-Location).Path + '/Assets/SSW/Tests/Maps~/Checks.cs')
& './Assets/SSW/Tests/Maps~/Verify.ps1' -Mode Catalog -Run CatalogNew
& './Assets/SSW/Tests/Maps~/Verify.ps1' -Mode Flow -Run FlowNew
& './Assets/SSW/Tests/Maps~/Verify.ps1' -Mode Effects -Run EffectsNew
```

Verify.ps1은 에디터 호스트와 개발 빌드 접속자를 실행한다. 기본 접속자 경로는 Builds/MapRotation/Game.exe이며 -Build로 바꿀 수 있다. 포트는 7797이다. 결과와 양쪽 상태는 Logs/MapRotation25/<Run>에 저장한다. 종료 시 테스트 접속자와 플레이 모드를 정리한다.

2026-09-25 검증:
- Catalog1: 맵 14개 모두 양쪽 생성, 스폰, 이전 투사체 정리, 움직이는 발판 위치 일치.
- Flow2: 4:3으로 끝나는 7세트에서 중복 없음, 세트 도중 맵 유지, 세트 종료 후 변경, 패자만 증강 선택, 기존 증강 유지, 체력 초기화.
- Effects9: 시간 변화 동기화 및 정상 배속 복원, 접속자의 발판 탑승과 이동 예측, 발판 파괴 후 낙하 동기화, 작은 라운드 종료 후 같은 맵의 모든 핀·줄 복구 및 양쪽 발판 위치 복원.

같은 PC의 두 프로세스로 검증했다. 별도 PC 및 Relay 지연 환경은 이번 검증에 포함하지 않았다. 이번 맵 네트워크 구성부터 프로토콜 9를 사용하므로 양쪽 모두 같은 최신 빌드가 필요하다.
