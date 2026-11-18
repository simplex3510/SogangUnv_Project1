---
name: project-sounds-gitignored
description: "사운드 파일은 gitignore된 Assets/Resources/Sounds에 있어 커밋되지 않음, 이동은 보류 결정"
metadata:
  node_type: memory
  type: project
  originSessionId: 66973cb2-cd5a-4f21-985c-1efcd7c1e654
  modified: 2026-11-18T04:29:10.398Z
---

사운드 음원은 `Assets/Resources/Sounds/`에 있고 `.gitignore`의 `/[Aa]ssets/Resources/*` 때문에 git에 올라가지 않는다. 씬의 GameAudio는 이 클립들을 GUID로 참조하므로 다른 PC에서는 할당이 비고 BGM Streaming 설정도 전달되지 않는다.

**Why:** 2026-11-18 사용자가 커밋 정리 때 "지금은 두기"로 이동을 보류했다.
**How to apply:** 다른 PC 작업·빌드·팀 공유 이야기가 나오면 먼저 이 문제를 상기시키고, 옮길 때는 Editor로 `Assets/Game/Audio` 등에 이동해 GUID를 유지한다.
