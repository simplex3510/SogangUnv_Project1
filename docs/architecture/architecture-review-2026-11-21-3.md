## Architecture Review Report
Date: 2026-11-21
Mode: single-gdd design/gdd/networking.md
Workflow: minimal
Engine: Unity 6000.6.8f1
GDDs Reviewed: 1
ADRs Reviewed: 2

> 요청 범위: `architecture-review-2026-11-21-2.md` 이후 ADR-0001 개정 3(방향 회전 외부 수정값, 트리거 버퍼, Relay 상태 파일, 정산 유지 조건, 검증 9·13) 반영 확인. 신선도 확인 결과 바뀐 입력은 ADR-0001뿐이다.

Reviewed-Content-Hash: docs/architecture/adr-0001-multiplayer-networking-architecture.md be78ad447995dbbecfeccc4dd589f748569441de
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
✅ Covered: 24
⚠️ Partial: 0
❌ Gaps: 0

TR-networking-010: 결정 4 외부 수정값과 Key Interfaces `VehicleSimState`에 대기 방향 회전 yaw가 추가돼 해결. 서버는 물리 스텝 뒤(결정 15의 6단계)에 이 값을 기록하고, 예측 차량은 그 상태에서 재시뮬레이션하며 다음 스텝에 적용하므로 순서가 맞다.

### Coverage Gaps (no ADR exists)
없음.

### Cross-ADR Conflicts

이전 "ADR-0001 결정 14 vs ADR-0002 결정 3(Relay 모드 준비 신호)"은 결정 14의 Relay 모드 항목(`CODE` 한 줄이 준비 신호, 호스트는 코드로 접속)으로 해결.

## Conflict: ADR-0001 결정 2 × ADR-0002 결정 2 (결합 결과)
Type: Pattern (🟡 비차단, 변화 없음)
ADR-0001 claims: 싱글도 같은 자식 서버 경로.
ADR-0002 claims: 서버는 항상 모든 인터페이스에서 수신.
Impact: 혼자 하는 플레이어도 첫 실행 때 Windows 방화벽 허용 창을 본다.
Resolution options:
  1. 세션 생성 때 혼자/친구와 선택, 혼자면 loopback 바인딩(하루 도중 참가 불가를 감수).
  2. 경고를 감수하고 서버 빌드·배포 ADR에서 안내 또는 설치 시 규칙 등록.

### ADR Dependency Order
Foundation: 1. ADR-0001 Multiplayer Networking Architecture (Accepted)
Depends on Foundation: 2. ADR-0002 Session Join Method (requires ADR-0001, Accepted)
순환 없음, 미해결 의존 없음(의존 섹션 변경 없음).

### GDD Revision Flags
없음 — GDD 가정은 Accepted ADR과 일치한다.

참고(플래그 아님): networking.md의 AC-NET-30·31이 AC-NET-29 앞에 놓여 있고, AC-NET-31은 정산 중 서버 비정상 종료 경우를 다루지 않는다.

### Engine Compatibility Issues

#### Engine Audit Results
Engine: Unity 6000.6.8f1
ADRs with Engine Compatibility section: 2 / 2

Deprecated API References: 없음.

Stale Version References:
  - `docs/engine-reference/unity/modules/networking.md`: 여전히 NGO 1.x 예시뿐(이전 지적 유지).

Post-Cutoff API Conflicts: 없음.

#### Engine Specialist Findings
- 개정 3 반영 확인: 트리거 콜백 버퍼(가이드라인), 정산 유지 조건에 정산 결과 수신 추가(결정 16), 복구 순간이동 속도 쓰기 예외, 지면 레이캐스트 허용, 검증 (9) 직접 보간 확인으로 변경, (13) 해결.
- 문구: 결정 14 둘째 항목 "설정 시간 안에 READY가 없으면"은 Relay 모드에서 "READY(Relay는 CODE)"여야 한다. 셋째 항목과 함께 읽으면 뜻은 통하므로 비차단.
- 가이드라인 159행(물리 콜백의 상태 변경은 서버에서만)과 166행(서버에서도 버퍼에만)은 겹치지만 모순되지 않는다.
- 예측 구조 자체의 실측(검증 6~8)은 여전히 첫 구현 스토리 대상.

### Architecture Document Coverage
Architecture document coverage: NOT ASSESSED — no docs/architecture/architecture.md (workflow `minimal`에서는 필수 아님).

---

### Verdict: CONCERNS
모든 요구사항이 Accepted ADR로 커버된다(24/24). 남은 것은 ADR-0002의 🟡 비차단 충돌(싱글 세션 방화벽 창) 하나다.

### Required ADRs
1. ADR-0002 보완: 싱글 세션 바인딩(loopback 여부)
2. 신규: 서버 빌드·배포 ADR(Dedicated Server 모듈, 방화벽, CI)
