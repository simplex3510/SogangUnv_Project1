# Story 004: 충돌 내구도 감소

> **Epic**: over-load
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-16

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 10 (충돌 내구도 감소와 피드백). 상세 규칙: `design/gdd/game-concept.md` 6.3·6.4·10.2, `design/gdd/collision-durability-rules.md` 2~4장

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating for 6.6)
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/gdd/game-concept.md` 10.2 (2026-11-16 확정).*

- [ ] 물류가 현재 내구도를 가진다. 생성 시 초기 내구도(`CargoSpec.Durability`)에서 시작하고, 초기 내구도 스냅샷은 바뀌지 않는다
- [ ] 플레이어 차량의 충돌 시작 시점에 `Δv = 충격량 크기 ÷ 차량 질량`을 계산하고, `damage = min((Δv − 1.5) × 12, 60)`을 적용한다. `Δv < 1.5`이면 감소하지 않는다
- [ ] 적재된 모든 물류에 같은 `damage`를 적용하고, Fragile 물류만 `× 1.5`를 곱한다. 특징이 여럿이어도 Fragile 배율만 적용한다
- [ ] 감소 후 0.5초 쿨다운 동안 추가 감소가 없다. 접촉 유지(밀어붙이기) 중에는 감소하지 않는다
- [ ] 지면 접촉·착지 충격(접촉 법선이 지면 판정 기준 이상)은 감소시키지 않는다
- [ ] 현재 내구도는 0 아래로 내려가지 않는다. 0 도달 시 폐기 전환 이벤트를 낸다(전환 처리 자체는 Story 005)
- [ ] `CargoTraitConfig`의 Fragile 내구도 감산값을 50에서 0으로 바꾸고, Fragile 감소 배율 필드(1.5)를 추가한다
- [ ] 하한선·계수·1회 상한·쿨다운·배율은 설정 데이터에 `TODO(tuning)`으로 둔다. 코드에 하드코딩하지 않는다
- [ ] 감소 판정을 `[CargoImpact]` 로그로 남기고, Play mode에서 벽 충돌 시 일반 물류와 Fragile의 감소량을 확인한다

---

## Implementation Notes

- 감소량 계산(Δv, 설정값 → 감소량)은 물리 콜백과 분리한 순수 함수로 둔다(`collision-durability-rules.md` 4장 3번).
- 내구도 단일 책임: 월드 오브젝트·NPC 코드는 내구도를 참조하지 않는다(`design/quick-specs/world-obstacle-objects-framework-2026-11-16.md` R1).
- 반발 계수 기준은 0이다. 차량·맵 물리 재질의 반발 계수를 확인한다.
- 수치 최종값은 Story 003(밸런싱)에서 실측으로 조정한다.
- **Performance**: no performance impact expected — 감소 계산은 충돌 시작 콜백에서만 1회 실행하고, 적재 물류 수(한도 내 소수)만큼 반복한다.

---

## Out of Scope

- Story 005: 폐기 물류 전환·처분 장소
- Story 006: 내구도 피드백 UI
- 남은 내구도의 보상 반영 (반영하지 않기로 확정)

---

## Files

- `Assets/Game/Scripts/Player/PlayerCargoImpactController.cs` (신규)
- `Assets/Game/Scripts/Player/CargoImpactConfig.cs` (신규)
- `Assets/Game/Configs/CargoImpactConfig.asset` (신규)
- `Assets/Game/Scripts/Logistics/Parcel.cs`
- `Assets/Game/Scripts/Logistics/CargoTraitConfig.cs`
- `Assets/Game/Configs/CargoTraitConfig.asset`
- `Assets/Game/Prefabs/Delivery.prefab`
- [test root: Assets/Tests/EditMode/ — 면제]

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: Test: waived at qa.level minimal

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None
- Unlocks: `production/epics/over-load/story-005-spoiled-cargo-disposal.md`, `production/epics/over-load/story-006-durability-feedback-ui.md`, `production/epics/over-load/story-003-balancing.md` (수치 조정)

## Completion Notes
**Completed**: 2026-11-16
**Criteria**: 8/9 passing (DEFERRED: 0 도달 시 `ParcelDepleted` 이벤트 발생 관찰 — Story 005 구독 시 함께 확인)
**Deviations**: 착지 판정의 접촉 법선 방향 미확인, `contactCount` 0이면 지면 처리, SampleMapDesign 콜라이더 3개 반발 계수 미확인, `[CargoImpact]` 로그가 빌드에 남음(튜닝 후 `#if UNITY_EDITOR`로 감쌀 것)
**Test Evidence**: waived at qa.level minimal (Play 관찰 기록: `production/qa/evidence/story-004-collision-durability/play-impact-log.md`)
**Reviewed by**: self, no independent review

## Calls beyond the design
- 충격량을 OnCollisionEnter에서 모아 다음 FixedUpdate에 한 번 판정(1스텝 지연), `PlayerCargoImpactController.cs` · kept 2026-11-16
- 같은 스텝 충격량은 벡터 합산, `PlayerCargoImpactController.cs` · kept 2026-11-16
- 모든 접촉 법선이 지면 기준 이상일 때만 지면 접촉으로 인정, `IsGroundContact` · kept 2026-11-16
- 쿨다운은 실제 감소가 일어난 뒤에만 시작(하한선 미만은 시작 안 함), `ResolveImpact` · kept 2026-11-16
- 적재 물류 0개여도 판정·쿨다운 진행, `ResolveImpact` · kept 2026-11-16
- 이미 내구도 0인 물류는 건너뜀, `DamageParcel` · kept 2026-11-16
- `ParcelDepleted`를 목록 순회 중 즉시 발생(Story 005에서 처리 시점 결정 필요), `ResolveImpact` · kept 2026-11-16
- 하한선 미만 로그 문구 "→ 무시"는 신규, `ResolveImpact` · kept 2026-11-16
- Fragile 배율 최소값 `Min(1)`, `CargoTraitConfig.cs` · kept 2026-11-16
- 설정이 하나라도 비면 에러를 남기고 컴포넌트를 끔, `PlayerCargoImpactController.cs` · kept 2026-11-16
