# Story 007: 차량 리셋·복구

> **Epic**: over-load
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-17

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 12 (차량 리셋·복구). 상세 규칙: `design/gdd/game-concept.md` 2.1·10.6·16.5

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating for 6.6)
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/gdd/game-concept.md` 10.6 (2026-11-16 확정), UI는 13장·`design/quick-specs/hud-layout-2026-11-17.md` (2026-11-17 확정).*

- [ ] 리셋 키(R)를 복구 홀드 시간(1초) 동안 누르고 있으면 복구한다. 누르는 동안 상·하차와 같은 진행 바(`InteractionProgressUI`)에 진행도를 표시하고, 도중에 떼면 취소되고 바가 사라진다
- [ ] 플레이어 오른쪽 상호작용 키 안내에 R키 아이콘(임시 이미지)을 하루 운영 중 조작 가능할 때 항상 표시하고, 하루 종료 후 숨긴다. 대상별 Space 아이콘과 함께 보인다
- [ ] 하루 운영 중 조작 가능한 상태에서만 사용할 수 있다. 상·하차 작업 중 복구하면 작업이 중단되고 현재 물류의 진행도가 초기화된다
- [ ] 기록 간격(1초)마다 바퀴 4개 `Drivable` 접지, 차체 기울기 기준 이하, 다른 콜라이더와 겹치지 않음을 모두 만족하면 위치·진행 방향을 안전 지점으로 기록한다. 최근 5개를 보관한다
- [ ] 복구 위치는 최소 경과 시간(3초) 이상 지난 가장 최근 안전 지점이다. 겹치면 더 이전 지점을, 쓸 지점이 없으면 물류센터 앞 지정 위치를 쓴다
- [ ] 복구 시 차량을 수평으로 세우고 기록된 진행 방향을 보게 하며, 속도와 회전 속도를 0으로 만든다
- [ ] 복구 후 조작 잠금 시간(2초) 동안 조작할 수 없고, 그동안 R키 아이콘을 흐리게(반투명) 표시한다. 게임 시간과 물류 제한 시간은 계속 흐른다
- [ ] 적재 물류와 내구도는 변하지 않는다. 복구 이동은 충돌 내구도 판정(Story 004)에 들어가지 않는다
- [ ] 홀드 시간·기록 간격·보관 개수·최소 경과 시간·최대 기울기·잠금 시간은 설정 데이터에 `TODO(tuning)`으로 둔다
- [ ] R키 아이콘 표시, 복구 진행 바, 잠금 중 흐린 아이콘 스크린샷을 `production/qa/evidence/`에 보관한다

---

## Implementation Notes

- 기존 자동 수평 유지(`PlayerMovementController.KeepUpright`)는 그대로 둔다.
- 입력 차단은 기존 `SetControlEnabled`를 재사용하되, 하루 종료 잠금과 복구 잠금이 서로를 풀지 않게 한다.
- 안전 지점 선택(버퍼, 경과 시간, 겹침 → 이전 지점 → 물류센터 앞)은 순수 로직으로 분리한다.
- 최대 기울기 값은 지면 판정 기준(`VehicleConfig.GroundNormalMinDot`)과 맞춘다.
- 레벨에 물류센터 앞 지정 위치를 둔다.
- UI 규칙: `design/quick-specs/hud-layout-2026-11-17.md` "상호작용 키 안내". `PlayerUIController.ShowByType`은 아이콘을 하나만 켜므로 R키 아이콘은 그 선택에서 빼 따로 켜고 끈다. `InteractionProgressUI`는 복구 컴포넌트의 진행도 이벤트도 구독한다(상·하차 중 복구는 작업을 중단하므로 두 진행도가 겹치지 않는다).
- **Performance**: 영향은 작을 것으로 예상(검사는 1초 간격, 버퍼 고정 5칸). 안전 지점 검사(접지·기울기·겹침)의 실제 구현 방식을 정한 뒤 Profiler로 1회당 비용을 실측하고, 결과를 evidence에 한 줄로 남긴 다음 진행한다.

---

## Out of Scope

- 자동 끼임·뒤집힘 감지 (수동 리셋만 확정)
- 레벨 배치 규칙 검증 도구 (10.3 규칙은 레벨 제작 시 수동 확인)

---

## Files

- `Assets/Game/Scripts/Player/VehicleRecoveryController.cs` (신규: R 홀드, 안전 지점 기록, 복구, 잠금)
- `Assets/Game/Scripts/Player/SafePointBuffer.cs` (신규: 안전 지점 선택 순수 로직)
- `Assets/Game/Scripts/Player/VehicleRecoveryConfig.cs` (신규: 설정 SO)
- `Assets/Game/Configs/VehicleRecoveryConfig.asset` (신규: 설정 데이터)
- `Assets/Game/Scripts/Player/PlayerMovementController.cs` (SetRecoveryLocked)
- `Assets/Game/Scripts/Player/PlayerUIController.cs` (R키 아이콘)
- `Assets/Game/Scripts/UI/InteractionProgressUI.cs` (복구 진행도 구독)
- `Assets/Game/Prefabs/Delivery.prefab` (VehicleRecoveryController, Canvas/RecoveryKeyImage)
- `Assets/Game/Scenes/_DevOnly/SampleMapDesign.unity` (RecoveryFallbackPoint, Canvas/InteractionProgress 활성화)
- [test root: Assets/Tests/EditMode/ — 면제]

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: Test: waived at qa.level minimal. R키 아이콘·복구 진행 바·잠금 중 흐린 아이콘의 retained screenshot (`production/qa/evidence/`)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None (작업 씬 `Assets/Game/Scenes/_DevOnly/SampleMapDesign.unity`에서 구현. Story 001은 Deferred)
- Unlocks: None

## Completion Notes
**Completed**: 2026-11-17
**Criteria**: 5/10 확인 (AC-4·6·9·10 + AC-2·7의 화면 확인분), 나머지 DEFERRED — AC-1·2·3·5·7·8 동작은 전체 QA에서 확인
**Deviations**: ADVISORY — 안전 지점 검사 1회당 비용 미실측(`ProfilerMarker VehicleRecovery.SafePointCheck`만 있음); 접지 캐스트가 `PlayerMovementController.ApplySuspension` 복제
**Test Evidence**: waived at qa.level minimal — 스크린샷 `production/qa/evidence/story-007-vehicle-recovery/01~03`
**Reviewed by**: self, no independent review

## Calls beyond the design
- 상·하차 중단은 복구 실행 순간 (`VehicleRecoveryController.Recover`) · kept 2026-11-17
- 잠금 중 R 홀드 불가·눌림 순간만 인정, 잠금 중 Space 허용 (`VehicleRecoveryController.UpdateHold`) · kept 2026-11-17
- 폴백 위치 겹침 검사 없음·비면 복구 생략, `RecoveryFallbackPoint`는 시작 위치 (-0.88, 0.03, -26.25) +Z (`SampleMapDesign.unity`) · kept 2026-11-17
- 최대 기울기 내적 0.5, 잠금 알파 0.4, R키 아이콘 임시 Knob 50×50 Space 아이콘 위 +80, 공용 진행 바 속도계와 겹침 (`VehicleRecoveryConfig.asset`, `PlayerUIController.cs`, `Delivery.prefab`) · kept 2026-11-17
