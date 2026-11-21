# ADR-0001: Multiplayer Networking Architecture

## Status
Accepted

## Date
2026-11-21 (개정 1: 결정 4·5·6 — 자기 차량 예측, 비신뢰+중복 입력. 근거 SPIKE-NOTE. 개정 2: 결정 4·5 보강, 결정 14~18 추가 — `VehicleSimState` 외부 수정값, 틱 일치, 서버 Script 모드 틱, 서버→호스트 파일 채널, 네트워크 이벤트 순서, 접속 끊김·정산 유지, 인원 설정 전달, 일시정지 제거. 근거 `docs/architecture/architecture-review-2026-11-21.md`, 사용자 결정. 개정 3: 결정 4·14·16·가이드라인·검증 보강 — 방향 회전 외부 수정값, Relay 상태 파일, 정산 유지 조건, 트리거 버퍼. 근거 `docs/architecture/architecture-review-2026-11-21-2.md`, 사용자 결정)

## Last Verified
2026-11-21 (엔진 참조 `modules/networking.md`는 6.3 기준 2026-02-13 검증분. 6.4~6.6 네트워킹 변경은 NOT SOURCEABLE)

## Decision Makers
사용자 + Claude (solo 모드, TD-ADR 생략)

## Summary
2~4인 협동 배달에서 서버 권한 Rigidbody 차량·물류·시계·정산 상태를 분리 실행 서버로 동기화할 구조가 없다. Netcode for GameObjects(NGO) + Unity Transport를 쓰고, 서버는 Dedicated Server 빌드를 호스트 클라이언트가 자식 프로세스로 기동하며, 차량은 서버 권한으로 시뮬레이션하되 소유 클라이언트는 자기 차량을 예측·재조정해 그리고, 다른 차량은 보간해 그린다.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6000.6.8f1 |
| **Domain** | Networking |
| **Layer** | Foundation |
| **Knowledge Risk** | HIGH |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `modules/networking.md`, `current-best-practices.md`(Multiplayer), `deprecated-apis.md`, `breaking-changes.md`(네트워킹 항목 없음) |
| **Post-Cutoff APIs Used** | NGO 2.x 통합 RPC(`[Rpc(SendTo.Server)]`·`SendTo.Owner`, 비신뢰 전송 지정), Dedicated Server 빌드 서브타깃. 스파이크(2026-11-21)에서 NGO 2.14.0 + Unity Transport 6.6.0(built-in)이 6000.6.8f1에서 컴파일·실행되고 `[Rpc(SendTo.Server)]`가 동작함을 확인. 예측용 `Physics.simulationMode`/`Physics.Simulate`는 컷오프 이전 API이나 6.6에서 미확인 |
| **Verification Required** | (1) NGO 버전 고정: 2.14.0 후보(스파이크 확인), 메인 프로젝트에서 재확인 (2) Dedicated Server 빌드가 이 프로젝트(URP, Input System, UI)에서 성공하고 `-batchmode`로 기동 (3) 서버 권한 Rigidbody 차량이 6.4~6.6 PhysX에서 기존과 같이 동작 (4) 클라이언트 NetworkRigidbody 보간이 차량 충돌 후 튀지 않음 (5) 호스트가 서버를 자식 프로세스로 띄우고 종료·강제 종료 시 서버가 함께 내려감 (6) 클라이언트 `SimulationMode.Script`에서 재시뮬레이션 비용(차량 4대 장면, RTT 200 ms 기준 서버 상태 1회당 약 10스텝)이 프레임 예산 안 (7) 소유 클라이언트에서 루트 렌더러·콜라이더를 꺼도 NetworkTransform 수신·소유권 RPC가 정상 (8) 같은 입력에서 서버와 예측 차량의 상태 차이(PhysX 비결정성) 크기 (9) 표시용 자식의 고정 스텝 사이 직접 보간이 떨림 없이 보이는지(예측 차량 Rigidbody interpolation은 None) (10) (stdout 대안을 쓸 때만) Windows Dedicated Server 빌드의 `Console.Out`이 부모의 리다이렉트로 오는지 (11) UTP disconnect 타임아웃 설정 이름·기본값(6.6 Unity Transport) (12) 포트 점유 시 바인딩 실패가 드러나는 방식 (13) 해결: 기존 코드가 `linearVelocity`로 6000.6.8f1에서 컴파일됨 (14) 끊김 콜백 시점에 소유 차량 NetworkObject가 파괴되는지 |

> 엔진 참조의 RPC 예시는 `[ServerRpc]`/`[ClientRpc]`(NGO 1.x 방식)이다. 본 ADR은 NGO 2.x 방식(`[Rpc(SendTo.…)]`)을 전제로 하며, 설치한 버전에서 어느 쪽이 유효한지 확인 후 그 방식 하나로 통일한다.

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None (기존 ADR 0건, `docs/registry/architecture.yaml` 비어 있음 확인) |
| **Enables** | 세션 참가 수단 ADR(ADR-0002), 서버 빌드·배포 ADR |
| **Blocks** | networking 에픽·스토리 전체(아직 생성 전) |
| **Ordering Note** | `/design-review`(2026-11-21)와 지연 스파이크(Open Question 2)는 완료. Open Question 1(2026-11-16 멀티 취소 이유)도 해결: 당시 발표 마감 내 구현 불가 판단. 발표·수업 프로젝트는 끝났고 이후 1인 개발로 마감 제약 없음(사용자, 2026-11-21) |

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
- 자기 차량 조작 반영 지연이 가장 중요한 체감 요소. 스파이크 실측: 예측 없이 기본 보간 시 RTT 17 ms에서 134 ms, +100 ms에서 약 230 ms. 사용자는 감수할 만하다고 봤으나 실시간성을 우선해 예측을 처음부터 넣기로 결정(2026-11-21).
- 클라이언트 요청은 서버가 검증·거부하고 표시를 서버 상태에 맞춘다.
- 서버 규칙 코드는 도메인 로직이고 네트워크 코드와 분리해 단위 테스트 가능해야 한다.

