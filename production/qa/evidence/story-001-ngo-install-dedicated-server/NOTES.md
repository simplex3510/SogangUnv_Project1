# Story 001 검증 기록 (2026-11-21)

Status: 완료(코드·빌드·기동·바인딩 실패·RPC 표기 모두 확인)

## 결과 요약 (ADR-0001 검증 항목 메모)
| 항목 | 결과 |
|---|---|
| 검증 1: NGO 2.14.0 / Transport 6.6.0 설치, 컴파일 | 통과. run1, run2 error CS 0건. Transport 6.6.0은 packages-lock에서 source=builtin |
| UNITY_SERVER 코드 컴파일 | 통과(run5, 임시 csc.rsp 후 삭제). 이후 실제 서버 빌드로도 확인(run7) |
| 기존 클라이언트 빌드 | 통과. run6, StandaloneWindows64 Result: Success, exit 0 (UI 씬 분기는 UNITY_SERVER 한정이라 클라이언트 동작 불변) |
| 검증 2: Dedicated Server 빌드 | 통과. run7 `result=Succeeded errors=0 warnings=0`, 출력 `D:\Unity Projects\_builds\story-001-server\Server.exe`. (windows-server 모듈 설치 전 run3은 `Dedicated Server support for Win is not installed.`로 실패) |
| 검증 2: `-batchmode -nographics` 기동·종료 사유 로그 | 통과. run8: `[Server] 기동 완료 port=7777 tick=30`, Exception 0건, exit code 0, 종료 사유 파일 run8-exit.txt = `OnServerStopped 이벤트` |
| 검증 12: 포트 점유 시 바인딩 실패 | 아래. run9 |
| RPC 표기 | `[Rpc(SendTo.…)]`로 확정. 아래 |

## 검증 12: 바인딩 실패 방식 (NetworkManager 단 실측, run9)
같은 포트 7777에 서버 2개를 띄웠다(run9a 첫 서버, run9b 두 번째). 두 번째 서버 로그 순서:
1. `LogError`: `Server failed to bind. This is usually caused by another process being bound to the same port.`
2. `OnTransportFailure` 이벤트(StartServer 도중 동기 발생)
3. `NetworkManager.StartServer()`가 **false 반환**
예외는 없다. 두 번째 서버 exit code 1, run9-exit.txt = `실패: OnTransportFailure 이벤트`.
결론: 반환값(false) + OnTransportFailure 이벤트 + LogError 셋 다 드러난다. 판정은 반환값 또는 이벤트로 하면 된다.
보조: 전송 계층만 에디터에서 실측(run4, BindProbe.cs.txt)해도 `UnityTransport.StartServer()` false.

## RPC 표기
`[Rpc(SendTo.Server)]` 등 `[Rpc(SendTo.…)]`로 통일(메서드 이름은 Rpc로 끝나야 함). 임시 프로브(`[Rpc(SendTo.Server)] private void PingRpc(int v)`)가 2.14.0에서 컴파일·IL 위빙 통과(run2). 프로브 삭제함.
`[ServerRpc]`/`[ClientRpc]`는 클래스가 남아 있으나 RequireOwnership 경로가 Obsolete(RpcAttributes.cs:65,101,154)라 쓰지 않는다.

## 구현 중 발견한 NGO 서버 부트스트랩 함정 (ADR 메모)
- 런타임에서 `AddComponent<NetworkManager>()`만 하면 `NetworkConfig`가 null이다. 비활성 GameObject에 구성하고 `NetworkConfig = new NetworkConfig{...}`를 넣은 뒤 활성화해야 한다.
- NGO의 ILPP 메시지 등록이 `RuntimeInitializeLoadType.AfterSceneLoad`라서, 부트스트랩을 BeforeSceneLoad/AfterAssembliesLoaded에서 돌리면 `Allowed types is not equal to the number of message type indices` 예외와 `There is no NetworkManager assigned` 로 StartServer가 실패한다. AfterSceneLoad에서 시작한다.
- Dedicated Server 빌드는 "Dedicated Server Optimizations"로 셰이더가 빠지고 UI Toolkit 텍스트가 예외를 매 프레임 낸다. UI 씬(`/UI.unity`)을 UNITY_SERVER에서만 불러오지 않게 `GameSceneLoader`에 분기를 넣었다. 카메라는 nographics에서 문제가 없어 그대로 둔다.

## 기타 관찰
- NGO가 패키지 설치 직후 `Assets/DefaultNetworkPrefabs.asset`을 자동 생성했다(Assets 루트에 둠). ServerNetworkConfig.PrefabsList가 참조한다.
- UnityTransport는 `-port`, `-ip` 커맨드라인 인자를 자체 처리한다. 부트스트랩은 별도 파싱하지 않는다.
- 종료 사유 파일의 첫 줄은 `Application.Quit` 때 `OnServerStopped`가 먼저 와서 그 이벤트로 남는다(정상 종료 구분은 exit code 0).
- 서버 기동 시 카메라·UI 외에 Systems/MainGameScene의 게임 로직 스크립트는 그대로 돌아간다(예외 없음 확인). 서버 권한 로직 분리는 후속 스토리.
