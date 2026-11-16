# Story 015: 일차·시각 HUD 좌상단

> **Epic**: over-load
> **Status**: Complete
> **Layer**: Presentation
> **Type**: UI
> **Estimate**: S
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-17

## Context

**GDD**: `design/gdd/game-concept.md` 3.3
**Requirement**: 상세 규칙: `design/quick-specs/hud-layout-2026-11-17.md` (화면 영역, 일차)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating for 6.6)
**Engine Notes**: UI Toolkit, Story 006·013의 `HudPanelSettings`와 한글 FontAsset 재사용.

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/quick-specs/hud-layout-2026-11-17.md` (2026-11-17 확정).*

- [ ] 일차 값이 있고 1부터 시작한다. 하루 동안 바뀌지 않는다
- [ ] 화면 좌상단에 시각 `HH:mm`을 위 줄에 크게, 그 아래 줄에 `DAY N`을 작게 표시한다(예: `09:30` / `DAY 1`). 시각은 게임 1분 단위로 바뀐다(초 버림)
- [ ] 정산 화면이 열리면 숨긴다
- [ ] 기존 TMP `GameTimeHUD` 표시가 화면에 중복되지 않는다
- [ ] GUI는 임시다. 주행 중·정산 중 스크린샷을 `production/qa/evidence/`에 보관한다

---

## Implementation Notes

- 일차는 `DailyOperationSystem`에 읽기 전용 값으로 둔다. 여러 날 진행이 없으므로 지금은 1 고정이어도 된다.
- 시각 문자열은 기존 `GameTimeSystem.CurrentTimeText`를 쓴다.
- 좌상단 HUD는 Story 016(오늘 수익)이 같은 블록 3행에 붙는다. UXML 블록을 그 구조로 만든다.
- UI는 게임 상태를 바꾸지 않는다.

---

## Out of Scope

- 여러 날 진행과 일차 증가
- 최종 GUI 디자인 (첫 릴리즈 전 임시 단계)

---

## Files

- `Assets/Game/Scripts/Systems/DailyOperationSystem.cs` (CurrentDay 추가)
- `Assets/Game/UI/Hud/StatusHud.cs`
- `Assets/Game/UI/Hud/StatusHud.uxml`
- `Assets/Game/UI/Hud/StatusHud.uss`
- `Assets/Game/Scenes/_DevOnly/SampleMapDesign.unity` (StatusHud 오브젝트 추가, Canvas/Timer 제거)
- [test root: 면제(qa.level minimal, UI 스토리 — 스크린샷 증거)]

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: UI
**Required evidence**: 주행 중 좌상단 HUD, 정산 화면에서 숨김 retained screenshot (`production/qa/evidence/`)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None
- Unlocks: `production/epics/over-load/story-016-today-income-hud.md`

## Completion Notes
**Completed**: 2026-11-17
**Criteria**: 5/5 passing (deferred: none)
**Deviations**: None
**Test Evidence**: `production/qa/evidence/story-015-day-clock-hud/` (01-driving-day-clock.png, 02-settlement-hidden.png); 테스트는 qa.level minimal에서 면제
**Reviewed by**: self, no independent review

## Calls beyond the design
- 시각 40px 굵게·일차 18px (`StatusHud.uss`, `TODO(tuning)`) · kept 2026-11-17
- 시각 위·일차 아래 2줄 배치 (사용자 요청, 인수 조건과 hud-layout 명세 반영) · kept 2026-11-17
- `CurrentDay`는 `StartDay()`가 아니라 초기값 1인 프로퍼티 (`DailyOperationSystem.cs`) · kept 2026-11-17
- 블록 패딩 10px 18px, 테두리 3px, 모서리 18px (`StatusHud.uss`) · kept 2026-11-17
- 정산 뒤에는 HUD를 다시 보이지 않음 (`StatusHud.cs`) · kept 2026-11-17
- `SettlementSystem`이 없으면 숨김 없이 계속 표시 (`StatusHud.cs`) · kept 2026-11-17
