# Quick Design Spec: 월드 오브젝트 카탈로그 — 도로 위 방해물

**Type**: Content Catalog
**Scope**: `world-obstacle-objects-framework-2026-11-16.md`(이하 프레임워크)의 후속 작업 1(모델 전수조사)과 2(오브젝트별 기획). 도로 위 방해물만 개별 기획하고, 나머지는 일괄 규칙으로 처리한다. 배치는 레벨 디자인(7.5)에서 정한다.
**GDD Reference**: `design/game-brief.md`, `design/gdd/collision-durability-rules.md`, 프레임워크 스펙
**Date**: 2026-11-16

## Overview

`Assets/Resources/`의 FBX 224개(Industrial 37, Roads 95, Suburban 40, Vehicles 52)를 조사했다. 그 가운데 주행에 직접 닿는 방해물을 프레임워크 R3의 세 축(물리 형태, 트리거, 효과·수명)으로 정의한다. 방향은 다음과 같다. 저속 운전은 거의 무피해다. 고속으로 무거운 것을 치면 내구도 손해가 난다. 가벼운 소품은 내구도 대신 효과로 손해를 준다.

## 1. 모델 전수조사

### 1.1. 제외 (지형·건물·부품)

| 분류 | 모델 | 사유 |
|---|---|---|
| 노면·타일 | Roads `road-*`, `tile-*`, Suburban `path-*`, `driveway-*` | 지형 |
| 건물 | Industrial `building-a`~`t`, Suburban `building-type-a`~`u` | 기존 정적 충돌체·배송지 |
| 차량 부품 | `Wheel`, `wheel-*`, `delivery-Wheels` | 부품 |
| 플레이어 차량 | `delivery`, `delivery-flat` | 플레이어 |
| 톤 불일치 차량 | `kart-*`, `race`, `race-future`, `tractor`, `tractor-police`, `tractor-shovel` | 도심 배송 톤과 맞지 않음 |

### 1.2. 정적 후보

| 그룹 | 모델 |
|---|---|
| 공사 | `construction-barrier`, `construction-cone`, `construction-fence`, `construction-light` |
| 콘 | `cone`, `cone-flat` |
| 표지·신호 | `road-sign-*`(empty, empty-hanging, stop, street, warning 및 object 변형), `sign-highway*` 3, `traffic-light*` 5 |
| 가로 시설 | `light-curved*`·`light-square*` 6, `electricity-pole*`·`electricity-side*` 6, `electricity-wires*` 2, `bridge-pillar*` 2 |
| 노변 소품 | `dumpster`, `box`, `planter`, `tree-large`, `tree-small`, `fence` 10종 |
| 산업 소품 | `shipping-container-a/b/c`, `detail-tank(-large)`, `chimney-*` 4, `solar-panel-*` 5, `water-tower`, `windmill(-low)` |
| 잔해 | `debris-*` 14 |

### 1.3. 동적(NPC) 후보

`sedan`, `sedan-sports`, `hatchback-sports`, `suv`, `suv-luxury`, `taxi`, `van`, `police`, `ambulance`, `truck`, `truck-flat`, `garbage-truck`, `firetruck`

### 1.4. 조사 중 발견 사항

1. **보행자 모델이 없다.** 보행자 NPC는 리소스를 확보할 때까지 보류한다(4장).
2. `Assets/Game/Prefabs/Props/` 6종(Cone=`construction-cone`, Fence=`fence`, Dumpster=`dumpster`, StreetLight=`light-square`, Tree=`tree-large`, Box=`box`)은 모두 Rigidbody 없는 고정 콜라이더다. 아직 어느 씬에도 배치되지 않았다. 이 카탈로그대로 수정한다.

## 2. 정적 오브젝트

질량은 모두 `TODO(tuning)`이다. "내구도"는 고속(5 m/s)으로 정지한 대상에 정면 충돌할 때 플레이어 차량(질량 100)이 받는 감소량이다. 반발 계수 0 기준으로 `Δv = v·m/(100+m)`, 고정 대상은 `Δv = v`로 계산했다(`collision-durability-rules.md` 2·3장). 효과 자체로는 내구도가 줄지 않는다(프레임워크 R1).

| ID | 모델 | 물리 형태 | 트리거 | 효과 | 수명 | 질량 | 내구도 (고속 / 일반) |
|---|---|---|---|---|---|---|---|
| OBJ-CONE | `cone`, `construction-cone` | 비고정 RB | 충돌 접촉 | 최고 속도 ×0.8, 1초 | 잔류 10초 후 제거 | 5 | 0 / 0 |
| OBJ-BOX | `box` | 비고정 RB | 충돌 접촉 | 없음 | 잔류 15초 후 제거 | 15 | 0 / 0 |
| OBJ-DUMPSTER | `dumpster` | 비고정 RB | 없음 | 없음 | 유지 | 80 | −26 / 미측정 (실측 2026-11-17, 계산값 −9) |
| OBJ-DEBRIS-S | `debris-bolt`, `debris-nut`, `debris-plate-small-a/b` | `isTrigger` | 트리거 진입 | 진행 방향 ±15° 틀기(방향 무작위) | 즉시 제거 | — | 0 / 0 |
| OBJ-DEBRIS-L | `debris-tire`, `debris-bumper`, `debris-door`, `debris-door-window`, `debris-plate-a/b`, `debris-spoiler-a/b`, `debris-drivetrain`, `debris-drivetrain-axle` | `isTrigger` | 트리거 진입 | 최고 속도 ×0.6, 2초 | 즉시 제거 | — | 0 / 0 |
| OBJ-ROADWORK | `construction-barrier`, `construction-fence`, `construction-light` | 고정 | 상호작용 3초(`construction-barrier`만) | 같은 그룹의 차단물 모두 제거(길 개방) | 즉시 제거 | — | −42 / −18 |
| OBJ-FIXTURE | `road-sign-*`, `traffic-light*`, `light-*`, `electricity-pole*`, `electricity-side*`, `sign-highway*`, `bridge-pillar*` | 고정 | 없음 | 없음 | 유지 | — | −42 / −18 |

