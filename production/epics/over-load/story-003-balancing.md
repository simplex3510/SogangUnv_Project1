# Story 003: 밸런싱 수치 확정과 마무리

> **Epic**: over-load
> **Status**: Deferred
> **Layer**: Feature
> **Type**: Config/Data
> **Estimate**: L
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature "밸런싱 수치 확정과 마무리" (Features 9번째 항목, dev-plan 8단계)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating for 6.6)
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` Features and `design/dev-plan.md` 8단계:*

- [ ] 기획서 16.4의 미결정 수치를 설정 데이터에 채운다
- [ ] 코드의 `// TODO(tuning)` 주석을 모두 해소한다
- [ ] Play 테스트로 하루 1회 완주해, 일반적인 플레이(배송 실패 1~2건)에서 최종 정산이 0 이상이고 별 1~2개가 나오는지 기록한다
- [ ] `game-concept.md` 12.4의 별점 경계값 `T1`·`T2`를 실측으로 조정하고 결과를 기록한다
- [ ] 스모크 점검 결과를 `production/qa/smoke-2026-11-16.md` 형식(날짜는 실행일)으로 남긴다

---

## Implementation Notes

- 수치는 코드가 아닌 `Assets/Game/Configs/`의 설정 에셋에 둔다 (게임플레이 값은 데이터 기반).
- 충돌 내구도 산식은 확정됐다(`game-concept.md` 10.2, 2026-11-16). 이 스토리에서는 `TODO(tuning)` 초기값만 실측으로 조정한다.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 001: 맵 배선과 1회 완주 가능 상태 확보
- Story 002: 별점 판정 로직과 UI

---

## Files

- `Assets/Game/Configs/*.asset` (수치 입력)
- `Assets/Game/Scripts/Logistics/*.cs` (`TODO(tuning)` 해소)

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Config/Data
**Required evidence**: smoke check pass — `production/qa/smoke-*.md` (qa.level minimal에서도 Config/Data는 스모크 기록).

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/over-load/story-001-map-level-wiring.md`, `production/epics/over-load/story-002-daily-star-rating.md`, `production/epics/over-load/story-004-collision-durability.md`, `production/epics/over-load/story-008-fresh-delivery-type.md`, `production/epics/over-load/story-009-overweight-loading.md`
- Unlocks: None
