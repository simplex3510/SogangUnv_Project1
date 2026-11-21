# ADR-0002: Session Join Method

## Status
Accepted

## Date
2026-11-21

## Last Verified
2026-11-21 (Unity 플러그인 `setup-multiplayer-services` 참조 문서 기준. 엔진 참조 `docs/engine-reference/unity/`에는 해당 서비스 항목 없음)

## Decision Makers
사용자 + Claude (solo 모드, TD-ADR 생략)

## Summary
ADR-0001의 호스트 PC 자식 서버에 다른 참가자가 접속할 수단이 정해지지 않았다(GDD `networking.md` 규칙 16). 1단계로 직접 IP:포트 접속(LAN·포트 포워딩)을 구현하고, 2단계로 Unity Multiplayer Services(`com.unity.services.multiplayer`)의 참가 코드 + Relay 중계를 추가하되, 서버 프로세스가 중계 세션을 열 수 있는지 스파이크로 먼저 검증한다.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6000.6.8f1 |
| **Domain** | Networking |
| **Layer** | Core |
| **Knowledge Risk** | HIGH |
| **References Consulted** | `docs/engine-reference/unity/modules/networking.md`(Unity Transport 주소·포트), Unity 플러그인 `setup-multiplayer-services` 참조(`entrypoints.md`, `dgs-entrypoint.md`, `workflows-prerequisites.md`) |
| **Post-Cutoff APIs Used** | 2단계만: `Unity.Services.Multiplayer`의 `SessionOptions.WithRelayNetwork()`/`WithDirectNetwork()`, `WithNetworkHandler(INetworkHandler)`, `NetworkRole.Server`, 참가 코드 참가. 1단계는 Unity Transport 주소·포트 설정뿐 |
| **Verification Required** | (1) 1단계: 서버가 모든 인터페이스에서 수신하고 같은 LAN의 다른 PC가 IP:포트로 접속 (2) 2단계 스파이크: 호스트 PC의 서버 프로세스(Dedicated Server 빌드)가 UGS 인증 후 Relay 세션을 만들고 NGO를 Server 역할로 시작해 참가 코드로 원격 클라이언트를 받는지, 경로 A(`IHostSession` + `INetworkHandler`)와 경로 B(`IServerSession`) 각각 — 참조 문서는 `IMultiplayerServerService`를 호스팅된 DGS용으로만 설명하므로 NOT SOURCEABLE (3) 서버 빌드에서 Authentication 패키지 동작 (4) `com.unity.services.multiplayer`의 6000.6.8f1 호환 버전 |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (Accepted 2026-11-21) |
| **Enables** | 로비·참가 UI의 UX 명세(GDD UI Requirements "참가 수단 UI는 참가 수단이 정해진 뒤") |
| **Blocks** | networking 에픽의 원격 참가 스토리(아직 생성 전) |
| **Ordering Note** | 2단계는 스파이크 결과가 나와야 확정된다. 스파이크가 실패하면 이 ADR을 개정한다 |

## Context

### Problem Statement
ADR-0001 결정 9는 클라이언트가 `(address, port)`만 받아 접속하고 그 값을 얻는 방식을 후속 ADR로 넘겼다. 플레이 환경은 LAN(개발·시연)과 인터넷(친구끼리) 둘 다다(사용자, 2026-11-21). 업체 선택 기준(사용자, 2026-11-21): Steam 출시 확정·이후 다른 플랫폼(닌텐도 등) 가능, 비용 0·플레이어 계정 불필요, 플레이어 설치 부담 없음. 후보 Steam·UGS·EOS·가상 LAN을 비교한 뒤 사용자가 모든 빌드에 UGS 하나를 쓰기로 결정했다. 인터넷에서는 호스트 PC가 공유기 NAT 뒤에 있어 직접 접속이 포트 포워딩 없이는 대개 실패한다(ADR-0001 Risks).

### Constraints
- 서버는 호스트 PC의 자식 프로세스이고 호스트 플레이어도 일반 클라이언트다(ADR-0001 결정 2). 호스트 클라이언트 프로세스가 네트워크 호스트(Host 역할)가 되면 안 된다.
- NGO 서버의 전송은 하나다. 한 세션에서 직접 접속과 중계 접속을 동시에 받는 것은 가정하지 않는다.
- 1인 개발: 클라우드 설정·코드 분량 최소화. 1단계는 클라우드 의존 없음.
- 정원 4명, Settlement 중 참가 거부, 하루 중 참가 허용(GDD 규칙 4·16).

### Requirements
- LAN에서 클라우드 계정 없이 접속.
- 인터넷에서 포트 포워딩 없이 접속(2단계).
- 정원 초과·정산 중 참가 거부는 서버가 판정(ADR-0001 connection approval).

