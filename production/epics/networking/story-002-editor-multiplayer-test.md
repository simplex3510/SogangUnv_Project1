# Story 002: 서버 부트스트랩 역할 분기와 에디터 멀티 테스트(Multiplayer Play Mode)

> **Epic**: networking
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: S
> **Manifest Version**: N/A (no control manifest)
> **Last Updated**: 2026-11-21

## Context

**GDD**: `design/gdd/networking.md`
**Requirement**: `TR-networking-001` (규칙 1: 서버를 별도 실행 단위로 분리). 개발 기반 스토리로 새 TR 없음

**ADR Governing Implementation**: ADR-0001: Multiplayer Networking Architecture (결정 12·19, 개정 4)
**ADR Decision Summary**: 서버 부트스트랩은 `MultiplayerRolesManager.ActiveMultiplayerRoleMask`로 분기해 에디터 서버 인스턴스와 서버 빌드가 같은 코드를 탄다. `UNITY_SERVER`는 빌드 전용 제약(서버 전용 컴파일, UI 씬 제외)에만 쓴다. 규칙·동기화 테스트는 Play Mode Scenario(메인 에디터 클라이언트 + 추가 에디터 Server 역할)로 한다.
**ADR Version**: 2026-11-21 (개정 4)

**Engine**: Unity 6000.6.8f1 | **Risk**: MEDIUM
**Engine Notes**: Multiplayer Play Mode 3.0.0·Dedicated Server 3.0.0은 6000.6.8f1 기본 포함(설치본 확인), Multiplayer Tools 2.2.12는 에디터 manifest 등록 버전. 이 프로젝트에서 실제 실행은 이 스토리가 처음. 네트워크 프리팹을 고친 뒤 가상 플레이어 접속이 깨지면 가상 플레이어를 재시작한다(커뮤니티 보고).

**Control Manifest Rules (this layer)**: N/A (no control manifest)

---

## Acceptance Criteria

*ADR-0001 결정 12·19에서 가져옴:*

- [ ] `com.unity.dedicated-server`, `com.unity.multiplayer.playmode`, `com.unity.multiplayer.tools`를 `Packages/manifest.json`에 추가한다. 컴파일 오류가 없다
- [ ] Project Settings > Multiplayer에서 Multiplayer Roles를 켠다. 서버 Build Profile의 역할은 Server다
- [ ] `DedicatedServerBootstrap`의 파일 전체 `#if UNITY_SERVER` 가드를 없애고, 역할이 Server일 때만 서버를 기동한다
- [ ] 서버 빌드를 `-batchmode`로 기동하면 Story 001과 같이 뜨고 종료 사유 로그가 남는다(기존 기능 깨짐 없음)
- [ ] 에디터를 Client 역할로 Play하면 서버가 뜨지 않고 기존 싱글 플레이가 이전처럼 동작한다
- [ ] 개발용 직접 접속: 설정(ScriptableObject)의 개발용 플래그가 켜져 있고 역할이 Client면, 에디터 클라이언트가 설정의 loopback 주소·포트로 접속한다. 플래그 기본값은 꺼짐이다
- [ ] Play Mode Scenario "Dev: Server + 2 Clients"(메인 에디터 Client, 추가 에디터 1 Server, 추가 에디터 1 Client)를 저장한다. 실행하면 서버 인스턴스 콘솔에 클라이언트 접속 2건이 찍힌다
- [ ] Play Mode Scenario 창과 서버 인스턴스 콘솔 스크린샷을 `production/qa/evidence/`에 보관한다

---

## Implementation Notes

- UI 씬 제외(`GameSceneLoader.cs`)는 서버 빌드의 셰이더·폰트 제약이므로 `#if UNITY_SERVER`를 유지한다(결정 12 예외).
- 포트·주소·개발용 플래그는 기존 `ServerNetworkConfig`에 둔다. 코드 상수 금지.
- 호스트 토큰·자식 프로세스·상태 파일은 만들지 않는다. 개발용 접속은 connection approval 없이 접속하는 최소 경로다.
- Unity 에디터 조작은 Unity CLI로 한다. 에디터가 열려 있는 동안 `.unity`·`.prefab`·`.asset`·`.meta`를 손으로 편집하지 않는다.

---

## Out of Scope

- Story 003: 호스트 자식 프로세스 기동, 상태 파일, 호스트 토큰, LAN 접속
- 검증 15(Script 물리 모드 서버 틱의 다중 인스턴스 동작): 서버 틱을 구현하는 스토리에서 확인
- Network Simulator 프리셋 적용(지연 테스트 스토리)

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: 실행 기록. `qa.level: minimal`이라 자동 테스트는 면제.
- Play Mode Scenario 실행 스크린샷과 서버 접속 로그 발췌, 서버 빌드 `-batchmode` 재기동 로그

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/networking/story-001-ngo-install-dedicated-server.md`
- Unlocks: `production/epics/networking/story-003-lan-direct-join.md`

## Files

- `Assets/Game/Scripts/Networking/DedicatedServerBootstrap.cs`
- `Assets/Game/Scripts/Networking/ServerNetworkConfig.cs`
- `Assets/Game/Configs/ServerNetworkConfig.asset`
- `Assets/Settings/PlayMode/Dev_Server_2Clients.asset`
- `Packages/manifest.json`
- `Packages/packages-lock.json`
- `ProjectSettings/MultiplayerManager.asset`
- `ProjectSettings/VirtualProjectsConfig.json`
- `ProjectSettings/Packages/`

## Completion Notes
**Completed**: 2026-11-21
**Criteria**: 8/8 passing (AC-5는 사용자 확인)
**Deviations**: 없음. 권고: 서버 빌드 `-batchmode` 재기동 로그(18:15)가 시작 시점 수정(DeferredStart.Start, 18:20) 앞에 찍힘. Player 3 클라이언트 콘솔에 MPPM 내부 예외 1건(GetBool main thread). MainGameScene.unity의 기존 미커밋 변경은 이 스토리 범위 밖.
**Test Evidence**: `production/qa/evidence/story-002-editor-multiplayer-test/` (Scenario 창, 서버 콘솔, Client 단독 Play, 서버 빌드 재기동 로그). 자동 테스트는 qa.level minimal이라 면제
**Reviewed by**: self, no independent review

## Calls beyond the design
- 역할 판정은 마스크 정확 일치, ClientAndServer면 아무것도 안 함 (`DedicatedServerBootstrap.cs`) · kept 2026-11-21
- 메인 에디터 활성 역할 ClientAndServer→Client (Scenario) · kept 2026-11-21
- 에디터 서버 인스턴스는 종료 사유 파일 없이 로그만 · kept 2026-11-21
- `DevDirectConnect` 코드 기본 false, `ServerNetworkConfig.asset`에서는 ON, 주소 127.0.0.1, 포트는 기존 Port 재사용 · kept 2026-11-21
- 서버 인스턴스 로그를 메인 에디터로 스트리밍 · kept 2026-11-21
- Scenario 이름 `Dev_Server_2Clients` (콜론 불가) · kept 2026-11-21
- 에디터에서 AssetDatabase로 설정을 찾는 `#if UNITY_EDITOR` 분기 · kept 2026-11-21
- Build Profile 역할은 서브타깃 Server(`m_Subtarget: 1`) · kept 2026-11-21