## Decision

1. **프레임워크**: NGO + Unity Transport(UTP). 설치할 정확한 버전은 6000.6.8f1에서 컴파일·실행으로 검증해 고정한다(검증 전 버전 번호는 이 문서에 쓰지 않는다).
2. **서버 구성**: Dedicated Server 빌드를 별도로 만든다. 호스트 클라이언트가 세션 생성 시 서버 실행 파일을 **자식 프로세스**로 기동(포트·부모 PID·세션 설정을 인자로 전달)하고 loopback으로 일반 클라이언트처럼 접속한다. 싱글도 같은 경로다. 서버는 부모 프로세스 종료 또는 호스트 접속 끊김을 감지하면 스스로 종료한다.
3. **권한 경계**: 도메인 규칙(시계·물류 생성·배정·마감·정산·별점·충돌 판정·복구)은 **서버에서만** 실행한다. 클라이언트는 입력·요청을 보내고 복제된 상태를 표시한다.
4. **차량**: 참가자 차량 1대 = 서버가 스폰하는 NetworkObject(프리팹 루트, 소유자 = 해당 참가자). 서버가 Rigidbody 물리를 돌리며 판정의 유일한 기준이다. 조작 입력은 입력 구조체 1개(방향·주행 모드·드리프트·복구)로 한정한다.
   - **다른 차량(비소유 클라이언트)**: 물리를 끄고(kinematic) 서버 권한 `NetworkTransform`/`NetworkRigidbody` 보간으로 표시한다. 보간 컴포넌트는 서버 권한이며 소유자(클라이언트) 권한 모드와 `AnticipatedNetworkTransform`은 금지한다(후자는 모든 관찰자의 보간을 끈다, NGO 2.14 소스 확인). kinematic 전환은 `NetworkRigidbody`의 `AutoUpdateKinematicState`(비권한 쪽 kinematic)에 맡긴다(NGO 2.14 `NetworkRigidbodyBase.cs` 확인).
   - **자기 차량(소유 클라이언트) — 클라이언트 측 예측**: 소유 클라이언트는 NetworkObject 루트의 렌더러·콜라이더를 끄고(루트는 네트워크 수신용으로만 남김), 로컬 전용 예측 차량(동적 Rigidbody, NetworkObject 아님)을 그린다.
     - 매 물리 스텝: 입력을 기록하고(입력 틱 = 클라이언트 물리 스텝 번호) 서버로 보낸 뒤, 같은 입력으로 예측 차량을 한 스텝 진행한다.
     - 재조정: 서버가 소유자에게 보내는 `VehicleSimState`(서버가 마지막으로 적용한 입력 틱 포함)를 받으면, 예측 차량을 그 상태로 되돌리고 그 틱 이후 기록된 입력을 다시 시뮬레이션한다. 재시뮬레이션 스텝이 설정 상한을 넘으면 되돌린 상태로 맞추고 끝낸다.
     - 클라이언트 물리는 `Physics.simulationMode = SimulationMode.Script`로 두고 매 FixedUpdate와 재시뮬레이션에서 `Physics.Simulate(fixedDeltaTime)`을 직접 호출한다. 클라이언트 씬의 동적 바디는 예측 차량뿐이라는 전제다(다른 차량·NPC는 kinematic 보간 표시).
     - 보정 스무딩: 재조정 전후 위치·회전 차이는 표시용 자식(메시·카메라 추적 대상)의 오프셋으로 옮겨 설정 시간 동안 0으로 줄인다. 차이가 설정 임계 이상(복구 순간이동 등)이면 오프셋 없이 즉시 맞춘다.
   - **판정은 서버만**: 예측 차량의 충돌·트리거는 표시와 물리 반응에만 쓰고 내구도·물류·장애물 효과를 바꾸지 않는다. 충돌 연출은 서버 결과 수신 후 재생한다(GDD 규칙 7). 예측 상태는 클라이언트 로컬 값이며 서버로 보내지 않는다(서버로 가는 것은 입력뿐 — `client_shared_state_write` 금지와 충돌 없음).
   - **차량 시뮬레이션 공용 코드**: 입력과 상태를 받아 다음 상태를 내는 차량 시뮬레이션은 `Unity.Netcode`를 참조하지 않는 하나의 코드로 두고 서버와 소유 클라이언트가 같이 쓴다(결정 10). 스텝 함수의 입력은 `VehicleInput`과 `VehicleSimState` 둘뿐이고, 그 밖의 값(싱글턴·static·다른 컴포넌트)을 읽지 않는다. 지면 판정용 물리 씬 질의(레이캐스트)는 허용한다. `VehicleSimState`에는 차량 내부 상태(주행 모드, 고속 게이지, 드리프트)와 **서버가 정하는 외부 수정값**을 모두 담는다.
     - 외부 수정값: 적재 속도 배율(8.6), 장애물 속도 효과 배율과 남은 시간(10.3), 복구 잠금 남은 시간(10.6), 조작 활성 여부(하루 시작·종료 잠금), 대기 중인 방향 회전 yaw(장애물 효과, 10.3. 다음 스텝에 적용한 뒤 0).
     - 외부 수정값은 서버만 바꾼다. 예측 차량은 마지막으로 받은 값으로 진행하며 남은 시간은 스텝마다 줄인다. 서버에서 새로 생긴 효과(트리거 진입·상차 완료)는 다음 `VehicleSimState` 수신 때 재조정으로 반영된다.
   - **틱 일치**: 서버와 클라이언트의 `Time.fixedDeltaTime`은 같은 설정 값(ScriptableObject)에서 읽는다. 입력 틱이 물리 스텝 번호이므로 값이 다르면 재조정이 성립하지 않는다.
   - **서버 입력 적용**: 서버는 고정 스텝마다(결정 15의 2단계) 입력 틱 순서대로 다음 입력 1개를 적용하고, 그 틱을 `VehicleSimState`의 마지막 적용 틱으로 기록한다. 그 틱의 입력이 없으면 입력 0을 적용한다(GDD Edge: 마지막 입력 유지 안 함). 대기 입력이 설정 개수를 넘으면 오래된 것부터 버린다.