## Decision

1. **세션 모드 2종**: 세션 생성 시 호스트가 `Direct` 또는 `Relay`를 고른다. 한 세션은 한 모드만 쓴다. 인터넷 참가 업체는 UGS 하나이며 Steam 빌드를 포함한 모든 빌드에 같다(사용자 결정, 2026-11-21).
2. **1단계 — Direct**:
   - 서버는 설정된 포트(기본값은 설정 파일, 코드 상수 금지)로 모든 인터페이스에서 수신한다.
   - 호스트 클라이언트는 loopback으로 접속하고, 로비 화면에 호스트 PC의 LAN IP:포트를 표시한다.
   - 참가자는 IP:포트를 입력해 접속한다. 인터넷 접속은 호스트가 포트 포워딩을 한 경우만 지원한다.
3. **2단계 — Relay(스파이크 통과 시)**:
   - 서버 프로세스가 UGS에 인증하고 `com.unity.services.multiplayer`로 Relay 세션을 만들며, NGO는 Server 역할로 시작한다. 스파이크는 두 경로를 따로 검증하고 통과한 쪽을 채택한다.
     - **경로 A**: `IMultiplayerService.CreateSessionAsync`(→ `IHostSession`) + `WithRelayNetwork` + `WithNetworkHandler`로 기본 NGO 연동을 끄고 핸들러에서 `StartServer`.
     - **경로 B**: `IMultiplayerServerService.CreateSessionAsync`(→ `IServerSession`) + `WithRelayNetwork`. 문서상 호스팅된 DGS용이라 호스팅 안 된 PC에서 되는지는 NOT SOURCEABLE.
   - 세션은 `IsPrivate = true`(목록 검색에서 숨김, 참가 코드로만 참가)로 만든다.
   - 서버는 참가 코드를 호스트 클라이언트에 전달한다(표준 출력 또는 서버가 쓰는 파일; ADR-0001의 자식 프로세스 채널).
   - 호스트 클라이언트를 포함한 모든 클라이언트가 참가 코드로 접속한다(전송이 하나이므로 호스트도 중계 경유).
   - 정원 판정 기준은 서버 connection approval(4명)이다. 세션 `MaxPlayers`는 "호스트 포함" 값이므로 경로 A에서는 서버 프로세스 자리를 더해 5로 둔다. 경로 B의 슬롯 계산은 스파이크에서 확인한다.
   - Relay 프로토콜(`WithNetworkOptions`의 `RelayProtocol`)은 스파이크에서 기본값을 확인한 뒤 고정한다.
4. **참가 입력 UI**: 참가 화면은 입력 칸 하나다. IPv4 `IP:포트` 형식이면 Direct, 아니면 참가 코드로 해석한다(IPv6는 지원하지 않는다). 세부 UI는 UX 단계에서 정한다.
5. **목록 검색·자동 매칭은 하지 않는다**: 친구 간 협동 전제. 공개 세션 목록, 매치메이킹, 호스트 이전(host migration)은 범위 밖이다(GDD: 호스트 이탈 시 서버 종료).
6. **네트워크 진입 코드 위치**: 모드 선택과 접속 정보 해석은 ADR-0001의 네트워크 어댑터 계층에만 둔다. 도메인 규칙은 접속 방식을 모른다.

### Architecture Diagram
```
Direct (1단계)                         Relay (2단계)
 Host Client ──loopback──┐              Host Client ──join code──┐
 Remote ──IP:port(LAN/PF)─┤             Remote ─────join code────┤
                          ▼                                      ▼
                 Server (child, UTP listen)            Unity Relay 중계
                                                                 │
                                                  Server (child, UGS 인증,
                                                   Relay 세션 생성, NGO Server)
```

### Key Interfaces
- `SessionMode` enum: `Direct`, `Relay`.
- 서버 기동 인자 추가(ADR-0001 인자에 더함): `--mode direct|relay`.
- 서버 → 호스트 클라이언트: 준비 완료 신호와 접속 정보(Direct: 포트, Relay: 참가 코드). ADR-0001의 자식 프로세스 채널을 쓴다.
- 참가 입력 해석: 문자열 → `(Direct, address, port)` 또는 `(Relay, joinCode)`.

