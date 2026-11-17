# Story 024: 동적으로 만드는 UI 요소를 UXML 템플릿으로 분리

> **Epic**: over-load
> **Status**: Complete
> **Layer**: Presentation
> **Type**: UI
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-18

## Context

**GDD**: `design/gdd/game-concept.md` 10.6장
**Requirement**: UI 백로그 UI-02 (`production/ui-backlog.md`). 코드에서 `new Label` 등으로 만드는 요소는 UI Builder에서 보거나 고칠 수 없다.

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating for 6.6)
**Engine Notes**: UI Toolkit에서 프리팹 역할은 UXML 템플릿(`VisualTreeAsset`)이다. 코드는 `[SerializeField] VisualTreeAsset`을 `Instantiate()`해서 값만 채운다.

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

- [ ] 다음 요소가 각각 별도 UXML 템플릿 파일로 있고, UI Builder에서 열어 볼 수 있다
  - 적재 패널(`DurabilityHud`): 그룹 헤더, 물류 행(제한 시간 바 포함), −N 팝업
  - 물류센터 창(`LogisticsCenterUI`): 물류 행
  - 토스트(`ToastHud`): 토스트 한 개
- [ ] 세 스크립트는 템플릿을 `Instantiate()`하고 값만 채운다. 요소 구조를 코드에서 `new`로 조립하지 않는다
- [ ] 부모 UXML에서 `<ui:Instance>`로 예시 요소를 넣어 UI Builder에서 전체 배치가 보이고, 예시는 런타임에 지워진다
- [ ] 화면에 보이는 모양·동작이 분리 전과 같다
- [ ] No regression: 적재 패널 접기·스크롤·제한 시간 바·−N 팝업, 물류센터 창 홀드 상차·스크롤, 토스트 표시·사라짐
- [ ] 적재 패널·물류센터 창·토스트의 분리 후 스크린샷과 UI Builder에서 연 템플릿 스크린샷을 `production/qa/evidence/story-024-uxml-element-templates/`에 보관한다

---

## Implementation Notes

- 템플릿은 각 UI 폴더에 둔다(예: `Assets/Game/UI/Hud/ParcelRow.uxml`, `ToastItem.uxml`, `Assets/Game/UI/LogisticsCenter/CenterRow.uxml`). 스타일은 기존 USS 클래스를 그대로 쓴다.
- 요소 조회는 `Q<>("이름")`. 이름은 템플릿에 고정한다.
- 매 프레임 갱신하는 값(바 폭 등)은 지금 코드 경로를 유지한다. 구조만 템플릿으로 옮긴다.
- 반복 요소를 `[UxmlElement]` 커스텀 컨트롤로 만들지는 않는다. 템플릿으로 충분하면 그것만 쓴다.
- 템플릿 참조 연결은 Unity CLI로 Editor에서 한다. `.prefab`·`.unity`를 직접 고치지 않는다.

---

## Out of Scope

- Canvas UI의 UI Toolkit 전환 (Story 025·026·027)
- 모양 변경 (최종 GUI 디자인은 첫 릴리즈 전 임시 단계)

---

## Files

- `Assets/Game/Scripts/UI/DurabilityHud.cs`
- `Assets/Game/Scripts/UI/LogisticsCenterUI.cs`
- `Assets/Game/UI/Hud/ToastHud.cs`
- `Assets/Game/UI/Hud/DurabilityHud.uxml`
- `Assets/Game/UI/Hud/ToastHud.uxml`
- `Assets/Game/UI/LogisticsCenter/LogisticsCenterWindow.uxml`
- `Assets/Game/UI/Hud/CargoGroupHeader.uxml`
- `Assets/Game/UI/Hud/CenterNavRow.uxml`
- `Assets/Game/UI/Hud/ParcelRow.uxml`
- `Assets/Game/UI/Hud/DamagePopup.uxml`
- `Assets/Game/UI/Hud/ToastItem.uxml`
- `Assets/Game/UI/LogisticsCenter/CenterCard.uxml`
- `Assets/Game/UI/LogisticsCenter/CenterRow.uxml`
- `Assets/Game/Prefabs/UI/Hud.prefab`
- `Assets/Game/Scenes/FailedScene.unity`
- `Assets/Game/Scenes/_DevOnly/DevScene.unity`
- `Assets/Game/Scenes/_DevOnly/SampleMapDesign.unity`

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: UI
**Required evidence**: 분리 후 적재 패널·물류센터 창·토스트, UI Builder 템플릿의 retained screenshot (`production/qa/evidence/story-024-uxml-element-templates/`). 테스트는 `qa.level: minimal`에서 생략

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/over-load/story-023-logistics-refresh-icon-svg.md` (같은 `LogisticsCenterUI.cs`)
- Unlocks: `production/epics/over-load/story-025-settlement-ui-toolkit.md`, `production/epics/over-load/story-026-world-parcel-icons-ui-toolkit.md` (새로 옮기는 UI도 템플릿 방식으로 만든다)

---

## Completion Notes
**Completed**: 2026-11-18
**Criteria**: 5/6 passing (1 deferred: No regression의 키 입력 동작 — 사용자가 플레이할 때 확인)
**Deviations**: None (참고: 이 스토리 범위 밖 파일 6개가 작업 트리에 이미 수정된 상태였음)
**Test Evidence**: `production/qa/evidence/story-024-uxml-element-templates/` (waived tests at qa.level minimal)
**Reviewed by**: self, no independent review

## Calls beyond the design
- 물류센터 HUD 행은 그룹 헤더와 별도 `Hud/CenterNavRow.uxml` 템플릿 · kept 2026-11-18
- 배송지 카드를 `LogisticsCenter/CenterCard.uxml` 템플릿으로 추가(신규 템플릿 7개) · kept 2026-11-18
- 태그·바·번호 라벨은 조건이 거짓이면 트리에서 제거(`RemoveFromHierarchy`) · kept 2026-11-18
- `LogisticsCenterWindow.uxml` windowRoot의 기본 `lc-root--hidden` 클래스 제거(런타임 `OnUIReload`가 정함) · kept 2026-11-18
