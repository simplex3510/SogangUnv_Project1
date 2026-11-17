---
name: project-ui-toolkit-migration
description: Canvas(UGUI) 기반 UI/HUD는 전부 UI Toolkit으로 전면 리팩토링할 예정
metadata:
  node_type: memory
  type: project
  originSessionId: 8346e30d-172e-4c56-9bd2-bc79f9f9f562
  modified: 2026-11-17T10:38:57.496Z
---

기존 Canvas(UGUI) 기반 UI/HUD(예: `DeliveryTargetParcelUI`, `PlayerObjectUI`, `InteractionProgressUI` 등 월드·오버레이 UI)는 나중에 전부 UI Toolkit으로 리팩토링할 예정이다(2026-11-17 사용자 언급, 일정 미정).

**Why:** 새 HUD(DurabilityHud, StatusHud, 물류센터 창)는 이미 UI Toolkit이라 두 체계가 섞여 있다.

**How to apply:** Canvas UI에 새 기능을 크게 쌓지 말고 최소 수정에 그친다. Canvas UI의 모양 문제(작은 아이콘, 색 불일치 등)는 리팩토링 때 다룰 항목으로 기록한다. 관련: [[feedback-ui-icons-svg]]
