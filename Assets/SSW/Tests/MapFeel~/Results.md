# 맵 연출·카메라·사쿠라 반영

2026-09-25. Base 35a9404에서 작업. 다른 팀원 폴더는 수정하지 않았다.

## 변경

- 대전 및 SSW 맵 씬 카메라를 직교 크기 9로 고정. 맵 중심과 좌우 시점은 유지한다.
- Map04 상승 파티클의 자동 반복을 끄고 실제 돌풍 시점에 재생한다. 서버와 접속자는 MotionFrame의 Pulse 번호로 같은 입력 틱에 힘을 적용한다. 재실행·순간이동에서 중복 적용을 막는다.
- Map17 배경색 변화는 시간 배율 변화가 시작하고 끝날 때 실행한다.
- Map02 폭포는 상승 속도 22, 가속도 130. Map15는 26, 240.
- 사쿠라 Map13이 예전 Assets/MapPrefab 복사본 대신 Assets/KDH/GameModules/Maps/KDH_Map 13.prefab을 직접 참조한다.
- SakuraField가 서버에서 가속 판정을 하고 양쪽에 이펙트를 재생한다. 꽃잎의 난수와 시작 시간도 공유한다.
- SakuraFx는 팀원의 원본 효과를 사용하는 변형 프리팹이다. 전역 풀 콜백은 제거하고 재생 종료·맵 교체 시 정리한다.
- 통신 형식 변경으로 Protocol 11을 사용한다. 양쪽 PC를 같은 새 빌드로 실행해야 한다.

## 검증

Unity 6000.5.2f1, Unity CLI 사용. 같은 PC의 에디터 호스트와 개발 빌드 접속자를 실제 UGS Relay로 연결했다. 별도 두 PC의 Wi-Fi 환경은 이번 검증에 포함되지 않았다.

- CheckAssets.cs: 497개 검사 통과. 14개 대전 맵, 사쿠라 의존성·충돌 설정·효과 정리, 8개 맵 씬 카메라 포함.
- SceneMatch~/Checks.cs: 763개 검사 통과. 14개 맵을 4개 화면 비율·양쪽 시점에서 크기 9 및 고정 위치로 확인.
- Water.cs: 실제 프리팹·충돌체·MotionMotor로 14개 이동 경로 실행. Map02는 측면 이동 시 y=7.85까지 올라가 상단 발판에 착지. Map15는 최고 y가 -2.18에서 5.53으로 증가.
- Verify.ps1 Relay3: 50개 검사 통과.
  - 카드 선택 중 상승 파티클 없음.
  - 양쪽에서 연속 3회 상승 효과와 실제 상승이 같은 프레임에 발생.
  - 접속자 상승 보정 최대값은 재현 당시 8.457에서 수정 후 0.
  - 추가 전송 지연 80ms, 지터 20ms, 패킷 손실 2% 설정에서도 상승 보정 최대값 0.
  - 두 폭포의 상단 높이 도달, 시간 배율 복원, 사쿠라 실제 파티클 충돌에 따른 양쪽 효과 및 속도 7→8.4→7 확인.
  - 사쿠라 재방문, 정상 라운드 교체, 스폰·카메라·이전 투사체 정리 확인.
- 개발 빌드 build_c224b51e0c02 성공: 오류 0, 경고 12.

강제 테스트 순간이동 직후 Map15의 접속자 위치 보정은 최대 0.508이었다. 모든 네트워크 상황에서 보정이 없다는 의미는 아니다. 펄스 표시가 누락된 연속 입력은 0.5초 뒤 서버가 적용하며, 입력 패킷 자체가 완전히 끊긴 기존 StepRemote 정지 동작은 이번 수정 범위 밖이다.

## 실행

프로젝트 루트에서 연결된 Unity 에디터에 다음을 실행한다.

~~~powershell
unity command eval_file --file "D:/unity_project/Mushrooms/Assets/SSW/Tests/MapFeel~/CheckAssets.cs" --caller plugin --skill unity-cli --project-path "D:/unity_project/Mushrooms" --format json
unity command eval_file --file "D:/unity_project/Mushrooms/Assets/SSW/Tests/MapFeel~/Water.cs" --caller plugin --skill unity-cli --project-path "D:/unity_project/Mushrooms" --format json
& "Assets/SSW/Tests/MapFeel~/Verify.ps1" -Run "새로운실행명" -Build "Builds/MapFeel/Game.exe"
~~~

원본 증거는 Logs/MapFeel25/AssetsChecks.json, Camera.json, Water.json, Relay3/Checks.txt, Relay3/Gust.json, Relay3/*.trace.*.json, Build3.json에 있다. Relay1은 테스트 배치 좌표 오류, Relay2는 수정 전 접속자 상승 튐 재현 기록이다.
플레이용 빌드 build_69c02529f6e1 성공: 오류 0, 경고 10. 출력 경로 Builds/Play/Game.exe.

최종 통합에서 Base 53fbedb의 칼 공격 변경을 보존했다. 재컴파일과 Packets.cs의 BoltSpec·MotionFrame·MotionState 직렬화 왕복 3건이 통과했다. 위 Relay3 수치는 해당 팀원 변경을 합치기 전 맵 수정 빌드의 기록이다.
