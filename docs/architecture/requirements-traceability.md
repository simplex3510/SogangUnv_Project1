# Architecture Traceability Index
Last Updated: 2026-11-21
Engine: Unity 6000.6.8f1

> 범위: `design/gdd/networking.md`만 추적했다(`/architecture-review` single-gdd, 2026-11-21, ADR-0001 개정 2 반영 재검토 포함). 다른 GDD는 아직 이 색인에 없다.

## Coverage Summary
- Total requirements: 24
- Covered: 24 (100%)
- Partial: 0
- Gaps: 0

## Full Matrix

| Requirement ID | GDD | System | Requirement | ADR Coverage | Status |
|---|---|---|---|---|---|
| TR-networking-001 | networking.md | Networking | 서버를 호스트 PC 자식 프로세스로 분리 실행, 호스트도 클라이언트 (규칙 1) | ADR-0001 결정 2 | ✅ |
| TR-networking-002 | networking.md | Networking | 서버 단일 권한, 클라이언트는 입력·요청만 (규칙 2) | ADR-0001 결정 3·10 | ✅ |
| TR-networking-003 | networking.md | Networking | 싱글 = 참가자 1명 같은 경로 (규칙 3) | ADR-0001 결정 2 | ✅ |
| TR-networking-004 | networking.md | Networking | 인원별 설정 값은 하루 시작 수락 시점 인원으로 고정 (규칙 3) | ADR-0001 결정 17 | ✅ |
| TR-networking-005 | networking.md | Networking | 정원 4명, 초과·정산 중 참가 거부 (규칙 4·16) | ADR-0002 가이드라인 | ✅ |
| TR-networking-006 | networking.md | Networking | 하루 도중 참가·복귀 최초 상태, 하루 상태 동기화 (규칙 4) | ADR-0001 결정 4·5·8 | ✅ |
| TR-networking-007 | networking.md | Networking | 전원 준비 후 하루 시작, 동시 조작 활성화 (규칙 5) | ADR-0001 결정 8, 이벤트 RPC | ✅ |
| TR-networking-008 | networking.md | Networking | 서버 시계, 클라이언트 표시 보정 (규칙 6) | ADR-0001 결정 7 | ✅ |
| TR-networking-009 | networking.md | Networking | 서버 권한 차량 물리·차량 간 충돌·Δv 판정 (규칙 7) | ADR-0001 결정 4 | ✅ |
| TR-networking-010 | networking.md | Networking | 자기 차량 예측·재조정, 다른 차량 보간 (규칙 8) | ADR-0001 결정 4·6 (개정 3: 방향 회전 외부 수정값) | ✅ |
| TR-networking-011 | networking.md | Networking | 물류 예약 순서 처리, 그룹 선택 건너뛰기 (규칙 9) | ADR-0001 결정 5 | ✅ |
| TR-networking-012 | networking.md | Networking | 차량별 적재·무게 한도 (규칙 10) | ADR-0001 결정 13 | ✅ |
| TR-networking-013 | networking.md | Networking | 하차·처분 서버 판정 (규칙 11) | ADR-0001 결정 3 | ✅ |
| TR-networking-014 | networking.md | Networking | 서버 복구 실행, 잠금은 입력 무시 (규칙 12) | ADR-0001 결정 4(복구 잠금 남은 시간) | ✅ |
| TR-networking-015 | networking.md | Networking | 팀 합산 정산·별점 (규칙 13) | ADR-0001 결정 3·5 | ✅ |
| TR-networking-016 | networking.md | Networking | 하루 종료 동시 잠금·정산 전환 (규칙 14) | ADR-0001 이벤트 RPC | ✅ |
| TR-networking-017 | networking.md | Networking | 호스트 정산 확인·정산 중 끊김 시 서버 종료, 다른 참가자 정산 화면 유지 (규칙 15) | ADR-0001 결정 11·16 | ✅ |
| TR-networking-018 | networking.md | Networking | 참가 수단 Direct / Relay (규칙 16) | ADR-0002 결정 1~4 | ✅ (2단계는 스파이크 후) |
| TR-networking-019 | networking.md | Networking | 호스트 이탈·서버 비정상 종료 시 종료, 고아 프로세스 없음 | ADR-0001 결정 2·11 | ✅ |
| TR-networking-020 | networking.md | Networking | 일반 참가자 이탈 시 차량 제거·예약 해제·흩어진 물류 | ADR-0001 결정 5 | ✅ |
| TR-networking-021 | networking.md | Networking | 무입력 구간 입력 0, 설정 시간 초과 시 접속 끊김 | ADR-0001 결정 16 | ✅ |
| TR-networking-022 | networking.md | Networking | 상차 완료와 이탈이 같은 프레임이면 이탈 먼저 | ADR-0001 결정 15 | ✅ |
| TR-networking-023 | networking.md | Networking | 규칙 위반 요청 거부, 표시 되돌림 | ADR-0001 가이드라인 | ✅ |
| TR-networking-024 | networking.md | Networking | 대역폭·갱신 주기·타임아웃 외부 설정, 실측 | ADR-0001 성능·가이드라인 | ✅ |

## Known Gaps
없음(❌ 0건, ⚠️ 0건). 남은 🟡 비차단 충돌(ADR-0002 싱글 세션 방화벽)은 `architecture-review-2026-11-21-3.md` 참고.

## Superseded Requirements
없음.
