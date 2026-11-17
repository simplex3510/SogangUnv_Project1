# Story 017: 방향 안내를 적재 물류 패널로 이전

> **Epic**: over-load
> **Status**: Complete
> **Layer**: Presentation
> **Type**: UI
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-17

## Context

**GDD**: `design/gdd/game-concept.md` 13장
**Requirement**: 상세 규칙: `design/quick-specs/hud-layout-2026-11-17.md` (방향 화살표)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating for 6.6)
**Engine Notes**: UI Toolkit `VisualElement` 회전(`style.rotate`)으로 화살표 방향 표시.

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/quick-specs/hud-layout-2026-11-17.md` (2026-11-17 확정; 거리 표시·cursor-fill 아이콘·접힘 표시 제거는 2026-11-17 사용자 요청으로 추가).*

- [ ] 상단 중앙 나침반 패널이 화면에서 사라진다
- [ ] 우상단 열은 위에서부터 물류센터 패널, 배송 현황 패널, 폐기 패널의 세 패널로 나뉜다(2026-11-17 사용자 요청). 물류센터 패널과 파란 화살표는 적재 물류가 없어도 항상 보인다
- [ ] 배송 현황 패널은 배송지 그룹이 없으면, 폐기 패널은 폐기 물류가 없으면 숨는다. 폐기 그룹의 숫자 키 번호는 항상 마지막이다
- [ ] 세 패널의 나침반 배지와 거리 열은 같은 x 위치에 정렬된다
- [ ] 배송지 그룹과 처분 장소 그룹 헤더의 가장 왼쪽에 원형 나침반 배지가 있고, 배지 안 화살표가 그 목적지를 가리킨다. 배지 테두리는 고정이고 화살표만 돈다
- [ ] 화살표는 차량 전방을 위로 하여 목적지 방향으로 회전하고, 주행 중 매 프레임 갱신된다
- [ ] 행 오른쪽 끝 열에 목적지까지의 수평 거리가 정수 미터(`24m`)로 오른쪽 정렬되어 표시되고, 주행 중 매 프레임 갱신된다
- [ ] 화살표 그림은 Bootstrap Icons `cursor-fill` SVG다(임시 GUI)
- [ ] 그룹 헤더에 접힘 표시(`▶`/`▼`)가 없다. 숫자 키 번호는 이름 앞 키캡 모양이며 접기/펼치기 동작은 그대로다
- [ ] 그룹 번호 키(Story 014)는 배송지·처분 장소 그룹에만 붙고 `물류센터` 행에는 없다
- [ ] No regression: 접기/펼치기·스크롤(Story 014), 내구도 표시·폐기 알림(Story 006)
- [ ] GUI는 임시다. 빈 패널(물류센터 행만), 여러 그룹, 처분 장소 그룹 스크린샷을 `production/qa/evidence/`에 보관한다

---

## Implementation Notes

- 방향 계산은 기존 `DeliveryNavigationCompass`의 목적지 수집 로직을 재사용한다. 패널을 다시 그릴 때 화살표 요소를 만들고, 회전만 매 프레임 바꾼다(패널 전체를 매 프레임 다시 그리지 않는다).
- 회전각 = 차량 전방과 목적지 방향의 수평면 부호 있는 각도.
- `DeliveryNavigationCompass`와 씬의 상단 패널 오브젝트를 제거한다.

---

## Out of Scope

- 미니맵
- 최종 GUI 디자인 (첫 릴리즈 전 임시 단계)

---

## Files

- `Assets/Game/Scripts/UI/DurabilityHud.cs`
- `Assets/Game/UI/Hud/DurabilityHud.uss`
- `Assets/Game/UI/Hud/DurabilityHud.uxml`
- `Assets/Game/UI/Icons/cursor-fill.svg` (Bootstrap Icons, MIT; 위쪽·무게중심 정렬)
- `Assets/Game/UI/Icons/box-seam-fill.svg` (Bootstrap Icons, MIT; 물류 유형 아이콘)
- `Assets/Game/Scripts/UI/DeliveryNavigationCompass.cs` (제거)
- `Assets/Game/Scenes/_DevOnly/SampleMapDesign.unity` (상단 나침반 오브젝트 제거)

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: UI
**Required evidence**: 빈 패널, 여러 그룹, 처분 장소 그룹의 retained screenshot (`production/qa/evidence/`)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/over-load/story-014-cargo-panel-fold-scroll.md` (같은 `DurabilityHud.cs`, 먼저 닫는다)
- Unlocks: None
- 영향: Story 008의 "나침반에 처리 순서 적용" 항목은 이 스토리 뒤에는 적재 패널 화살표를 뜻한다

## Completion Notes
**Completed**: 2026-11-17
**Criteria**: 11/12 passing (DEFERRED: 접기/펼치기·스크롤·내구도·폐기 알림 회귀 — 전체 QA 때 확인)
**Deviations**: ADVISORY — 화면 문구가 코드에 직접 적혀 있음(Story 020에서 정리); `04-arrow-pivot-headings.png`는 배지 이전 형태에서 찍은 피벗 확인용. OUT OF SCOPE — `DeliveryTargetParcelUI.cs`, `LogisticsCenterUI.cs`, `SettlementUI.cs`, `HudPanelSettings.asset`, `MalgunGothicDynamic.asset`, `Assets/Game/UI.meta`, `Hud.meta`, `Icons.meta`는 다른 스토리 작업
**Test Evidence**: `production/qa/evidence/story-017-compass-in-cargo-panel/` (01 빈 패널, 02 배송 현황, 03 폐기 패널, 04 방향별 피벗). 테스트는 `qa.level: minimal`이라 면제
**Reviewed by**: self, no independent review

## Calls beyond the design
- 우상단 열 폭 420px, 패널 간격 10px, 패널 테두리(물류센터 파랑, 폐기 rgb(85,88,95)) (`DurabilityHud.uss`) · kept 2026-11-17
- 나침반 배지 34px 원·화살표 20px, 거리 18px 오른쪽 정렬 정수 m(수평 거리) (`DurabilityHud.uss`, `DurabilityHud.cs`) · kept 2026-11-17
- 이름 22px, 숫자 키캡 22px, 개수 18px 회색 (`DurabilityHud.uss`) · kept 2026-11-17
- 물류 상자 아이콘 28px(Normal 노랑 rgb(222,175,95), Express 빨강, Fresh 초록, 폐기 rgb(85,88,95)), 가격 굵게, 내구도 한 줄 14px, 태그 13px, 간격 8px (`DurabilityHud.uss`) · kept 2026-11-17
- 목적지와 수평으로 겹치면 직전 각도 유지, 처분 장소 목적지는 `DisposalSite.Instance` (`DurabilityHud.cs`) · kept 2026-11-17
- `0`/`~`는 폐기 그룹까지 함께 열고 닫음 (`DurabilityHud.cs`) · kept 2026-11-17
- 물류센터·폐기 패널 좌우 여백 22px(열 정렬용), 폐기 패널 안 카드 테두리 제거 (`DurabilityHud.uss`) · kept 2026-11-17
