# ADR-0001: Multiplayer Networking Architecture

## Status
Proposed

## Date
2026-11-21

## Last Verified
2026-11-21 (엔진 참조 `modules/networking.md`는 6.3 기준 2026-02-13 검증분. 6.4~6.6 네트워킹 변경은 NOT SOURCEABLE)

## Decision Makers
사용자 + Claude (solo 모드, TD-ADR 생략)

## Summary
2~4인 협동 배달에서 서버 권한 Rigidbody 차량·물류·시계·정산 상태를 분리 실행 서버로 동기화할 구조가 없다. Netcode for GameObjects(NGO) + Unity Transport를 쓰고, 서버는 Dedicated Server 빌드를 호스트 클라이언트가 자식 프로세스로 기동하며, 차량은 서버 권한 + 클라이언트 보간으로 복제한다.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6000.6.8f1 |
| **Domain** | Networking |
| **Layer** | Foundation |
| **Knowledge Risk** | HIGH |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `modules/networking.md`, `current-best-practices.md`(Multiplayer), `deprecated-apis.md`, `breaking-changes.md`(네트워킹 항목 없음) |
| **Post-Cutoff APIs Used** | NGO 2.x 통합 RPC(`[Rpc(SendTo.Server)]`), Dedicated Server 빌드 서브타깃. 둘 다 6000.6.8f1 호환 미확인 |
| **Verification Required** | (1) 6000.6.8f1과 호환되는 `com.unity.netcode.gameobjects` 버전(Package Manager에서 설치·컴파일 확인) (2) Dedicated Server 빌드가 이 프로젝트(URP, Input System, UI)에서 성공하고 `-batchmode`로 기동 (3) 서버 권한 Rigidbody 차량이 6.4~6.6 PhysX에서 기존과 같이 동작 (4) 클라이언트 NetworkRigidbody 보간이 차량 충돌 후 튀지 않음 (5) 호스트가 서버를 자식 프로세스로 띄우고 종료·강제 종료 시 서버가 함께 내려감 |

> 엔진 참조의 RPC 예시는 `[ServerRpc]`/`[ClientRpc]`(NGO 1.x 방식)이다. 본 ADR은 NGO 2.x 방식(`[Rpc(SendTo.…)]`)을 전제로 하며, 설치한 버전에서 어느 쪽이 유효한지 확인 후 그 방식 하나로 통일한다.

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None (기존 ADR 0건, `docs/registry/architecture.yaml` 비어 있음 확인) |
| **Enables** | 세션 참가 수단 ADR(코드·IP·Relay), 차량 클라이언트 예측 ADR(AC-NET-13 실측 결과가 나쁠 때), 서버 빌드·배포 ADR |
| **Blocks** | networking 에픽·스토리 전체(아직 생성 전) |
| **Ordering Note** | 이 ADR 수락 전에 `design/gdd/networking.md`의 `/design-review`와 Open Questions 1·2를 정리하는 것이 안전하다 |

## Context

### Problem Statement
GDD `networking.md`는 서버 권한(차량 물리·물류·시계·정산), 분리 실행 서버, 호스트 플레이어도 클라이언트, 하루 중 참가·복귀를 확정했다. 현재 코드는 단일 프로세스 MonoBehaviour 구조(`PlayerMovementController`가 Rigidbody를 FixedUpdate에서 `AddForce`로 구동, `GameTimeSystem`·`DeliveryGenerationSystem` 등이 로컬 상태 보유)라 기술 선택과 서버/클라이언트 경계가 필요하다.

### Constraints
- 서버가 상태의 유일한 기준이고 클라이언트가 직접 바꾸는 공유 상태는 없다(GDD 규칙 2).
- 싱글은 참가자 1명인 같은 세션이며 멀티 전용 규칙 분기 금지(규칙 3).
- 참가·복귀는 하루 중에도 가능(규칙 4), 호스트 이탈 시 서버 종료(Edge Cases).
- 충돌 판정은 물리 콜백과 분리(`collision-durability-rules.md` 4장 3항), 모든 차량이 동적 바디(같은 문서 4장 2항).
- 1인 개발·솔로 모드: 직접 구현 분량 최소화.
- 대역폭·갱신 주기·프레임 예산은 근거 없음(NOT SOURCEABLE) → 초기값은 튜닝 값으로 두고 실측.

