# Story 012: NPC 차량 골격

> **Epic**: over-load
> **Status**: Ready
> **Layer**: Feature
> **Type**: Integration
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature (주행 방해 오브젝트 — NPC 구조). 상세 규칙: `design/quick-specs/world-obstacle-objects-framework-2026-11-16.md` R5·R6, `design/quick-specs/world-objects-catalog-2026-11-16.md` 3장

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating for 6.6)
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From the framework spec R5·R6 and the catalog 3장.*

- [ ] NPC 차량은 동적(비 키네마틱) Rigidbody로 움직인다. 체급은 NPC-CAR(질량 100, 9종)와 NPC-HEAVY(질량 250, 4종)이며, 카탈로그 3장의 모델 목록과 같다
- [ ] NPC는 배치된 웨이포인트를 따라 설정 속도(`TODO(tuning)`)로 주행한다. 경로와 스폰 수는 레벨 기획에서 정한다
- [ ] NPC에 효과 컴포넌트(Story 010)를 붙이면 에디터 검증이 경고한다
- [ ] 하루 시작 시 활성화되고, 하루 종료 시 조작 잠금과 함께 정지한다
- [ ] 충돌 시 플레이어 차량이 물리적으로 밀린다. 내구도는 플레이어의 Δv 판정으로만 줄고, NPC 쪽 직접 페널티는 없다
- [ ] 정지한 NPC에 고속 정면 충돌할 때 내구도 감소량이 카탈로그 3장 값(CAR −12, HEAVY −25)의 ±20% 안에 들어온다. 벗어나면 카탈로그를 실측값으로 갱신한다
- [ ] NPC 주행과 충돌 장면의 스크린샷을 `production/qa/evidence/`에 보관한다

---

## Implementation Notes

- 이동은 키네마틱 `MovePosition`이 아니라 힘/속도로 구동한다. 키네마틱은 무한 질량이라 벽과 같아진다(`collision-durability-rules.md` 4장 2번).
- 웨이포인트 추종은 최소 구현이다. 회피, 신호, 차선 규칙은 넣지 않는다.
- 하루 시작·종료 연결은 `DailyOperationSystem`의 기존 이벤트를 쓴다.

---

## Out of Scope

- 보행자 NPC (리소스 없음, 카탈로그 4장)
- 교통 AI(회피·신호·차선)
- 경로 설계와 스폰 수 (Story 001 레벨 기획)

---

## Files

- 신규: `Assets/Game/Scripts/WorldObjects/NpcVehicle.cs`, NPC 데이터 SO
- 신규: NPC 차량 프리팹(`Assets/Game/Prefabs/`)
- [test root: Assets/Tests/PlayMode/ — 면제]

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: Test: waived at qa.level minimal. NPC 주행·충돌의 retained screenshot (`production/qa/evidence/`)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/over-load/story-004-collision-durability.md`, `production/epics/over-load/story-010-obstacle-modules.md` (에디터 검증 대상 컴포넌트)
- Unlocks: `production/epics/over-load/story-001-map-level-wiring.md` (NPC 경로 배치)
