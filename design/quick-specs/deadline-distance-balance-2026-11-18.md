# Quick Design Spec: 제한 시간·신선도 거리 항 밸런싱

**Type**: Addition
**System**: 배송 제한 시간 / 신선도 (Delivery Deadline)
**GDD Reference**: 해당 GDD 없음 — `design/quick-specs/fresh-delivery-type-addition-2026-11-16.md` (2항 신선도 산정)
**Date**: 2026-11-18

## Change Summary

제한 시간(Fresh는 신선도)의 거리 항을 유형별로 따로 조절할 수 있게 하고, 기본값을 실제 이동 시간에 맞게 다시 잡는다.

## Motivation

현행 식은 `T = (T_base + 초/m × D) × M_type + R`이고 SO 값은 `_baseSeconds` 45000, `_secondsPerMeter` 10이다.
게임 속도가 450배, 차량 속도가 3~5 m/s이므로 1m 이동에 게임 시간 90~150초가 드는데, 거리 항은 미터당 10초뿐이다.
100m 배송이면 거리 항이 전체의 약 2%라서 거리가 늘어도 제한 시간이 거의 늘지 않고, 먼 배송이 항상 불리하다.
또 `M_type`이 거리 항에도 곱해져 Fresh(0.35)는 거리 보정이 더 약해진다.

## Design Delta

기존 규칙 (`fresh-delivery-type-addition-2026-11-16.md` 2항):

> T_fresh = T_base(D) * M_fresh + R

이 스펙은 다음과 같이 바꾼다.

`T = (T_base × M_type) + (초/m × D × M_dist_type) + R`  (결과는 0 이상으로 제한)

- `D`는 XZ 평면 직선거리(m), 시간 단위는 게임 초.
- `M_type`은 기본 시간에만, `M_dist_type`은 거리 항에만 곱한다.

## New Rules / Values

| SO | 필드 | 의미 | 기본값 |
|---|---|---|---|
| `DeliveryDeadlineConfig` | `_baseSeconds` | 고정 제한 시간(게임 초) | 9000 (기존 45000) |
| `DeliveryDeadlineConfig` | `_secondsPerMeter` | 1m당 추가 시간(게임 초) | 120 (기존 10) |
| `DeliveryTypeConfig` 엔트리 | `_deadlineMultiplier` | `M_type`, 기본 시간 배율 | 기존 값 유지 |
| `DeliveryTypeConfig` 엔트리 | `_distanceMultiplier` (신규, Min 0) | `M_dist`, 거리 항 배율 | Express/Standard/Fresh 모두 1.0 |

- 신규 필드가 없는 기존 에셋은 1로 읽힌다(필드 초기값 1).
- 값은 플레이테스트로 조정하는 시작점이다. 맵의 실제 배송 거리 범위는 확인하지 못했다.

## Affected Systems

| System | Impact | Action Required |
|--------|--------|-----------------|
| `DeliveryDeadlineSystem.CreateParcel` | 식 변경, 거리 배율 인자 추가 | 코드 수정 |
| `DeliveryGenerationSystem` | 호출 시 `M_dist` 전달 | 코드 수정 |
| `DeliveryTypeConfig` / `DeliveryTypeEntry` | `M_dist` 필드·조회 추가 | 코드 수정 |
| `DeliveryDeadlineConfig.asset`, `DeliveryTypeConfig.asset` | 값 변경 | SO 값 변경(Editor 통해) |
| 정산·보상 | 영향 없음(제한 시간 값만 바뀜) | 없음 |

## Acceptance Criteria

- [ ] `M_dist`가 `DeliveryTypeConfig.asset`에서 읽히고 거리 항에만 곱해진다.
- [ ] 같은 유형에서 거리가 2배면 거리 항이 2배가 된다.
- [ ] 생성 로그에 기본 시간 항과 거리 항이 따로 표시된다.
- [ ] 신규 필드가 없는 기존 에셋도 `M_dist = 1`로 동작한다.
- [ ] 기존 기능 깨짐 없음: Fresh의 상차 후 감소, Express의 생성 후 감소, 정산 집계.

## GDD Update Required?

No. `fresh-delivery-type-addition-2026-11-16.md`의 `T_base(D)` 설명에 `M_dist` 한 줄을 추가할 수는 있으나 필수는 아니다.
