# Quick Design Spec: 상호작용 구역 떠오르는 빛 표현

**Type**: New Small System
**Scope**: 상호작용 트리거 구역(물류센터·폐기장·배송 목적지)에 은은하게 위로 떠오르는 빛기둥을 그린다. 상호작용 판정·UI·사운드는 건드리지 않는다.
**Date**: 2026-11-17
**Estimated Implementation**: 약 3~4시간
**GDD Reference**: 해당 GDD 없음 — `design/gdd/delivery-cargo-vehicle-rules.md` 참고만

## Overview

각 상호작용 구역 프리팹의 트리거 BoxCollider 크기에 맞춘 박스 메시에 반투명 가산(additive) 셰이더를 입힌다. 바닥은 가장 밝고 위로 갈수록 투명해지며, 그 안에서 노이즈 무늬가 위로 천천히 흐른다. 플레이어는 "여기가 상호작용 구역"임을 멀리서도 알아본다. 추가 텍스처·VFX 에셋은 쓰지 않는다.

## Core Rules

1. 셰이더 `Game/InteractionZoneGlow` (URP Unlit HLSL): Transparent 큐, ZWrite Off, Blend SrcAlpha One, Cull Off(앞뒤 벽이 모두 보여야 기둥처럼 읽힘). 프로퍼티는 CBUFFER에 넣는다.
2. 알파 = 높이 페이드 × 흐르는 노이즈 × 옆벽 마스크. 노이즈는 텍스처 없이 셰이더 안 value-noise 수식으로 만든다.
   - 높이 페이드: `pow(1 - y01, FadePower)` (y01 = 오브젝트 공간 높이 0~1)
   - 노이즈: `noise((worldX + worldZ, worldY - time * RiseSpeed) * NoiseScale)`을 [1−NoiseContrast, 1]로 매핑한다. 벽을 따라가는 가로·세로 좌표라서 무늬가 위로 흐른다.
   - 옆벽 마스크: `1 - |normal.y|`로 윗면·바닥면을 지운다.
3. 컴포넌트 `InteractionZoneVisual` (구역 프리팹 루트): Start에서 트리거 BoxCollider 아래에 콜라이더 없는 큐브 메시(`InteractionZoneGlow`)를 만들고, 트리거의 크기·중심에 맞춰 바닥에서 `Height`(월드 단위)만큼 세운다. 물리·판정에 영향이 없다.
4. 색과 높이는 구역 종류별로 프리팹 필드에서 지정한다. 색은 MaterialPropertyBlock으로 넣어 머티리얼(`Assets/Game/Shaders/InteractionZoneGlow.mat`) 1개를 공유한다. 나머지 Knob(밝기·페이드·흐름·무늬)은 공유 머티리얼에서 조절한다.
5. 항상 표시한다. 상호작용 가능 여부나 플레이어 접근에 따른 반응은 이번 범위에서 제외한다(후속).
6. 대상 프리팹: `LogisticsCenter`, `DisposalSite`, `DeliveryTargets/_BuildingBase` (트리거 박스가 있는 3종).

## Tuning Knobs

| Knob | Default | Range | Category | Rationale |
|------|---------|-------|----------|-----------|
| Height | 0.8 | 0.2–3 | feel | 차량 높이(약 0.6)보다 조금 높게. 1.5는 이 맵 스케일에서 2층 높이까지 올라와 과했다 |
| Color | 센터 (0.3, 0.8, 1) · 폐기장 (1, 0.45, 0.25) · 목적지 (0.4, 1, 0.45) | — | feel | 종류 구분 |
| Intensity | 0.6 | 0.1–2 | feel | "은은함"의 핵심. 높이면 눈에 띔 |
| FadePower | 2.0 | 0.5–4 | curve | 클수록 바닥에 몰린 빛 |
| RiseSpeed | 0.25 | 0.05–1 | feel | 위로 흐르는 속도 |
| NoiseScale | 3.0 | 1–8 | feel | 무늬 촘촘함 |
| NoiseContrast | 0.5 | 0–1 | feel | 0이면 균일한 막, 1이면 강한 줄무늬 |

Height·Color는 구역 프리팹의 직렬화 필드에, 나머지는 공유 머티리얼에 둔다(비게임플레이 시각 값). 셰이더 하드코딩은 하지 않는다.

## Acceptance Criteria

- [ ] 3종 구역 모두에서 트리거 박스와 같은 면적으로 빛기둥이 보인다.
- [ ] 빛이 위로 갈수록 사라지고, 무늬가 위로 흐른다 (Play 모드 스크린샷 + 짧은 관찰).
- [ ] 빛기둥 위에서 차량이 지나가도 충돌·상호작용 판정이 이전과 같다 (큐브에 콜라이더 없음).
- [ ] Mobile 렌더러 에셋에서도 핑크(셰이더 오류) 없이 렌더된다.
- [ ] 경험: 낮 화면에서 "은은하다"는 인상이고 바닥·도로 가독성을 해치지 않는다.
- [ ] 스크린샷을 `production/qa/evidence/interaction-zone-glow/`에 보관한다.

## Systems Index

추적 임계치 미만이라 이 Quick Spec만으로 충분하다. 별도 항목은 추가하지 않는다.
