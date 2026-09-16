# 작은 맵 3개

## 다리

중앙 다리를 공격해 통로를 바꾸는 맵. 다리가 무너져도 아래쪽으로 이동할 수 있다. 다리 조각의 체력은 10이며 7초 후 복구된다. 플레이어와 겹치면 복구를 기다린다.

## 그네

작은 정사각형 44개가 줄에 매달린 가로형 점프맵. 기본 바닥 없이 정사각형 위에서 시작한다. 높낮이가 다른 아래 경로와 왼쪽, 중앙, 오른쪽의 오르막을 오가며 이동한다.

발판은 플레이어 너비에 가까운 1.3 × 1.3 크기다. 발판을 밟거나 걸으면 2D 물리로 흔들리고 회전한다. 위쪽 작은 고정점을 공격하면 해당 줄이 끊어지고 발판이 떨어진다. R을 누르거나 맵 아래로 추락하면 발판과 고정점이 복구된다.

## 바람

양쪽 바람을 타고 위층으로 올라가는 맵. 계단식 발판도 있어 바람이 꺼져 있을 때 이동할 수 있다. 각 버튼은 같은 쪽 바람을 켜고 끄며, 공격은 바람을 통과한다.

## 실행

메인 메뉴 → 샌드박스 → 연습장 / 다리 / 그네 / 바람. 선택 화면에는 맵 이름만 표시한다.

A/D 이동, Space 점프. 공격은 기존 마술사 조작인 마우스 좌클릭을 누른 뒤 놓기다. R은 위치와 맵 초기화, M은 기존 맵 선택 창이다. Esc 메뉴의 계속하기는 플레이를 재개하고 나가기는 메인 메뉴로 돌아간다.

Assets/SSW/Scenes의 Crate.unity, Swing.unity, Wind.unity를 열고 Play해도 된다. 그네에는 기본 바닥이나 보조 고정 발판이 없다. 줄은 LineRenderer, 연결과 회전은 DistanceJoint2D와 Rigidbody2D를 사용한다.

## 편집

그네는 Unity 씬을 열고 MCP로 실제 GameObject를 배치했다. Hierarchy의 Swing 아래 Path, Left, Middle, Right에서 각 Hang을 옮기면 그네 전체가 이동한다. Pin, Block, Rope를 각각 선택해 편집할 수도 있다. 줄 길이는 Block의 Distance Joint 2D에서 조절한다. 블럭은 독립된 SpriteRenderer와 BoxCollider2D이며 줄은 편집 중에도 두 연결점을 따라간다.

Swing 루트는 메뉴에서 사용하는 Assets/SSW/Maps/Swing.prefab과 연결된다. 씬에서 위치를 바꾼 뒤 Overrides → Apply All로 적용하면 메뉴 진입에도 같은 배치가 반영된다. 맵 생성 스크립트는 사용하지 않는다.

다리와 바람의 배치는 Assets/SSW/Maps의 Crate, Wind 프리팹에서 편집할 수 있다. 실제 플레이 검증 기록은 같은 폴더의 Checks.txt, JumpChecks.txt, RouteChecks.txt, SwingChecks.txt, MenuChecks.txt에 있다.

소개용 HUD나 안내 카드는 추가하지 않았다. 세 맵은 로컬 연습용이다.

시각 참고: [Landfall의 ROUNDS 공식 이미지](https://landfall.se/rounds-press-kit). 단순한 발판과 열린 이동 경로를 참고하고 배치는 새로 만들었다.
