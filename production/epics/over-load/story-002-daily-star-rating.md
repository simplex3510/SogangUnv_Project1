# Story 002: 하루 별점 피드백(3구간)

> **Epic**: over-load
> **Status**: Ready
> **Layer**: Feature
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 7 (하루 별점 피드백, 3구간)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating for 6.6)
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` Goal & fail state: 하루 결과는 별 0~3개, 발생한 물류 대비 조건부 성공 비율로 판정 (구간 산식·조건 미정).*

- [ ] 정산 시 하루에 발생한 물류 대비 조건부 성공 비율로 별 0~3개를 판정한다
- [ ] 성공 조건은 배송 형태별로 판정한다. Express는 제한 시간 내 완료까지 충족해야 성공이다
- [ ] 모든 물류가 형태별 조건을 충족해 배송 완료되면 별 3개다
- [ ] 구간 경계값과 형태별 성공 조건은 설정 데이터에 둔다. 코드에 하드코딩하지 않는다 (수치·조건은 미정)
- [ ] 발생 물류가 0개인 날도 오류 없이 처리하고, 그 결과를 명시한다
- [ ] 정산 화면(임시 UI)에 별점이 표시되고, 스크린샷을 `production/qa/evidence/`에 보관한다

---

## Implementation Notes

- 현행 정산은 `SettlementSystem`(`FinalSettlement = DeliveryIncome - DeliveryFailurePenalty - LateDeliveryLoss`)이고 음수·하한 없음. 별점은 이 결과 위에 얹는다.
- 구간 산식과 형태별 성공 조건은 브리프에서 `INTENT UNKNOWN`. Story 003에서 확정하며, 이 스토리는 확정 없이도 만들 수 있게 데이터로 분리한다.
- 첫 릴리즈 전 개발 단계: GUI는 임시이고 기획 최종 업데이트로 이 스토리가 바뀔 수 있다.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 003: 구간 산식과 형태별 성공 조건의 최종 확정
- 일일 목표 수익 기준 또는 대체안 (브리프에서 검토 중, 이번 범위 아님)

---

## Files

- `Assets/Game/Scripts/Logistics/SettlementSystem.cs`
- `Assets/Game/Scripts/UI/SettlementUI.cs`
- [test root: Assets/Tests/EditMode/ — 면제]

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: Test: waived at qa.level minimal. 정산 화면의 retained screenshot은 `production/qa/evidence/`에 필수.

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/over-load/story-001-map-level-wiring.md`
- Unlocks: `production/epics/over-load/story-003-balancing.md`
