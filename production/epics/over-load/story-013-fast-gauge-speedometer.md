# Story 013: 고속 모드 게이지와 속도계 HUD

> **Epic**: over-load
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-17

## Context

**GDD**: `design/game-brief.md`
**Requirement**: 상세 규칙: `design/quick-specs/fast-mode-gauge-speedometer-2026-11-17.md`, `design/gdd/game-concept.md` 10.4

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating for 6.6)
**Engine Notes**: UI Toolkit 시범 도입(스토리 006과 같은 `Assets/Game/UI/Hud/`). 아크는 Painter2D 커스텀 VisualElement.

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/quick-specs/fast-mode-gauge-speedometer-2026-11-17.md` (2026-11-17 확정).*

- [ ] 고속 모드인 동안 게이지가 `T_drain`(8초)에 걸쳐 1 → 0으로 줄고, 0이 되면 일반 모드로 강제 전환된다(`DrivingModeChanged` 발생)
- [ ] 일반 모드로 바뀐 뒤 `T_delay`(1.5초)가 지나면 게이지가 `T_recharge`(12초)에 걸쳐 0 → 1로 찬다
- [ ] 모드가 바뀐 뒤 `T_cooldown`(1초) 동안 Ctrl 전환 요청을 무시한다. 강제 전환에도 쿨다운이 걸린다
- [ ] 강제 전환(소진) 뒤에는 게이지가 `g_reenter`(0.3) 이상이어야 고속으로 들어갈 수 있다. 게이지 0에서는 고속 진입 불가
- [ ] 기존 전환 조건(접지, 상호작용 중 아님)은 그대로다. 하루 종료로 조작이 꺼지면 게이지·쿨다운이 멈춘다
- [ ] 다섯 값과 속도 표시 배율(4), 아크 최대값(80 km/h)은 `VehicleConfig`에 `TODO(tuning)`으로 둔다
- [ ] 화면 중앙 하단에 속도계(반원 아크 + 정수 속도 + `km/h`)와 그 아래 게이지 바를 UI Toolkit으로 표시한다. 소진 상태의 바는 경고색
- [ ] 플레이 중 속도계와 게이지 바의 스크린샷을 `production/qa/evidence/story-013-fast-gauge-speedometer/`에 보관한다

---

## Implementation Notes

- 게이지·쿨다운은 MonoBehaviour와 분리된 순수 클래스(`FastModeGauge`)로 두고 `PlayerMovementController`가 `Update`에서 `Tick`한다.
- `PlayerMovementController`는 HUD용으로 `ForwardSpeed`, `FastGauge`(읽기 전용 상태)를 노출한다.
- 속도계는 `UIDocument` + UXML/USS. 아크는 `SpeedArc`(Painter2D). 스토리 006의 `HudPanelSettings`·한글 FontAsset이 있으면 재사용한다.
- 기존 `DrivingModeHUD`(TMP 모드 텍스트)는 그대로 둔다.
- **Performance**: 매 프레임 float 연산 몇 개와 아크 1개 재그리기(값이 바뀔 때만).

---

## Out of Scope

- 게이지 소진·쿨다운 사운드/이펙트
- 고속 진입 거부 시 안내 문구
- 최종 수치 확정 (Story 003)

---

## Files

- `Assets/Game/Scripts/Player/FastModeGauge.cs` (신규)
- `Assets/Game/Scripts/Player/VehicleConfig.cs`
- `Assets/Game/Scripts/Player/PlayerMovementController.cs`
- `Assets/Game/UI/Hud/SpeedometerHUD.cs`, `SpeedArc.cs`, `Speedometer.uxml`, `Speedometer.uss` (신규)
- `Assets/Game/Scenes/_DevOnly/SampleMapDesign.unity` (SpeedometerHUD 오브젝트 추가)
- [test root: Assets/Tests/EditMode/ — 면제]

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: Test: waived at qa.level minimal. 속도계·게이지 바 retained screenshot (`production/qa/evidence/`)

**Status**: [x] `production/qa/evidence/story-013-fast-gauge-speedometer/` (fast-mode-driving.png 72 km/h·게이지 감소, forced-normal-exhausted.png 강제 전환 후 주황 바, fast-mode-draining.png 정지 중 감소)

---

## Dependencies

- Depends on: None (스토리 006의 PanelSettings·폰트가 있으면 재사용)
- Unlocks: `production/epics/over-load/story-003-balancing.md` (게이지 수치 조정)

## Completion Notes
**Completed**: 2026-11-17
**Criteria**: 8/8 passing (AC-1~5 사용자 확인, AC-6~8 코드·스크린샷 확인)
**Deviations**: None. 증거 스탬프 없음(RECEIPT: NONE) — 증거가 코드보다 오래됐는지는 알 수 없음
**Test Evidence**: waived at qa.level minimal; 스크린샷 `production/qa/evidence/story-013-fast-gauge-speedometer/` (3장)
**Reviewed by**: self, no independent review

## Calls beyond the design
- 강제 전환(소진)은 접지·상호작용 여부와 무관하게 즉시 일반 모드로 전환 (`PlayerMovementController.cs` Update) · kept 2026-11-17
- 속도는 후진도 절댓값 표시, 경고색 주황·정상색 초록, 게이지 바는 속도계 패널 안 아크 아래 (`Speedometer.uss`, `SpeedometerHUD.cs`) · kept 2026-11-17
