# Story 016: 오늘 수익 HUD

> **Epic**: over-load
> **Status**: Complete
> **Layer**: Presentation
> **Type**: UI
> **Estimate**: S
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-17

## Context

**GDD**: `design/gdd/game-concept.md` 11·13장
**Requirement**: 상세 규칙: `design/quick-specs/hud-layout-2026-11-17.md` (오늘 수익)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating for 6.6)
**Engine Notes**: UI Toolkit, Story 015의 좌상단 HUD 블록 3행(시각·일차 아래).

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/quick-specs/hud-layout-2026-11-17.md` (2026-11-17 확정).*

- [ ] 좌상단 일차 아래 줄에 오늘 수익을 정수로 표시한다
- [ ] 하루 시작 시 0이다
- [ ] 배송 완료 시점에 그 물류의 보상(기본 + 거리 + 시간 추가 + 특징) − 시간 초과 손실만큼 바뀐다
- [ ] 폐기·미배송 실패 페널티는 HUD에 넣지 않는다. 정산 화면 최종 금액과의 차이는 실패 페널티 합계와 같다
- [ ] 음수면 경고색(정산 화면 손실색)으로 표시한다
- [ ] 정산 화면이 열리면 숨긴다
- [ ] GUI는 임시다. 0, 양수, 음수(가능하면) 상태 스크린샷을 `production/qa/evidence/`에 보관한다

---

## Implementation Notes

- 금액 계산은 `SettlementSystem`이 한다. 배송 완료 물류 목록(`_deliveredParcels`)의 보상 − 시간 초과 손실 합계를 읽기 전용 값과 변경 이벤트로 노출한다. 정산 계산과 같은 항목 정의를 써서 값이 어긋나지 않게 한다.
- UI는 금액을 계산하지 않고 표시만 한다.

---

## Out of Scope

- 날짜를 넘어 유지되는 누적 재화와 사용처 (11.5, 후속 마일스톤)
- 금액 변화 연출(팝업·애니메이션)
- 최종 GUI 디자인 (첫 릴리즈 전 임시 단계)

---

## Files

- `Assets/Game/Scripts/Logistics/SettlementSystem.cs`
- `Assets/Game/UI/Hud/StatusHud.cs`
- `Assets/Game/UI/Hud/StatusHud.uxml`
- `Assets/Game/UI/Hud/StatusHud.uss`

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: UI
**Required evidence**: 0·양수·음수 상태와 정산 화면에서 숨김 retained screenshot (`production/qa/evidence/`)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/over-load/story-015-day-clock-hud.md` (좌상단 HUD 블록)
- Unlocks: None

## Completion Notes
**Completed**: 2026-11-17
**Criteria**: 7/7 passing (none deferred)
**Deviations**: None. 작업 트리의 다른 진행 작업(Story 008 등) 변경은 이 스토리 범위 밖(참고)
**Test Evidence**: waived at qa.level minimal; retained screenshots `production/qa/evidence/story-016-today-income-hud/` (01~07)
**Reviewed by**: self, no independent review
**Note**: 수익 패널은 일차·시각 패널과 별도 패널로 분리했고 왼쪽에 아이콘 칸을 두었다(StatusHud `_incomeIcon`에 Sprite 지정). 양수·음수 상태는 테스트 물류를 배송 완료 처리 함수에 넣어 확인했다.

## Calls beyond the design
- 하루 리셋 처리 없음, 씬 시작 시 0, 이벤트는 배송 완료 때만 (`SettlementSystem.cs`) · kept 2026-11-17
- 금액 숫자만 표시, 22px 흰색, 반올림, 0은 흰색 (`StatusHud.cs`, `StatusHud.uss`) · kept 2026-11-17
- 아이콘 칸 28×28px, 미지정 시 옅은 테두리의 빈 칸 (`StatusHud.uss`) · kept 2026-11-17
