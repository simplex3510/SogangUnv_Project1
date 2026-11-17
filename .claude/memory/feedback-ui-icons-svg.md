---
name: feedback-ui-icons-svg
description: UI 아이콘은 폰트 기호(↻ 등) 대신 임시 SVG를 쓴다
metadata:
  node_type: memory
  type: feedback
  originSessionId: 8346e30d-172e-4c56-9bd2-bc79f9f9f562
  modified: 2026-11-17T10:38:53.361Z
---

UI의 아이콘·기호는 폰트 글리프(↻, ▲ 같은 유니코드 기호)로 넣지 않고, 임시 SVG 아이콘을 쓴다(예: `Assets/Game/UI/Icons/`의 Bootstrap Icons, MIT).

**Why:** 2026-11-17 Story 008 검증에서 `↻`가 맑은 고딕에 없어 대체 글자로 깨져 보였다. 사용자가 기호 대신 SVG로 하자고 정함.

**How to apply:** 새 UI에 아이콘이 필요하면 SVG 이미지를 쓰고 USS `background-image`·tint로 색을 준다. 폰트 기호가 보이면 SVG로 바꾸자고 제안한다. 관련: [[project-ui-toolkit-migration]]
