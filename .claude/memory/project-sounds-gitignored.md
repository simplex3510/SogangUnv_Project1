---
name: project-sounds-gitignored
description: 외부 에셋 팩(사운드·모델·스카이박스)은 Assets/ExternalAssets/에 두고 git에 올리지 않는다. Resources 폴더는 쓰지 않는다
metadata:
  node_type: memory
  type: project
  originSessionId: 6b158355-7790-4df2-8b7f-808bce59099f
  modified: 2026-11-18T15:24:59.127Z
---

사운드, 차량·도로·건물 모델, 스카이박스 같은 외부 에셋 팩은 `Assets/ExternalAssets/`에 있고 `.gitignore`의 `/[Aa]ssets/ExternalAssets/*` 때문에 git에 올라가지 않는다(폴더 유지용 `.gitkeep`만 추적). 2026-11-19에 `Assets/Resources/`에서 Editor로 옮겼다(GUID 유지).

**Why:** 용량이 커서 GitHub에 올리지 않는 것이 팀의 의도다(사용자 확인 2026-11-19). `Resources` 폴더는 씬 참조와 상관없이 전부 빌드에 들어가므로 빌드 최적화를 위해 쓰지 않기로 했다. 코드에 `Resources.Load` 호출은 없다.
**How to apply:** 새 외부 에셋은 `Assets/ExternalAssets/` 아래에 둔다. 다른 PC는 에셋 파일을 직접 받아 이 폴더에 넣어야 씬의 GUID 참조가 채워진다. 이전 PC에 `Assets/Resources/`가 남아 있으면 Editor로 `ExternalAssets`로 옮기라고 알린다. 사운드 압축(Vorbis 품질 100%)은 아직 손대지 않았다.
