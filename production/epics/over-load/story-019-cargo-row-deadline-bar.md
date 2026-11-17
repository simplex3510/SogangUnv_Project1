# Story 019: 적재 패널 물류 행 제한 시간 바

> **Epic**: over-load
> **Status**: Complete
> **Layer**: Presentation
> **Type**: UI
> **Estimate**: S
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-17

## Context

**GDD**: `design/gdd/game-concept.md` 13장
**Requirement**: 상세 규칙: `design/quick-specs/hud-layout-2026-11-17.md` (물류 행)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating for 6.6)
**Engine Notes**: UI Toolkit, 바 너비는 `style.width` 퍼센트로 갱신.

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/quick-specs/hud-layout-2026-11-17.md` (2026-11-17 확정).*

- [ ] 제한 시간이 있는 물류(`HasDeadline`: Express, Fresh) 행 아래쪽에 가로 바가 있고, 길이 = 남은 제한 시간 ÷ 전체 제한 시간이다
- [ ] 바는 주행 중 매 프레임 줄어든다. 패널 전체를 다시 그리지 않고 바 너비만 갱신한다
- [ ] Normal 행은 바 없이 아이콘·가격·태그·내구도만 표시한다
- [ ] Express가 제한 시간을 넘으면 바가 비고 `#지연` 태그가 붙으며, 행은 남는다
- [ ] 폐기 물류 행(처분 장소 그룹)에는 바가 없다
- [ ] No regression: 접기/펼치기·스크롤(Story 014), 내구도 갱신·폐기 알림(Story 006)
- [ ] Express 바는 빨강, Fresh 바는 초록이다(유형 아이콘 색과 같음, 임시 결정 2026-11-17)
- [ ] GUI는 임시다(최종 색·아이콘 그림은 최종 GUI에서 정한다). Normal·Express 혼합 그룹, 줄어드는 바, 시간 초과 행 스크린샷을 `production/qa/evidence/`에 보관한다

---

## Implementation Notes

- `AddParcelRow`에서 `HasDeadline`이면 바 요소를 만들고, 물류 → 바 요소 사전을 둬 `Update`에서 너비만 바꾼다. 패널을 다시 그릴 때 사전을 비운다.
- 시간 초과 여부는 `Parcel`의 기존 값(`IsOverdue`/`RemainingDeadline`)을 읽는다. UI는 시간을 계산하지 않는다.
- `#지연` 태그가 새로 붙는 순간은 패널을 한 번 다시 그려도 된다.

---

## Out of Scope

- Fresh 유형 자체 (Story 008. 008이 끝나면 `HasDeadline` 기준으로 바가 자동 적용된다)
- 최종 바 색과 아이콘 그림(임시 색은 위 기준대로)
- 최종 GUI 디자인 (첫 릴리즈 전 임시 단계)

---

## Files

- `Assets/Game/Scripts/UI/DurabilityHud.cs`
- `Assets/Game/UI/Hud/DurabilityHud.uss`

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: UI
**Required evidence**: Normal·Express 혼합 그룹, 줄어드는 바, 시간 초과 행의 retained screenshot (`production/qa/evidence/`)

**Status**: [x] `production/qa/evidence/story-019-cargo-row-deadline-bar/`

---

## Dependencies

- Depends on: `production/epics/over-load/story-014-cargo-panel-fold-scroll.md` (같은 `DurabilityHud.cs`, 먼저 닫는다). Story 017과 같은 파일이라 동시에 진행하지 않는다
- Unlocks: Story 008의 "적재 UI에서 신선도 바 감소"가 이 바를 쓴다

## Completion Notes
**Completed**: 2026-11-17
**Criteria**: 7/8 passing (DEFERRED: 접기/펼치기·스크롤·내구도·폐기 알림 회귀 — 전체 QA 때 확인)
**Deviations**: ADVISORY — `#지연` 문구가 코드에 직접 적혀 있음(Story 020에서 정리); 폐기 행 바 없음은 스크린샷 없이 코드 조건으로 확인. OUT OF SCOPE — `DeliveryTargetParcelUI.cs`, `LogisticsCenterUI.cs`, `SettlementUI.cs`, `HudPanelSettings.asset`, `MalgunGothicDynamic.asset`은 다른 스토리 작업
**Test Evidence**: `production/qa/evidence/story-019-cargo-row-deadline-bar/` (01 혼합 그룹·시간 초과, 02 줄어든 바, 03 시간 초과 전환). 테스트는 `qa.level: minimal`이라 면제
**Reviewed by**: self, no independent review

## Calls beyond the design
- 바 클래스를 `parcel-deadline`(`--fresh`)로 일반화 (`DurabilityHud.uss`, `DurabilityHud.cs`) · kept 2026-11-17
- `#지연` 칩은 폐기 행에는 붙이지 않음 (`DurabilityHud.cs`) · kept 2026-11-17
- 시간 초과가 새로 생기면 패널을 한 번 다시 그림 (`DurabilityHud.cs`) · kept 2026-11-17