### Requirements
- 최대 4명, 한 세션의 상태가 전원에게 같게 보임.
- 자기 차량 조작 반영 지연이 가장 중요한 체감 요소(합격선 미정, AC-NET-13이 측정).
- 클라이언트 요청은 서버가 검증·거부하고 표시를 서버 상태에 맞춘다.
- 서버 규칙 코드는 도메인 로직이고 네트워크 코드와 분리해 단위 테스트 가능해야 한다.

## Decision

1. **프레임워크**: NGO + Unity Transport(UTP). 설치할 정확한 버전은 6000.6.8f1에서 컴파일·실행으로 검증해 고정한다(검증 전 버전 번호는 이 문서에 쓰지 않는다).
2. **서버 구성**: Dedicated Server 빌드를 별도로 만든다. 호스트 클라이언트가 세션 생성 시 서버 실행 파일을 **자식 프로세스**로 기동(포트·부모 PID·세션 설정을 인자로 전달)하고 loopback으로 일반 클라이언트처럼 접속한다. 싱글도 같은 경로다. 서버는 부모 프로세스 종료 또는 호스트 접속 끊김을 감지하면 스스로 종료한다.
3. **권한 경계**: 도메인 규칙(시계·물류 생성·배정·마감·정산·별점·충돌 판정·복구)은 **서버에서만** 실행한다. 클라이언트는 입력·요청을 보내고 복제된 상태를 표시한다.
4. **차량**: 참가자 차량 1대 = 서버가 스폰하는 NetworkObject(프리팹 루트, 소유자 = 해당 참가자). 서버가 Rigidbody 물리를 돌린다. 클라이언트 쪽 차량은 물리를 끄고(kinematic) 서버 Transform을 보간해 표시한다. 자기 차량도 예측하지 않는다. 조작 입력은 고정 주기로 서버에 보내는 입력 구조체 1개(방향·주행 모드·드리프트·복구)로 한정한다.
   - 보간 컴포넌트(`NetworkTransform`/`NetworkRigidbody` 계열)의 권한은 **서버 권한**으로 설정한다. 소유자(클라이언트) 권한 모드는 금지한다. 소유권은 입력 RPC를 보낼 자격과 카메라·HUD 대상 판별에만 쓴다.
   - 클라이언트 kinematic 전환은 직접 코드로 하지 않고 컴포넌트 동작에 맡기되, 설치 버전에서 그렇게 동작하는지 첫 스토리에서 확인한다(안 되면 `OnNetworkSpawn`에서 `!IsServer`일 때 kinematic).
   - 입력 구조체의 틱 번호는 NGO 네트워크 틱이다. 서버는 FixedUpdate마다 그 시점에 받은 최신 입력을 적용한다.
5. **복제 수단**: 
   - 연속 값(차량·NPC 위치) → `NetworkTransform`/`NetworkRigidbody` 계열 보간.
   - 하루 공통 상태(세션 상태, 서버 시계 기준값, 물류 목록·예약, 정산 결과) → 서버 소유 NetworkBehaviour 한 곳의 `NetworkVariable`/`NetworkList`(INetworkSerializable 구조체).
   - 일회성 이벤트(조작 활성화·잠금, 충돌 피드백, 입·퇴장 토스트, 정산 전환) → 서버→클라이언트 RPC.
   - 흩어진 물류 오브젝트 → 서버가 스폰하는 NetworkObject.
