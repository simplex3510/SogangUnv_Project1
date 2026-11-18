# Story 006: 내구도 피드백 UI 3종

> **Epic**: over-load
> **Status**: Complete
> **Layer**: Presentation
> **Type**: UI
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-17

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 10 (충돌 내구도 감소와 피드백). 상세 규칙: `design/gdd/game-concept.md` 13장

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating for 6.6)
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/gdd/game-concept.md` 13장 (2026-11-16 확정).*

- [ ] 내구도 감소가 발생한 충돌마다 차량 위에 일반 물류 기준 감소량 `−N`이 잠시 표시된다
- [ ] 감소가 0인 충돌(하한선 미만, 쿨다운 중, 적재 물류 없음)에는 표시하지 않는다
- [ ] 적재 물류 UI에 물류별 현재/초기 내구도가 표시되고, 충돌 후 값이 갱신된다
- [ ] 물류가 폐기로 전환되면 화면에 알림이 표시된다
- [ ] 하루 종료(조작 잠금) 후에는 새 표시가 생기지 않는다
- [ ] GUI는 임시다. 세 화면 각각의 스크린샷을 `production/qa/evidence/`에 보관한다

---

## Implementation Notes

- 기존 UI 베이스(`GameObjectUI`, `PlayerObjectUI`)와 HUD 배치 방식을 따른다.
- 표시 내용은 Story 004의 감소 이벤트와 Story 005의 폐기 전환 이벤트를 구독해 얻는다. UI가 내구도를 계산하지 않는다.

---

## Out of Scope

- 사운드·이펙트 연출
- 최종 GUI 디자인 (첫 릴리즈 전 임시 단계)

---

## Files

- `Assets/Game/Scripts/UI/DurabilityHud.cs`
- `Assets/Game/UI/Hud/DurabilityHud.uxml`
- `Assets/Game/UI/Hud/DurabilityHud.uss`
- `Assets/Game/UI/Hud/MalgunGothicDynamic.asset`
- `Assets/Game/Scripts/Player/PlayerCargoImpactController.cs`
- `Assets/Game/Scripts/Player/PlayerInteractionController.cs`

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: UI
**Required evidence**: 감소량 표시·적재 물류 내구도·폐기 알림 화면의 retained screenshot (`production/qa/evidence/`)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/over-load/story-004-collision-durability.md`, `production/epics/over-load/story-005-spoiled-cargo-disposal.md`
- Unlocks: None

## Completion Notes
**Completed**: 2026-11-17
**Criteria**: 6/6 passing (none deferred)
**Deviations**: ADVISORY — UI 문자열을 코드에 직접 넣음(`.claude/rules/ui-code.md` 현지화 규칙; 프로젝트에 현지화 시스템 없음, 기존 uGUI와 같은 방식). ADVISORY — 팝업 애니메이션 끄기 옵션 없음(모션 설정 시스템 없음). ADVISORY — 구현 노트의 "기존 uGUI 베이스"와 달리 사용자 결정으로 UI Toolkit 시범 적용. 참고 — `Assets/Game/Configs/CargoTraitConfig.asset` `_baseDurability: 1`(코드 기본 100)이라 충돌 1회에 전 물류 폐기·하루 즉시 종료(이 스토리 범위 밖). 참고 — 씬 배선은 `Assets/Game/Scenes/_DevOnly/SampleMapDesign.unity`에 저장됨(같은 저장에 스토리 013 속도계와 기존 미저장 변경 포함). 참고 — 증거 캡처 후 `DurabilityHud.cs`가 접기/스크롤 작업(quick-spec `cargo-panel-fold-scroll-2026-11-17`)으로 바뀌는 중이고, `MalgunGothicDynamic.asset`은 플레이 중 동적 글리프 추가로 바뀜.
**Test Evidence**: `production/qa/evidence/story-006-durability-feedback-ui/` (01-cargo-panel-before, 02-damage-popup-and-discard-notice, 03-damage-popup, 04-after-day-end-no-popup)
**Reviewed by**: self, no independent review

## Calls beyond the design
- −N은 올림 정수, 차량 위 1m에서 0.9초 동안 60px 상승·마지막 45% 페이드, 빨강 40px 굵게 (`DurabilityHud.cs`, `DurabilityHud.uss`) · kept 2026-11-17
- 적재 패널: 우상단 폭 340px, 배송지별 그룹(그룹명 = GameObject 이름), 폐기 물류는 "처분 장소" 그룹에 흐리게, 빈 패널 숨김, 유형 아이콘은 색 블록(Normal 파랑·Express 빨강), 가격 = 기본+거리+시간+특징 보상 합, 내구도 올림 정수 (`DurabilityHud.cs`, `DurabilityHud.uss`) · kept 2026-11-17
- 폐기 알림: "물류가 폐기되었습니다 — 처분 장소로 이동", 2.5초, 화면 위쪽 가운데, 빨간 테두리 (`DurabilityHud.cs`, `DurabilityHud.uss`) · kept 2026-11-17
- HudPanelSettings를 스토리 013과 공유, 맑은 고딕 OS 동적 폰트(Windows 전용), 문자열 코드 직접 기입, 팝업 애니메이션 끄기 옵션 없음 (`Assets/Game/UI/Hud/`) · kept 2026-11-17
