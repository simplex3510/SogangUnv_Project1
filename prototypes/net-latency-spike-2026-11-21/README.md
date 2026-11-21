<!-- PROTOTYPE - NOT FOR PRODUCTION -->
# net-latency-spike-2026-11-21

- **가설**: ADR-0001 구조(서버 권한 차량, 예측 없음, NetworkTransform 보간)에서 클라이언트 조작 지연을 감수할 수 있다.
- **실행**: 프로젝트는 저장소 밖 `D:\Unity Projects\net-latency-spike-2026-11-21`. 빌드 `Builds\Spike\Spike.exe`를 `-batchmode -nographics -mode server`로 1개, `-mode client -delay <추가 RTT ms>`로 2개 실행. 그 밖의 인자는 `Assets/Spike/SpikeBoot.cs` 주석.
- **상태**: concluded
- **결과**: 기본 보간으로 감수할 만함. 측정값과 다음 할 일은 [SPIKE-NOTE.md](SPIKE-NOTE.md).