6. **입력 전달**: 초기에는 신뢰성 RPC로 고정 주기 전송한다. GDD의 "입력이 없는 구간은 0(마지막 입력 유지 안 함)" 규칙과 맞기 위해서다. AC-NET-13 실측에서 지연이 문제면 비신뢰 전송 + 중복 전송으로 바꾸는 것을 후속 결정으로 한다.
7. **시계**: 서버가 게임 시계 기준값을 갱신 시점에만 복제하고, 클라이언트는 그 사이를 로컬 `deltaTime`으로 진행해 표시하며 어긋남이 설정 값을 넘으면 서버 값으로 맞춘다. 판정은 서버 값만 쓴다.
8. **세션 상태 머신**: GDD의 Closed/Lobby/DayStarting/DayRunning/Settlement를 서버가 소유하고 전이를 복제한다. 참가·이탈은 전이가 아니다.
9. **접속 정보**: 클라이언트는 `(address, port)`만 받아 접속한다. 이 값을 어떻게 얻는지(코드·IP·Relay)는 후속 ADR이며, 이 ADR은 방식을 정하지 않는다.
10. **코드 구조**: 도메인 규칙 클래스(예: 시계·배정·마감·충돌 판정·정산)는 `Unity.Netcode`를 참조하지 않는다. NetworkBehaviour 어댑터가 도메인 클래스를 서버에서 구동하고 결과를 복제한다. UI는 복제된 상태를 읽기 전용 인터페이스로 구독한다.
11. **호스트 식별**: 호스트 클라이언트가 세션 생성 때 임의 토큰을 만들어 서버 기동 인자(`--host-token`)로 넘기고, 자기 접속 요청 페이로드(connection approval)에도 같은 토큰을 넣는다. 서버는 토큰이 맞는 clientId를 호스트로 기록한다. "하루 시작"·"세션 닫기"·"정산 확인 후 서버 종료" 요청과 호스트 이탈 판정은 이 clientId로만 한다. 첫 접속 순서로 호스트를 정하지 않는다.
12. **씬·프리팹 일치**: 서버와 클라이언트 빌드는 같은 게임 씬과 같은 NetworkPrefabs 목록을 쓴다. 건물·배송지처럼 씬에 놓인 정적 오브젝트는 네트워크 동기화 대상이 아니다(NetworkObject 미부착). 동적 상태가 필요한 것만 서버가 스폰한다. 서버 전용 부트스트랩(카메라·UI 생성 생략)은 빌드 정의 기호로 분기한다.
13. **적재 물류 표현**: 차량에 실린 물류는 NetworkObject로 재부모화하지 않는다. 적재는 `ParcelState`의 상태·차량 ID로만 복제하고 클라이언트가 표시한다.

### Architecture Diagram
```
 호스트 PC
 ┌────────────────────────┐   spawn(child) / parent-PID watch
 │ Host Client (Game build)│ ───────────────────────────────┐
 └──────────┬─────────────┘                                  ▼
            │ loopback            ┌───────────────────────────────────┐
            │                     │ Dedicated Server (Server build)    │
 ┌──────────┴──────────┐ UTP      │  Domain rules (no Netcode refs)    │
 │ Client 2..4 (remote)│◄────────►│  Session state machine             │
 └─────────────────────┘  RPC /   │  Rigidbody vehicles, NPC, parcels  │
                          NetVar  │  NetworkBehaviour adapters         │
                                  └───────────────────────────────────┘
 클라이언트: 입력 송신 → 서버 / 서버 상태 수신 → 보간·UI 표시 (공유 상태 쓰기 없음)
```

### Key Interfaces
- `VehicleInput` (INetworkSerializable struct): 방향, 주행 모드, 드리프트, 복구, 틱 번호. 소유 클라이언트 → 서버.
- `SessionState` (enum, 서버 소유 NetworkVariable): Closed/Lobby/DayStarting/DayRunning/Settlement.
- `ParcelState` (INetworkSerializable struct): 물류 ID, 상태(기존 `ParcelStatus` enum 재사용), 예약자 ID, 적재 차량 ID, 내구도, 남은 제한 시간, 폐기 여부, 목적지. 서버 소유 NetworkList.
- 요청 RPC(클라이언트 → 서버, 결과는 복제로 수신): 상차 시작·중단, 하차, 처분, 준비·준비 취소, 하루 시작(호스트만).
- 서버 → 클라이언트 이벤트 RPC: 조작 활성화·잠금, 충돌 피드백, 참가자 입·퇴장, 정산 전환.
- 서버 기동 인자: `--port`, `--parent-pid`, `--host-token`, 세션 설정 JSON 경로.
- 접속 요청 페이로드: 호스트 토큰(호스트만), 표시 이름.