5. **복제 수단**: 
   - 연속 값(차량·NPC 위치) → `NetworkTransform`/`NetworkRigidbody` 계열 보간(비소유 클라이언트 표시용).
   - 자기 차량 권위 상태 → 서버 → 소유자 RPC(`VehicleSimState`, 비신뢰). 서버는 물리 스텝 직후 상태를 기록하고, NGO 네트워크 틱마다 마지막 기록 1개를 보낸다(주기는 NetworkManager 틱 설정 값). 상태에 입력 틱이 들어 있어 같은 상태의 중복 전송이나 건너뜀은 재조정에 영향이 없다.
   - 하루 공통 상태(세션 상태, 서버 시계 기준값, 물류 목록·예약, 정산 결과) → 서버 소유 NetworkBehaviour 한 곳의 `NetworkVariable`/`NetworkList`(INetworkSerializable 구조체).
   - 일회성 이벤트(조작 활성화·잠금, 충돌 피드백, 입·퇴장 토스트, 정산 전환) → 서버→클라이언트 RPC.
   - 흩어진 물류 오브젝트 → 서버가 스폰하는 NetworkObject.
6. **입력 전달**: 매 물리 스텝 비신뢰 RPC(`[Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable, InvokePermission = RpcInvokePermission.Owner)]`, NGO 2.14 확인)로 보내고, 각 패킷에 최근 `InputRedundancy`개 입력(초기값은 튜닝 값)을 함께 담는다. 서버는 입력 틱으로 중복을 거른다. `InputRedundancy`는 한 패킷이 MTU 안에 들어가는 범위로 상한을 둔다. 입력 틱은 NGO 네트워크 틱이 아니라 클라이언트 물리 스텝 번호다. 패킷이 하나 유실돼도 다음 패킷이 채우므로, 신뢰성 전송의 재전송 대기로 입력이 몰려 도착해 예측이 어긋나는 일을 피한다.
7. **시계**: 서버가 게임 시계 기준값을 갱신 시점에만 복제하고, 클라이언트는 그 사이를 로컬 `deltaTime`으로 진행해 표시하며 어긋남이 설정 값을 넘으면 서버 값으로 맞춘다. 판정은 서버 값만 쓴다.
8. **세션 상태 머신**: GDD의 Closed/Lobby/DayStarting/DayRunning/Settlement를 서버가 소유하고 전이를 복제한다. 참가·이탈은 전이가 아니다.
9. **접속 정보**: 클라이언트는 `(address, port)`만 받아 접속한다. 이 값을 어떻게 얻는지(코드·IP·Relay)는 후속 ADR이며, 이 ADR은 방식을 정하지 않는다.
10. **코드 구조**: 도메인 규칙 클래스(예: 시계·배정·마감·충돌 판정·정산)는 `Unity.Netcode`를 참조하지 않는다. NetworkBehaviour 어댑터가 도메인 클래스를 서버에서 구동하고 결과를 복제한다. UI는 복제된 상태를 읽기 전용 인터페이스로 구독한다.
11. **호스트 식별**: 호스트 클라이언트가 세션 생성 때 임의 토큰을 만들어 서버 기동 인자(`--host-token`)로 넘기고, 자기 접속 요청 페이로드(connection approval)에도 같은 토큰을 넣는다. 서버는 토큰이 맞는 clientId를 호스트로 기록한다. "하루 시작"·"세션 닫기"·"정산 확인 후 서버 종료" 요청과 호스트 이탈 판정은 이 clientId로만 한다. 첫 접속 순서로 호스트를 정하지 않는다.
12. **씬·프리팹 일치**: 서버와 클라이언트 빌드는 같은 게임 씬과 같은 NetworkPrefabs 목록을 쓴다. 건물·배송지처럼 씬에 놓인 정적 오브젝트는 네트워크 동기화 대상이 아니다(NetworkObject 미부착). 동적 상태가 필요한 것만 서버가 스폰한다. 서버 전용 부트스트랩(카메라·UI 생성 생략)은 빌드 정의 기호로 분기한다.
13. **적재 물류 표현**: 차량에 실린 물류는 NetworkObject로 재부모화하지 않는다. 적재는 `ParcelState`의 상태·차량 ID로만 복제하고 클라이언트가 표시한다.
14. **서버 → 호스트 채널(상태 파일)**:
   - 호스트가 기동 인자 `--status-file <경로>`(사용자 임시 폴더, 세션마다 새 이름)를 넘기고, 서버는 그 파일에 상태 한 줄을 쓴다: `READY port=<n>`, `FAIL reason=<code>`, `CODE <joinCode>`(ADR-0002 2단계). 쓸 때는 임시 이름으로 쓴 뒤 바꿔 달아 반쯤 쓴 파일을 읽지 않게 한다.
   - 호스트 클라이언트는 짧은 주기로 파일을 확인해 (Direct) READY를 받은 뒤에만 loopback 접속한다. FAIL이거나 설정 시간 안에 READY가 없으면 서버 프로세스를 종료하고 세션 생성 실패를 표시한다. 세션이 끝나면 호스트가 파일을 지운다.
   - Relay 모드: 서버는 READY 대신 `CODE <joinCode>` 한 줄만 쓰고, 이것이 준비 신호다. 호스트는 loopback이 아니라 그 코드로 접속한다(ADR-0002 결정 3).
   - 대안: 표준 출력 한 줄 프로토콜. Windows 플레이어 빌드의 stdout 리다이렉트가 검증되면(검증 항목 10) 쓸 수 있으나, 호스트가 출력을 계속 읽지 않으면 파이프가 차서 서버가 멈출 수 있어 기본으로 하지 않는다.
   - 예전 서버 접속 방지: connection approval에서 페이로드에 호스트 토큰이 있는데 서버의 `--host-token`과 다르면 거부한다(사유: 다른 세션). 호스트는 거부를 받으면 세션 생성 실패로 처리한다.
   - 포트는 Direct 모드의 포트 포워딩을 위해 설정 값 고정이다(자동 할당 쓰지 않음). 바인딩 실패가 `StartServer` 반환값·예외·전송 이벤트 중 무엇으로 드러나는지는 검증 항목 (12).
