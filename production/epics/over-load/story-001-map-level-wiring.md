# Story 001: 맵 레벨 게임플레이 배선과 1회 완주

> **Epic**: over-load
> **Status**: Ready
> **Layer**: Feature
> **Type**: Integration
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 6 (맵 구성요소 프리팹과 레벨 디자인, dev-plan 7.5c)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating for 6.6)
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` (Goal & fail state, Features) and `design/dev-plan.md` 7.5c:*

- [ ] 새 맵 씬에서 플레이어 스폰, 물류센터, 배송지 5곳이 시스템·UI와 연결되어 있다
- [ ] 09:00~18:00 하루를 시작부터 정산 화면까지 1회 완주할 수 있다
- [ ] 물류센터~배송지 실측 거리를 기록하고, 제한 시간·보상 계수 조정 대상을 정리한다
- [ ] 도로·건물·장애물 충돌이 의도대로 동작하고, 차량이 맵 밖으로 빠지지 않는다
- [ ] 완주 장면의 스크린샷을 `production/qa/evidence/`에 보관한다

---

## Implementation Notes

- 7.5a 프리팹(`Assets/Game/Prefabs/Map/`)과 7.5b 레벨 배치는 완료 상태. 남은 것은 7.5c 배선.
- `.unity`·`.prefab`·`.meta`는 Editor가 열려 있을 때 직접 편집하지 않는다 (Unity CLI 사용).

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 002: 하루 별점 판정
- Story 003: 제한 시간·보상 계수의 최종 수치 확정

---

## Files

- `Assets/Game/Scenes/` (맵 씬 배선)
- `Assets/Game/Scripts/Manager/GameManager.cs` (필요 시)
- [test root: Assets/Tests — 면제]

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: 자동 테스트는 `qa.level: minimal`이라 면제. 플레이 가능한 장면의 retained screenshot은 `production/qa/evidence/`에 필수.

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None
- Unlocks: `production/epics/over-load/story-002-daily-star-rating.md`, `production/epics/over-load/story-003-balancing.md`
