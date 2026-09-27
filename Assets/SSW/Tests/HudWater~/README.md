# 체력바와 맵 충돌 검증

Unity CLI로 `MapStart.unity`, `SuperUltraLegendScene.unity`를 포함한 Windows Development 빌드를 `Builds/HudWater28/Game.exe`에 만든다. 저장된 씬에서 재생을 종료한 뒤 프로젝트 루트에서 실행한다.

```powershell
& 'Assets/SSW/Tests/HudWater~/Peers.ps1' -Run Check1
```

동일 PC의 Editor 호스트와 Windows 클라이언트에 각각 60ms 지연과 10ms 지터를 건다. 결과와 스크린샷은 `Logs/HudWater28/Check1`에 저장한다. 실행 후 테스트 클라이언트를 종료하고 원래 씬과 직업 선택을 복원한다.

- `Corner.cs`: 실제 플레이어 캡슐과 이동 모터로 발판 각도, 모서리 접근 위치, 이동 방향을 조합한 135개 경우에서 공중 정지가 없는지 확인한다.
- `Run.cs`: 여섯 직업의 이동·피격 후 체력바 위치, 양쪽 소유 증강 개수, 상대 목록의 아래 배치, 많은 증강의 스크롤 영역을 검사한다.
- 8·12번 맵의 수위 타이머를 완료 시점으로 이동하고 빈 수면 구간에 양쪽 플레이어를 배치한다. 맵 충돌체와 피해 설정은 유지한다. 수면을 통과한 뒤 잠깐 움직임을 고정하여 수중 피해와 복제를 확인한다.
- 16·17번 맵의 긴 로프 발판에서 양쪽 소유자가 모서리를 벗어나고, 위에 착지하고, 걷고, 점프하는지 확인한다. 다른 플레이어만 고정하고 발판 물리는 유지한다.
- 실제 사망에 따른 라운드 교체 후 증강과 체력바가 복원되는지 확인한다.

공통 이동 회귀 검사는 기존 `Move~/JumpChecks.cs`, `Move~/Placement.cs`를 Unity CLI로 실행한다. 별도 두 PC의 UGS Relay 접속 검증은 이 테스트에 포함되지 않는다.
