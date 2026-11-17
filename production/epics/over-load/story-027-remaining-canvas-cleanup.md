# Story 027: 남은 Canvas UI 정리 (HoldInteractButton, GameTimeHUD) + 적재 패널 마우스 접기 + 제한 시간 표기

> **Epic**: over-load
> **Status**: Complete
> **Layer**: Presentation
> **Type**: UI
> **Estimate**: L
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-18

## Context

**GDD**: `design/gdd/game-concept.md` 10.6장
**Requirement**: Canvas → UI Toolkit 전면 전환의 마지막 단계. 이 스토리가 끝나면 게임 UI에 UGUI Canvas가 남지 않는다.
UI 백로그 — 적재 패널 그룹을 숫자키뿐 아니라 마우스 클릭으로도 접고 펼 수 있어야 한다(사용자 요청 2026-11-18).
물류센터 창·적재 패널의 시간 표기를 시각/남은 시간으로 구분한다(사용자 결정 2026-11-18, 아이콘 쌍 + 문구).

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating for 6.6)
**Engine Notes**: 2026-11-18 grep 기준 `GameTimeHUD`·`HoldInteractButton`은 `DevScene.unity`·`FailedScene.unity`에서만 참조된다. 시계는 Story 015(`StatusHud`)가, 홀드 상차는 물류센터 창 `BorderProgress`가 이미 UI Toolkit으로 맡고 있을 가능성이 크다.

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

- [ ] `GameTimeHUD`와 `HoldInteractButton` 각각에 대해 실제로 쓰이는지 확인하고 결과를 Completion Notes에 적는다
- [ ] 쓰이지 않으면 스크립트와 씬·프리팹의 해당 Canvas 오브젝트를 지운다. 쓰이면 같은 기능을 UI Toolkit으로 옮긴다
- [ ] `Assets/Game/`의 게임 UI 스크립트에 `using UnityEngine.UI;`·`using TMPro;`가 남아 있지 않다 (에디터 도구 제외)
- [ ] 게임 씬(`SampleMapDesign`, `DevScene`, `FailedScene`)에 UGUI Canvas가 남아 있지 않다. 남겨야 하는 것이 있으면 이유를 Completion Notes에 적는다
- [ ] No regression: 세 씬 모두 Play 진입 시 에러 없음, 시계·상차 동작
- [ ] 세 씬 Play 화면 스크린샷을 `production/qa/evidence/story-027-remaining-canvas-cleanup/`에 보관한다
- [ ] 배송 현황·폐기 패널의 그룹 헤더를 마우스로 클릭하면 그 그룹이 접히거나 펼쳐진다. 숫자키로 토글한 것과 같은 결과다
- [ ] 헤더 클릭이 차량 조작·상호작용 입력을 막거나 동시에 실행하지 않는다
- [ ] No regression: 숫자키 1~9 토글, `0` 전체 펼침, `` ` `` 전체 접기, 휠 스크롤
- [ ] 헤더 클릭 전과 후 스크린샷을 같은 evidence 폴더에 보관한다
- [ ] 물류센터 창의 Express 행은 시계 아이콘 + `hh:mm까지`(만료 시각), Fresh 행은 모래시계 아이콘 + `hh:mm 남음`(신선도 총량)으로 보인다. ↻ 아이콘은 쓰지 않는다
- [ ] 적재 패널에서 제한 시간 바가 있는 행(Express, 상차한 Fresh)은 바 오른쪽에 같은 표기가 보인다. Fresh는 신선도가 줄어드는 대로 숫자가 갱신된다
- [ ] `남음`은 분 단위로 올림한다. 0이 아닌 값이 `00:00 남음`으로 보이지 않는다
- [ ] Normal 행과 폐기 행에는 지금처럼 시간 표기가 없다
- [ ] Express와 Fresh가 섞인 물류센터 창과 적재 패널 스크린샷을 같은 evidence 폴더에 보관한다

---

## Implementation Notes

- 참조 확인은 Unity CLI로 Editor에서 한다(씬 오브젝트·프리팹 검색). 삭제도 Editor에서 한다. `.prefab`·`.unity`를 직접 고치지 않는다.
- 쓰이지 않는 코드를 옮기지 않는다. 지우는 쪽이 기본이다.
- 마지막에 TMP 폰트 에셋(`MalgunGothicTMP.asset`)이 아직 쓰이는지 확인하고, 안 쓰이면 지울지 사용자에게 묻는다.
- 마우스 접기: 헤더는 Story 024의 `CargoGroupHeader.uxml`에 있다. 지금은 `picking-mode="Ignore"`이므로 헤더 요소만 `Position`으로 바꾸고 `ClickEvent`를 등록한다. 토글은 숫자키와 같은 경로를 쓴다(`_expandedGroups`, `RefreshCargoPanel`).
- 플레이 중 마우스 커서가 보이는지 확인한다. 안 보이면 사용자에게 묻는다.
- 시간 표기: 아이콘은 Bootstrap Icons `clock`, `hourglass-split` SVG를 `Assets/Game/UI/Icons/`에 둔다. Story 024 템플릿 `CenterRow.uxml`, `ParcelRow.uxml`에 아이콘·라벨 요소를 넣는다. `hh:mm`은 게임 시계와 같은 게임 시간 단위다. `arrow-clockwise.svg`가 다른 데서 쓰이지 않으면 지울지 사용자에게 묻는다.

---

## Out of Scope

- 에디터 도구 UI(IMGUI, MapPainter 등)
- 새 UI 기능 (적재 패널 마우스 접기·제한 시간 표기는 포함)
- Fresh 신선도 길이 등 값 밸런싱 (게임 안에서 조정)

---

## Files

- `Assets/Game/Scripts/UI/GameTimeHUD.cs`, `HoldInteractButton.cs` (삭제)
- `Assets/Game/Scenes/_DevOnly/DevScene.unity`, `Assets/Game/Scenes/FailedScene.unity` (Canvas 삭제)
- `Assets/Game/Scripts/UI/DurabilityHud.cs`, `Assets/Game/Scripts/UI/LogisticsCenterUI.cs`
- `Assets/Game/UI/Hud/CargoGroupHeader.uxml`, `ParcelRow.uxml`, `DurabilityHud.uss`
- `Assets/Game/UI/LogisticsCenter/CenterRow.uxml`, `LogisticsCenterWindow.uxml`, `LogisticsCenterWindow.uss`
- `Assets/Game/UI/Icons/clock.svg`, `hourglass-split.svg`, `x-lg.svg` (새 아이콘)

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: UI
**Required evidence**: 세 씬 Play 화면의 retained screenshot (`production/qa/evidence/story-027-remaining-canvas-cleanup/`). 테스트는 `qa.level: minimal`에서 생략

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/over-load/story-025-settlement-ui-toolkit.md`, `production/epics/over-load/story-026-world-parcel-icons-ui-toolkit.md`, `production/epics/over-load/story-024-uxml-element-templates.md` (같은 헤더 템플릿)
- Unlocks: None

