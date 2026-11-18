# Story 001: 맵 레벨 게임플레이 배선과 1회 완주

> **Epic**: over-load
> **Status**: Deferred
> **Deferred**: 2026-11-16 — 추가·변경된 기획(배선 대상 씬 미정 등)을 반영해 판단하지 못해 순서를 뒤로 미룸. 재개 전에 최신 기획으로 내용을 다시 검토하고 Status를 Ready로 되돌린다.
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
- [ ] 물류센터~배송지 5곳의 실측 거리를 `production/qa/evidence/story-001-distances.md`에 표로 기록하고, 조정이 필요한 제한 시간·보상 계수 항목을 같은 파일에 나열한다
- [ ] 차량이 도로·건물·장애물 콜라이더를 통과하지 않는다(고속 모드 최고 속도로 종류별 1회씩 충돌해 확인). 맵 경계 4면 어디서도 차량이 맵 밖으로 나가지 않는다
- [ ] 완주 장면의 스크린샷을 `production/qa/evidence/`에 보관한다

---

## Implementation Notes

- 7.5a 프리팹(`Assets/Game/Prefabs/Map/`)과 7.5b 레벨 배치는 완료 상태. 남은 것은 7.5c 배선.
- `.unity`·`.prefab`·`.meta`는 Editor가 열려 있을 때 직접 편집하지 않는다 (Unity CLI 사용).
- **Performance**: 목표 60fps(Editor Play mode). 배선 완료 후 Profiler로 하루 1회 완주 중 프레임을 확인하고, 결과를 distances 파일에 한 줄로 남긴다.

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
- Unlocks: `production/epics/over-load/story-003-balancing.md`
