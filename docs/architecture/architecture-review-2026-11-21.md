## Architecture Review Report
Date: 2026-11-21
Mode: single-gdd design/gdd/networking.md
Workflow: minimal
Engine: Unity 6000.6.8f1
GDDs Reviewed: 1
ADRs Reviewed: 2

> 요청 범위: `design/gdd/networking.md`, ADR-0001, ADR-0002, `prototypes/net-latency-spike-2026-11-21/SPIKE-NOTE.md`. 전체 행렬은 `requirements-traceability.md`.

Reviewed-Content-Hash: docs/architecture/adr-0001-multiplayer-networking-architecture.md ca0bee9f2bbe5f45606ed8a2d16900f0d1cd58e0
Reviewed-Content-Hash: docs/architecture/adr-0002-session-join-method.md 2c7a34c8a5a092d1a1d62b3109e9b7ca68289c5b
Reviewed-Content-Hash: design/gdd/collision-durability-rules.md dd05e873069925d8fd1d366917e57ed0949e4725
Reviewed-Content-Hash: design/gdd/delivery-cargo-vehicle-rules-plan.md d5c99e6afe07bd0a8610df8123c36ef64f60f85b
Reviewed-Content-Hash: design/gdd/delivery-cargo-vehicle-rules.md f557c3e4fb68c1a6604df1f8b77ad5d9cdecb422
Reviewed-Content-Hash: design/gdd/game-concept.md 6fb5b712dca22198d1be733be9b68e1763d1108e
Reviewed-Content-Hash: design/gdd/networking.md 9e3515d313b3d03cc1290012bf7e767be838e7ee
Reviewed-Content-Hash: docs/architecture/tr-registry.yaml 6f7f1e7c79cdb9acdce60fa357010b6395951d49
Reviewed-Content-Hash: docs/engine-reference/unity/PLUGINS.md 8d99063a63269d629be11e9e00e9f460188b689a
Reviewed-Content-Hash: docs/engine-reference/unity/VERSION.md 559ef47ff5d2b91324339b40b6dbf0befbc8e3f9
Reviewed-Content-Hash: docs/engine-reference/unity/breaking-changes.md 55fde59baa3374d61559c6dca8454e70ef37f2db
Reviewed-Content-Hash: docs/engine-reference/unity/current-best-practices.md e590bd628239f22b623712a3233de43b4a4a07e2
Reviewed-Content-Hash: docs/engine-reference/unity/deprecated-apis.md e0df042ea6f487626fc9ffaa9cae5bcd3e185d26
Reviewed-Content-Hash: docs/engine-reference/unity/modules/animation.md e0601eeba72565d71e1dd2a8bb267eb966d10a5f
Reviewed-Content-Hash: docs/engine-reference/unity/modules/audio.md 56c04c15cb9a356b87a27df5d0fcf67f18db9302
Reviewed-Content-Hash: docs/engine-reference/unity/modules/input.md 1b64a2381167d17c1b796d4d5f93a7e1fcc651e6
Reviewed-Content-Hash: docs/engine-reference/unity/modules/navigation.md 484a867a2fe1acf86b2071f7950b10afaab8ffab
Reviewed-Content-Hash: docs/engine-reference/unity/modules/networking.md ab0ada43362406731784044326eed68fd25aa3b1
Reviewed-Content-Hash: docs/engine-reference/unity/modules/physics.md 191e8d8d502363a99e80670942d251ef819a3976
Reviewed-Content-Hash: docs/engine-reference/unity/modules/rendering.md 292081320361b18c12715f7ed11dae2a38ba5f2d
Reviewed-Content-Hash: docs/engine-reference/unity/modules/ui.md 87de2ff30ce1c8d9499b6bcf084947201852e6ab
Reviewed-Content-Hash: project.yaml 4e0070e765e035a8264a4ae0a7d330a589cf3224
Reviewed-Content-Hash: .claude/docs/technical-preferences.md 9d7277797cfe8349f245a62efa9c76c4b830601d
Reviewed-Content-Hash: .claude/skills/architecture-review/SKILL.md 8c1dd9d6f27f396e3b769b939de27d780d7c486a
Reviewed-Content-Hash: .claude/skills/architecture-review/references/1-load.md 9a65759ebdbb2cb1f9744775bfed18cb25e76347
Reviewed-Content-Hash: .claude/skills/architecture-review/references/2-extract-requirements.md d08ea0b0e9ab9760b0d8bfe0cd9115f0bc40f8e2
Reviewed-Content-Hash: .claude/skills/architecture-review/references/3-traceability.md bb7b6b65c5e5e5b8869fa692413aa831615dede7
Reviewed-Content-Hash: .claude/skills/architecture-review/references/4-conflicts.md 0d30f8173b23248a32523a365b606dd1ce9642ab
Reviewed-Content-Hash: .claude/skills/architecture-review/references/5-engine.md 9a0de3ccca214e473e9433450462e0c6e57f9de2
Reviewed-Content-Hash: .claude/skills/architecture-review/references/6-document-coverage.md dcce78482ec2ce60048989b6f9d1f797dd0a9a0e
Reviewed-Content-Hash: .claude/skills/architecture-review/references/7-report.md a43e17378df952791974dab5f10d7a98c16156fe
Reviewed-Content-Hash: .claude/skills/architecture-review/references/8-write.md bf6d8946fc7b9e19eda7f65503a5c8f593a0d2b1
Reviewed-Content-Hash: .claude/skills/architecture-review/references/9-handoff.md ade419da14b200ae3bc8d00ed3f2c04707a6275b
Reviewed-Content-Hash: .claude/skills/architecture-review/references/lens-engine.md d7770f2737d185cf51a338aaf550fa01b773fb07

