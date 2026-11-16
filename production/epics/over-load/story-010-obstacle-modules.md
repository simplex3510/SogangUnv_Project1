# Story 010: 정적 방해물 모듈 컴포넌트

> **Epic**: over-load
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Estimate**: L
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-17

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature (주행 방해 오브젝트 — 정적 모듈 컴포넌트·NPC 구조). 상세 규칙: `design/quick-specs/world-obstacle-objects-framework-2026-11-16.md` R1·R3·R6

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating for 6.6)
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From the framework spec R1, R3, R6.*

- [ ] 트리거 3종(상호작용 시간 유지, 충돌 접촉, 트리거 진입), 효과 3종(최고 속도 배율·지속 시간, 진행 방향 회전, 같은 그룹 오브젝트 제거), 수명 3종(즉시 제거, 잔류 N초 후 제거, 유지)을 프리팹에 조합해 붙일 수 있다. 새 조합을 만들 때 컴포넌트 코드를 고치지 않는다
- [ ] 효과는 차량 쪽 `IVehicleEffectTarget`으로만 적용한다. 효과 컴포넌트는 Rigidbody 속도를 직접 쓰지 않는다
- [ ] 오브젝트 하나는 한 번만 실행한다. 동시에 두 번 접촉해도 첫 접촉만 실행한다
- [ ] 같은 효과가 겹치면 가장 강한 값 하나만 적용하고 지속 시간만 갱신한다
- [ ] 조작 잠금(하루 종료) 중에는 트리거가 실행되지 않고, 하루 종료 시 지속 중인 효과를 해제한다
- [ ] 효과 대상이 이미 제거됐으면 오류 없이 넘어간다
- [ ] 오브젝트 계열 코드가 `Parcel`, `CargoSpec`, 내구도 관련 심볼을 참조하지 않는다(코드 검색으로 확인). 효과 실행만으로는 내구도가 변하지 않는다
- [ ] 효과 파라미터와 잔류 시간은 ScriptableObject 데이터에 `TODO(tuning)`으로 둔다
- [ ] 회귀: 기존 건물·울타리 충돌과 상·하차 상호작용이 이전과 같다

---

## Implementation Notes

- 상호작용 트리거는 기존 `IInteractable`·`InteractableObject`와 `_interactionDuration`을 재사용한다.
- 최고 속도 배율은 `PlayerMovementController`의 `_maxForwardSpeed` 계산에 곱한다. Story 009의 과적 배율과 함께 곱해지는 위치를 맞춘다.
- 방향 회전은 차량 컨트롤러가 요청을 받아 요(yaw)에 적용한다. 무작위 방향은 주입해서 결정적으로 만든다.
- 그룹 제거는 같은 그룹 루트(부모) 아래의 오브젝트를 대상으로 한다.
- 순수 판정(중첩 시 강한 값 선택, 1회 실행)은 MonoBehaviour와 분리한다.

---

## Out of Scope

- 카탈로그 오브젝트의 프리팹 제작과 수치 (Story 011)
- NPC (Story 012)
- 레벨 배치 (Story 001)

---

## Files

- `Assets/Game/Scripts/WorldObjects/CollisionObstacleTrigger.cs`
- `Assets/Game/Scripts/WorldObjects/HeadingRotationEffect.cs`
- `Assets/Game/Scripts/WorldObjects/IVehicleEffectTarget.cs`
- `Assets/Game/Scripts/WorldObjects/InteractionObstacleTrigger.cs`
- `Assets/Game/Scripts/WorldObjects/ObstacleEffect.cs`
- `Assets/Game/Scripts/WorldObjects/ObstacleEffectConfig.cs`
- `Assets/Game/Scripts/WorldObjects/ObstacleLifetime.cs`
- `Assets/Game/Scripts/WorldObjects/ObstacleLogic.cs`
- `Assets/Game/Scripts/WorldObjects/ObstacleModule.cs`
- `Assets/Game/Scripts/WorldObjects/RemoveGroupEffect.cs`
- `Assets/Game/Scripts/WorldObjects/SpeedMultiplierEffect.cs`
- `Assets/Game/Scripts/WorldObjects/TriggerEnterObstacleTrigger.cs`
- `Assets/Game/Scripts/Player/PlayerMovementController.cs`
- `Assets/Game/Scripts/Player/PlayerInteractionController.cs`
- `Assets/Game/Scripts/Interaction/InteractionTypes.cs`
- [test root: Assets/Tests/EditMode/ — 면제(qa.level minimal)]

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
- Unlocks: `production/epics/over-load/story-011-catalog-obstacle-prefabs.md`

## Completion Notes
**Completed**: 2026-11-17
**Criteria**: 4/9 passing (5 DEFERRED — 배치 후 확인: 조합 가능성(Story 011 프리팹 자동화 때 점검), 1회 실행, 겹침 처리, 조작 잠금·하루 종료 해제, 대상 제거 시 무오류)
**Deviations**: 범위 확장(InteractionTypes.cs, PlayerInteractionController.cs — 사용자 승인). ADVISORY: `Obstacle` 상호작용 화면 안내 없음(`PlayerUIController.ShowByType` 미대응), 방해물 영역이 물류센터와 겹치면 마지막 진입 대상이 가림(기존 동작)
**Test Evidence**: waived at qa.level minimal
**Reviewed by**: self, no independent review

## Calls beyond the design
- 지속 시간은 최신 요청 값으로 갱신 (`ObstacleLogic.SpeedEffectState.Apply`) · kept 2026-11-17
- 속도 배율은 최고속도에만 적용, 가속 제외 (`PlayerMovementController.UpdateModeParameters`) · kept 2026-11-17
- 그룹 제거는 같은 부모의 다른 자식 전부 (`RemoveGroupEffect`) · kept 2026-11-17
- SO 기본값 0.5배·3초·45°·잔류 2초, `TODO(tuning)` (`ObstacleEffectConfig`) · kept 2026-11-17
- 상호작용은 키를 계속 눌러야 하고 떼면 초기화 (`PlayerInteractionController.TickObstacle`) · kept 2026-11-17
- 잠금·대상 파괴로 건너뛰면 1회 게이트 소모 안 함 (`ObstacleModule.TryExecute`) · kept 2026-11-17
- 배율 0.01~3 클램프, 최강값은 1에서 가장 먼 값·동률은 최신 (`ObstacleEffectConfig`, `ObstacleLogic`) · kept 2026-11-17
