# Story 003: 같은 LAN 2대 Direct 참가 (AC-NET-30)

> **Epic**: networking
> **Status**: In Progress
> **Layer**: Core
> **Type**: Integration
> **Estimate**: L
> **Manifest Version**: N/A (no control manifest)
> **Last Updated**: 2026-11-21

## Context

**GDD**: `design/gdd/networking.md`
**Requirement**: `TR-networking-018` (규칙 16: 참가 수단 1단계 IP:포트 직접 접속). 함께 쓰는 `TR-networking-001`(서버 자식 프로세스, 호스트도 클라이언트)

**ADR Governing Implementation**: ADR-0002: Session Join Method (보조: ADR-0001 결정 2·11·14)
**ADR Decision Summary**: 세션 모드는 Direct/Relay 두 가지이며 1단계는 Direct다. 서버는 설정 포트로 모든 인터페이스에서 수신하고, 호스트는 loopback으로 접속하며 로비에 LAN IP:포트를 표시한다. 참가자는 IP:포트를 입력해 접속한다. 호스트는 서버를 자식 프로세스로 기동하고 상태 파일로 준비 신호를 받는다.
**ADR Version**: 2026-11-21

**Engine**: Unity 6000.6.8f1 | **Risk**: HIGH
**Engine Notes**: 1단계는 Unity Transport 주소·포트 설정뿐이라 포스트 컷오프 API 사용은 적다. 바인딩 실패 방식은 Story 001에서 확인한 결과를 쓴다. 호스트 접속 때 connection approval에 호스트 토큰을 넣어야 한다.

**Control Manifest Rules (this layer)**: N/A (no control manifest)

---

## Acceptance Criteria

*GDD `design/gdd/networking.md` AC-NET-30과 ADR-0002 결정 2·4, ADR-0001 결정 2·11·14에서 가져옴:*

- [ ] 호스트가 Direct 세션을 만들면 서버가 자식 프로세스로 기동한다. 서버는 설정 파일의 포트로 모든 인터페이스에서 수신하고 상태 파일에 `READY port=<n>`을 쓴다. 호스트는 READY를 받은 뒤에만 loopback으로 접속한다
- [ ] READY가 설정 시간 안에 오지 않거나 `FAIL`이면 서버 프로세스를 종료하고 세션 생성 실패를 표시한다
- [ ] 호스트의 접속 요청에 호스트 토큰을 넣고, 서버는 토큰이 맞는 clientId를 호스트로 기록한다. 토큰이 다른 접속(예전 서버)은 거부한다
- [ ] 로비 화면에 호스트 PC의 LAN IP:포트가 표시된다
- [ ] 참가 입력 칸에 IPv4 `IP:포트`를 입력하면 Direct로 해석해 접속한다. 형식이 틀리면 참가 실패를 표시한다
- [ ] **AC-NET-30**: 같은 LAN의 PC 2대에서 호스트가 Direct 세션을 만들고 다른 PC가 그 IP:포트로 참가하면, 두 클라이언트의 로비 참가자 목록이 같은 2명을 표시한다
- [ ] 호스트 클라이언트를 종료하거나 강제 종료하면 서버 자식 프로세스가 함께 내려가고 고아 프로세스가 남지 않는다
- [ ] PC 2대 로비 화면 스크린샷을 `production/qa/evidence/`에 보관한다

---

## Implementation Notes

*ADR-0002 Decision 2·4·6, ADR-0001 Decision 2·11·14에서:*

- 포트·READY 대기 시간은 외부 설정에 둔다(코드 상수 금지). 포트는 자동 할당이 아니라 고정이다.
- 모드 선택과 접속 정보 해석(`SessionMode`, 문자열 → `(Direct, address, port)`)은 네트워크 어댑터 계층에만 둔다. 도메인 규칙은 접속 방식을 모른다.
- 서버 기동 인자: 포트, 부모 PID, `--host-token`, `--status-file <사용자 임시 폴더의 새 이름>`, `--mode direct`. 상태 파일은 임시 이름으로 쓴 뒤 바꿔 달아 반쯤 쓴 파일을 읽지 않게 한다. 세션이 끝나면 호스트가 파일을 지운다.
- 서버는 부모 프로세스 종료 또는 호스트 접속 끊김을 감지하면 스스로 종료한다.
- 세션마다 어댑터가 Transport 설정을 처음부터 다시 구성한다.
- 정원 4명 판정(connection approval)과 로비 준비·시작 흐름은 이 스토리 범위가 아니다. 로비는 참가자 목록 표시까지만 임시 GUI로 만든다. UI는 임시이며 UI Toolkit 전환 때 다시 만든다.
- 첫 실행 때 Windows 방화벽 허용 창이 뜨는 것은 감수한다(ADR-0002 결정 2).

---

## Out of Scope

*다른 스토리가 맡는다 — 여기서 구현하지 않는다:*

- 후속: 정원 4명 초과·정산 중 참가 거부(AC-NET-04), 하루 도중 참가(AC-NET-05), 준비·하루 시작(AC-NET-06~08)
- 후속: 참가 코드 + Relay(2단계, ADR-0002 스파이크 통과 후)
- 후속: 방화벽 안내 문구, 서버 빌드·배포
- 후속: 호스트 이탈 시 남은 참가자 처리(AC-NET-23·29)

---

## Files

- `Assets/Game/Scripts/Networking/ServerSession.cs`
- `Assets/Game/Scripts/Networking/NetworkSessionAdapter.cs`
- `Assets/Game/Scripts/Networking/LobbyGui.cs`
- `Assets/Game/Scripts/Networking/ServerNetworkConfig.cs`
- `Assets/Game/Scripts/Networking/DedicatedServerBootstrap.cs`
- `Assets/Game/Configs/ServerNetworkConfig.asset`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: playtest doc. PC 2대 로비 스크린샷을 `production/qa/evidence/`에 보관. `qa.level: minimal`이라 자동 테스트는 면제.

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/networking/story-001-ngo-install-dedicated-server.md`, `production/epics/networking/story-002-editor-multiplayer-test.md`
- Unlocks: 정원·준비·하루 시작 등 후속 networking 스토리