15. **서버 틱과 네트워크 이벤트 처리 순서**:
   - 서버도 `Physics.simulationMode = SimulationMode.Script`로 두고, 서버 어댑터 한 곳이 고정 스텝마다 아래 순서를 직접 호출한다. Unity 기본 루프에서는 `FixedUpdate` 뒤에 물리와 `OnCollision*`이 돌아 판정이 한 스텝 늦어지므로 쓰지 않는다.
     1. 큐 반영: (1) 이탈 (2) 참가 (3) 요청(받은 순서)
     2. 차량 입력 적용(입력 틱 순서, 결정 4)
     3. `Physics.Simulate(fixedDeltaTime)` — 이 동안 들어온 접촉은 버퍼에 모으기만 한다
     4. 충돌 판정(버퍼의 접촉으로 Δv 판정, 순수 함수)
     5. 도메인 시계·판정(3.7 순서)
     6. 차량별 `VehicleSimState` 기록
   - NGO 콜백(접속·끊김)과 요청 RPC는 받는 즉시 도메인을 바꾸지 않고 큐에 넣는다. 그래서 같은 스텝의 상차 완료와 이탈은 이탈이 먼저다(GDD Edge).
   - 끊긴 참가자의 차량 NetworkObject는 NGO가 콜백 시점에 파괴할 수 있다(UNVERIFIED). 큐에서 꺼낼 때 파괴 여부를 확인하거나 차량 프리팹에 `DontDestroyWithOwner`를 켜 서버가 직접 제거한다.
16. **접속 끊김 판정과 정산 화면 유지**:
   - 접속 끊김은 Unity Transport의 disconnect 타임아웃 하나로 판정한다(값은 외부 설정, GDD `TODO(tuning)`). 클라이언트는 입력 0도 매 스텝 보내므로 입력 미수신과 연결 침묵이 같다. 별도 앱 레벨 타이머는 두지 않는다.
   - 클라이언트 빌드는 `Application.runInBackground = true`로 둔다. 꺼지면 창이 비활성일 때 입력 송신이 멈춰 끊김으로 처리된다.
   - 클라이언트가 접속 끊김을 받았을 때 마지막으로 받은 `SessionState`가 Settlement이고 정산 결과를 받았으면 안내 없이 정산 화면을 유지한다(GDD 규칙 15). 그 외 상태면 연결 끊김 안내 후 메인 화면이다.