## Completion Notes
**Completed**: 2026-11-18
**Criteria**: 15/15 passing (DevScene·FailedScene Play 확인은 사용자 결정으로 SampleMapDesign만 기준)
**Deviations**: 없음. 범위 밖 변경(`GameManager.cs`, `PlayerInteractionController.cs`, `GameTimeSystem.cs`, `HudPanelSettings.asset`, `MalgunGothic*.asset`, `SampleMapDesign.unity`)은 이전부터 있던 미커밋 변경. `arrow-clockwise.svg`·닫기 `x-lg.svg`는 사용자 요청으로 삭제·추가
**Test Evidence**: `production/qa/evidence/story-027-remaining-canvas-cleanup/` (SampleMapDesign Play, 헤더 클릭 전·후, Express+Fresh 물류센터 창·적재 패널, 닫기 아이콘). 테스트는 qa.level minimal에서 생략
**Reviewed by**: self, no independent review (엔진 API 점검: unity-ui-specialist, 문제 없음)
**Run result**: OBSERVED — SampleMapDesign Play, 에러 0

- `GameTimeHUD`·`HoldInteractButton`은 DevScene·FailedScene Canvas에서만 쓰여 스크립트째 삭제. 두 씬은 쓰지 않는 씬이라 Canvas만 지우고 HUD 프리팹은 넣지 않음
- `EventSystem` 오브젝트는 UI Toolkit 입력에 필요할 수 있어 남김
- `MalgunGothicTMP.asset`은 `TMP Settings` 기본 폰트로만 참조됨. 삭제하려면 기본 폰트도 함께 변경 필요 (미결정)
- Advisory: `DurabilityHud.LateUpdate`가 Fresh 행 문자열을 프레임마다 새로 만듦 (분이 바뀔 때만 만들도록 개선 가능)
- Advisory: `DailyOperationConfig`(_gameSpeed 600→60), `DeliveryRewardConfig`(_distanceFeePerMeter 10→2) 값이 바뀐 상태 — 이 스토리 작업이 아님

## Calls beyond the design
- 시간 표기 아이콘·글자 13px, 색 rgb(215,222,232), 간격 8px/3px (`LogisticsCenterWindow.uss`, `DurabilityHud.uss`) · kept 2026-11-18
- Express 시각은 분 내림, Fresh 남은 시간은 분 올림 (`LogisticsCenterUI.FormatDeadline`) · kept 2026-11-18
- 적재 행 바를 감싼 `deadlineRow` 추가 (`ParcelRow.uxml`) · kept 2026-11-18
- 닫기 아이콘 22px, 호버 시 rgb(255,120,120) (`LogisticsCenterWindow.uss`) · kept 2026-11-18
