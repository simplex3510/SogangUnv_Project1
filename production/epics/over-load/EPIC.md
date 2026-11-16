# Epic: over-load

> **Goal**: 차량을 직접 몰아 물류를 고르고 싣고 배송지에 내려, 제한 시간과 적재 한도 사이에서 하루 수익을 만드는 캐주얼 배달 시뮬레이터. *(design/game-brief.md Pitch)*
> **Scope**: 브리프 Features 중 미구현(`[ ]`) 항목. 구현 완료 항목(`[x]`)은 스토리가 없다.
> **Order**: 브리프 Build order (맵 → 별점 → 밸런싱)
> **주의**: 첫 릴리즈 전 개발 단계(GUI 임시). 기획 최종 업데이트 예정이라 스토리 전반이 바뀔 수 있다.

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | 맵 레벨 게임플레이 배선과 1회 완주 | Integration | Deferred | N/A (minimal) |
| 002 | 하루 별점 피드백(3구간) | Logic | Complete | N/A (minimal) |
| 003 | 밸런싱 수치 확정과 마무리 | Config/Data | Deferred | N/A (minimal) |
| 004 | 충돌 내구도 감소 | Logic | Complete | N/A (minimal) |
| 005 | 폐기 물류 전환과 처분 장소 | Integration | Complete | N/A (minimal) |
| 006 | 내구도 피드백 UI 3종 | UI | Complete | N/A (minimal) |
| 007 | 차량 리셋·복구 | Logic | Ready | N/A (minimal) |
| 008 | Fresh 배송 유형 | Integration | Ready | N/A (minimal) |
| 009 | 무게 한도 초과 적재와 속도 감소 | Logic | Complete | N/A (minimal) |
| 010 | 정적 방해물 모듈 컴포넌트 | Logic | Complete | N/A (minimal) |
| 011 | 카탈로그 정적 오브젝트 프리팹화 | Integration | Ready | N/A (minimal) |
| 012 | NPC 차량 골격 | Integration | Ready | N/A (minimal) |
| 013 | 고속 모드 게이지와 속도계 HUD | Logic | Complete | N/A (minimal) |
| 014 | 적재 물류 패널 접기/펼치기와 스크롤 | UI | In Review | N/A (minimal) |
| 015 | 일차·시각 HUD 좌상단 | UI | Ready | N/A (minimal) |
| 016 | 오늘 수익 HUD | UI | Ready | N/A (minimal) |
| 017 | 방향 안내를 적재 물류 패널로 이전 | UI | Ready | N/A (minimal) |
| 018 | 하단 중앙 HUD 정리 | UI | Ready | N/A (minimal) |
| 019 | 적재 패널 물류 행 제한 시간 바 | UI | Ready | N/A (minimal) |
| 020 | 표시 문구 한글 통일 | UI | Ready | N/A (minimal) |
| 021 | 토스트 알림 | UI | Ready | N/A (minimal) |
