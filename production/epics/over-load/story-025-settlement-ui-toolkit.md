# Story 025: 정산 화면 UI Toolkit 전환 + 최상단 노출

> **Epic**: over-load
> **Status**: Complete
> **Layer**: Presentation
> **Type**: UI
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-18

## Context

**GDD**: `design/gdd/game-concept.md` 10.6장, 정산 규칙은 기존 `SettlementSystem`
**Requirement**: UI 백로그 UI-06 (`production/ui-backlog.md`). 결정(2026-11-18): 정산 화면을 다른 모든 UI보다 앞에 그린다. Canvas → UI Toolkit 전면 전환의 일부.

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating for 6.6)
**Engine Notes**: UI Toolkit 패널 간 순서는 PanelSettings `Sort Order`로 정한다. 지금 HUD는 `HudPanelSettings` 0, 물류센터 창은 `WindowPanelSettings` 10. 한글 FontAsset은 기존 것을 재사용한다.

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

- [ ] 하루가 끝나면 정산 화면이 UI Toolkit으로 표시되고, 최종 금액·요약·항목별 내역(신선 보너스 포함)이 이전과 같은 값으로 보인다
- [ ] 최종 금액이 음수이면 손실 색으로 보인다
- [ ] 정산 화면이 HUD, 플레이어 상호작용 패널(`Space`·`R`), 물류센터 창, 토스트보다 앞에 그려진다. 상호작용 대상 위에서 하루가 끝나도 `Space` 패널이 정산 화면 위로 보이지 않는다
- [ ] 정산 화면용 Canvas와 `SettlementUI`의 UGUI·TMP 참조가 남아 있지 않다
- [ ] No regression: 정산 금액 계산, 하루 종료 흐름, 다음 단계 진행 입력
- [ ] 정산 화면(이익·손실 각 한 장), 상호작용 대상 위에서 하루 종료 때의 스크린샷을 `production/qa/evidence/story-025-settlement-ui-toolkit/`에 보관한다

---

## Implementation Notes

- 정산 화면 전용 PanelSettings를 두고 Sort Order를 물류센터 창(10)보다 크게 한다(예: 100). 순서 규칙이므로 이유를 한 줄 주석으로 남긴다.
- 항목별 내역 한 줄은 Story 024 방식대로 UXML 템플릿으로 만든다.
- 손실 색 등 기존 `[SerializeField]` 색 값은 USS로 옮긴다.
- 씬 배치와 Canvas 제거는 Unity CLI로 Editor에서 한다. `.prefab`·`.unity`를 직접 고치지 않는다.
- UI는 게임 상태를 바꾸지 않는다.

---

## Out of Scope

- 상호작용 가능 판정에 "운영 중" 조건을 넣는 게임 규칙 변경 (정산 화면이 가리므로 하지 않는다)
- 정산 화면 레이아웃 재디자인

---

## Files

- `Assets/Game/Scripts/UI/SettlementUI.cs`
- `Assets/Game/UI/Settlement/SettlementWindow.uxml`
- `Assets/Game/UI/Settlement/SettlementWindow.uss`
- `Assets/Game/UI/Settlement/SettlementRow.uxml`
- `Assets/Game/UI/Settlement/SettlementPanelSettings.asset`
- `Assets/Game/Scenes/_DevOnly/DevScene.unity`
- `Assets/Game/Scenes/_DevOnly/SampleMapDesign.unity`
- `Assets/Game/UI/Hud/ToastHud.cs` (리로드 예외 수정, 스토리 범위 밖)

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: UI
**Required evidence**: 정산 화면(이익·손실), 대상 위 하루 종료의 retained screenshot (`production/qa/evidence/story-025-settlement-ui-toolkit/`). 테스트는 `qa.level: minimal`에서 생략

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/over-load/story-024-uxml-element-templates.md` (템플릿 방식)
- Unlocks: `production/epics/over-load/story-027-remaining-canvas-cleanup.md`

## Completion Notes
**Completed**: 2026-11-18
**Criteria**: 6/6 passing (none deferred)
**Deviations**: OUT OF SCOPE — `ToastHud.cs` 리로드 예외 수정(`ClearAll`이 해제된 `_stack`을 지우던 기존 버그). 이익 스크린샷은 `SettlementUI.Show`에 양수 결과를 직접 넣어 찍음(실제 수입 플레이 아님)
**Test Evidence**: waived at qa.level minimal — 스크린샷 `production/qa/evidence/story-025-settlement-ui-toolkit/`
**Reviewed by**: self, no independent review

## Calls beyond the design
- 카드 700x560, 패딩 28/40/24/40, 테두리 3px, 라운드 18px (`SettlementWindow.uss`) · kept 2026-11-18
- 배경 덮개 `rgba(0,0,0,0.6)`, 글자 48/22/24, 요약·항목 사이 구분선 (`SettlementWindow.uss`) · kept 2026-11-18
- 최종·요약 가운데 정렬, 금액 열 최소 폭 120px, 손실색 `rgb(242,102,89)` (`SettlementWindow.uss`) · kept 2026-11-18
- 정산 화면을 닫는 방법 없음, 기존과 같음 (`SettlementUI.cs`) · kept 2026-11-18
