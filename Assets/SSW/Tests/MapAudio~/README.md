맵 사운드 검증

Unity CLI로 아래 씬을 순서대로 포함한 Windows Development 빌드를 만든다.

- Assets/SSW/Tests/MapStart.unity
- Assets/SSW/SuperUltraLegendScene.unity

출력 경로는 Builds/MapSound27/Game.exe다. 프로젝트 루트에서 `./Assets/SSW/Tests/MapAudio~/Peers.ps1 -Run 실행이름`을 실행한다. 저장된 씬에서 Play를 끈 상태여야 한다. 종료 시 기존 씬과 선택 직업을 복원한다.

동일 PC의 Editor 호스트와 Windows 접속자에 각각 60ms 지연과 10ms 지터를 적용한다. 실제 전투 씬과 등록된 맵을 사용하며, 캐릭터를 안정적으로 배치할 테스트 발판을 추가한다. 브레스와 시계는 복제되는 시작 시각을 조정하여 주기를 앞당긴다. 화산은 실제 파티클 생성과 원본 자동 방출을 검사한다. 벚꽃은 파티클을 캐릭터에 충돌시키며, 경계는 실제 Collider 접촉과 반동을 검사한다.

양쪽 SoundProbe의 큐별 정확한 재생 횟수와 오디오 출력 샘플을 확인한다. 같은 파티클의 반복 전송, 벚꽃 동시 충돌, 경계의 지속 접촉과 재진입, 모서리 충돌, 비전투 상태도 검사한다. 결과는 Logs/MapSound27/실행이름/Result.json에 저장된다. 물리적인 두 PC의 UGS Relay 검증은 별도다.