### Implementation Guidelines
- 도메인 규칙 코드는 `Unity.Netcode`를 **참조하지 않아야 한다**(must never). 네트워크 어댑터만 참조한다.
- `NetworkManager.Singleton`은 어댑터 계층 밖에서 쓰지 않아야 한다. 필요한 값은 주입한다.
- 클라이언트는 공유 상태를 직접 쓰지 않아야 한다(쓰기는 서버 소유 NetworkVariable/RPC 요청만).
- 서버는 모든 클라이언트 요청을 검증하고, 어긋나면 거부하며 클라이언트 표시를 서버 상태로 되돌린다.
- 클라이언트 차량은 물리·충돌 판정을 실행하지 않아야 한다. 충돌 연출은 서버 결과 수신 후 재생한다.
- 충돌 판정은 물리 콜백과 분리해 서버에서 순수 함수로 호출한다(단위 테스트 대상).
- 네트워크 주기·타임아웃·보간 값은 외부 설정(ScriptableObject)에 둔다. 코드에 상수로 박지 않는다.
- 서버 프로세스는 종료 사유를 로그 파일에 남긴다.
- NetworkObject는 프리팹 루트에만 붙이고 NetworkPrefabs 리스트에 등록한다.
- 물리 콜백(`OnCollision*`, `OnTrigger*`)에서 게임 상태를 바꾸는 코드는 서버에서만 실행해야 한다(`IsServer` 가드 또는 서버 전용 컴포넌트). 클라이언트 kinematic 차량의 콜라이더도 콜백을 일으킬 수 있다.
- static 가변 상태를 두지 않아야 한다. 불가피하면 `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`에서 초기화한다. 현재 `ProjectSettings/EditorSettings.asset`은 `m_EnterPlayModeOptionsEnabled: 1`, `m_EnterPlayModeOptions: 0`이며 6.6에서 이것이 도메인 리로드 생략인지는 NOT SOURCEABLE이라, 생략된다고 가정하고 작성한다.
- RPC 표기는 설치한 NGO 버전에서 확인한 한 방식(`[Rpc(SendTo.…)]` 또는 `[ServerRpc]`/`[ClientRpc]`)으로 통일한다. 두 방식을 섞지 않아야 한다.
- 복제 구조체(`ParcelState` 등)에는 관리형 `string`을 넣지 않는다. 정수 ID나 고정 길이 타입을 쓴다.
- 코드의 `EntityId`를 int로 캐스팅하거나 `GetInstanceID()`를 쓰지 않는다(6.4 deprecated, 6.5 removed, `deprecated-apis.md`).

## Alternatives Considered

### Alternative 1: NGO + 자기 차량 클라이언트 예측·서버 보정
- **Description**: 자기 차량을 로컬에서 먼저 시뮬레이션하고 서버 값과 다르면 보정
- **Pros**: 조작 지연 체감이 가장 낮다
- **Cons**: 차량 물리·충돌을 클라이언트와 서버 두 곳에서 재현해야 해 복잡하고, 충돌 후 보정이 눈에 띈다. 1인 개발 분량 초과 위험
- **Rejection Reason**: 지연 실측(AC-NET-13) 전에는 필요 근거가 없다. 실측이 나쁘면 후속 ADR로 추가한다

### Alternative 2: Mirror 등 서드파티 프레임워크
- **Description**: 커뮤니티 프레임워크 사용
- **Pros**: 오래된 프로젝트에서 검증, 자료 많음
- **Cons**: 6000.6.8f1 호환 근거 없음. 엔진 참조·Unity 플러그인 문서가 NGO 기준이라 지원 범위 밖
- **Rejection Reason**: 공식 프레임워크와 엔진 참조 문서를 따른다

