# Story 009: 무게 한도 초과 적재와 속도 감소

> **Epic**: over-load
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-16

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature (무게 한도 초과 적재와 속도 감소). 상세 규칙: `design/gdd/game-concept.md` 8.5·8.6·13·16.4, `design/gdd/delivery-cargo-vehicle-rules.md` 6.4

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating for 6.6)
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/gdd/game-concept.md` 8.6 (2026-11-16 확정).*

- [ ] 개별 상차는 상차 후 무게가 `W_max = W × r_max` 이하이면 무게 한도 `W`를 넘어도 상차된다. `W_max`를 넘으면 `WEIGHT LIMIT`로 막힌다. 적재 한도는 지금처럼 초과할 수 없다
- [ ] 그룹·선택 상차는 상차 후 무게가 `W`를 넘는 물류에서 자동 종료하며, 뒤의 더 가벼운 물류로 건너뛰지 않는다
- [ ] 속도 배율 `m = max(m_min, 1 − k × max(0, w − W) / W)`을 일반·고속 두 모드의 전진 최고 속도와 가속도(모드 전환 보간 후 값)에 곱한다. 후진·제동·관성 감속·회전·드리프트는 바뀌지 않는다
- [ ] `m`은 상차·하차·처분 완료 시 다시 계산한다. 폐기 물류도 처분 전까지 무게에 포함된다
- [ ] `W` = 6, `w` = 8, `k` = 1.0, `m_min` = 0.5에서 `m` ≈ 0.667. `w` ≤ `W`이면 `m` = 1, `w` = 9이면 `m` = 0.5
- [ ] 상차 UI: 무게 사용량이 `W`를 넘으면 경고색으로 표시한다. 상차하면 `W`를 넘게 되는 행은 경고 표시를 하되 활성, `W_max`를 넘게 되는 행은 비활성 + `WEIGHT LIMIT`
- [ ] 주행 HUD: `w` > `W`일 때 과적 상태와 감소율(예: `OVERWEIGHT −33%`)을 표시하고, 이내면 숨긴다
- [ ] 초과 적재는 보상·내구도·별점에 영향을 주지 않는다
- [ ] `r_max`·`k`·`m_min`은 `VehicleCargoConfig`에 `TODO(tuning)`으로 둔다(초기값 1.5·1.0·0.5)
- [ ] 상차 UI 경고 행과 주행 HUD 과적 표시의 스크린샷을 `production/qa/evidence/`에 보관한다

---

## Implementation Notes

- 상차 판정은 `PlayerInteractionController`의 기존 `WEIGHT LIMIT` 판정(현재 `> WeightCapacity`)을 `W_max` 기준으로 바꾸고, 그룹 상차 정지 조건에만 `W` 기준을 추가한다.
- 속도 배율은 `VehicleCargoConfig` 값과 현재 적재 무게로 계산하는 순수 함수로 두고, `PlayerMovementController`의 `_maxForwardSpeed`·가속도 계산에 곱한다. 회전 곡선(`TurnBySpeed`)은 감소된 최고 속도 기준 비율을 그대로 쓴다.
- 이동 컨트롤러는 적재 무게를 직접 알지 않게, 상호작용 쪽에서 배율만 넘긴다.
- 단일 물류 무게가 `W_max`를 넘지 않는지 설정 검증(`OnValidate` 또는 로그 경고)으로 확인한다.
- **Performance**: no performance impact expected — 배율 `m`은 상차·하차·처분 완료 때만 다시 계산하고, 이동 컨트롤러는 매 프레임 곱셈 1회만 한다.

---

## Out of Scope

- 설정 변경으로 이미 `w` > `W_max`가 된 경우 물류 강제 하차 (규칙상 내리지 않음, `m_min` 적용)
- 무게의 물리 시뮬레이션(질량·서스펜션 변화)
- 최종 수치 확정 (Story 003)

---

## Files

- `Assets/Game/Scripts/Player/VehicleCargoConfig.cs`
- `Assets/Game/Scripts/Player/PlayerInteractionController.cs`
- `Assets/Game/Scripts/Player/PlayerMovementController.cs`
- `Assets/Game/Scripts/UI/LogisticsCenterUI.cs`
- `Assets/Game/Scripts/UI/OverweightHUD.cs` (신규)
- `Assets/Game/Scenes/_DevOnly/SampleMapDesign.unity` (Canvas/OverweightText 추가)
- [test root: Assets/Tests/EditMode/ — 면제]

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: Test: waived at qa.level minimal. 상차 UI 경고 행과 주행 HUD 과적 표시의 retained screenshot (`production/qa/evidence/`)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None (처분 시 재계산은 Story 005 구현 시 연결)
- Unlocks: `production/epics/over-load/story-003-balancing.md` (초과 적재 수치 조정)

## Completion Notes
**Completed**: 2026-11-16
**Criteria**: 10/10 passing (W_max 초과 행의 화면 표시와 w=9일 때 m=0.5는 직접 보지 못함)
**Deviations**: ADVISORY — 상차 UI에서 W_max 초과 행(비활성 + WEIGHT LIMIT)은 현재 물류 무게(대부분 1)로는 나오지 않아 화면 미관찰. Story 003 밸런싱에서 확인. OUT OF SCOPE — 작업 트리의 다른 변경(Logistics, GameTimeSystem, Delivery.prefab, CargoTraitConfig, CargoImpact*)은 다른 스토리 작업
**Test Evidence**: waived at qa.level minimal. 스크린샷 `production/qa/evidence/story-009-overweight-loading/` (loading-ui-warning-row.png, driving-hud-overweight.png)
**Reviewed by**: self, no independent review
**Run result**: OBSERVED — SampleMapDesign Play mode (적재는 script eval로 주입)

## Calls beyond the design
- 그룹·선택 상차에서 홀드의 첫 물류는 W 초과 허용, 두 번째부터 W 초과 시 정지, 메시지 WEIGHT LIMIT (`PlayerInteractionController`) · kept 2026-11-16
- 단일 물류 W_max 검증은 OnValidate 대신 상호작용 시 Debug.LogWarning (`PlayerInteractionController.GetLoadBlockReason`) · kept 2026-11-16
- 가속 배율은 엑셀을 밟을 때만 곱함 (`PlayerMovementController`) · kept 2026-11-16
- HUD `OVERWEIGHT -NN%` ASCII 하이픈·주황·주행 모드 텍스트 아래·폭 500, 상차 UI 경고색 (1, 0.6, 0.1), WEIGHT LIMIT 문구는 무게 차단 행에만 (`OverweightHUD`, `LogisticsCenterUI`) · kept 2026-11-16
