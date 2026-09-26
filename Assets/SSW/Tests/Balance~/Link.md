# 플레이어 연결

`439db0367f1af2373db97513072fe8aad195b88e`를 정상 병합한 뒤 Unity CLI `GunLink.cs`로 적용했다.

- 대상: `Assets/SSW/Resources/Network/Player.prefab`, GunCast fileID `3167200579317346795`.
- `_balance`: `Assets/SSW/Resources/Network/GunBalance.asset`, GUID `59aba008e14f56b48bb6206f9b17fcc3`, fileID `11400000`.
- 이전 로컬 숫자 5개는 최종 코드의 설정 참조로 교체했다. 초기 변경은 `Logs/Integration/GunCastInitial.patch`에 보관했으며 별도 중복 적용하지 않았다.
- 프리팹의 최종 diff는 `_balance` 한 줄이다. 기존 Player Physics, 문어의 심장 HP 3, 총구·이펙트·다른 참조는 유지한다.
- `Logs/Stats/GunLink.json`: CLI 성공, 컴포넌트 ID 확인. 이 기록은 실제 명중·힘·감속 검증 완료를 의미하지 않는다. 해당 검증은 총잡이 효과 담당이 이어받는다.

Base `7026689760d418522eaa94c19f9710a57175032e`의 ID85 빈 슬롯·기본 주머니 동작을 포함하며, 진행 중인 맵 변경은 이 연결 커밋에 포함하지 않는다.
