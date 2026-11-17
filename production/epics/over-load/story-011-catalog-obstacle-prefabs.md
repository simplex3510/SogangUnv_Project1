# Story 011: 카탈로그 정적 오브젝트 프리팹화

> **Epic**: over-load
> **Status**: Complete
> **Layer**: Feature
> **Type**: Integration
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-17

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature (주행 방해 오브젝트). 상세 규칙: `design/quick-specs/world-objects-catalog-2026-11-16.md` 2장

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating for 6.6)
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From the catalog 2장 and its Acceptance Criteria.*

- [ ] OBJ-CONE, OBJ-BOX, OBJ-DUMPSTER, OBJ-DEBRIS-S, OBJ-DEBRIS-L, OBJ-ROADWORK, OBJ-FIXTURE를 Story 010 컴포넌트 조합만으로 프리팹화한다. 물리 형태·트리거·효과·수명·질량은 카탈로그 2장 표와 같다
- [ ] CONE은 충돌하면 `cone-flat` 메시로 바뀌고, 최고 속도 ×0.8 1초가 적용되며, 10초 후 사라진다
- [ ] DEBRIS-S는 진입 시 진행 방향을 ±15° 틀고, DEBRIS-L은 최고 속도 ×0.6 2초를 적용한다. 둘 다 즉시 사라지며 내구도는 변하지 않는다
- [ ] ROADWORK의 `construction-barrier`를 3초 상호작용하면 같은 그룹의 차단물이 모두 사라진다
- [ ] 머리 위 모델(`electricity-wires*`, `road-sign-empty-hanging`)은 콜라이더가 없고, 표에 없는 정적 후보는 효과 없는 고정 콜라이더다
- [ ] `Assets/Game/Prefabs/Props/` 6종(Cone, Box, Dumpster, Fence, StreetLight, Tree)이 카탈로그 정의와 일치한다
- [ ] 고속 정면 충돌 시 내구도 감소량이 카탈로그 "내구도" 열의 ±20% 안에 들어온다(Play mode 로그). 벗어나면 카탈로그 수치를 실측값으로 갱신한다
- [ ] 각 오브젝트 동작(콘 쓰러짐, 잔해 효과, 공사 차단 개방)의 스크린샷을 `production/qa/evidence/`에 보관한다

---

## Implementation Notes

- 물리 재질의 반발 계수는 0이다(`collision-durability-rules.md` 3장).
- 프리팹·씬 파일은 Editor가 열려 있을 때 직접 편집하지 않고 Unity CLI로 다룬다.
- 오브젝트 프리팹을 MapPainter `Props` 카테고리에 등록해서 레벨 배치(Story 001)에 쓸 수 있게 한다.

---

## Out of Scope

- 레벨 배치 (Story 001)
- 최종 수치 확정 (Story 003)

---

## Files

- `Assets/Game/Scripts/WorldObjects/SwapMeshEffect.cs`
- `Assets/Game/Scripts/Player/PlayerCargoImpactController.cs`
- `Assets/Game/Scripts/Player/CargoImpactConfig.cs`
- `Assets/Game/Editor/ObstaclePrefabBuilder.cs`
- `Assets/Game/Configs/Obstacles/ObstacleCone.asset`
- `Assets/Game/Configs/Obstacles/ObstacleBox.asset`
- `Assets/Game/Configs/Obstacles/ObstacleDebrisS.asset`
- `Assets/Game/Configs/Obstacles/ObstacleDebrisL.asset`
- `Assets/Game/PhysicsMaterials/ObstacleNoBounce.asset`
- `Assets/Game/Prefabs/Props/Cone.prefab`
- `Assets/Game/Prefabs/Props/Box.prefab`
- `Assets/Game/Prefabs/Props/Dumpster.prefab`
- `Assets/Game/Prefabs/Props/DebrisS.prefab`
- `Assets/Game/Prefabs/Props/DebrisL.prefab`
- `Assets/Game/Prefabs/Props/Roadwork.prefab`
- `Assets/Game/Prefabs/Props/RoadSign.prefab`
- `Assets/Game/Prefabs/Props/OverheadWires.prefab`

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: Test: waived at qa.level minimal. 오브젝트 동작의 retained screenshot (`production/qa/evidence/`)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/over-load/story-010-obstacle-modules.md`, `production/epics/over-load/story-004-collision-durability.md`
- Unlocks: `production/epics/over-load/story-001-map-level-wiring.md` (배치 가능한 방해물), `production/epics/over-load/story-003-balancing.md`

---

## Completion Notes
**Completed**: 2026-11-17
**Criteria**: 8/8 passing (AC-4 키 입력 대신 `TickInteraction` 호출로 확인, AC-5 `road-sign-empty-hanging` 전용 프리팹 없음 — 둘 다 사용자가 통과로 인정)
**Deviations**: 충돌 Δv 판정을 수평 속도 변화(판정 창 0.06초)로 바꾸면서 Story 004 코드(`PlayerCargoImpactController.cs`, `CargoImpactConfig.cs`)와 설계 문서(`collision-durability-rules.md`, `game-concept.md`)를 고쳤다(범위 밖, 사용자 위임). `PlayerCargoImpactController.cs` 작업 트리 변경에 Story 006의 `ImpactDamageApplied`(미커밋)가 섞여 있다. AC-7: Dumpster 실측 −26(계산값 −9)이라 카탈로그를 실측값으로 갱신했다. 원인은 질량 중심 아래 접촉과 Patch 마찰 2배(Improved Patch Friction 꺼짐)로 추정하며, 마찰 설정은 그대로 둔다.
**Test Evidence**: waived at qa.level minimal. 스크린샷 8장과 Play 모드 관찰 기록은 `production/qa/evidence/story-011-catalog-obstacle-prefabs/`
**Reviewed by**: self, no independent review
**알려진 문제 (미해결)**: Box Δv 1.41로 하한선 1.5까지 여유 0.09, DebrisL 타이어가 도로에 반쯤 묻힘, 도로 타일 경계(z≈−25.5)에서 차량이 튀고 속도 1.6 손실, 상호작용 진행 막대가 속도계 뒤에 겹침(Story 011 범위 밖)

## Calls beyond the design
- `SwapMeshEffect` 컴포넌트 추가(`Assets/Game/Scripts/WorldObjects/SwapMeshEffect.cs`) · kept 2026-11-17
- Cone 모델을 `Vehicles/cone.fbx`로 교체(`Assets/Game/Prefabs/Props/Cone.prefab`) · kept 2026-11-17
- 킷별 배율 Vehicles 0.25·Roads 1.875, Box 0.25(`Assets/Game/Editor/ObstaclePrefabBuilder.cs`) · kept 2026-11-17
- Roadwork 부품을 긴 축으로 이어 붙이고 상호작용 여백 0.4(`ObstaclePrefabBuilder.cs`) · kept 2026-11-17
- Cone·Dumpster 메시 콜라이더 convex(`ObstaclePrefabBuilder.cs`, 프리팹) · kept 2026-11-17
- 대표 모델 debris-bolt·debris-tire·road-sign-stop·electricity-wires, Debris 트리거 여백 없음(`ObstaclePrefabBuilder.cs`) · kept 2026-11-17
- 충돌 Δv를 수평 속도 변화(판정 창 0.06초)로 변경(`PlayerCargoImpactController.cs`, `CargoImpactConfig.cs`) · kept 2026-11-17
- Dumpster 카탈로그 −9 → −26(`design/quick-specs/world-objects-catalog-2026-11-16.md`) · kept 2026-11-17
