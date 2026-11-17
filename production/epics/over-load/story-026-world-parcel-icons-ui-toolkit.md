# Story 026: 배송지·처분장 월드 아이콘 UI Toolkit 전환 + Fresh 색

> **Epic**: over-load
> **Status**: Complete
> **Layer**: Presentation
> **Type**: UI
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-18

## Context

**GDD**: `design/gdd/game-concept.md` 10.6장, `design/quick-specs/fresh-delivery-type-addition-2026-11-16.md`
**Requirement**: UI 백로그 UI-04 (`production/ui-backlog.md`). Canvas → UI Toolkit 전면 전환의 일부. 배송 유형 색은 적재 패널(`DurabilityHud.uss`)의 임시 규칙을 따른다: Normal 노랑, Express 빨강, Fresh 초록, 폐기 어두운 회색.

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating for 6.6)
**Engine Notes**: 화면 공간 UI Toolkit 패널에 두고 월드 → 패널 좌표 변환으로 배송지·처분장 위치를 따라간다(Story 022 `PlayerInteractionHud`와 같은 방식). 아이콘은 `box-seam-fill.svg`를 USS tint로 칠한다.

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

- [ ] 배송지 위에 그 배송지로 가는 물류 아이콘이 이전과 같은 조건·개수로 보이고, 화면 밖이거나 카메라 뒤면 숨는다
- [ ] 아이콘 색이 배송 유형을 따른다(Normal·Express·Fresh, 적재 패널과 같은 색). 적재된 물류는 이전처럼 적재 상태가 구분된다
- [ ] 적재한 Fresh 물류 아이콘 아래 신선도 바가 남은 신선도만큼 보인다
- [ ] 처분장 위에 폐기 대상 물류 아이콘이 이전과 같은 조건으로 보인다
- [ ] `DeliveryTargetParcelUI`·`DisposalSiteParcelUI`와 그 프리팹에 Canvas·UGUI `Image`가 남아 있지 않다
- [ ] No regression: 배송지·처분장 상호작용, 적재 패널 표시
- [ ] Normal·Express·Fresh가 섞인 배송지, 적재한 Fresh의 신선도 바, 처분장 아이콘 스크린샷을 `production/qa/evidence/story-026-world-parcel-icons-ui-toolkit/`에 보관한다

---

## Implementation Notes

- 배송지·처분장마다 Canvas를 두는 대신, HUD 패널 하나에 월드 아이콘 묶음을 모으고 각 대상이 자기 묶음을 등록하는 방식이 가장 작다. 대상별 `UIDocument`가 더 단순하면 그쪽을 쓴다(구현 때 판단, Calls beyond the design에 기록).
- 아이콘 묶음 하나는 Story 024 방식대로 UXML 템플릿으로 만든다.
- 유형 색은 `DurabilityHud.uss`의 `.parcel-icon--fresh` 등 클래스를 공용 USS로 옮겨 함께 쓴다. 색을 두 곳에 복사하지 않는다.
- 아이콘 크기·머리 위 높이는 USS/`[SerializeField]` `TODO(tuning)`.
- 프리팹·씬 수정은 Unity CLI로 Editor에서 한다. `.prefab`·`.unity`를 직접 고치지 않는다.

---

## Out of Scope

- 아이콘 디자인 변경(유형별 다른 모양 등)
- 미니맵·화면 가장자리 방향 표시

---

## Files

- `Assets/Game/UI/Hud/WorldParcelIconsHud.cs` (신규: 월드 아이콘 패널·묶음 등록)
- `Assets/Game/UI/Hud/WorldParcelIcons.uxml`, `WorldParcelIcons.uss`, `WorldIconGroup.uxml`, `WorldParcelIcon.uxml` (신규)
- `Assets/Game/UI/Hud/ParcelIcon.uss` (신규: 유형 색 공용), `DurabilityHud.uss`, `ParcelRow.uxml` (색 이동·공용 USS 참조)
- `Assets/Game/Scripts/UI/DeliveryTargetParcelUI.cs`, `DisposalSiteParcelUI.cs` (UI Toolkit로 재작성)
- `Assets/Game/Prefabs/UI/Hud.prefab` (WorldParcelIconsHud 추가), `Assets/Game/Prefabs/DisposalSite.prefab`, `Assets/Game/Prefabs/DeliveryTargets/_BuildingBase.prefab` (Canvas 제거)

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: UI
**Required evidence**: 유형 혼합 배송지, Fresh 신선도 바, 처분장 아이콘의 retained screenshot (`production/qa/evidence/story-026-world-parcel-icons-ui-toolkit/`). 테스트는 `qa.level: minimal`에서 생략

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/over-load/story-024-uxml-element-templates.md` (템플릿 방식, 같은 `DurabilityHud` 파일)
- Unlocks: `production/epics/over-load/story-027-remaining-canvas-cleanup.md`

## Completion Notes
**Completed**: 2026-11-18
**Criteria**: 7/7 passing (동작 항목 AC-1·AC-6은 사용자가 플레이로 확인)
**Deviations**: None (스코프 밖 변경 파일은 이 스토리 작업이 아님)
**Test Evidence**: waived at qa.level minimal; 스크린샷 `production/qa/evidence/story-026-world-parcel-icons-ui-toolkit/` (스크립트로 만든 장면)
**Reviewed by**: self, no independent review
**Run result**: OBSERVED — 유형별 색 아이콘, 적재 Fresh 테두리+신선도 바, 처분장 회색 아이콘

## Calls beyond the design
- HUD 패널 하나에 묶음 등록 방식(WorldParcelIconsHud.cs) · kept 2026-11-18
- 적재 구분은 파란 테두리(WorldParcelIcons.uss) · kept 2026-11-18
- 신선도 바는 아이콘 아래 8px(WorldParcelIcons.uss) · kept 2026-11-18
- 아이콘 40px, 원점 위 1m(WorldParcelIcons.uss, `_iconOffset`) · kept 2026-11-18