### Implementation Guidelines
- 포트·타임아웃은 외부 설정에 둔다(코드 상수 금지).
- Relay 관련 코드(`Unity.Services.*` 참조)는 네트워크 어댑터 어셈블리에만 둔다. 도메인 규칙 어셈블리는 참조하지 않아야 한다.
- `Unity.Services.Multiplayer.Server`를 쓰게 되면 해당 코드는 `UNITY_SERVER` 조건부 컴파일 또는 asmdef `defineConstraints`로 서버 빌드에만 포함해야 한다(플러그인 `dgs-entrypoint.md`).
- 정원·정산 중 거부는 서버 connection approval에서 판정한다. 세션 서비스의 `MaxPlayers`만 믿지 않는다.
- 2단계 코드는 스파이크 통과 전에는 메인 코드에 넣지 않는다.
- Direct(`StartClient` 직접 호출)와 Relay(세션 SDK가 NGO 기동) 두 경로가 같은 `NetworkManager`·Transport 설정을 덮어쓰지 않도록, 세션마다 어댑터가 Transport 설정을 처음부터 다시 구성해야 한다.
- 경로 B를 채택하면 Dedicated Server 빌드(`UNITY_SERVER`)가 필수가 된다. ADR-0001 대안 4(클라이언트 빌드 `-batchmode`)로 되돌아가면 경로 B는 컴파일되지 않으므로, 그때는 `ENABLE_UCS_SERVER` 정의 사용 여부를 함께 결정해야 한다.

## Alternatives Considered

### Alternative 1: 직접 IP만
- **Description**: IP:포트 입력만 지원
- **Pros**: 클라우드 의존 없음, 가장 단순
- **Cons**: 인터넷 친구 플레이에 포트 포워딩 필요
- **Rejection Reason**: 플레이 환경이 인터넷 포함(사용자 결정). 1단계로만 채택

### Alternative 2: Relay만
- **Description**: 모든 접속을 참가 코드로
- **Pros**: NAT 문제 없음, 진입 방식 하나
- **Cons**: 개발·LAN에도 클라우드 프로젝트·인터넷 필요, 서버 프로세스의 Relay 세션 생성이 미검증
- **Rejection Reason**: 미검증 경로에 전체 접속을 걸 수 없음

### Alternative 3: Steam 네트워크(Steamworks 중계, SDR)
- **Description**: Steam 빌드에서 Steam 친구 초대 + Steam 중계
- **Pros**: Steam 출시 확정(사용자, 2026-11-21)과 맞음, 친구 초대 UX, 추가 계정·설치 없음, 중계 무료라는 주장(공식 요금 문구 미확인)
- **Cons**: Steam 전용이라 다른 플랫폼은 별도 구현, NGO 연동이 커뮤니티 Transport(Facepunch)에 의존하고 컴파일 오류 이슈 보고 있음, 자식 서버 프로세스에 Steam 게임 서버 식별자를 주는 경로 미검증
- **Rejection Reason**: 사용자 결정(2026-11-21) — 모든 빌드(Steam 포함)에 참가 방식 하나를 쓰고 NGO 공식 연동을 택함. Steam 친구 초대는 필요하면 UGS 참가 코드를 전달하는 수단으로 나중에 얹을 수 있다(별도 결정)

### Alternative 5: Epic Online Services(P2P 중계)
- **Description**: EOS 로비 + P2P 중계
- **Pros**: 무료를 내세움, 콘솔 크로스플레이, Epic 계정 불필요(PC는 기기 ID)
- **Cons**: NGO 연동이 커뮤니티 Transport 의존, 중계 트래픽 무료 범위 미확인
- **Rejection Reason**: NGO 공식 연동이 없음

### Alternative 6: 업체 없음(가상 LAN 앱 + 직접 IP)
- **Description**: 플레이어가 Tailscale·ZeroTier 등을 설치하고 1단계 Direct로 접속
- **Pros**: 추가 코드·비용 없음
- **Cons**: 플레이어마다 앱 설치·가입 필요
- **Rejection Reason**: "플레이어 설치 부담 없음" 기준(사용자, 2026-11-21) 위반. 2단계 전 테스트 수단으로만 허용

### Alternative 4: 호스트 클라이언트가 Relay 세션의 Host가 되고 서버를 겸함
- **Description**: 세션 SDK 기본 흐름(`IHostSession`, Host 역할)
- **Pros**: SDK가 공식 지원하는 흐름
- **Cons**: ADR-0001의 분리 실행·호스트도 클라이언트 결정과 충돌
- **Rejection Reason**: ADR-0001과 충돌. 스파이크 실패 시 ADR-0001과 함께 재검토 대상

## Consequences

### Positive
- LAN에서는 클라우드 없이 바로 개발·시연할 수 있다.
- 인터넷 친구 플레이에 포트 포워딩이 필요 없어진다(2단계).
- 접속 방식이 어댑터 계층에 갇혀 도메인 규칙에 영향이 없다.

### Negative
- Relay 모드에서는 호스트 플레이어도 중계를 거치므로 호스트 지연이 늘어난다(참가자 간 지연은 비슷해짐).
- 2단계는 UGS 프로젝트 연결·인증·서비스 요금 정책에 의존한다.
- 모드가 2개라 접속 테스트 경로가 2배다.