---

### Traceability Summary
Total requirements: 24
✅ Covered: 18
⚠️ Partial: 6 (TR-networking-004, 010, 014, 017, 021, 022)
❌ Gaps: 0

### Partial Coverage

- **TR-networking-010·014 (최우선)**: 예측 차량의 순수 스텝 함수에 필요한 외부 값이 `VehicleSimState`에 정의되지 않았다. 현재 `PlayerMovementController`는 적재 속도 배율(`_interactionController.SpeedMultiplier`), 장애물 속도 효과(배율·지속시간), 복구 잠금, 조작 활성 잠금, `GameManager.IsPaused`를 읽는다(126, 287~291행). ADR-0001은 "주행 모드·고속 게이지·드리프트 등"만 명시한다. 빠지면 적재·장애물·복구 직후마다 예측이 어긋나 재조정이 반복된다. → ADR-0001 결정 4에 서버가 정하는 외부 수정값(적재 배율, 장애물 효과와 남은 시간, 잠금 플래그)을 `VehicleSimState` 또는 별도 구조체로 명시. static 전역(`GameManager.IsPaused`)은 서버 경로에서 제거.
- **TR-networking-017**: 호스트 정산 확인으로 서버가 종료되면 남은 클라이언트는 접속 끊김을 받는다. 호스트 이탈 처리(연결 끊김 안내 → 메인)와 구분할 규칙이 없다. → 마지막 수신 세션 상태가 Settlement이면 안내 없이 정산 화면 유지.
- **TR-networking-022**: 접속 끊김 콜백·요청 RPC와 FixedUpdate 도메인 틱의 처리 순서가 정해지지 않아 "이탈 먼저" 규칙을 보장할 수 없다. → 어댑터가 네트워크 이벤트를 큐에 모아 도메인 틱의 정해진 위치에서 반영.
- **TR-networking-004**: 인원별 설정 값 고정을 서버 도메인에 전달하는 데이터 경로가 없다.
- **TR-networking-021**: GDD의 입력 미수신 타임아웃과 NGO/UTP 자체 접속 타임아웃의 관계가 정해지지 않았다.

### Coverage Gaps (no ADR exists)
없음.

### Cross-ADR Conflicts

## Conflict: ADR-0002 vs ADR-0001
Type: Integration (🟡 비차단)
ADR-0002 claims: 서버 → 호스트 클라이언트로 준비 완료 신호와 접속 정보(포트·참가 코드)를 "ADR-0001의 자식 프로세스 채널"로 전달한다.
ADR-0001 claims: 서버 기동 인자(`--port`, `--parent-pid`, `--host-token`, 설정 JSON)만 정의. 서버 → 호스트 채널은 없다.
Impact: 2단계 참가 코드 전달 경로가 없다. 1단계에서도 스파이크 함정(남은 서버가 포트를 잡으면 새 서버 바인딩 실패, 클라이언트는 에러 없이 예전 서버에 접속)을 호스트가 감지할 수단이 없다.
Resolution options:
  1. ADR-0001 Key Interfaces에 서버 → 호스트 채널 추가(표준 출력 한 줄 프로토콜: `READY port=…` / `FAIL bind` / `CODE …`), 호스트는 READY 수신 후 접속.
  2. 위에 더해 connection approval 응답에 세션 ID를 넣어 예전 서버 접속을 거부.

