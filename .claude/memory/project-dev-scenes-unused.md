---
name: project-dev-scenes-unused
description: 밸런싱·QA 기준 씬은 Release/MainGameScene(Boot부터 Play). SampleMapDesign 맵 값은 개발용 임시값. DevScene·FailedScene은 미사용
metadata:
  node_type: memory
  type: project
  originSessionId: 71e8e95e-deaa-4068-9b60-da4ce8c288b5
  modified: 2026-11-18T15:24:55.740Z
---

`DevScene.unity`, `FailedScene.unity`는 쓰지 않는 씬이다. 두 씬의 콘솔 에러나 빠진 HUD는 문제로 보고하지 않는다.

밸런싱과 QA 실측의 기준은 릴리즈 씬 `Assets/Game/Scenes/Release/MainGameScene.unity`다. 이 씬을 열고 Play하면 `BootPlayModeStarter`가 같은 폴더의 `Boot.unity`부터 시작한다(Systems·UI 씬은 additive). `_DevOnly/SampleMapDesign`과 GDD·story-001의 맵 값(배송지 3곳/5곳 등)은 개발 환경 임시값이라 릴리즈 씬과 전혀 다르다. 2026-11-19 기준 릴리즈 씬에는 배송지가 39곳(물류센터에서 2.5~26.3 m)이고, 처분장은 2곳이다.

**Why:** 사용자 답변(2026-11-19, /team-qa): "해당 문서들은 개발 환경의 임시값이었음. 현행 릴리즈 버전 씬에서는 전혀 다름". 이전 규칙(2026-11-18, story-027)은 DevScene·FailedScene 대신 SampleMapDesign을 쓰라는 것이었다.
**How to apply:** 밸런싱 계산, 거리·시간 측정, 하루 완주 QA는 릴리즈 씬에서 한다. SampleMapDesign은 UI 확인 같은 개발용 검증에만 쓰고, 그 맵의 수치로 밸런스를 판단하지 않는다.
