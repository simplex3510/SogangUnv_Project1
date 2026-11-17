# Story 022: 플레이어 상호작용 UI의 UI Toolkit 전환

> **Epic**: over-load
> **Status**: Complete
> **Layer**: Presentation
> **Type**: UI
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-17

## Context

**GDD**: `design/gdd/game-concept.md` 10.6·13장
**Requirement**: 상세 규칙: `design/quick-specs/player-interaction-ui-toolkit-2026-11-17.md` (UI 백로그 UI-01)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating for 6.6)
**Engine Notes**: UI Toolkit, `HudPanelSettings`와 한글 FontAsset 재사용. 월드 → 패널 좌표 변환으로 플레이어 화면 위치를 따라간다. 아이콘은 USS `background-image`·tint.

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/quick-specs/player-interaction-ui-toolkit-2026-11-17.md` (2026-11-17).*

- [ ] 상차·하차·폐기·방해물 대상이 있을 때 플레이어 오른쪽에 `Space` + 해당 종류 아이콘 패널이 하나만 보이고, 없으면 숨는다. 처분 장소에서는 하차 아이콘이 아니라 `trash3-fill`이다
- [ ] 홀드 중 그 패널 안이 진행도만큼 아래에서 위로 차고, 떼거나 끝나면 비워진다
- [ ] 정상 주행 중에는 복구 패널이 보이지 않는다
- [ ] 차량이 뒤집히거나 끼인 채 `T_stuck`이 지나면 플레이어 왼쪽에 `R` 패널이 보이고, R 홀드 중 아래에서 위로 찬다
- [ ] 정상 주행 중 R을 누르면 복구 패널이 나타나 차오르고, 떼면 숨으며, 복구 동작은 이전과 같다
- [ ] 복구 후 잠금 중과 하루 종료 뒤에는 복구 패널이 보이지 않는다
- [ ] 플레이어 프리팹에 Canvas UI가 남아 있지 않고, 하단 중앙 속도계와 겹치는 진행 바가 없다
- [ ] No regression: 상·하차·폐기·복구·방해물 동작과 홀드 시간, 물류센터 창 행 테두리 진행
- [ ] GUI는 임시다. 상차 대상 근처, 상차 중 채움, 처분 장소 폐기 아이콘, 갇힘 상태 R 패널, R 홀드 중 스크린샷을 `production/qa/evidence/`에 보관한다

---

## Implementation Notes

- 아이콘: Bootstrap Icons(MIT) `box-arrow-in-down`(상차), `box-arrow-up`(하차), `trash3-fill`(폐기), `hand-index-thumb-fill`(방해물)을 `Assets/Game/UI/Icons/`에 추가한다.
- 처분 장소는 상호작용 종류가 `Unload`다. 대상이 `DisposalSite`인지로 폐기 아이콘을 고른다. `InteractionTypes`에 값을 추가한다면 직렬화 값이 밀리지 않게 끝에 붙인다.
- 진행도는 기존 이벤트를 그대로 쓴다: `PlayerInteractionController.InteractionProgressChanged`, `VehicleRecoveryController.RecoveryProgressChanged`.
- 갇힘 판정은 `VehicleRecoveryController`에 둔다. 새 값 `T_stuck`(1.5초), `v_stuck`(0.5 m/s)은 `VehicleRecoveryConfig`에 `TODO(tuning)`. 뒤집힘 기준은 기존 `MinBodyUpDot`을 쓴다.
- `KeyIconStateChanged`의 visible 의미가 "운영 중"에서 "갇힘 또는 R 홀드 중"으로 바뀐다. 구독자를 모두 확인한다.
- 패널 크기·좌우 오프셋·채움색은 USS/`[SerializeField]` `TODO(tuning)`.
- 플레이어 프리팹의 Canvas 제거와 씬 배치는 Unity CLI로 Editor에서 한다. `.prefab`·`.unity`를 직접 고치지 않는다.
- UI는 게임 상태를 바꾸지 않는다.

---

## Out of Scope

- 다른 Canvas UI(배송지 물류 아이콘 등)의 UI Toolkit 전환
- 상호작용 차단 상태의 패널 표시 변경 (차단 사유는 토스트 그대로)
- 최종 GUI 디자인 (첫 릴리즈 전 임시 단계)

---

## Files

- `Assets/Game/UI/Hud/PlayerInteractionHud.cs`, `.uxml`, `.uss` (신규: 플레이어 좌우 키 안내 패널)
- `Assets/Game/UI/Icons/box-arrow-in-down.svg`, `box-arrow-up.svg`, `trash3-fill.svg`, `hand-index-thumb-fill.svg` (신규)
- `Assets/Game/Scripts/Player/VehicleRecoveryController.cs` (갇힘 판정, `KeyIconStateChanged` 의미 변경)
- `Assets/Game/Scripts/Player/VehicleRecoveryConfig.cs` (`StuckSeconds`, `StuckSpeed`)
- `Assets/Game/Scripts/Player/PlayerMovementController.cs` (`HasThrottleInput`)
- `Assets/Game/Scripts/Player/PlayerInteractionController.cs` (`IsTargetDisposalSite`)
- 삭제: `Assets/Game/Scripts/Player/PlayerUIController.cs`, `Assets/Game/Scripts/UI/PlayerObjectUI.cs`, `Assets/Game/Scripts/UI/InteractionProgressUI.cs`
- `Assets/Game/Prefabs/Delivery.prefab` (Canvas·PlayerUIController 제거), `Assets/Game/Prefabs/UI/Hud.prefab` (PlayerInteractionHud 추가)
- `Assets/Game/Scenes/_DevOnly/SampleMapDesign.unity`, `_DevOnly/DevScene.unity`, `FailedScene.unity` (Canvas/InteractionProgress 제거)

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: UI
**Required evidence**: 상차 대상 근처, 상차 중 채움, 처분 장소 폐기 아이콘, 갇힘 상태 R 패널, R 홀드 중의 retained screenshot (`production/qa/evidence/story-022-player-interaction-ui-toolkit/`). 테스트는 `qa.level: minimal`에서 생략

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None
- Unlocks: None

## Completion Notes
**Completed**: 2026-11-17
**Criteria**: 6/9 passing (DEFERRED — requires playtest session: AC-2의 홀드 해제·완료 시 비워짐, AC-5 정상 주행 R 홀드 표시·숨김, AC-6 잠금 중·하루 종료 뒤 숨김, AC-8 회귀)
**Deviations**: None. 참고(OUT OF SCOPE): `GameManager.cs`, `GameTimeSystem.cs`, `MalgunGothicTMP.asset`, `HudPanelSettings.asset`, `PlayModeFocus.cs`, `Assets/UI Toolkit/`가 작업 트리에서 함께 바뀌어 있음. 관찰: 물류센터 창이 열리면 행 홀드 상차 중 플레이어 패널 채움이 창에 가림, 하루 종료 뒤 정산 화면에서도 대상 위면 Space 패널이 보임(기존 Canvas와 같은 조건)
**Test Evidence**: `production/qa/evidence/story-022-player-interaction-ui-toolkit/` 01~05 (테스트는 waived at qa.level minimal)
**Reviewed by**: self, no independent review

## Calls beyond the design
- 패널 크기 상호작용 72x88·복구 56x88, 아이콘 32px (`PlayerInteractionHud.uss` 상단 변수) · kept 2026-11-17
- 플레이어 기준 좌우 오프셋 70px (`PlayerInteractionHud.cs` `_rightOffset`, `_leftOffset`) · kept 2026-11-17
- 채움색 상호작용 파랑 `rgba(78,120,180,.85)`, 복구 주황 `rgba(200,150,60,.85)` (`PlayerInteractionHud.uss`) · kept 2026-11-17
- 글자 크기 `Space` 18px, `R` 30px (`PlayerInteractionHud.uss`) · kept 2026-11-17
- 끼임 판정은 후진 입력도 입력으로 봄 (`PlayerMovementController.HasThrottleInput`) · kept 2026-11-17
- 갇힘 타이머는 운영 중이고 잠금이 아닐 때만 누적, 그 밖에는 0 (`VehicleRecoveryController.UpdateStuck`) · kept 2026-11-17
