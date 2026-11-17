---
name: reference-unity-cli-capture-in-background
description: CLI로 플레이 모드 스크린샷을 찍을 때 Editor가 백그라운드면 게임이 멈춰 오래된 화면이 찍힌다 — runInBackground와 Refresh로 해결
metadata:
  type: reference
---

Unity CLI(`unity command capture game_view`)로 증거 스크린샷을 찍을 때 두 가지 함정이 있다(2026-11-17 Story 017에서 확인).

1. Editor 창이 포커스가 없으면 `Application.runInBackground`가 false라 플레이 모드가 진행되지 않는다. UI 레이아웃이 계산되지 않고(`layout` NaN) 플레이 시작 프레임이 그대로 찍힌다. 플레이 모드에 들어간 직후 `unity command script eval --code 'Application.runInBackground = true; ...'`로 켜면 된다. 런타임 값이라 플레이 종료 후 false로 돌아오고 PlayerSettings는 바뀌지 않는다.
2. USS·UXML 파일을 셸이나 파일 도구로 고치면 Editor가 백그라운드일 때 다시 불러오지 않는다. 플레이 전에 `AssetDatabase.Refresh()`를 실행해야 바뀐 스타일이 적용된다(`unity recompile`도 Refresh를 한다).

**How to apply:** UI 스토리의 /dev-story 실행·관찰 단계에서 캡처 전에 둘 다 한다. 찍은 뒤에는 반드시 이미지를 열어 시계·패널 내용이 갱신됐는지 본다.