## Conflict: ADR-0001 결정 2 × ADR-0002 결정 2 (결합 결과)
Type: Pattern (🟡 비차단)
ADR-0001 claims: 싱글도 같은 자식 서버 경로.
ADR-0002 claims: 서버는 항상 모든 인터페이스에서 수신.
Impact: 혼자 하는 플레이어도 첫 실행 때 Windows 방화벽 허용 창을 본다.
Resolution options:
  1. 세션 생성 때 혼자/친구와 선택, 혼자면 loopback 바인딩(하루 도중 참가 불가를 감수).
  2. 경고를 감수하고 서버 빌드·배포 ADR에서 안내 또는 설치 시 규칙 등록.

### ADR Dependency Order
Foundation: 1. ADR-0001 Multiplayer Networking Architecture (Accepted)
Depends on Foundation: 2. ADR-0002 Session Join Method (requires ADR-0001, Accepted)
순환 없음, 미해결 의존 없음.

### GDD Revision Flags

| GDD | Assumption | Reality | Action |
|-----|-----------|---------|--------|
| networking.md 규칙 1·16, Open Questions 3·4 | ADR-0001·0002가 Proposed | 둘 다 Accepted(2026-11-21) | 표기 수정 |
| networking.md Open Question 5 | NGO 버전·RPC 방식 NOT SOURCEABLE | 스파이크: NGO 2.14.0 + `[Rpc(SendTo.Server)]`가 6000.6.8f1에서 동작 | 해결로 갱신(메인 프로젝트 재확인만 남김) |
| networking.md Acceptance Criteria | ADR-0002 Accepted 후 1단계 기준 추가 예정 | 이미 Accepted | 직접 접속 AC 추가 |
| networking.md States and Transitions | Settlement 중 호스트 접속 끊김 미정의 | TR-networking-017 참고 | 전이 추가 |

systems-index.md가 없어 색인 상태는 갱신하지 않았다.

### Engine Compatibility Issues

#### Engine Audit Results
Engine: Unity 6000.6.8f1
ADRs with Engine Compatibility section: 2 / 2

Deprecated API References: 없음(ADR-0001이 `GetInstanceID` 금지를 명시).

Stale Version References:
  - `docs/engine-reference/unity/modules/networking.md`: 6.3 기준, NGO 1.x(`[ServerRpc]`/`[ClientRpc]`) 예시뿐. 스파이크로 확인한 NGO 2.14 사실(통합 `[Rpc]`, `DebugSimulator` 폐기, `AnticipatedNetworkTransform` 보간 끔, `NetworkTransform.Interpolate`는 권한 쪽 값 복제)을 반영해야 이후 코드가 1.x 방식으로 쓰이지 않는다.

Post-Cutoff API Conflicts: 없음. ADR-0002 2단계 API는 엔진 참조가 아니라 Unity 플러그인 문서 출처(ADR에 명시).

#### Engine Specialist Findings
- `SimulationMode.Script`에서 Rigidbody 자동 보간 동작 여부 unverified. 동작하지 않으면 예측 차량이 고정 스텝 단위로 떨린다. ADR의 보정 스무딩은 재조정 오차만 다룬다. → 첫 스토리에서 확인, 필요하면 표시용 자식에 스텝 간 보간을 직접 적용.
- 입력 틱 = 클라이언트 물리 스텝 번호이므로 서버와 클라이언트의 `fixedDeltaTime`이 같아야 한다. ADR에 명시 없음.
- `VehicleSimState` 송신 주기가 "서버 틱마다"로만 적혀 NGO 틱(30)인지 서버 FixedUpdate인지 모호하다.
- 예측 구조(결정 4)는 실험으로 검증되지 않았다. 스파이크는 예측 없는 구조만 측정했다. 검증 항목 (6) 재시뮬레이션 비용, (7) 숨긴 루트 동작, (8) PhysX 비결정성 크기를 첫 구현 스토리의 실측 대상으로 둔다.

### Architecture Document Coverage
Architecture document coverage: NOT ASSESSED — no docs/architecture/architecture.md (workflow `minimal`에서는 필수 아님).

---

### Verdict: CONCERNS
차단할 충돌과 ❌ 누락은 없고 두 ADR 모두 Accepted다. 부분 커버 6건과 🟡 충돌 2건이 남았으며, 그중 TR-networking-010·014(예측 차량 입력 범위)는 구현 전 ADR-0001 개정이 가장 싸다.

### Required ADRs
1. ADR-0001 개정: `VehicleSimState`·외부 수정값, 서버 → 호스트 채널, 정산 중 서버 종료 처리, 네트워크 이벤트 처리 순서, `fixedDeltaTime` 일치
2. ADR-0002 보완: 싱글 세션 바인딩(loopback 여부)
3. 신규: 서버 빌드·배포 ADR(Dedicated Server 모듈, 방화벽, CI)