17. **인원별 설정 전달**: 서버 어댑터가 하루 시작 요청을 수락하는 순간의 참가자 수를 도메인 초기화(3.4) 인자로 넘긴다. 도메인은 그 값으로 인원별 설정을 골라 하루 동안 바꾸지 않는다. 참가·이탈은 이 값을 바꾸지 않는다(GDD 규칙 3).
18. **일시정지 제거**: 모든 모드에서 게임 시간·물리를 멈추는 기능을 두지 않는다(GDD 3.2, 규칙 3). `GameManager.IsPaused`와 이를 읽는 조작 가드는 마이그레이션 때 제거한다. 가이드 패널 확인창의 정지(`design/quick-specs/guide-panel-addition-2026-11-18.md`)도 정지 없는 동작으로 바뀐다.

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
 자기 차량: 입력 → 로컬 예측 차량 즉시 진행 → VehicleSimState 수신 시 되감기·재시뮬레이션 → 표시 오차 스무딩
 다른 차량: NetworkTransform 스냅샷 보간 (내 화면에서 과거 시점)
```

### Key Interfaces
- `VehicleInput` (INetworkSerializable struct): 방향, 주행 모드, 드리프트, 복구, 입력 틱(클라이언트 물리 스텝 번호). 소유 클라이언트 → 서버, 최근 `InputRedundancy`개를 묶어 비신뢰 RPC.
- `VehicleSimState` (INetworkSerializable struct): 마지막 적용 입력 틱, 위치, 회전, 선속도, 각속도, 주행 모드, 고속 게이지, 드리프트 상태, 적재 속도 배율, 장애물 속도 배율, 장애물 효과 남은 시간, 복구 잠금 남은 시간, 조작 활성 여부, 대기 방향 회전 yaw. 서버 → 소유자 비신뢰 RPC. 차량 시뮬레이션 공용 코드의 입출력 타입과 같다.
- `SessionState` (enum, 서버 소유 NetworkVariable): Closed/Lobby/DayStarting/DayRunning/Settlement.
- `ParcelState` (INetworkSerializable struct): 물류 ID, 상태(기존 `ParcelStatus` enum 재사용), 예약자 ID, 적재 차량 ID, 내구도, 남은 제한 시간, 폐기 여부, 목적지. 서버 소유 NetworkList.
- 요청 RPC(클라이언트 → 서버, 결과는 복제로 수신): 상차 시작·중단, 하차, 처분, 준비·준비 취소, 하루 시작(호스트만).
- 서버 → 클라이언트 이벤트 RPC: 조작 활성화·잠금, 충돌 피드백, 참가자 입·퇴장, 정산 전환.
- 서버 기동 인자: `--port`, `--parent-pid`, `--host-token`, `--status-file`, 세션 설정 JSON 경로.
- 상태 파일 한 줄(서버 → 호스트): `READY port=<n>` / `FAIL reason=<code>` / `CODE <joinCode>`.
- connection approval 거부 사유: 정원 초과, 정산 중, 다른 세션(호스트 토큰 불일치).
- 접속 요청 페이로드: 호스트 토큰(호스트만), 표시 이름.

### Implementation Guidelines
- 도메인 규칙 코드는 `Unity.Netcode`를 **참조하지 않아야 한다**(must never). 네트워크 어댑터만 참조한다.
- `NetworkManager.Singleton`은 어댑터 계층 밖에서 쓰지 않아야 한다. 필요한 값은 주입한다.
- 클라이언트는 공유 상태를 직접 쓰지 않아야 한다(쓰기는 서버 소유 NetworkVariable/RPC 요청만).
- 서버는 모든 클라이언트 요청을 검증하고, 어긋나면 거부하며 클라이언트 표시를 서버 상태로 되돌린다.
- 클라이언트는 충돌 판정(내구도)을 실행하지 않아야 한다. 예측 차량의 물리 반응은 허용하되 판정·연출은 서버 결과 수신 후에만 한다.
- 재시뮬레이션 중에는 소리·이펙트·UI 갱신·로그를 내지 않아야 한다(같은 스텝이 여러 번 실행된다).
- 소유 클라이언트는 자기 NetworkObject 루트의 콜라이더를 꺼야 한다. 켜 두면 예측 차량이 자기 보간 프록시와 부딪힌다.
- 예측·재조정 값(재시뮬레이션 상한, 스무딩 시간, 스냅 임계, `InputRedundancy`, 서버 입력 대기 상한)은 외부 설정에 둔다.
- 충돌 판정은 물리 콜백과 분리해 서버에서 순수 함수로 호출한다(단위 테스트 대상).
- 네트워크 주기·타임아웃·보간 값은 외부 설정(ScriptableObject)에 둔다. 코드에 상수로 박지 않는다.
- 서버 프로세스는 종료 사유를 로그 파일에 남긴다.
- NetworkObject는 프리팹 루트에만 붙이고 NetworkPrefabs 리스트에 등록한다.
- 물리 콜백(`OnCollision*`, `OnTrigger*`)에서 게임 상태를 바꾸는 코드는 서버에서만 실행해야 한다(`IsServer` 가드 또는 서버 전용 컴포넌트). 클라이언트 kinematic 차량의 콜라이더도 콜백을 일으킬 수 있다.
- static 가변 상태를 두지 않아야 한다. 불가피하면 `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`에서 초기화한다. 현재 `ProjectSettings/EditorSettings.asset`은 `m_EnterPlayModeOptionsEnabled: 1`, `m_EnterPlayModeOptions: 0`이며 6.6에서 이것이 도메인 리로드 생략인지는 NOT SOURCEABLE이라, 생략된다고 가정하고 작성한다.
- RPC 표기는 설치한 NGO 버전에서 확인한 한 방식(`[Rpc(SendTo.…)]` 또는 `[ServerRpc]`/`[ClientRpc]`)으로 통일한다. 두 방식을 섞지 않아야 한다.
- 복제 구조체(`ParcelState` 등)에는 관리형 `string`을 넣지 않는다. 정수 ID나 고정 길이 타입을 쓴다.
- 코드의 `EntityId`를 int로 캐스팅하거나 `GetInstanceID()`를 쓰지 않는다(6.4 deprecated, 6.5 removed, `deprecated-apis.md`).
- 차량 스텝 함수는 `VehicleInput`·`VehicleSimState` 외의 값을 읽지 않아야 한다(static·싱글턴·`GetComponent` 금지).
- NGO 콜백·요청 RPC 핸들러에서 도메인 상태를 바로 바꾸지 않는다. 큐에 넣고 결정 15 순서로 반영한다.
- 물리 콜백(`OnCollision*`·`OnTrigger*`)은 서버에서도 접촉·진입·이탈을 버퍼에 넣기만 하고 판정·효과 적용을 하지 않는다.
- 예측 차량 Rigidbody의 `interpolation`은 None으로 두고 표시용 자식에서 고정 스텝 사이를 직접 보간한다(재시뮬레이션이 `Physics.Simulate`를 여러 번 부르므로).
- Rigidbody 속도 직접 쓰기는 재조정(예측 차량을 서버 상태로 되돌릴 때)과 서버의 복구 순간이동에서만 허용한다. 그 밖의 이동은 힘으로 한다.
- 서버와 클라이언트 모두 종료 시 `Physics.simulationMode`를 `FixedUpdate`로 되돌린다.

## Alternatives Considered

### Alternative 1: 예측 없음 — 모든 차량 서버 상태 보간 (개정 전 결정)
- **Description**: 자기 차량도 서버 결과를 보간해서만 표시
- **Pros**: 구현이 가장 단순, 클라이언트·서버 물리 중복 없음
- **Cons**: 조작 지연이 RTT + 보간 버퍼만큼 생김(스파이크: 134~230 ms)
- **Rejection Reason**: 실시간 조작감을 우선해 사용자가 예측 도입을 결정(2026-11-21)

### Alternative 5: NGO `AnticipatedNetworkTransform`
- **Description**: NGO 내장 예측 컴포넌트(Anticipate·Smooth·OnReanticipate)
- **Pros**: 보정 스무딩과 재조정 콜백을 제공
- **Cons**: 이 컴포넌트를 단 오브젝트는 모든 관찰자에서 보간이 꺼짐(NGO 2.14 소스 `Interpolate = false`), 위치·회전만 다루고 물리 재시뮬레이션은 직접 구현해야 함
- **Rejection Reason**: 다른 차량 보간 유지 요구와 충돌

### Alternative 6: 재시뮬레이션 없는 간이 예측
- **Description**: 로컬에서 먼저 움직이고 서버 값이 오면 그쪽으로 끌어당기기만 함
- **Pros**: 구현이 가벼움
- **Cons**: RTT가 클수록 끌어당김이 크게 보이고, 충돌·급회전 뒤 어긋남이 누적
- **Rejection Reason**: 재조정 품질 부족

### Alternative 7: 소유자 권한 차량 이동
- **Description**: 소유 클라이언트가 자기 차량을 직접 움직이고 서버는 받아들임
- **Pros**: 예측 없이도 지연 0
- **Cons**: 서버 권한(GDD 규칙 2)·충돌 판정 일관성·부정행위 방지를 잃음
- **Rejection Reason**: GDD 규칙 2와 충돌(레지스트리 `vehicle_replication.not`)

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
- 자기 차량 조작 반영이 RTT와 무관하게 로컬 프레임 수준이 된다(구현 후 AC-NET-13으로 측정).

### Negative
- 싱글 플레이도 서버 자식 프로세스를 띄워 시작 시간과 메모리가 늘어난다(측정 필요).
- 빌드 타깃이 2개(Game, Dedicated Server)로 늘고 CI·배포가 복잡해진다.
- 차량 물리를 서버와 소유 클라이언트 두 곳에서 돌리고, 클라이언트는 재시뮬레이션까지 해 CPU 비용이 늘어난다.
- 예측 오차 보정이 보일 수 있다. 특히 차량끼리 접촉할 때 크다(내 화면에서 다른 차량은 과거 시점에 있음).
- 기존 `PlayerMovementController`를 `VehicleSimState` + 순수 스텝 함수로 재구성해야 한다(마이그레이션 2단계 확대).
- 기존 단일 프로세스 코드(플레이어 입력·물류·시계 등)를 서버/클라이언트로 나누는 마이그레이션이 필요하다.
- 금지 패턴: 도메인 규칙의 Netcode 참조, 클라이언트의 공유 상태 직접 쓰기, 어댑터 밖의 `NetworkManager.Singleton` 사용.

## Risks
- **NGO 버전이 6000.6.8f1과 호환되지 않음**: 구체 사유로 `GetInstanceID()`가 6.5에서 제거되고 `EntityId`로 바뀐 점이 있다(`deprecated-apis.md`). 이를 반영하지 않은 NGO는 컴파일이 깨질 수 있다. 설치·컴파일 검증을 구현 첫 스토리로 두고, 불가하면 이 ADR을 Superseded로 두고 재결정.
- **Hub 에디터 6000.6.8f1에 Dedicated Server 빌드 모듈이 없음**: 첫 스토리에서 모듈 설치 여부 확인.
- **물리 틱과 네트워크 틱 불일치로 보간 품질 저하**: 두 값을 설정에 두고 실측. 관계 설정법은 NOT SOURCEABLE.
- **서버 헤드리스 실행에서 `Camera.main`·UI·URP 의존 코드가 오류**: 서버 부트스트랩 분기(결정 12), 서버 빌드에서 MeshCollider 메시가 빠지지 않는지 첫 스토리에서 확인. 모두 UNVERIFIED.
- **에디터 개발 루프가 번거로움(Play 모드마다 서버 빌드 필요)**: 에디터에서는 서버를 별도 에디터 인스턴스나 빌드로 띄우는 개발용 경로를 둔다. Multiplayer Play Mode 같은 도구의 6.6 지원 여부는 NOT SOURCEABLE.
- **Dedicated Server 빌드가 URP·Input System 등과 함께 성공하지 않음**: 대안 4로 되돌린다.
- **비소유 클라이언트에서 차량이 충돌 후 튀거나 끊김**: 보간 방식(LegacyLerp/Lerp/SmoothDampening·스무딩)은 설정 값으로 두고 플레이테스트로 정한다. 스파이크에서 스무딩 끔이 기본보다 지연이 짧았다.
- **PhysX 비결정성으로 예측이 계속 조금씩 어긋남**: 보정 스무딩으로 가리고 검증 항목 (8)로 크기를 잰다. 크면 예측 차량 물리를 단순화하거나 접촉 중 재조정 빈도를 조정한다.
- **재시뮬레이션 때 물리 콜백·트리거가 여러 번 발생**: 판정은 서버만 하므로 상태에는 영향이 없지만, 클라이언트 표시용 콜백은 재시뮬레이션 플래그로 막는다.
- **`SimulationMode.Script` 전환으로 클라이언트의 다른 물리 동작이 멈춤**: 클라이언트에 예측 차량 외 동적 바디(연출용 잔해 등)가 생기면 그 단계도 같은 `Physics.Simulate`로 돌아가므로 재시뮬레이션 때 같이 움직인다. 연출용 동적 바디는 두지 않거나 별도 물리 씬으로 분리한다.
- **Script 모드에서 kinematic 프록시 위치 반영 시점**: 비소유 차량·NPC의 `Rigidbody.position` 대입이 다음 `Physics.Simulate`까지 물리 씬에 반영되지 않을 수 있고 NGO는 `Physics.SyncTransforms`를 호출하지 않는다. 예측 차량이 다른 차량과 접촉할 때 위치가 한 스텝 늦을 수 있어 첫 스토리에서 확인(UNVERIFIED).
- **Rigidbody 보간과 NetworkTransform 보간의 이중 적용**: NetworkRigidbody는 Interpolate가 켜지면 비권한 쪽에서 Rigidbody 보간만 쓴다(`NetworkRigidbodyBase.cs`). 소유 클라이언트의 숨긴 루트에는 Rigidbody 보간을 꺼 불필요한 비용을 없앤다.
- **접촉 이벤트 중복**: `RigidbodyContactEventManager`(`Physics.ContactEvent` 구독)를 쓰면 재시뮬레이션 중 접촉 이벤트가 중복된다. 쓰지 않거나 재시뮬레이션 플래그로 거른다.
- **`Physics.simulationMode`가 Play 세션 사이에 남음**: 도메인 리로드를 생략하는 Play Mode 설정에서는 종료 시 `SimulationMode.FixedUpdate`로 되돌린다. 6.6에서 이 API의 상태는 NOT SOURCEABLE이라 첫 스토리에서 컴파일·동작 확인.
- **서버 입력 대기열이 지터로 비거나 쌓임**: 대기 상한과 빈 틱 입력 0 규칙으로 처리하고 실측 후 조정. 클라이언트가 서버보다 얼마나 앞서 달릴지(입력 버퍼) 조정 방식은 NOT SOURCEABLE.
- **호스트 PC의 자식 서버에 원격 참가자가 NAT 때문에 접속 불가**: 참가 수단 ADR(Relay 포함)에서 해결. 그 전에는 LAN·포트 포워딩 전제.
- **비신뢰 입력이 연속 유실**: `InputRedundancy`를 넘는 연속 유실이면 그 틱은 입력 0이 되고 예측이 보정된다. 유실률은 Network Profiler로 실측.
- **서버 프로세스 고아화(호스트 비정상 종료)**: 부모 PID 감시 + 접속 끊김 시 종료로 이중 방어. AC-NET-23·29로 검증.
- **싱글 시작이 느림**: 서버 기동 시간 측정, 기준 초과 시 싱글만 StartHost로 돌리는 결정은 GDD 규칙 3과 충돌하므로 ADR 개정 필요.
- **상태 파일 경합**: 이전 세션 파일을 읽지 않도록 세션마다 새 경로를 쓰고 임시 이름 → 바꿔 달기로 쓴다.
- **UTP 타임아웃이 길면**: 끊김 확정 전까지 서버는 그 차량에 입력 0을 적용한다. 값은 설정으로 두고 실측해 줄인다.
- **서버 Script 모드**: 서버의 모든 동적 바디(NPC 포함)가 어댑터의 `Physics.Simulate` 호출로만 움직인다. 호출이 빠지면 월드 전체가 멈추므로 어댑터 하나가 소유한다.
- **외부 수정값 변화 순간의 예측 오차**: 상차 완료·장애물 진입은 서버에서 먼저 일어나므로 RTT만큼 늦게 반영되고 재조정이 한 번 생긴다. 보정 스무딩으로 가린다.
- **일시정지 제거**: 싱글 플레이어도 메뉴·확인창 중 시간이 흐른다. 가이드 패널 quick-spec과 다르다.

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
| networking.md | 규칙 8·AC-NET-13: 조작 지연 | 결정 4(자기 차량 예측·재조정·보정 스무딩)·6(비신뢰+중복 입력) |
| collision-durability-rules.md | 4장 2·3항: 모든 차량 동적 바디, 판정과 콜백 분리 | 결정 4, 가이드라인 |
| game-concept.md | 3·8·10·12장 | 서버가 규칙 그대로 실행(결정 3) |
| networking.md | 규칙 3: 인원별 설정은 하루 시작 수락 시점 인원 | 결정 17 |
| networking.md | 규칙 8·12: 예측과 복구·잠금·적재·장애물 효과 일치 | 결정 4(외부 수정값) |
| networking.md | 규칙 15: 서버 종료 후 정산 화면 유지 | 결정 16 |
| networking.md | Edge: 입력 미수신 → 접속 끊김 | 결정 16 |
| networking.md | Edge: 상차 완료와 이탈이 같은 프레임이면 이탈 먼저 | 결정 15 |
| collision-durability-rules.md | 4장 3항: 판정을 물리 콜백과 분리 | 결정 15(접촉 버퍼 → 판정 단계) |

## Performance Implications
- **CPU**: 서버가 4대 차량 + NPC 물리를 처리. 예산 NOT SOURCEABLE → 구현 후 `/perf-profile`로 실측
- **Memory**: 서버 프로세스 1개 추가(싱글 포함). 크기 미측정
- **Load Time**: 서버 기동 시간만큼 세션 생성이 늘어남. 측정 필요
- **Network**: 최대 4클라이언트, 차량·NPC 위치 보간 복제. 대역폭·틱 주기는 근거 없음(NOT SOURCEABLE), 설정 값으로 두고 Network Profiler로 실측

## Migration Plan
1. NGO 설치·버전 검증, Dedicated Server 빌드 타깃 생성(첫 스토리).
2. 차량 분리: `PlayerMovementController`를 입력 읽기 / 차량 시뮬레이션(`VehicleSimState` + 순수 스텝 함수, Netcode 미참조) / Rigidbody 적용 어댑터로 나눈다. 서버와 예측 차량이 같은 시뮬레이션을 쓴다. 이어서 예측·재조정·보정 스무딩을 붙인다.
3. 시계·물류 생성·배정·마감·정산을 서버에서만 실행하도록 어댑터 추가. UI는 복제 상태 구독.
4. 싱글 플로우를 서버 자식 프로세스 경로로 전환하고 기존 QA 테스트 케이스(AC-NET-03)로 확인.
5. 멀티 기능(예약·참가·복귀·이탈 물류) 순서로 추가.

## Validation Criteria
- AC-NET-01~31(`design/gdd/networking.md`) 통과(AC-NET-13은 측정만).
- 싱글 기존 QA 테스트 케이스 전부 통과(AC-NET-03).
- 서버 자식 프로세스 기동·종료·호스트 강제 종료 시 고아 프로세스 없음.
- 지연 시뮬레이션에서 조작·충돌 표시가 허용 가능한지 플레이테스트. NGO 2.14의 `UnityTransport.DebugSimulator`는 폐기(효과 없음)라, UTP `SimulatorPipelineStage`를 붙인 드라이버(스파이크 방식) 또는 Multiplayer Tools의 Network Simulator를 쓴다.
- 예측: 추가 RTT 100 ms에서 자기 차량 입력 → 화면이 로컬 프레임 수준(합격선은 실제 차량으로 측정 후 GDD에 기록), 재조정 보정이 직선 주행에서 보이지 않음.
- 남은 서버 프로세스가 포트를 잡은 상태에서 세션을 만들면 세션 생성 실패가 표시되고 예전 서버에 붙지 않는다.
- 적재 직후·감속 장애물 통과·복구 직후에 예측 차량 위치 보정이 스냅 임계를 넘지 않는다.
- 클라이언트 창을 비활성으로 두어도 접속이 유지된다.

## Related
- `design/gdd/networking.md`, `design/gdd/collision-durability-rules.md`, `design/gdd/game-concept.md`
- `docs/engine-reference/unity/modules/networking.md`
- `prototypes/net-latency-spike-2026-11-21/SPIKE-NOTE.md`(지연 실측, 개정 근거)
- ADR-0002(세션 참가 수단). 후속 ADR(미작성): 서버 빌드·배포
