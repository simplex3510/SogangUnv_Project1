# Story 001: NGO 설치와 Dedicated Server 빌드·기동 확인

> **Epic**: networking
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Integration
> **Estimate**: M
> **Manifest Version**: N/A (no control manifest)
> **Last Updated**: 2026-11-21

## Context

**GDD**: `design/gdd/networking.md`
**Requirement**: `TR-networking-001` (규칙 1: 서버를 호스트 PC의 별도 실행 단위로 분리)

**ADR Governing Implementation**: ADR-0001: Multiplayer Networking Architecture
**ADR Decision Summary**: NGO + Unity Transport를 쓰고, 서버는 Dedicated Server 빌드를 호스트 클라이언트가 자식 프로세스로 기동한다. 서버와 클라이언트는 같은 게임 씬·NetworkPrefabs 목록을 쓴다.
**ADR Version**: 2026-11-21

**Engine**: Unity 6000.6.8f1 | **Risk**: HIGH
**Engine Notes**: NGO 2.14.0 + Unity Transport 6.6.0은 스파이크(프로젝트 밖)에서만 확인됨. 메인 프로젝트(URP, Input System, UI)에서 재확인이 이 스토리의 목적. RPC 표기는 설치한 버전에서 확인한 한 방식으로 통일한다.

**Control Manifest Rules (this layer)**: N/A (no control manifest)

---

## Acceptance Criteria

*ADR-0001 Verification Required (1)(2)(3)(12)과 Decision 12에서 가져옴:*

- [ ] NGO 2.14.0과 Unity Transport 6.6.0을 메인 프로젝트에 설치하고 `Packages/manifest.json`에 버전을 고정한다. 6000.6.8f1에서 컴파일 오류가 없다 *(검증 1)*
- [ ] 설치한 NGO 버전에서 유효한 RPC 표기(`[Rpc(SendTo.…)]` 여부)를 확인하고 한 방식으로 정해 기록한다
- [ ] Dedicated Server 빌드가 이 프로젝트(URP, Input System, UI)에서 성공한다 *(검증 2)*
- [ ] 빌드한 서버를 `-batchmode`로 기동하면 에러 없이 뜨고, 종료 사유를 포함한 로그 파일이 남는다 *(검증 2)*
- [ ] 서버 빌드가 클라이언트와 같은 게임 씬·NetworkPrefabs 목록을 쓴다. 서버 전용 부트스트랩(카메라·UI 생성 생략)은 빌드 정의 기호로 분기한다 *(결정 12)*
- [ ] 같은 포트가 이미 점유된 상태에서 서버를 기동하면 바인딩 실패가 반환값·예외·전송 이벤트 중 무엇으로 드러나는지 확인해 기록한다 *(검증 12)*
- [ ] 서버 빌드·패키지 추가 뒤에도 기존 클라이언트(싱글) 빌드와 에디터 플레이가 이전처럼 동작한다
- [ ] 빌드 결과와 서버 기동 로그 발췌를 `production/qa/evidence/`에 보관한다

---

## Implementation Notes

*ADR-0001 Decision 1·12, Implementation Guidelines:*

- 버전은 `Packages/manifest.json`에 고정한다. 엔진 참조의 RPC 예시(`[ServerRpc]`)는 NGO 1.x 방식이므로 쓰지 않고 설치한 버전에서 확인한 방식 하나로 통일한다.
- 건물·배송지 같은 씬 정적 오브젝트는 NetworkObject를 붙이지 않는다. 이 스토리에서 NetworkObject 스폰이나 서버 로직은 만들지 않는다(빌드·기동 확인까지).
- 네트워크 주기·타임아웃·포트는 외부 설정(ScriptableObject)에 둔다. 코드에 상수로 박지 않는다.
- 서버 프로세스는 종료 사유를 로그 파일에 남긴다.
- Unity 에디터 조작은 Unity CLI로 한다(`unity command … --caller plugin --skill unity-cli`). 에디터가 열려 있는 동안 `.unity`·`.prefab`·`.asset`·`.meta`를 손으로 편집하지 않는다.
- 이 스토리의 확인 결과(바인딩 실패 방식, 확정 RPC 표기)는 ADR-0001 검증 항목에 반영할 메모로 남긴다. ADR 개정 자체는 별도.
- 서버 부트스트랩의 역할 분기는 ADR-0001 개정 4(결정 12·19)에 따라 Story 002로 이관. 이 스토리는 개정 전 결정 12(빌드 정의 기호) 기준으로 완료한다.

---

## Out of Scope

*다른 스토리가 맡는다 — 여기서 구현하지 않는다:*

- Story 002: 서버 부트스트랩 역할 분기·에디터 멀티 테스트(Multiplayer Play Mode)
- Story 003: Direct 세션 생성·참가, 서버 자식 프로세스 기동·상태 파일 채널
- 차량 예측·재조정, 도메인 서버 권한 이전(후속 스토리)
- 검증 항목 (3)~(11)·(13)·(14)

---

## Files

- `Packages/manifest.json`, `Packages/packages-lock.json`
- `ProjectSettings/ProjectSettings.asset` (Preloaded Assets)
- `Assets/Game/Scripts/Networking/ServerNetworkConfig.cs`
- `Assets/Game/Scripts/Networking/DedicatedServerBootstrap.cs`
- `Assets/Game/Scripts/Systems/GameSceneLoader.cs` (UNITY_SERVER 분기)
- `Assets/Game/Editor/ServerBuild.cs`
- `Assets/Game/Configs/ServerNetworkConfig.asset`
- `Assets/DefaultNetworkPrefabs.asset`

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: playtest doc / 실행 기록. `qa.level: minimal`이라 자동 테스트는 면제.
- 빌드 성공 결과와 `-batchmode` 서버 로그 발췌를 `production/qa/evidence/`에 보관

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None
- Unlocks: `production/epics/networking/story-002-editor-multiplayer-test.md`

## Completion Notes
**Completed**: 2026-11-21
**Criteria**: 8/8 passing (deferred: none)
**Deviations**: ADVISORY — `Packages/manifest.json`을 에디터를 거치지 않고 직접 편집함(프로젝트 규칙 위반, 결과는 정상). OUT OF SCOPE — `MainGameScene.unity` 에디터 재저장 변경, ADR-0001·`networking.md` 수정은 이 스토리 작업이 아님.
**Test Evidence**: waived at qa.level minimal. 실행 기록: `production/qa/evidence/story-001-ngo-install-dedicated-server/` (run7 서버 빌드, run8 기동, run9 포트 충돌, client-start-screen.png)
**Reviewed by**: self, no independent review (엔진 API 점검은 unity-specialist, 문제 없음)

## Calls beyond the design
- 서버 설정 에셋을 Preloaded Assets로 읽음 (`ProjectSettings/ProjectSettings.asset`, `ServerNetworkConfig.cs`) · kept 2026-11-21
- 서버 시작 시점 AfterSceneLoad (`DedicatedServerBootstrap.cs`) · kept 2026-11-21
- 서버 빌드에서 UI 씬 생략 (`GameSceneLoader.cs`) · kept 2026-11-21
- 검증용 인자 `-exitLog`, `-autoQuitSeconds` (`DedicatedServerBootstrap.cs`) · kept 2026-11-21
- `DefaultNetworkPrefabs.asset`을 Assets 루트에 둠 · kept 2026-11-21
- 서버 빌드 뒤 에디터 서브타깃을 Player로 복원 (`ServerBuild.cs`) · kept 2026-11-21
