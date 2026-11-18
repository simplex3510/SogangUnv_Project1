---
name: project-dev-scenes-unused
description: DevScene·FailedScene은 쓰지 않는 씬 — 검증·수정 대상은 SampleMapDesign
metadata:
  node_type: memory
  type: project
  originSessionId: 71e8e95e-deaa-4068-9b60-da4ce8c288b5
  modified: 2026-11-17T18:59:48.198Z
---

`DevScene.unity`, `FailedScene.unity`는 쓰지 않는 씬이다. Play 검증, 스크린샷, 기존 기능 확인은 `SampleMapDesign`에서만 한다. 두 씬의 콘솔 에러나 빠진 HUD는 문제로 보고하지 않는다.

**Why:** 사용자 지적(2026-11-18, story-027) — "이름만 봐도 알잖아". 스토리 기준에 세 씬이 적혀 있어도 마찬가지.
**How to apply:** 스토리나 브리프가 이 두 씬을 대상으로 적고 있으면 SampleMapDesign으로 좁혀서 진행하고, 그렇게 했다고 한 줄 남긴다.