### Alternative 3: 호스트 클라이언트 프로세스 안에서 서버 실행(StartHost)
- **Description**: 호스트 플레이어 프로세스가 서버를 겸함
- **Pros**: 구현·빌드가 가장 단순, 서버 빌드 불필요
- **Cons**: GDD가 확정한 분리 실행·호스트도 일반 클라이언트 규칙과 어긋나고 호스트 지연이 0이라 불공평
- **Rejection Reason**: GDD 결정(2026-11-21)과 충돌

### Alternative 4: 같은 빌드를 -batchmode로 재기동한 헤드리스 자식 프로세스
- **Description**: Dedicated Server 빌드 없이 클라이언트 실행 파일을 헤드리스로 실행
- **Pros**: 빌드 타깃이 1개
- **Cons**: 서버에 그래픽·UI 자산이 포함되어 용량·메모리가 커지고 서버 전용 코드 분리가 흐려짐
- **Rejection Reason**: 서버 코드 경계를 빌드 단위로 강제하는 편이 안전하다. 다만 Dedicated Server 빌드가 이 프로젝트에서 안 되면 이 대안으로 되돌린다

## Consequences

### Positive
- GDD의 서버 권한·분리 실행·싱글=참가자 1명 규칙을 그대로 구현한다.
- 도메인 규칙이 네트워크와 분리돼 단위 테스트 가능하다.
- 서버 코드·자산이 빌드 단위로 분리된다.

### Negative
- 싱글 플레이도 서버 자식 프로세스를 띄워 시작 시간과 메모리가 늘어난다(측정 필요).
- 빌드 타깃이 2개(Game, Dedicated Server)로 늘고 CI·배포가 복잡해진다.
- 자기 차량 조작 지연이 RTT + 보간 지연만큼 생긴다.
- 기존 단일 프로세스 코드(플레이어 입력·물류·시계 등)를 서버/클라이언트로 나누는 마이그레이션이 필요하다.
- 금지 패턴: 도메인 규칙의 Netcode 참조, 클라이언트의 공유 상태 직접 쓰기, 어댑터 밖의 `NetworkManager.Singleton` 사용.

