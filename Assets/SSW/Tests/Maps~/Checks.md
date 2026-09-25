# 맵·이동 검증

작은 라운드가 끝날 때마다 새 맵을 생성한다. 14개를 한 번씩 사용한 뒤 다시 섞으며 직전 맵의 연속 등장은 막는다. 최대 21라운드에서도 목록이 소진되지 않는다. 증강 선택은 세트 패자에게만 제공하고, 세트 안에서는 증강을 유지한 채 체력·위치·맵 오브젝트를 초기화한다.

원본 Assets/MapPrefab은 수정하지 않는다. Assets/SSW/Maps/Battle의 참조 프리팹에서 폭포를 LiftZone에 연결하고 화산의 흔들림을 서버에서 동기화한다. MotionCast는 움직이는 지형과 겹쳤을 때 실제 접촉 방향으로 위치를 복원하며 점프·경사·단방향 발판 내려가기를 유지한다.

편집 중인 씬을 저장하고 플레이 모드를 종료한 뒤 실행한다. Verify.ps1은 StartMenu의 저장된 로그인 세션을 사용한다. 개발 빌드에 MainMenu·SuperUltraLegendScene·StartMenu와 대전 맵을 포함해야 한다.

```powershell
unity command eval_file --project-path (Get-Location).Path --caller plugin --skill unity-cli --format json --file ((Get-Location).Path + '/Assets/SSW/Tests/Maps~/Checks.cs')
unity command eval_file --project-path (Get-Location).Path --caller plugin --skill unity-cli --format json --file ((Get-Location).Path + '/Assets/SSW/Tests/Maps~/Terrain.cs')
& './Assets/SSW/Tests/Maps~/Verify.ps1' -Run VerifyNew
```

Verify.ps1은 같은 PC의 에디터 호스트와 개발 빌드 접속자를 실제 UGS Relay 빠른 매칭으로 연결한다. 기본 빌드는 Builds/TerrainFix/Game.exe이며 -Build로 변경한다. 결과는 Logs/StartMenu25/<Run>에 저장하며 종료 시 방·접속자·플레이 모드를 정리한다. 이미 확인한 지형 검증을 생략하고 전체 경기만 확인하려면 -SkipTerrain을 사용한다.

2026-09-25 확인:
- 기존 이동 코드에서 흔들리는 바닥 관통을 재현했다. 수정 후 300회 연속 이동, 45도 경사 점프, 단방향 발판 통과·착지·내려가기, 위치 복원, 폭포 진입·이탈을 통과했다.
- RelayTerrain4: StartMenu에서 빠른 매칭과 전투 씬 진입, 양쪽 화산 바닥 착지·점프, 두 폭포의 상승과 이탈, VS·승리 닉네임 태그 제거를 확인했다.
- RelayFlow5: 최대 21라운드 4:3 경기에서 라운드마다 맵 변경, 최초 14개 중복 없음, 재순환 시 연속 중복 없음, 작은 라운드 카드 선택 생략, 세트 패자만 선택, 증강 유지·체력·스폰 초기화를 확인했다.

별도 PC 테스트는 하지 않았다. 네트워크 프로토콜 10과 같은 버전의 방 검색 키를 사용하므로 양쪽을 함께 갱신해야 한다. Results.json의 Catalog1·Flow2·Effects9는 규칙 변경 전의 과거 검증 기록이다.
