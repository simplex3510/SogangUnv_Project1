<!-- PROTOTYPE - NOT FOR PRODUCTION -->
# Spike Note — 서버 권한 차량 조작 지연 (2026-11-21)

> 스파이크 프로젝트(버리는 코드): `D:\Unity Projects\net-latency-spike-2026-11-21` (저장소 밖, 커밋 안 함)
> 관련: `design/gdd/networking.md` 규칙 7·8, Open Question 2, AC-NET-13 / ADR-0001 결정 4·6

## Question tested

ADR-0001 구조(서버 권한 Rigidbody 차량, 자기 차량 예측 없음, NetworkTransform 보간, 입력은 고정 주기 신뢰성 RPC)에서 서버가 아닌 클라이언트의 입력→화면 반영 지연은 얼마이고, 직접 몰아 봤을 때 받아들일 만한가?

## Setup

- Unity 6000.6.8f1, Netcode for GameObjects 2.14.0, Unity Transport 6.6.0(built-in). 6000.6.8f1에서 컴파일·실행 확인(ADR-0001 검증 항목 일부)
- Windows 빌드 1개를 서버(`-batchmode -nographics`) 1개 + 클라이언트 2개로 실행, 한 PC
- 지연: 클라이언트 드라이버 파이프라인에 UTP `SimulatorPipelineStage`를 붙여 추가 RTT 0/50/100(/150) ms. NGO 2.14의 `DebugSimulator`·`SetDebugSimulatorParameters`는 폐기(효과 없음)라 직접 드라이버를 만들었다
- 측정: 정지 상태에서 가속 입력 → 클라이언트 화면의 차량이 2 mm 이동할 때까지, 조건별 30회 자동 반복. 구간 분해는 시스템 QPC(`QueryPerformanceCounter`)로 프로세스 간 비교
- 차량: 가속 15 m/s², 최고 15 m/s, 틱 30, 60 fps. 기본 보간 = LegacyLerp + 스무딩(최대 0.1초)

## Result: YES — 기본 설정으로 감수할 만함 (사용자 직접 주행, 2026-11-21)

입력→화면 중앙값:

| 추가 RTT | 기본 보간(모든 차량) | 보간 끔(모든 차량) |
|---|---|---|
| 0 (실측 RTT 약 17 ms) | 134 ms | 50 ms |
| 100 | 약 230 ms | 150 ms |

- 사용자 체감: 보간이 들어간 상태에서 약간의 지연은 느껴지지만 감수할 만하다. 다른 클라이언트 차량에는 보간이 있어야 한다.
- 구간: 입력→서버 도착은 약 30 ms + RTT/2. 나머지 대부분은 서버→화면(NetworkTransform 복제·보간)이다. 틱 30→60은 거의 차이가 없었다.

## Pitfalls found (구현 때 그대로 걸림)

- NetworkTransform의 `Interpolate`는 권한(서버) 쪽 값이 복제된다. 클라이언트에서만 끄면 효과가 없다. 자기 차량만 다르게 하려면 별도 처리가 필요하다.
- Mono의 `Stopwatch.GetTimestamp()`는 프로세스별 기준이라 프로세스 간 시각 비교에 쓸 수 없다.
- 출발이 아주 느린 차량(마찰·감쇠로 저속)은 보간·스무딩 때문에 지연이 470 ms까지 부풀려 보였다. 지연 측정은 실제 게임 가속 조건에서 해야 한다.
- 종료되지 않은 서버 프로세스가 포트를 잡으면 새 서버는 바인딩에 실패하고 클라이언트는 예전 서버에 붙는다(에러 없이). 서버 프로세스 수명 관리(ADR-0001 결정 2)에서 같은 상황이 생길 수 있다.

## Not covered

- 실제 네트워크의 지터·패킷 손실(시뮬레이터 지연만), PC 2대 이상
- 실제 게임 차량 컨트롤러(최고 3~5 m/s, `VehicleConfig`)와 맵에서의 체감
- 차량끼리 충돌 연출 지연, 다른 차량의 끊김 체감의 정량 측정

## What to do next

- **결론 갱신(2026-11-21, 사용자 결정)**: 예측 없이도 감수할 만했지만, 실시간 조작감을 우선해 자기 차량 클라이언트 예측을 처음부터 넣기로 했다. 다른 차량은 보간을 유지한다. ADR-0001 결정 4·5·6을 개정했다.
- 자기 차량만 보간을 끄는 안(예측 없이)은 기각. 자기 차량 위치가 틱(30 Hz)마다 끊겨 들어오고 카메라가 따라가 화면이 떨린다. 예측 도입으로 대체됐다.
- NGO `AnticipatedNetworkTransform`은 모든 관찰자의 보간을 끄므로 쓰지 않는다(ADR-0001 대안 5).
- GDD `networking.md` Open Question 2와 AC-NET-13에 이 측정값을 참고값으로 남기고, 합격선은 실제 게임 차량으로 다시 잰 뒤 정한다.
