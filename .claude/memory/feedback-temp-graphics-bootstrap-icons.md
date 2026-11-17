---
name: feedback-temp-graphics-bootstrap-icons
description: 임시 GUI의 그래픽(아이콘·화살표 등)은 텍스트 글리프나 색 블록 대신 Bootstrap Icons SVG를 쓴다
metadata:
  node_type: memory
  type: feedback
  originSessionId: 2dc512e0-1ce3-4f76-a824-476c29b2b490
  modified: 2026-11-17T02:13:05.222Z
---

임시 단계라도 그래픽 이미지(아이콘, 화살표 등)는 가능하면 Bootstrap Icons(https://icons.getbootstrap.com/, MIT)의 SVG를 쓴다. 글자 글리프(▲ 등)나 단색 블록으로 대신하지 않는다.

**Why:** 2026-11-17 Story 017에서 방향 화살표를 `▲` 글리프로 만들었을 때 사용자가 요청했다.
**How to apply:** UI 작업에서 그림이 필요하면 SVG를 `Assets/Game/UI/Icons/`에 받아 UI Toolkit `background-image`로 쓰고 색은 `-unity-background-image-tint-color`로 준다. 최종 GUI 디자인 전까지의 임시 규칙이다. [[project-stage-pre-release]]
