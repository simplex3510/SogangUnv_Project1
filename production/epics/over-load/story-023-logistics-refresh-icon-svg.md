# Story 023: 물류센터 행 ↻ 기호를 SVG 아이콘으로 교체

> **Epic**: over-load
> **Status**: Complete
> **Layer**: Presentation
> **Type**: UI
> **Estimate**: S
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-18

## Context

**GDD**: `design/gdd/game-concept.md` 10.6장
**Requirement**: UI 백로그 UI-03 (`production/ui-backlog.md`). 임시 아이콘은 Bootstrap Icons SVG를 쓴다.

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating for 6.6)
**Engine Notes**: UI Toolkit. 아이콘은 USS `background-image`·`-unity-background-image-tint-color`로 그린다(Story 022와 같은 방식).

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

- [ ] 물류센터 창의 제한 시간 행에서 남은 시간 앞에 `↻` 글자 대신 SVG 아이콘이 보인다
- [ ] 아이콘 크기·색이 같은 행의 시간 글자와 어울리고, 글자와 세로 중앙이 맞는다
- [ ] 코드와 UXML·USS에 `↻` 글리프가 남아 있지 않다
- [ ] No regression: 물류센터 창의 행 배치, 제한 시간 표시, 홀드 상차
- [ ] 아이콘이 보이는 물류센터 창 스크린샷을 `production/qa/evidence/story-023-logistics-refresh-icon-svg/`에 보관한다

---

## Implementation Notes

- 현재 위치: `Assets/Game/Scripts/UI/LogisticsCenterUI.cs` 442행 근처, `↻{분}:{초}` 문자열.
- Bootstrap Icons(MIT) `arrow-clockwise`를 `Assets/Game/UI/Icons/`에 추가한다.
- 시간 글자는 Label 그대로 두고, 그 앞에 아이콘 `VisualElement`를 두는 방식이 가장 작다. 아이콘 크기·색은 USS `TODO(tuning)`.

---

## Out of Scope

- 물류센터 행을 UXML 템플릿으로 분리하는 일 (Story 024)

---

## Files

- `Assets/Game/Scripts/UI/LogisticsCenterUI.cs`
- `Assets/Game/UI/LogisticsCenter/LogisticsCenterWindow.uss`
- `Assets/Game/UI/Icons/arrow-clockwise.svg` (신규, .meta 포함)

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: UI
**Required evidence**: 아이콘이 보이는 물류센터 창의 retained screenshot (`production/qa/evidence/story-023-logistics-refresh-icon-svg/`). 테스트는 `qa.level: minimal`에서 생략

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None
- Unlocks: `production/epics/over-load/story-024-uxml-element-templates.md` (같은 `LogisticsCenterUI.cs`, 먼저 닫는다)

## Completion Notes
**Completed**: 2026-11-18
**Criteria**: 4/5 passing (홀드 상차 회귀는 DEFERRED — 전체 QA에서 확인)
**Deviations**: None
**Test Evidence**: `production/qa/evidence/story-023-logistics-refresh-icon-svg/01-fresh-row-icon.png` (테스트는 qa.level minimal에서 생략)
**Reviewed by**: self, no independent review

## Calls beyond the design
- 아이콘 13px·글자색 tint (`Assets/Game/UI/LogisticsCenter/LogisticsCenterWindow.uss` `.lc-deadline-icon`) · kept 2026-11-18
- 아이콘-글자 간격 3px (`.lc-deadline-time--icon`) · kept 2026-11-18
- 아이콘은 Fresh 행에만 표시 (`Assets/Game/Scripts/UI/LogisticsCenterUI.cs`) · kept 2026-11-18