## Risks
- **NGO 버전이 6000.6.8f1과 호환되지 않음**: 구체 사유로 `GetInstanceID()`가 6.5에서 제거되고 `EntityId`로 바뀐 점이 있다(`deprecated-apis.md`). 이를 반영하지 않은 NGO는 컴파일이 깨질 수 있다. 설치·컴파일 검증을 구현 첫 스토리로 두고, 불가하면 이 ADR을 Superseded로 두고 재결정.
- **Hub 에디터 6000.6.8f1에 Dedicated Server 빌드 모듈이 없음**: 첫 스토리에서 모듈 설치 여부 확인.
- **물리 틱과 네트워크 틱 불일치로 보간 품질 저하**: 두 값을 설정에 두고 실측. 관계 설정법은 NOT SOURCEABLE.
- **서버 헤드리스 실행에서 `Camera.main`·UI·URP 의존 코드가 오류**: 서버 부트스트랩 분기(결정 12), 서버 빌드에서 MeshCollider 메시가 빠지지 않는지 첫 스토리에서 확인. 모두 UNVERIFIED.
- **에디터 개발 루프가 번거로움(Play 모드마다 서버 빌드 필요)**: 에디터에서는 서버를 별도 에디터 인스턴스나 빌드로 띄우는 개발용 경로를 둔다. Multiplayer Play Mode 같은 도구의 6.6 지원 여부는 NOT SOURCEABLE.
- **Dedicated Server 빌드가 URP·Input System 등과 함께 성공하지 않음**: 대안 4로 되돌린다.
- **서버 권한 Rigidbody 차량이 보간 표시에서 충돌 후 튀거나 늦음**: AC-NET-13·플레이테스트 실측 후 예측 ADR 추가.
- **호스트 PC의 자식 서버에 원격 참가자가 NAT 때문에 접속 불가**: 참가 수단 ADR(Relay 포함)에서 해결. 그 전에는 LAN·포트 포워딩 전제.
- **입력 신뢰성 RPC의 순서 대기로 입력이 몰려 도착**: 실측 후 비신뢰 전송 + 중복 전송으로 전환.
- **서버 프로세스 고아화(호스트 비정상 종료)**: 부모 PID 감시 + 접속 끊김 시 종료로 이중 방어. AC-NET-23·29로 검증.
- **싱글 시작이 느림**: 서버 기동 시간 측정, 기준 초과 시 싱글만 StartHost로 돌리는 결정은 GDD 규칙 3과 충돌하므로 ADR 개정 필요.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| networking.md | 규칙 1: 서버와 클라이언트 분리 실행, 호스트도 클라이언트 | 결정 2: Dedicated Server 자식 프로세스 + loopback 접속 |
| networking.md | 규칙 2·11·13: 서버만 상태의 기준, 판정은 서버 | 결정 3·10 |
| networking.md | 규칙 3: 싱글 = 참가자 1명 같은 규칙 | 결정 2: 싱글도 같은 서버 경로 |
| networking.md | 규칙 4·Edge: 하루 중 참가·복귀, 호스트 이탈 시 서버 종료 | 결정 2(부모 PID·접속 끊김 감시), 결정 8 |
| networking.md | 규칙 5·6: 하루 시작 동시 활성화, 서버 시계 | 결정 7·8, 이벤트 RPC |
| networking.md | 규칙 7·12: 서버 권한 차량 물리·충돌·복구 | 결정 4, 가이드라인(판정 분리) |
| networking.md | 규칙 9: 물류 예약 | 결정 5(ParcelState 예약자), 요청 RPC |
| networking.md | 규칙 8·AC-NET-13: 조작 지연 | 결정 4·6, Risks(실측 후 예측 ADR) |
| collision-durability-rules.md | 4장 2·3항: 모든 차량 동적 바디, 판정과 콜백 분리 | 결정 4, 가이드라인 |
| game-concept.md | 3·8·10·12장 | 서버가 규칙 그대로 실행(결정 3) |

## Performance Implications
- **CPU**: 서버가 4대 차량 + NPC 물리를 처리. 예산 NOT SOURCEABLE → 구현 후 `/perf-profile`로 실측
- **Memory**: 서버 프로세스 1개 추가(싱글 포함). 크기 미측정
- **Load Time**: 서버 기동 시간만큼 세션 생성이 늘어남. 측정 필요
- **Network**: 최대 4클라이언트, 차량·NPC 위치 보간 복제. 대역폭·틱 주기는 근거 없음(NOT SOURCEABLE), 설정 값으로 두고 Network Profiler로 실측

## Migration Plan
1. NGO 설치·버전 검증, Dedicated Server 빌드 타깃 생성(첫 스토리).
2. 도메인 규칙과 입력/표시를 분리: `PlayerMovementController`의 입력 읽기와 물리 구동을 나눠 서버는 `VehicleInput`을 받아 구동.
3. 시계·물류 생성·배정·마감·정산을 서버에서만 실행하도록 어댑터 추가. UI는 복제 상태 구독.
4. 싱글 플로우를 서버 자식 프로세스 경로로 전환하고 기존 QA 테스트 케이스(AC-NET-03)로 확인.
5. 멀티 기능(예약·참가·복귀·이탈 물류) 순서로 추가.

## Validation Criteria
- AC-NET-01~29(`design/gdd/networking.md`) 통과(AC-NET-13은 측정만).
- 싱글 기존 QA 테스트 케이스 전부 통과(AC-NET-03).
- 서버 자식 프로세스 기동·종료·호스트 강제 종료 시 고아 프로세스 없음.
- 지연 시뮬레이션(Network Simulator)에서 조작·충돌 표시가 허용 가능한지 플레이테스트.

## Related
- `design/gdd/networking.md`, `design/gdd/collision-durability-rules.md`, `design/gdd/game-concept.md`
- `docs/engine-reference/unity/modules/networking.md`
- 후속 ADR(미작성): 세션 참가 수단, 서버 빌드·배포, 차량 클라이언트 예측(조건부)
