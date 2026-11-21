## Architecture Review Report
Date: 2026-11-21
Mode: single-gdd design/gdd/networking.md
Workflow: minimal
Engine: Unity 6000.6.8f1
GDDs Reviewed: 1
ADRs Reviewed: 2

> 요청 범위: 같은 날 이전 리포트(`architecture-review-2026-11-21.md`) 이후 바뀐 ADR-0001 개정 2(결정 4·5 보강, 결정 14~18)와 `networking.md`(AC-NET-30·31, 정산 중 끊김 전이·Edge). 신선도 확인 결과 바뀐 입력은 이 두 파일뿐이다.

Reviewed-Content-Hash: docs/architecture/adr-0001-multiplayer-networking-architecture.md c24de302a9eb43f5c9c52a2e8970b1e6ee7f419b
Reviewed-Content-Hash: docs/architecture/adr-0002-session-join-method.md 2c7a34c8a5a092d1a1d62b3109e9b7ca68289c5b
Reviewed-Content-Hash: design/gdd/collision-durability-rules.md dd05e873069925d8fd1d366917e57ed0949e4725
Reviewed-Content-Hash: design/gdd/delivery-cargo-vehicle-rules-plan.md d5c99e6afe07bd0a8610df8123c36ef64f60f85b
Reviewed-Content-Hash: design/gdd/delivery-cargo-vehicle-rules.md f557c3e4fb68c1a6604df1f8b77ad5d9cdecb422
Reviewed-Content-Hash: design/gdd/game-concept.md 6fb5b712dca22198d1be733be9b68e1763d1108e
Reviewed-Content-Hash: design/gdd/networking.md 757e88bcadcbf663fd9a1a34a304e8e779871acb
Reviewed-Content-Hash: docs/architecture/tr-registry.yaml c4c2c2c37694ac6fa77420396de267467531b1e5
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
✅ Covered: 23
⚠️ Partial: 1 (TR-networking-010)
❌ Gaps: 0

이전 부분 커버 6건 중 5건 해결: 004(결정 17), 014(결정 4 복구 잠금), 017(결정 16), 021(결정 16), 022(결정 15).

### Partial Coverage

- **TR-networking-010**: 결정 4의 외부 수정값(적재 배율, 장애물 속도 배율·남은 시간, 복구 잠금, 조작 활성)에 **장애물 방향 회전 요청**이 없다. `HeadingRotationEffect`는 `IVehicleEffectTarget.RequestHeadingRotation`으로 `PlayerMovementController._pendingYawDegrees`에 값을 쌓고 다음 스텝에서 적용한다(174·182행). 스텝 함수가 `VehicleInput`·`VehicleSimState`만 읽는다는 결정과 맞추려면 이 값도 `VehicleSimState`에 들어가야 한다. 빠져도 재조정으로 따라가지만 회전 순간마다 스냅 임계를 넘을 수 있다. → 결정 4 외부 수정값과 Key Interfaces `VehicleSimState`에 "대기 중인 방향 회전(yaw)" 추가.

### Coverage Gaps (no ADR exists)
없음.

### Cross-ADR Conflicts

## Conflict: ADR-0001 결정 14 vs ADR-0002 결정 3
Type: Integration (🟡 비차단, 이전 충돌의 남은 부분)
ADR-0001 claims: 호스트 클라이언트는 상태 파일에서 `READY`를 받은 뒤에만 loopback 접속한다.
ADR-0002 claims: Relay 모드에서는 호스트도 참가 코드로 접속한다(중계 경유).
Impact: Relay 모드에서 호스트가 무엇을 기다려 어디로 접속할지, 상태 파일이 "한 줄"인데 `READY`와 `CODE`를 어떤 순서로 쓰는지 정해지지 않았다. 1단계(Direct)에는 영향 없음.
Resolution options:
  1. 결정 14에 "Relay 모드는 `CODE <joinCode>` 한 줄이 준비 신호이며 호스트는 그 코드로 접속" 추가.
  2. ADR-0002 2단계 스파이크 결과와 함께 정리.

이전 충돌 "서버 → 호스트 채널 없음"은 결정 14(상태 파일, 토큰 불일치 거부)로 해결됐다.

## Conflict: ADR-0001 결정 2 × ADR-0002 결정 2 (결합 결과)
Type: Pattern (🟡 비차단, 변화 없음)
Impact: 혼자 하는 플레이어도 첫 실행 때 Windows 방화벽 허용 창을 본다. ADR-0002가 바뀌지 않아 이전 리포트의 해결 방안 그대로.

