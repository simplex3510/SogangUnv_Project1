# Story 018: 하단 중앙 HUD 정리

> **Epic**: over-load
> **Status**: Complete
> **Layer**: Presentation
> **Type**: UI
> **Estimate**: S
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-17

## Context

**GDD**: `design/gdd/game-concept.md` 13장
**Requirement**: 상세 규칙: `design/quick-specs/hud-layout-2026-11-17.md` ("하단 중앙" 절)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating for 6.6)
**Engine Notes**: 속도계는 UI Toolkit(Story 013), 과적·차단 문구는 현재 uGUI + TMP. 과적 아이콘을 속도계 UXML로 옮길지 uGUI로 둘지는 구현 시 작은 쪽을 고른다.

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/quick-specs/hud-layout-2026-11-17.md` (2026-11-17 확정).*

- [ ] 주행 모드 글자 HUD(`DrivingModeHUD`, FAST/NORMAL)가 화면에 없다
- [ ] 고속 모드일 때 속도계 아래 게이지가 강조색으로 바뀌고, 일반 모드로 돌아오면 원래 색이다. 소진 경고색(Story 013)과 구분된다
- [ ] 무게 한도 초과 적재 중 속도계 바로 위 중앙에 작은 과적 아이콘과 감소율(`−33%`)이 보이고, 한도 이내면 숨겨진다. 기존 `OVERWEIGHT −NN%` 글자는 없다
- [ ] 상차·하차 진행 바가 플레이어 오른쪽에 실제로 보이는지 확인한다. 하차 진행이 보이지 않으면 따로 스토리를 만든다(이 스토리에서 구현하지 않음)
- [ ] GUI는 임시다(아이콘·강조색 그림 미정). 일반·고속 모드, 과적 아이콘 스크린샷을 `production/qa/evidence/`에 보관한다

---

## Implementation Notes

- 강조색은 USS 클래스 토글로 한다. 모드 값은 `PlayerMovementController`의 기존 주행 모드 상태를 읽는다.
- 과적 여부·감소율은 기존 `OverweightHUD`가 읽는 값을 그대로 쓴다. UI가 감소율을 계산하지 않는다.

---

## Out of Scope

- 표시 문구 한글화 (Story 020)
- 차단 사유 문구 (Story 021 토스트 알림으로 이전)
- 과적 외 적재 상태 아이콘
- 최종 아이콘·색 디자인 (첫 릴리즈 전 임시 단계)

---

## Files

- `Assets/Game/Scripts/UI/DrivingModeHUD.cs` (제거, `.meta` 포함)
- `Assets/Game/Scripts/UI/OverweightHUD.cs` (제거, `.meta` 포함 — 과적 표시를 속도계 UXML로 이전)
- `Assets/Game/UI/Hud/SpeedometerHUD.cs`, `Speedometer.uxml`, `Speedometer.uss`
- `Assets/Game/UI/Icons/exclamation-triangle-fill.svg` (+ `.meta`, Bootstrap Icons)
- `Assets/Game/Scenes/_DevOnly/SampleMapDesign.unity` (`DrivingModeText`, `OverweightText` 오브젝트 제거)
- `Assets/Game/Scenes/_DevOnly/DevScene.unity`, `Assets/Game/Scenes/FailedScene.unity` (`DrivingModeText` 제거 — Missing Script 방지)

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: UI
**Required evidence**: 일반·고속 모드 게이지, 과적 아이콘의 retained screenshot (`production/qa/evidence/`)

**Status**: [x] production/qa/evidence/story-018-hud-layout-audit/ (normal·fast·overweight.png)

---

## Dependencies

- Depends on: `production/epics/over-load/story-013-fast-gauge-speedometer.md` (속도계·게이지, 먼저 닫는다)
- Unlocks: None

## Completion Notes
**Completed**: 2026-11-17
**Criteria**: 5/5 passing (AC-4는 사용자가 화면에서 확인, 보관 이미지 없음)
**Deviations**: ADVISORY — AC-4 진행 바는 보관 스크린샷 없이 사용자 확인으로만 기록. OverweightHUD는 수정 대신 삭제(UXML로 이전), DevScene·FailedScene에서도 DrivingModeText 제거
**Test Evidence**: `production/qa/evidence/story-018-hud-layout-audit/` (waived tests at qa.level minimal)
**Reviewed by**: self, no independent review

## Calls beyond the design
- 고속 게이지 강조색 파랑 rgb(64,170,255) (`Speedometer.uss`) · kept 2026-11-17
- 과적 아이콘·문구 주황 (`Speedometer.uss`) · kept 2026-11-17
- 아이콘 ⚠ exclamation-triangle-fill, 감소율 `−NN%` (`SpeedometerHUD.cs`) · kept 2026-11-17
- 과적 표시를 속도계 UXML로 이전, OverweightHUD 삭제 · kept 2026-11-17