### 2.1. 세부 규칙

1. **OBJ-CONE:** 충돌하면 메시를 `cone-flat`으로 바꿔 쓰러진 모습을 보여 준다. 연출이며 효과가 아니다.
2. **OBJ-DEBRIS-S 방향 틀기:** ±는 실행 시 무작위로 정한다. 테스트는 방향 값을 주입해 결정적으로 만든다.
3. **OBJ-ROADWORK 그룹:** 같은 부모(그룹 루트) 아래의 차단물을 한 그룹으로 본다. 상호작용 시간은 기존 `_interactionDuration`을 쓴다. 개방은 그룹당 한 번이다(프레임워크 R3 "한 번만 실행").
4. **효과 중첩:** CONE과 DEBRIS-L의 감속이 겹치면 더 강한 ×0.6을 적용하고 지속 시간만 갱신한다(프레임워크 R3).
5. **OBJ-DUMPSTER 실측 (2026-11-17, Story 011):** 고속 정면 충돌 실측은 −25 ~ −28이다. 계산값 −9의 약 3배이고, 콜라이더 구성(메시 3개, 루트 박스, `Model` 박스 1개)과 관계없이 같다. 원인은 두 가지가 겹친 것이다.
   - 차량 앞쪽 아래(도로 위 0.03 ~ 0.18 m)가 Dumpster의 질량 중심(0.19 m)보다 아래를 민다. Dumpster가 넘어지려 하고, 도로가 이를 막느라 수직 충격량이 자중의 6 ~ 8배가 된다.
   - 도로 마찰이 설정값(0.6)의 2배로 작동한다. 실측 비율은 2.00 ~ 2.03이다. 프로젝트 물리 설정이 Patch 마찰이고 Improved Patch Friction이 꺼져 있기 때문으로 보인다(설정을 켜서 비교하지는 않았다).
   - 그 결과 들어온 충격의 50 ~ 70%가 바닥 마찰로 사라진다. Dumpster는 0.9 ~ 1.4 m/s만 밀려 나가고, 차량은 판정 창 내내 감속한다.

   비고정 상대 전반(NPC 차량 포함)이 같은 영향을 받는다. 마찰 설정을 바꾸면 이 값을 다시 잰다(Story 003). 원본 로그: `production/qa/evidence/story-011-catalog-obstacle-prefabs/play-observation.md`.
6. **일괄 처리:** 표에 없는 정적 후보(`fence*`, `planter`, `tree-*`, 산업 소품)는 효과 없는 고정 콜라이더로 둔다. `electricity-wires*`와 `road-sign-empty-hanging`처럼 머리 위에 있는 모델은 콜라이더를 두지 않는다.

## 3. 동적 오브젝트 (NPC 차량)

프레임워크 R5에 따라 효과 컴포넌트는 없고, 동적(비 키네마틱) Rigidbody로 둔다.

| ID | 모델 | 질량 | 내구도 (정지 NPC에 고속 / 일반 정면 충돌) |
|---|---|---|---|
| NPC-CAR | `sedan`, `sedan-sports`, `hatchback-sports`, `suv`, `suv-luxury`, `taxi`, `van`, `police`, `ambulance` | 100 | −12 / 0 |
| NPC-HEAVY | `truck`, `truck-flat`, `garbage-truck`, `firetruck` | 250 | −25 / −8 |

- 마주 오는 NPC와 충돌하면 상대 속도가 더해져 위 값보다 크다. 실측 후 질량을 조정한다.
- 이동 규칙(경로, 속도, 스폰 수)은 레벨 기획(7.5)에서 정한다.

## 4. 보류

| 항목 | 상태 |
|---|---|
| 보행자 NPC | 리소스 필요. 모델을 확보하면 이 카탈로그에 체급을 추가한다. 규칙은 프레임워크 R5를 따른다. |

## Tuning Knobs

모두 `TODO(tuning)`이다. 위치는 프레임워크의 Tuning Knobs 표를 따른다.

| Knob | 초기값 | 근거 |
|---|---|---|
| CONE 감속 | ×0.8, 1초 | 가볍게 치고 지나가도 되지만 손해가 있음 |
| DEBRIS-L 감속 | ×0.6, 2초 | 피해야 할 낙하물 |
| DEBRIS-S 방향 틀기 | ±15° | 진로를 흔들지만 벽에 박을 정도는 아님 |
| CONE/BOX 잔류 | 10초 / 15초 | 치운 흔적을 잠시 남김 |
| ROADWORK 상호작용 | 3초 | 우회 대 개방의 선택 |
| 질량 | CONE 5, BOX 15, DUMPSTER 80, NPC-CAR 100, NPC-HEAVY 250 | 2장 내구도 열 |

## Acceptance Criteria

- [ ] 2장과 3장의 각 ID를 프레임워크 컴포넌트 조합만으로 프리팹화할 수 있다.
- [ ] 고속 정면 충돌 시 2·3장 "내구도" 열과 같은 감소량이 Play mode 로그에 나온다(±20%, 실측 후 갱신).
- [ ] CONE 충돌 시 `cone-flat`으로 바뀌고 10초 후 사라진다.
- [ ] DEBRIS 진입 시 효과가 한 번 실행되고 내구도는 변하지 않는다.
- [ ] ROADWORK 상호작용 3초 후 같은 그룹의 차단물이 모두 사라진다.
- [ ] `Prefabs/Props/` 6종이 이 카탈로그 정의와 일치한다.
