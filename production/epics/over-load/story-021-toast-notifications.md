# Story 021: 토스트 알림

> **Epic**: over-load
> **Status**: Complete
> **Layer**: Presentation
> **Type**: UI
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-17

## Context

**GDD**: `design/gdd/game-concept.md` 4.2·13장
**Requirement**: 상세 규칙: `design/quick-specs/hud-layout-2026-11-17.md` ("토스트 알림" 절)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating for 6.6)
**Engine Notes**: UI Toolkit, `HudPanelSettings`와 한글 FontAsset 재사용. 등장·퇴장은 USS transition(`translate`, `opacity`).

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/quick-specs/hud-layout-2026-11-17.md` (2026-11-17 확정).*

- [x] 동적 물류가 생성되면 상단 중앙에 `새 물류 — {배송지} · {유형}` 알림이 뜬다. 하루 시작 시 정적 물류에는 뜨지 않는다
- [x] 상호작용이 막히면 차단 사유가 토스트로 뜬다. 기존 `PlayerUIController` 문구 텍스트는 화면에 나오지 않는다
- [x] 물류가 폐기로 전환되면 폐기 알림이 토스트로 뜬다. 기존 `DurabilityHud` 폐기 라벨은 화면에 나오지 않는다
- [x] 알림은 위에서 내려오며 나타나고(0.3초), 유지(모든 알림 같은 시간, 기본 2초) 후 위로 올라가며 사라진다(0.3초)
- [x] 최신 알림이 위에 쌓이고 최대 3개까지 보인다. 넘치면 가장 오래된 알림이 즉시 퇴장한다
- [x] 같은 문구가 표시 중에 다시 오면 새로 쌓지 않고 그 알림의 유지 시간만 다시 시작한다
- [x] 하루 종료(조작 잠금) 뒤에는 새 알림이 뜨지 않는다. 남은 알림은 정산 화면과 상관없이 유지 시간이 지나면 사라진다 (2026-11-17 변경)
- [ ] No regression: 차단 시 상호작용 동작, 폐기 전환(Story 005), 내구도 표시(Story 006) — DEFERRED: 전체 QA에서 확인
- [x] GUI는 임시다. 동적 물류 알림, 차단 사유, 폐기, 3개 쌓임 스크린샷을 `production/qa/evidence/`에 보관한다

---

## Implementation Notes

- `DeliveryGenerationSystem`에 동적 물류 생성 이벤트(`Action<Parcel>`)를 추가한다. 정적 생성 경로에서는 발생시키지 않는다.
- 토스트 컴포넌트 하나가 세 이벤트를 구독한다: 생성 이벤트, `PlayerInteractionController.InteractionBlocked`, `DeliveryDeadlineSystem.ParcelSpoiled`(현재 폐기 라벨과 같은 트리거).
- 큐 규칙(최대 개수, 중복 병합, 유지 시간)은 UI 로직 안의 작은 순수 클래스로 둔다. 시간 값은 `[SerializeField]` `TODO(tuning)`.
- 이전 후 `PlayerUIController`의 `_messageText` 표시 코드와 `DurabilityHud`의 폐기 라벨을 지운다.
- UI는 게임 상태를 바꾸지 않는다.

---

## Out of Scope

- 알림 소리
- 알림 클릭·스와이프 상호작용
- 최종 GUI 디자인 (첫 릴리즈 전 임시 단계)

---

## Files

- `Assets/Game/Scripts/Logistics/DeliveryGenerationSystem.cs` (DynamicParcelGenerated 이벤트)
- `Assets/Game/Scripts/Player/PlayerUIController.cs` (차단 문구 표시 제거)
- `Assets/Game/Scripts/UI/DurabilityHud.cs` (폐기 라벨 제거, 비어 있으면 Player 자동 찾기), `Assets/Game/UI/Hud/DurabilityHud.uxml`, `Assets/Game/UI/Hud/DurabilityHud.uss` (폐기 라벨 제거)
- `Assets/Game/UI/Hud/ToastHud.cs`, `Assets/Game/UI/Hud/ToastHud.uxml`, `Assets/Game/UI/Hud/ToastHud.uss` (신규, 종류별 색 변수·공통 유지 시간)
- `Assets/Game/Prefabs/UI/Hud.prefab` (신규: StatusHud·SpeedometerHUD·DurabilityHud·ToastHud 묶음)
- `Assets/Game/Scenes/_DevOnly/SampleMapDesign.unity` (HUD를 Hud 프리팹 인스턴스로 교체)

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: UI
**Required evidence**: 동적 물류 알림, 차단 사유, 폐기, 3개 쌓임의 retained screenshot (`production/qa/evidence/`)

**Status**: [x] production/qa/evidence/story-021-toast-notifications/ (00~08)

---

## Dependencies

- Depends on: `production/epics/over-load/story-014-cargo-panel-fold-scroll.md` (같은 `DurabilityHud.cs`, 먼저 닫는다). Story 017·019와 같은 파일이라 동시에 진행하지 않는다
- Unlocks: None
- 참고: Story 020(한글 문구)은 차단 사유 문자열을 바꾼다. 순서와 무관하게 토스트는 넘겨받은 문자열을 그대로 표시한다

## Completion Notes
**Completed**: 2026-11-17
**Criteria**: 8/9 passing (기준 8 회귀: DEFERRED — 전체 QA에서 확인)
**Deviations**: None. 범위 밖이지만 요청된 작업으로 `Hud.prefab` 묶음과 DurabilityHud Player 자동 찾기를 `## Files`에 기록
**Test Evidence**: production/qa/evidence/story-021-toast-notifications/ (00~08 스크린샷, stamp 일치)
**Reviewed by**: self, no independent review
**변경 이력**: 구현 후 사용자 요청으로 (1) 모든 알림이 유지 시간 값 하나를 사용, (2) 정산 화면이 열려도 알림을 지우지 않음으로 기준 4·7과 `design/quick-specs/hud-layout-2026-11-17.md`를 고침

## Calls beyond the design
- 유형 표시명 일반/신속/신선 배송 (`ToastHud.cs`) · kept 2026-11-17
- 배송지는 오브젝트 이름 그대로 표시 (`ToastHud.cs`) · kept 2026-11-17
- 위 24px·간격 8px·글자 22px·슬레이트 배경, 새 물류 파랑·차단 주황·폐기 빨강 테두리 (`ToastHud.uss`) · kept 2026-11-17
- 이동 거리 24px·ease-out (`ToastHud.uss`) · kept 2026-11-17
- 유지 시간은 게임 배속과 무관한 실제 시간 (`ToastHud.cs`) · kept 2026-11-17
- 퇴장 중인 알림과 같은 문구가 오면 새로 쌓임 (`ToastQueue`) · kept 2026-11-17
