# Story 008: Fresh 배송 유형

> **Epic**: over-load
> **Status**: Complete
> **Layer**: Feature
> **Type**: Integration
> **Estimate**: L
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-17

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 13 (Fresh 배송 유형). 상세 규칙: `design/gdd/game-concept.md` 6.2·7.1~7.4·11.1·11.6·12.2·12.4·13장·14.2, `design/quick-specs/fresh-delivery-type-addition-2026-11-16.md`

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating for 6.6)
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/gdd/game-concept.md` 1.8 (2026-11-16 확정).*

- [ ] `DeliveryType`에 Fresh를 추가하고, `DeliveryTypeConfig`에 생성 확률·신선도 계수·감소 시작 방식(상차 완료 시점)·실패 페널티 계수를 둔다. 정적·동적 생성 모두 같은 확률을 쓰고 생성 로그에 `Fresh`로 나온다
- [ ] 신선도 총량 `T_fresh`는 생성 시 계산해 저장하며, 같은 거리의 Express 제한 시간보다 짧다
- [ ] 물류센터에서 대기하는 동안 신선도가 줄지 않는다(게임 시간 경과 전후로 같은 값). 물류마다 상차가 완료된 순간부터 게임 시간 기준으로 줄어든다
- [ ] 신선도가 0이 되면 폐기 물류로 전환된다(Story 005 공통 규칙). 실패 페널티는 Fresh 실패 계수를 쓰고 한 번만 기록되며, 시간 초과 손실은 없다
- [ ] 폐기 전 배송 완료 시 신선도 보상 `B_fresh × clamp(남은 신선도 ÷ 총량, 0, 1)`을 하차 완료 시점에 지급한다. 50% 남았을 때 `B_fresh × 0.5`(±1)다
- [ ] 처리 순서는 상차 Fresh(남은 신선도 짧은 순) → Express → 대기 Fresh(총량 짧은 순) → Normal이며, 상차 UI·하차·나침반에 같은 순서를 적용한다
- [ ] 정산 상세 내역에 신선도 보상 합계를 표시한다. 별점(Story 002)에서 Fresh는 폐기 전 배송 완료 시 성공으로 센다
- [ ] UI: `FRS` 태그, 물류센터에서는 신선도 총량 표시와 가득 찬 바 고정, 상차 후 배송지 아이콘·적재 UI에서 신선도 바 감소. 각 화면의 스크린샷을 `production/qa/evidence/`에 보관한다
- [ ] 생성 확률·`M_fresh`·`B_fresh`·실패 계수는 설정 데이터에 `TODO(tuning)`으로 둔다(제안: 0.15, Express 계수 × 0.5, Express 시간 보상 × 2, Express와 같음)
- [ ] 회귀 없음: Normal·Express의 생성, 제한 시간, 보상, 정산 결과가 기존과 같다

---

## Implementation Notes

- 처리 순서 비교는 기존 공통 비교(`Parcel.CompareProcessingOrder`)를 확장한다.
- 신선도 보상은 생성 시점 고정 원칙의 예외다. 최대 보상과 총량은 생성 시점 값을 쓰고, 남은 비율만 하차 시점에 계산한다(14.2).
- `DeliveryDeadlineConfig._baseSeconds`가 과대값이라 그대로 두면 Fresh도 만료되지 않는다. 검증 시 임시로 낮추고, 최종값은 Story 003에서 정한다.

---

## Out of Scope

- Story 005: 폐기 전환·처분 장소 자체
- Story 003: 수치 최종 조정

---

## Files

- `Assets/Game/Scripts/Logistics/DeadlineStart.cs` (신규)
- `Assets/Game/Scripts/Logistics/DeliveryType.cs`
- `Assets/Game/Scripts/Logistics/DeliveryTypeConfig.cs`
- `Assets/Game/Scripts/Logistics/Parcel.cs`
- `Assets/Game/Scripts/Logistics/DeliveryDeadlineSystem.cs`
- `Assets/Game/Scripts/Logistics/DeliveryGenerationSystem.cs`
- `Assets/Game/Scripts/Logistics/RewardSystem.cs`
- `Assets/Game/Scripts/Logistics/DeliveryRewardConfig.cs`
- `Assets/Game/Scripts/Logistics/SettlementSystem.cs`
- `Assets/Game/Scripts/Player/PlayerInteractionController.cs`
- `Assets/Game/Scripts/UI/LogisticsCenterUI.cs`
- `Assets/Game/Scripts/UI/DeliveryTargetParcelUI.cs`
- `Assets/Game/Scripts/UI/SettlementUI.cs`
- `Assets/Game/Scripts/UI/DurabilityHud.cs` (범위 확장, 사용자 승인)
- `Assets/Game/UI/Hud/DurabilityHud.uss`
- `Assets/Game/Configs/DeliveryTypeConfig.asset` (Fresh 항목, Editor로 추가)
- [test root: Assets/Tests/EditMode/ — 면제]

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: Test: waived at qa.level minimal. UI 화면(물류센터 행, 상차 후 신선도 바, 정산 신선도 보상)의 retained screenshot

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/over-load/story-005-spoiled-cargo-disposal.md`, `production/epics/over-load/story-002-daily-star-rating.md`
- Unlocks: `production/epics/over-load/story-003-balancing.md` (수치 조정)

---

## Completion Notes
**Completed**: 2026-11-17
**Criteria**: 10/10 확인. AC-5는 비율 0.4695에서 46.95로 확인했고 정확히 50% 지점은 못 맞췄다. AC-6은 상차 UI·하차 순서만 확인했고 나침반 순서는 보지 않았다.
**Deviations**: `DeliveryRewardConfig.asset`이 `## Files`에 없다(ADVISORY). 표시 문제 2건: 물류센터 행 `↻`가 맑은 고딕에 없어 대체 글자로 보임(SVG로 교체 예정), 배송지 아이콘(Canvas)에서 Fresh가 파랑(UI Toolkit 전환 때 처리). 검증은 Fresh 생성·차량 이동·상차를 스크립트로 진행했다.
**Test Evidence**: 테스트는 qa.level minimal이라 면제. 스크린샷 `production/qa/evidence/story-008-fresh-delivery-type/` 01~04, stamp 일치
**Reviewed by**: self, no independent review

## Calls beyond the design
- Fresh 생성 확률 0.15는 상대 가중치라 실제 약 13% (`DeliveryTypeConfig.asset`) · kept 2026-11-17
- B_fresh 100(`DeliveryRewardConfig.asset`), M_fresh 0.35(`DeliveryTypeConfig.asset`) · kept 2026-11-17
- 신선도 바 모양과 Fresh 초록 아이콘(임시) · kept 2026-11-17
- 신선도 감소는 `Parcel.LoadCompletionTime`에 상차 시각을 저장해 계산 · kept 2026-11-17
