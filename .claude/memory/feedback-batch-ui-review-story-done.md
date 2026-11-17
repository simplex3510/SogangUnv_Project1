---
name: feedback-batch-ui-review-story-done
description: UI/HUD 스토리의 /code-review와 /story-done은 개별로 돌리지 않고 나중에 사용자가 한 번에 확인한다
metadata:
  node_type: memory
  type: feedback
  originSessionId: 57a6ba94-6baa-4e47-824e-2e24df8ffe1d
  modified: 2026-11-16T20:07:29.126Z
---

UI/HUD 관련 스토리는 구현 직후 /code-review·/story-done을 스토리마다 돌리지 않는다. 사용자가 나중에 모아서 한 번에 확인한다.

**Why:** 2026-11-17 사용자가 "UI 및 HUD 관련 리뷰 및 스토리 done은 한 번에 하자"를 '나중에 일괄 확인'으로 의도함. 그 자리에서 리뷰를 시작하자 중단시킴.

**How to apply:** UI/HUD 스토리 구현이 끝나면 In Review 상태로 두고 리뷰·done은 제안만 하거나 생략한다. 사용자가 일괄 리뷰를 요청할 때 실행. 관련: [[project-stage-pre-release]]
