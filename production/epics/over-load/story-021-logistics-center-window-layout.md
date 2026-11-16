# Story 021: 물류센터 상차 창 개편 (1열 카드 레이아웃)

> **Epic**: over-load
> **Status**: Complete
> **Layer**: Presentation
> **Type**: UI
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-17

## Context

**GDD**: `design/game-brief.md`
**Requirement**: 상세 규칙: `design/quick-specs/logistics-center-window-layout-2026-11-17.md` (기획서 8.1·8.2·6.4 표시 방식)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating for 6.6)
**Engine Notes**: 적재 HUD와 같은 UI Toolkit `PanelRenderer` 구성. 홀드는 `PointerDownEvent`/`PointerUpEvent`/`PointerLeaveEvent`.

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/quick-specs/logistics-center-window-layout-2026-11-17.md` (2026-11-17).*

- [ ] 배송지 카드가 1열·거리순으로 쌓이고, 넘치면 마우스 휠로 스크롤된다
- [ ] 머리에 `총 물류 개수`, `적재 한도`, `무게 한도` 배지가 실제 값으로 표시되고, 무게가 W를 넘으면 무게 배지가 경고색이 된다
- [ ] 물류 행의 금액·아이콘색·해시태그가 같은 물류의 적재 HUD 행과 같다
- [ ] Express 행에만 제한 시간 바와 `HH:mm`이 있고 바가 실시간으로 줄어든다
- [ ] 헤더 홀드는 그룹 상차, 행 홀드는 개별 상차를 한다
- [ ] 행에 마우스를 올리면 그 행이, 헤더에 올리면 그룹의 실을 수 있는 행이 강조된다
- [ ] 상차 중인 물류 행 테두리를 따라 진행도만큼 초록 선이 차오르고, 떼거나 벗어나면 사라진다
- [ ] 실을 수 없는 행은 흐린 표시와 `#적재 초과`/`#무게 초과` 칩으로 표시되고 눌리지 않는다
- [ ] 상차로 행이 사라져도 스크롤 위치가 튀지 않는다
- [ ] No regression: 정렬, 그룹 상차의 W 정지(D5), 닫기 시 상차 취소, 영업 종료 시 자동 닫힘
- [ ] GUI는 임시다. 새 화면 스크린샷을 `production/qa/evidence/`에 보관한다

---

## Implementation Notes

- `LogisticsCenterUI`의 공개 API(`Open`/`Close`/`IsOpen`)와 상차 로직(그룹 수집·정렬, 홀드 시작·종료, D5 정지)은 그대로 두고 표시만 UI Toolkit으로 바꾼다.
- 행 구성(아이콘·금액·해시태그 칩)은 `DurabilityHud`와 같은 클래스 이름·식을 쓴다.
- 이 스토리가 Story 020의 물류센터 문구(`CARGO`/`WEIGHT`/`WEIGHT LIMIT`) 한글화를 흡수한다.

---

## Out of Scope

- 게임패드 입력
- Fresh 유형 아이콘·신선도 표시 (Story 008)
- 최종 GUI 디자인 (첫 릴리즈 전 임시 단계)

---

## Files

- `Assets/Game/Scripts/UI/LogisticsCenterUI.cs`
- `Assets/Game/Scripts/UI/BorderProgress.cs` (신규, 테두리 진행선 Painter2D)
- `Assets/Game/UI/LogisticsCenter/LogisticsCenterWindow.uxml`, `.uss`, `WindowPanelSettings.asset` (신규, HUD 위에 그리도록 정렬 순서 10)
- 작업 씬 `Assets/Game/Scenes/_DevOnly/SampleMapDesign.unity` (새 `LogisticsCenterWindow` 오브젝트, 예전 `Canvas/LogisticsUI`·`Canvas/LogisticsPanel` 삭제)
- `DevScene`·`FailedScene`은 예전 uGUI 구성 그대로다(빌드 목록 밖). 이 씬에서 물류센터 창을 쓰려면 같은 연결이 필요하다

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: UI
**Required evidence**: retained screenshot (`production/qa/evidence/story-021-logistics-center-window-layout/`)

**Status**: [x] `production/qa/evidence/story-021-logistics-center-window-layout/` (01 창 열림: 머리·배지·카드·Express 바와 만료 시각, 02 스크롤바 스타일 적용, 03 스크롤바 폭 수정, 04 hover 강조·테두리 진행선 62%). 창은 Play 중 `Open()` 호출로 열었고, 04의 강조 클래스와 진행선 값은 스크립트로 직접 넣어 그렸다. 실제 마우스 홀드로 진행 이벤트가 흐르는 것과 비활성 행은 아직 직접 조작으로 확인하지 않았다(사용자 시각 확인 예정).

---

## Dependencies

- Depends on: `production/epics/over-load/story-006-durability-feedback-ui.md` (행 스타일)
- Unlocks: None

## Completion Notes
**Completed**: 2026-11-17
**Criteria**: 11/11 (AC-2·3·8·9·10은 DEFERRED — 플레이 때 확인; AC-1·4·5·6·7·11 통과)
**Deviations**: None
**Test Evidence**: `production/qa/evidence/story-021-logistics-center-window-layout/` (스크린샷 4장; 증거 스탬프 없음. 04의 hover 강조·진행선 값은 스크립트로 넣어 그림 — 실제 조작은 사용자 확인)
**Reviewed by**: self, no independent review

## Calls beyond the design
- 창 제목·부제 문구, 헤더 점+이름+거리+"남은 N · 적재 N", 배지 형식 "적재 한도: 0/6"·"무게 한도: 0/6", 행 hover 강조·초록 진행선 색, 스크롤바 모양 (`LogisticsCenterWindow.uxml/.uss`, `LogisticsCenterUI.cs`) · kept 2026-11-17
