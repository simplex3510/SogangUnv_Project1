# Story 005: 폐기 물류 전환과 처분 장소

> **Epic**: over-load
> **Status**: Complete
> **Layer**: Feature
> **Type**: Integration
> **Estimate**: L
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-17

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 11 (폐기 물류와 처분 장소). 상세 규칙: `design/gdd/game-concept.md` 11.6

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating for 6.6)
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/gdd/game-concept.md` 11.6 (2026-11-16 확정), 2026-11-16 사용자 수정 반영(배송지를 처분 장소로 재설정).*

- [ ] 현재 내구도가 0이 되면(Story 004) 물류 상태가 폐기(`Spoiled`)로 바뀐다
- [ ] 전환 즉시 배송 실패가 확정되고 배송 실패 페널티 `P × M_type`이 매겨진다. 정산에는 실패로 집계하고 시간 초과 손실은 적용하지 않는다
- [ ] 폐기 물류의 배송지는 처분 장소로 재설정된다. 상차 UI·적재 UI에서 처분 장소 그룹으로 묶여 표시되고, 배송지 물류 아이콘과 나침반도 처분 장소를 목적지로 표시한다
- [ ] 원래 배송지의 하차 대상·아이콘·나침반에서는 폐기 물류가 빠진다
- [ ] 폐기 물류는 처분할 때까지 적재 한도와 무게 한도를 그대로 차지한다
- [ ] 하차 진행 중에 폐기로 전환되면 그 물류의 하차를 중단하고 진행도를 초기화한다
- [ ] 처분 장소에서 상호작용 키를 누르고 있으면 폐기 물류를 1개씩 처분한다. 1개당 처분 소요시간은 설정값이고, 처분된 물류만큼 적재·무게 한도가 회복된다. 보상·비용은 없다
- [ ] 폐기 물류가 없을 때 처분 장소에서 상호작용 키를 누르면 "처분할 물류 없음"을 표시한다
- [ ] 영업 종료 시 이미 폐기된 물류에 페널티를 다시 매기지 않고, 차량에 남은 폐기 물류는 정리한다. 별점(Story 002)에서 폐기 물류는 실패로 센다
- [ ] 처분 장소를 작업 씬 `Assets/Game/Scenes/_DevOnly/SampleMapDesign.unity`에 1곳 배치한다(물류센터에서 떨어진 곳)

---

## Implementation Notes

- 처분 장소는 기존 상호작용 기반(`InteractableObject`)과 배송지 구조를 재사용한다. 처분은 처분 장소로 "하차"하는 흐름으로 볼 수 있다.
- 목적지 재설정 규칙은 `game-concept.md` 11.6 규칙 3(2026-11-16 수정)을 따른다. 처분 장소는 배송지 할당·일일 할당 한도의 대상이 아니다. 나침반에서 처분 장소 화살표의 배치(배송지 다음)는 제안이므로 구현 시 확인한다.
- Fresh 배송 유형 도입 시 같은 폐기 규칙을 쓴다(`design/quick-specs/fresh-delivery-type-addition-2026-11-16.md`).
- 처분 소요시간은 설정 데이터에 `TODO(tuning)`으로 둔다.

---

## Out of Scope

- Story 004: 내구도 감소 계산
- Story 006: 폐기 알림 UI
- Fresh 배송 유형(신선도 0 폐기)

---

## Files

- `Assets/Game/Scripts/Logistics/DisposalSite.cs`
- `Assets/Game/Scripts/Logistics/DisposalConfig.cs`
- `Assets/Game/Scripts/UI/DisposalSiteParcelUI.cs`
- `Assets/Game/Scripts/Logistics/Parcel.cs`
- `Assets/Game/Scripts/Logistics/ParcelStatus.cs`
- `Assets/Game/Scripts/Logistics/DeliveryTarget.cs`
- `Assets/Game/Scripts/Logistics/DeliveryDeadlineSystem.cs`
- `Assets/Game/Scripts/Logistics/SettlementSystem.cs`
- `Assets/Game/Scripts/Player/PlayerInteractionController.cs`
- `Assets/Game/Scripts/Systems/DailyOperationSystem.cs`
- `Assets/Game/Scripts/UI/DeliveryTargetParcelUI.cs`
- `Assets/Game/Scripts/UI/DeliveryNavigationCompass.cs`
- `Assets/Game/Scripts/UI/LogisticsCenterUI.cs`
- `Assets/Game/Configs/DisposalConfig.asset`
- `Assets/Game/Prefabs/DisposalSite.prefab`
- `Assets/Game/Scenes/_DevOnly/SampleMapDesign.unity`

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: Test: waived at qa.level minimal. Play mode에서 폐기 전환 → 처분 장소 이동 → 처분까지 로그와 스크린샷으로 확인

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/over-load/story-004-collision-durability.md` (작업 씬 `Assets/Game/Scenes/_DevOnly/SampleMapDesign.unity`에서 구현. Story 001은 Deferred)
- Unlocks: `production/epics/over-load/story-006-durability-feedback-ui.md`

---

## Completion Notes
**Completed**: 2026-11-17
**Criteria**: 7/10 passing (DEFERRED: 적재 UI 처분 그룹 행(AC3 일부), AC4 원래 배송지 제외, AC6 하차 중 폐기 중단, AC9 영업 종료 정리 — UI·HUD 스토리 또는 플레이 때 확인)
**Deviations**: None (참고: `GameTimeSystem.cs`의 기존 미커밋 변경은 이 스토리 범위 밖)
**Test Evidence**: waived at qa.level minimal. 스크린샷 `production/qa/evidence/story-005-spoiled-cargo-disposal/` (01~03)
**Reviewed by**: self, no independent review

## Calls beyond the design
- 마지막 남은 물류가 폐기되면 "모든 배송 완료"로 하루 즉시 종료 (`DailyOperationSystem.cs`) · kept 2026-11-17
- 처분 장소 외형은 배송지 건물 재사용, 위치 (-12,0,-5), 나침반 처분 화살표는 배송지 다음·주황색. 건물 mesh는 사용자가 추후 임시로 변경해 구분 예정 · kept 2026-11-17
- 메시지 "NO PARCEL TO DISPOSE"(영문), 처분 1개 1초(`DisposalConfig`, TODO tuning), 적재 UI 처분 행(목록 끝 비활성 버튼) · kept 2026-11-17
- 페널티는 `SettlementSystem.FinalizeDay`에서 집계, 처분이 끝난 물류는 `Failed` 상태 · kept 2026-11-17
