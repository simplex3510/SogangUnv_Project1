# Story 014: 적재 물류 패널 접기/펼치기와 스크롤

> **Epic**: over-load
> **Status**: Complete
> **Layer**: Presentation
> **Type**: UI
> **Estimate**: S
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-17

## Context

**GDD**: `design/game-brief.md`
**Requirement**: 상세 규칙: `design/quick-specs/cargo-panel-fold-scroll-2026-11-17.md` (Story 006 적재 물류 패널 확장)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating for 6.6)
**Engine Notes**: UI Toolkit `ScrollView`(스크롤바 숨김). 휠·키는 Input System `Keyboard.current`/`Mouse.current`로 직접 읽는다.

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/quick-specs/cargo-panel-fold-scroll-2026-11-17.md` (2026-11-17 확정).*

- [x] 기본 상태에서 모든 배송지 그룹이 접혀 헤더(번호·이름·개수)만 보인다
- [x] 키보드 상단 숫자열 `1`~`9`가 패널 위에서부터 해당 번호 그룹만 토글하고, 없는 번호는 아무 일도 없다
- [x] `0`은 전체 열기, `~`(Backquote)는 전체 닫기. 숫자패드 키는 반응하지 않는다
- [x] 충돌·적재 변화로 패널이 다시 그려져도 펼침 상태가 유지된다(그룹 키 = 배송지 이름, 폐기 그룹 = "처분 장소")
- [x] 목록이 최대 높이(480px)를 넘으면 마우스 휠·PageUp/PageDown으로 스크롤되고, 접어서 내용이 줄면 스크롤 위치가 범위 안으로 돌아온다
- [x] 하루 종료(조작 잠금) 후 키 입력이 무시된다
- [x] 긴 배송지 이름은 말줄임되고 `×N` 개수는 항상 보인다
- [x] No regression: 폐기 알림, 감소량 −N 팝업, 물류별 내구도 갱신(Story 006)
- [x] GUI는 임시다. 각 상태의 스크린샷을 `production/qa/evidence/`에 보관한다

---

## Implementation Notes

- `DurabilityHud`에 펼침 상태(`HashSet<string>`)와 그룹 순서를 두고, 입력 시 `RefreshCargoPanel`로 다시 그린다. UI는 게임 상태를 바꾸지 않는다.
- 휠은 포인터 위치와 무관하게 처리하므로 `ScrollView` 자체 휠 처리를 막고(`contentViewport` picking ignore) `scrollOffset`을 직접 움직인다. 범위 제한은 ScrollView 스크롤러가 한다.
- 휠 값 크기는 플랫폼마다 달라 방향만 쓰고 한 칸 = `_scrollStep`(120px).
- 키는 `InputActionReference` 대신 `Keyboard.current`로 읽는다(코드에 `shortcut:` 주석). 키 재설정·게임패드가 필요해지면 액션 에셋으로 옮긴다.
- 최대 높이·스크롤 단위는 `[SerializeField]`(임시 GUI 단계).

---

## Out of Scope

- 게임패드 입력
- 키 재설정
- 최종 GUI 디자인 (첫 릴리즈 전 임시 단계)

---

## Files

- `Assets/Game/Scripts/UI/DurabilityHud.cs`
- `Assets/Game/UI/Hud/DurabilityHud.uxml` (`cargoList` ScrollView 추가)
- `Assets/Game/UI/Hud/DurabilityHud.uss` (그룹 헤더 제목·개수 분리)

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: UI
**Required evidence**: 각 상태의 retained screenshot (`production/qa/evidence/`)

**Status**: [x] `production/qa/evidence/story-014-cargo-panel-fold-scroll/` (01 기본 접힘, 02 `2`키로 2번 그룹 펼침, 03 `0` 전체 열기·높이 480 제한, 04 PageDown+휠 스크롤(240px), 05 `~` 전체 닫기 후 스크롤 0). 숫자패드 `1` 무시, 하루 종료 후 `~` 무시는 상태 조회로 확인. 테스트 중에만 Input System 포커스 설정을 바꿔 큐 입력을 Game view로 보냈고 끝난 뒤 되돌렸다.

---

## Dependencies

- Depends on: `production/epics/over-load/story-006-durability-feedback-ui.md` (적재 물류 패널)
- Unlocks: None

## Completion Notes
**Completed**: 2026-11-17
**Criteria**: 9/9 (AC-4·5·6·7·8은 DEFERRED — 플레이 때 확인; AC-1·2·3·9는 스크린샷·확인으로 통과)
**Deviations**: ADVISORY — 그룹 헤더의 ▶/▼ 아이콘은 제거하고 그 자리에 나침반 화살표를 둬야 함(사용자 요청). 현재 코드에는 아이콘이 남아 있고, 변경은 Story 017에서 처리하기로 함(수락)
**Test Evidence**: `production/qa/evidence/story-014-cargo-panel-fold-scroll/` (스크린샷 5장; 증거 스탬프 없음)
**Reviewed by**: self, no independent review

## Calls beyond the design
- 그룹 헤더 `[번호] 이름 ×개수` 형식, 자식 행 증가 즉시 반영(애니메이션 없음), 수량 0인 그룹은 사라지고 번호 재부여 (`DurabilityHud.cs`, `DurabilityHud.uss`) · kept 2026-11-17
- ▶/▼ 아이콘 · 제거하고 나침반 화살표로 대체하라는 요청, Story 017로 넘김 · changed 2026-11-17