## Risks
- **서버 프로세스가 Relay 세션을 열 수 없음**(NOT SOURCEABLE): 2단계 스파이크로 확인. 실패 시 대안 4로 ADR-0001과 함께 재검토하거나 Direct + 포트 포워딩 안내로 유지.
- **서버 빌드에서 UGS 인증 불가 또는 익명 인증이 서버 세션에 무효**(NOT SOURCEABLE): 스파이크 항목. 실패 시 위와 같다.
- **같은 PC의 호스트 클라이언트와 서버 프로세스가 같은 익명 플레이어 ID로 로그인**(NOT SOURCEABLE): 인증 프로필 분리 가능 여부를 스파이크에서 확인.
- **Relay 세션 수명**(할당 유지 조건, 서버가 떠날 때 세션 삭제 여부, 하루 내내 참가 가능 여부, NOT SOURCEABLE): 스파이크에서 AC-NET-23과 함께 확인.
- **UTP의 모든 인터페이스 수신 설정 방법**(NOT SOURCEABLE): 1단계 검증 항목 (1)로 확인.
- **Windows 방화벽이 서버 프로세스 수신을 차단**: 1단계 첫 실행 시 확인. 안내 문구 또는 설치 시 규칙 등록은 배포 ADR에서 정한다.
- **LAN IP 표시가 여러 네트워크 어댑터에서 틀림**: 표시 후보를 모두 보여 주는 것으로 처리.
- **`com.unity.services.multiplayer` 버전 호환**: 2단계 설치 시 컴파일로 확인.
- **무료 한도 초과 과금**: 검색 시점(2026-11-21) 요금 페이지 기준 월 평균 동접 50명과 동접당 대역폭 일정량까지 무료이고 초과분은 종량제다([요금](https://unity.com/products/gaming-services/pricing)). "비용 0" 기준은 이 한도 안에서만 성립한다. 2단계 착수 시 공식 페이지를 다시 확인하고, 차량 복제 대역폭 실측값(ADR-0001)으로 한도 소진 속도를 추정한다.
- **Steam 빌드의 초대 UX**: Steam 친구 초대 없이 참가 코드만 쓴다. 필요하면 Steam 초대로 참가 코드를 전달하는 기능을 별도 결정으로 추가한다.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| networking.md | 규칙 16: 호스트가 세션을 만들고 다른 참가자가 참가, 참가 수단 미정 | 결정 1~4 |
| networking.md | 규칙 4·Edge: 정원 4명 초과 거부, 정산 중 참가 거부 | 서버 connection approval 판정(가이드라인) |
| networking.md | 규칙 1: 호스트도 일반 클라이언트 | Direct: loopback 접속, Relay: 호스트도 참가 코드 접속 |
| networking.md | Edge: 호스트 이탈 시 서버 종료 | 호스트 이전 범위 밖(결정 5) |
| networking.md | UI Requirements: 참가 수단 UI는 수단 결정 후 | 결정 4(입력 칸 하나, 세부는 UX) |

## Performance Implications
- **CPU**: 영향 없음
- **Memory**: 2단계에서 서비스 SDK 로드분
- **Load Time**: Relay 모드 세션 생성 시 인증·세션 생성 왕복 추가(측정 필요)
- **Network**: Relay 모드는 중계 경유로 왕복 지연 증가. 수치 NOT SOURCEABLE, AC-NET-13 측정에 모드별로 기록

## Migration Plan
1. 1단계: ADR-0001 첫 스토리 뒤에 Direct 모드(포트 설정, LAN IP 표시, IP:포트 입력).
2. 2단계 선행: UGS 프로젝트 연결, 대시보드에서 Relay 서비스 활성화, `com.unity.services.multiplayer` 설치.
3. 2단계 스파이크: 별도 브랜치·프로토타입에서 경로 A·B를 각각 검증(서버 인증, 같은 PC 익명 ID, `MaxPlayers` 슬롯, 세션 수명). 결과를 이 ADR에 기록.
4. 스파이크 통과 시 Relay 모드 추가, 실패 시 이 ADR 개정.

## Validation Criteria
- 같은 LAN의 PC 2대 이상에서 IP:포트로 접속해 AC-NET-01·04·05 통과.
- 2단계: 서로 다른 네트워크의 PC가 참가 코드로 접속해 AC-NET-01 통과, 호스트 이탈 시 AC-NET-23 통과.
- 정원 초과·정산 중 참가가 두 모드 모두에서 거부.

## Related
- ADR-0001 (`docs/architecture/adr-0001-multiplayer-networking-architecture.md`) — 의존
- `design/gdd/networking.md` 규칙 1·4·16, UI Requirements, Open Questions 3
