# Story 029: 게임 설명 패널 + Esc 단계 처리와 퇴근 확인창

> **Epic**: over-load
> **Status**: Complete
> **Layer**: Presentation
> **Type**: UI
> **Estimate**: L
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-18

## Context

**GDD**: `design/quick-specs/guide-panel-addition-2026-11-18.md` (하루 운영 GDD 없음 — `design/game-brief.md`와 이 퀵 스펙이 기록)
**Requirement**: 시작 메뉴의 "게임 설명" 버튼이 스크롤되는 설명 패널을 연다(로고·버튼은 숨기고 오른쪽 영역에 맞춤, X로 닫음). Esc는 퇴근 확인창 → 게임 설명 패널 → 물류센터 창 순으로 닫고, 열린 것이 없으면 게임을 정지하고 퇴근 확인창(예/아니오)을 띄운다.

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED
**Engine Notes**: UI Toolkit `PanelRenderer`(Story 028과 같은 구성). Play Mode 도메인 리로드가 꺼질 수 있어 static 상태는 OnDisable에서 되돌린다.

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

- [ ] 게임 설명을 누르면 로고와 출근·게임 설명·퇴근 버튼이 모두 사라지고 패널이 오른쪽 영역에 뜬다
- [ ] 16:9와 21:9에서 패널이 차량을 가리지 않는다
- [ ] 마우스 휠과 ↑/↓/PageUp/PageDown으로 끝까지 스크롤되고 본문이 잘리지 않는다
- [ ] X를 누르면 패널이 사라지고 시작 메뉴와 로고가 다시 나온다. 다시 열면 맨 위부터 보인다
- [ ] 패널이 열린 동안 하루가 시작되지 않고 카메라와 HUD 상태가 변하지 않는다
- [ ] 키보드만으로 열기→스크롤→닫기가 되고, 닫으면 포커스가 게임 설명 버튼에 있다
- [ ] 게임 설명 패널이 열려 있을 때 Esc는 패널만 닫고 확인창은 뜨지 않는다
- [ ] 물류센터 창이 열려 있을 때 Esc는 창만 닫고 확인창은 뜨지 않는다
- [ ] 하루 운영 중, 시작 메뉴, 정산 화면에서 Esc를 누르면 `게임을 정지하고 퇴근하시겠습니까?` 확인창이 뜬다
- [ ] 확인창이 열려 있는 동안 게임 시계, 물류 시간(Express·Fresh), 차량이 멈추고 키 입력이 무시된다. 10초 뒤 `아니오`를 눌러도 영업 시각과 제한 시간이 그대로다
- [ ] 확인창에서 Esc 또는 `아니오`는 재개, `예`는 종료(Editor 플레이 중지)다. 처음 포커스는 `아니오`이고 ←/→/Enter로 선택된다
- [ ] Esc 한 번에 두 가지 동작이 겹치지 않는다
- [ ] No regression: 출근·퇴근 버튼, 카메라 블렌드, 키보드 포커스 강조, 물류센터 창의 X 닫기와 상호작용 닫기가 이전과 같다
- [ ] 시작 메뉴, 게임 설명 패널, 퇴근 확인창 스크린샷을 `production/qa/evidence/story-029-guide-panel-esc/`에 보관한다

---

## Implementation Notes

- 게임 설명 패널과 퇴근 확인창은 `StartMenu.uxml`에 `startMenu` 형제로 추가하고 `StartMenuUI`가 맡는다(새 PanelRenderer를 만들지 않는다). 확인창이 HUD·정산 위에 보이는지 확인한다.
- `GameManager`가 Esc를 한 곳에서 받아, 우선순위가 높은 핸들러부터 호출하고 처음 `true`를 돌려준 것에서 멈춘다. 핸들러가 하나도 처리하지 못하면 기존처럼 `Quit()`한다. 우선순위: 퇴근 확인창 300, 게임 설명 200, 물류센터 100, 확인창 열기 0.
- 정지는 `GameManager.SetPaused`가 `Time.timeScale`을 0으로 두고, 조작 스크립트(이동·상호작용·복구)는 `GameManager.IsPaused`를 보고 입력을 무시한다. 정지 전 상태(속도, 진행 중인 효과)를 지우는 `SetControlEnabled(false)`는 쓰지 않는다. `OnDisable`에서 `timeScale`과 `IsPaused`를 되돌린다.
- 퇴근 버튼(시작 메뉴)은 확인창 없이 즉시 `Quit()`한다.
- `ControlsRequested` 이벤트는 삭제한다. 버튼 문구는 `게임 설명`이다.
- 본문은 스펙의 "패널 본문 (확정 초안)"을 UXML에 그대로 쓴다. 닫기 아이콘은 Bootstrap Icons `x-lg` SVG.

