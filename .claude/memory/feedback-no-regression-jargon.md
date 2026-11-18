---
name: feedback-no-regression-jargon
description: "사용자에게 말할 때 \"회귀(regression)\" 대신 \"기존 기능 깨짐 여부\"처럼 풀어서 쓴다"
metadata:
  node_type: memory
  type: feedback
  originSessionId: f720133b-cd5f-456b-9255-c402d21c6c0b
  modified: 2026-11-17T17:51:25.089Z
---

사용자에게 보이는 말(답변, 질문, 요약, 스토리 확인 질문)에서 "회귀", "회귀 없음", "No regression"을 쓰지 않는다. "기존 기능 깨짐 여부", "원래 되던 기능이 그대로 되는지"처럼 풀어서 쓴다.

**Why:** 사용자가 "회귀"라는 말을 몰라 헷갈려 했다(2026-11-18, story-025 /story-done 중).

**How to apply:** 스토리 인수 기준의 "No regression: …" 항목을 물을 때도 "이번 수정으로 기존 기능(…)이 깨지지 않았는지"로 바꿔 묻는다. 문서 템플릿의 영어 헤딩·키는 그대로 둔다.
