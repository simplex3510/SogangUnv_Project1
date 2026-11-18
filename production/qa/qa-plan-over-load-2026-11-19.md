# QA Plan: Epic over-load

**Date**: 2026-11-19
**Scope**: epic `over-load` (feature scope), 31개 스토리 중 29개 대상 + 밸런싱 실측
**Config**: qa.level minimal, team.size individual, review_mode solo
**Strategy**: `production/qa/qa-strategy-over-load-2026-11-19.md`

## Story Classification Table

Phase 2 전략 문서의 Strategy Table을 따른다. 요약:

| 구분 | 스토리 |
|---|---|
| Logic | 002, 004, 007, 009, 010, 013 |
| Integration | 005, 008, 011 |
| Config/Data | 003 (밸런싱 실측만) |
| UI | 006, 014, 015, 016, 017, 018, 019, 020, 021(창 레이아웃), 021(토스트), 022, 023, 024, 025, 026, 027, 028, 029 |
| 제외 | 001 (Deferred), 012 (미구현) |

## Automated Test Requirements

없음 (qa.level: minimal). Logic·Integration·Config 스토리는 테스트가 WAIVED이며, 아래 Manual QA Scope의 AC 확인이 실행 증거다.

## Manual QA Scope

전부 `Assets/Game/Scenes/Release/MainGameScene.unity`(Play 시 Boot부터)에서 진행한다.

| 배치 | 내용 | 관련 스토리·위험 |
|---|---|---|
| 0 | 스모크: Play → 출근 → 하루 시작까지 크래시 없음 | 진입 조건 |
| 1 | 하루 완주(현실 시간, 정산, 별, N·S), 무실패 판과 실패 1~2건 판 비교, 유형별 제한 시간·거리 로그 | 002, 003, B-1~B-4 |
| 2 | Express 먼 배송, Fresh 가까운·먼 배송(일반·고속), 상·하차 6건 시간, 배송지별 주행 시간 | 003, 008, 013, B-4, B-5 |
| 3 | 일반·고속 정면 충돌 감소량, 벽 스침 3회, Heavy 2개 포함 6개 적재, 폐기→처분 흐름 | 004, 005, 009, 010, 011, B-10~B-12 |
| 4 | 실제 입력: 물류센터 마우스 홀드 상차·비활성 행, 시작 화면 클릭·↑/↓, 설명 패널 Esc·PageUp/Down·21:9, R키 복구 | 021 창, 028, 029, 007 |
| UI 확인 | 보관된 스크린샷으로 판정, 재촬영 불필요 | 006, 014~020, 021 토스트, 022~027 |

Waived 스토리(AC를 수동으로 확인): 002, 003, 004, 005, 007, 008, 009, 010, 011, 013.

## Out of Scope

- 001: Deferred. 거리표·완주 증거는 이 스토리 작업 때 만든다.
- 012: 미구현(Ready).
- 씬 분리 작업(`GameSceneLoader`, `Scenes/Release/`, `MainGameScene` 삭제): 소속 스토리가 없다. 스모크 배치 0에서 크래시 여부만 본다.
- 시각적 완성도: GUI가 임시다.

## Entry Criteria

1. 스모크 보고서 `production/qa/smoke-*.md`가 PASS 또는 PASS WITH WARNINGS일 것. **현재 충족 안 됨(UNKNOWN). 사용자 결정으로 경고를 안고 진행한다.**
2. 빌드가 안정적일 것: `Release/MainGameScene`에서 Play → 출근 → 하루 시작까지 크래시 없음(배치 0).
3. 범위 내 스토리 파일의 Status가 In Progress, In Review, Complete 중 하나일 것. 003은 Deferred지만 실측 대상으로만 포함한다.

## Exit Criteria

- 범위 내 모든 스토리가 PASS, PASS WITH NOTES, FAIL(버그 등록 `production/qa/bugs/BUG-NNNN.md`) 중 하나로 판정됨.
- 배치 1~3의 밸런싱 실측값이 기록됨(003 AC 결과 입력용).
- 미확정 스토리는 사인오프 표에 사유와 함께 남김.