---

## Out of Scope

- 일시정지 메뉴(확인창이 떠 있는 동안만 정지), 설정 화면
- 패널 열기·닫기 애니메이션
- 배송지·처분 장소 월드 아이콘 쪽 Esc 처리(창이 아님)

---

## Files

- `Assets/Game/Scripts/UI/StartMenuUI.cs` (게임 설명 패널, 퇴근 확인창, Esc 핸들러, `ControlsRequested` 삭제)
- `Assets/Game/UI/StartMenu/StartMenu.uxml`, `StartMenu.uss` (게임 설명 패널·확인창 추가)
- `Assets/Game/Scripts/Manager/GameManager.cs` (Esc 핸들러 목록, `SetPaused`/`IsPaused`)
- `Assets/Game/Scripts/UI/LogisticsCenterUI.cs` (Esc로 닫기 핸들러 등록)
- `Assets/Game/Scripts/Player/PlayerMovementController.cs`, `PlayerInteractionController.cs`, `VehicleRecoveryController.cs` (정지 중 입력 무시)

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: UI
**Required evidence**: 메뉴·게임 설명 패널·퇴근 확인창의 retained screenshot (`production/qa/evidence/story-029-guide-panel-esc/`). 테스트는 `qa.level: minimal`에서 생략

**Status**: [x] `production/qa/evidence/story-029-guide-panel-esc/` (01 메뉴, 02 게임 설명 패널, 03 메뉴 위 확인창, 04 인게임 확인창). Esc 처리와 정지는 Editor에서 Esc 핸들러를 직접 호출해 확인했고, 실제 키 입력·마우스 클릭·↑/↓/PageUp/PageDown·21:9는 직접 확인하지 않았다.

---

## Dependencies

- Depends on: Story 028 (Complete)
- Unlocks: None

## Completion Notes
**Completed**: 2026-11-18
**Criteria**: 14/14 passing (스크롤·열기/닫기·실제 Esc·확인창·21:9·기존 기능은 사용자가 직접 확인)
**Deviations**: 없음. 이 스토리와 무관한 변경(`DeliveryTarget-Type-B.prefab`, `MalgunGothicDynamic.asset`, `FailedScene` 삭제)이 작업 트리에 있음
**Test Evidence**: `production/qa/evidence/story-029-guide-panel-esc/` (01 메뉴, 02·02b·02c 게임 설명, 03·04 확인창). 스탬프 변경 없음. 테스트는 qa.level minimal에서 생략
**Reviewed by**: self, no independent review
**Run result**: OBSERVED — SampleMapDesign Play: 게임 설명 패널(차량 미가림), 메뉴·인게임 퇴근 확인창, 정지 중 게임 시계 고정, 새 에러 없음

## Calls beyond the design
- 정지는 `Time.timeScale` 0 + 조작 스크립트 `IsPaused` 입력 무시, R 복구 기다림 중단, BGM 계속 (`GameManager.cs` 등) · kept 2026-11-18
- Esc 핸들러를 처리한 UI가 없으면 확인 없이 즉시 종료 (`GameManager.cs`) · kept 2026-11-18
- 확인창 모양(60% 디밍·박스·버튼 200x72px), 게임 설명 부제와 물류 특징 한 줄 설명 (`StartMenu.uss`, `StartMenu.uxml`) · kept 2026-11-18
- 키 배지 표기는 `마우스 휠`, `R (유지)`로 사용자가 변경 · changed and applied 2026-11-18