### ADR Dependency Order
Foundation: 1. ADR-0001 Multiplayer Networking Architecture (Accepted)
Depends on Foundation: 2. ADR-0002 Session Join Method (requires ADR-0001, Accepted)
순환 없음, 미해결 의존 없음(의존 섹션 변경 없음).

### GDD Revision Flags
이전 플래그 4건(Proposed 표기, Open Question 5, Direct AC, Settlement 끊김 전이) 모두 반영됨. 새 플래그 없음 — GDD 가정은 검증된 엔진 동작·Accepted ADR과 일치한다.

참고(플래그 아님):
- AC-NET-30·31이 AC-NET-29 앞에 놓여 번호 순서가 어긋난다.
- AC-NET-31은 "호스트 클라이언트 종료"만 다룬다. Edge의 "서버 비정상 종료"(정산 중) 경우는 판정 기준이 없다.
- 결정 18(일시정지 제거)은 game-concept 3.2·브리프 범위 밖과 일치한다. 달라지는 것은 가이드 패널 quick-spec이며 그 문서에 주석이 달렸다.

### Engine Compatibility Issues

#### Engine Audit Results
Engine: Unity 6000.6.8f1
ADRs with Engine Compatibility section: 2 / 2

Deprecated API References: 없음.

Stale Version References:
  - `docs/engine-reference/unity/modules/networking.md`: 여전히 NGO 1.x 예시뿐(이전 지적 유지).

Post-Cutoff API Conflicts: 없음.

#### Engine Specialist Findings
- **트리거 콜백도 버퍼 대상이어야 한다.** 가이드라인은 `OnCollision*`만 버퍼에 넣으라고 한다. 그러나 장애물(`TriggerEnterObstacleTrigger`, `InteractionObstacleTrigger`)과 상호작용 구역(`PlayerInteractionController`)은 `OnTriggerEnter`/`Exit`에서 효과를 바로 적용한다. 서버 Script 모드에서 이 콜백은 `Physics.Simulate` 도중 불리므로, 결정 15의 4단계(충돌 판정)·5단계(도메인 판정)보다 먼저 도메인이 바뀌어 순서 보장이 깨진다. → 가이드라인을 `OnCollision*`·`OnTrigger*` 모두로 확장.
- **정산 화면 유지 조건.** 결정 16은 마지막 `SessionState`가 Settlement인지만 본다. "정산 결과를 받았는지"도 조건에 넣는 편이 안전하다. 둘은 같은 NetworkBehaviour라 대개 함께 도착하지만 문서로 보장된 것은 아니다.
- **문서 안의 작은 어긋남.**
  - 검증 항목 (9)는 "Rigidbody 보간이 동작하는지 확인"인데, 가이드라인은 이미 "interpolation None + 표시용 자식 직접 보간"으로 정했다. (9)를 "직접 보간 결과 확인"으로 바꿀 것.
  - 가이드라인 "속도 직접 쓰기는 재조정에서만"과 달리, 복구 때 서버가 속도를 0으로 만든다(`PlayerMovementController` 159행). 예외로 명시할 것.
  - 스텝 함수가 지면 레이캐스트(물리 씬 질의)를 쓰는 것은 "다른 값을 읽지 않는다" 규칙의 허용 범위로 적어 둘 것.
- **검증 항목 (13) 해결.** 기존 코드가 `Rigidbody.linearVelocity`로 6000.6.8f1에서 컴파일된다.
- **이전 지적 반영 확인.** `fixedDeltaTime` 일치(결정 4 틱 일치), `VehicleSimState` 송신 주기(결정 5), 예측 차량 보간(가이드라인·검증 9). 예측 구조 자체의 실측(검증 6~8)은 여전히 첫 구현 스토리 대상이다.

### Architecture Document Coverage
Architecture document coverage: NOT ASSESSED — no docs/architecture/architecture.md (workflow `minimal`에서는 필수 아님).

---

### Verdict: CONCERNS
차단할 충돌과 ❌ 누락은 없다. 부분 커버 1건(TR-networking-010 방향 회전), 🟡 충돌 2건, 트리거 콜백 버퍼 규칙이 남았다. 모두 ADR-0001 문장 보강으로 해결된다.

### Required ADRs
1. ADR-0001 보강(소규모): 외부 수정값에 방향 회전 추가, 트리거 콜백 버퍼, Relay 모드 상태 파일 규칙, 정산 유지 조건, 검증 (9)·속도 쓰기 예외 정리
2. ADR-0002 보완: 싱글 세션 바인딩(loopback 여부) — 이전 리포트에서 이어짐
3. 신규: 서버 빌드·배포 ADR(Dedicated Server 모듈, 방화벽, CI) — 이전 리포트에서 이어짐
