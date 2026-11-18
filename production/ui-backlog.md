# UI 메모 (backlog)

아직 개발하지 않는 UI 관련 메모. `/quick-design` 등으로 UI 작업을 정할 때 사용자가 이 목록 전체를 확인하고, 고른 항목만 스토리로 만든다. 스토리가 된 항목은 줄을 긋고 스토리 경로를 적는다.

## 열린 항목

### UI-07. 정산 별점을 SVG 별 아이콘으로, 맨 위에 강조 (2026-11-19)
- 문제: 정산 화면에서 별점이 `별점 0 성공 3 / 전체 9`처럼 텍스트이고, 다른 정보와 같은 줄에 있어 눈에 띄지 않는다.
- 요청: 별을 SVG 아이콘(Bootstrap Icons)으로 그리고, 다른 정보보다 가장 위에 따로 두어 강조한다.
- 출처: `/team-qa` Story 002 수동 QA(사용자 피드백). 관련 스토리 002, 025.

### UI-08. 밀집한 배송지 중 하차 대상을 알아보기 어려움 (2026-11-19)
- 문제: 릴리즈 씬은 배송지가 밀집해 있어, 배송할 때 어느 건물에 하차해야 하는지 나침반이나 거리 표시만으로 알기 어렵다.
- 방향 후보: 현재 적재 물류의 목적지 건물을 월드에서 강조(마커·아이콘·윤곽), 가까이 갔을 때 하차 대상 표시.
- 출처: `/team-qa` 스모크 세션(사용자 피드백). 밸런싱 실측(story-003)은 이 항목과 물류 수 조정 이후로 보류했다.

### UI-09. 물류센터 창에서 물류가 없는 배송지는 숨김 (2026-11-19)
- 문제: 배송지가 많아지면 물류센터 창에 물류가 0개인 배송지까지 모두 나와서 스크롤 피로도가 높다.
- 요청: 그 배송지로 갈 물류가 없으면 창에 표시하지 않는다.
- 관련 코드: `Assets/Game/Scripts/UI/LogisticsCenterUI.cs` (물류센터 창 행).
- 출처: 사용자 피드백.

## 닫힌 항목 상세 (2026-11-18 스토리화)

### UI-02. 동적으로 만드는 UI 요소를 템플릿으로 분리 (2026-11-17)
- 문제: 여러 UI 요소를 C#에서 `new Label` 등으로 만들고 있어서, UI Builder에서는 빈 틀만 보이고 요소 하나하나를 보거나 고칠 수 없다.
- 지금 코드에서 만드는 곳:
  - `Assets/Game/Scripts/UI/DurabilityHud.cs`: 적재 패널 그룹 헤더, 물류 행, 제한 시간 바, −N 팝업
  - `Assets/Game/Scripts/UI/LogisticsCenterUI.cs`: 물류센터 창 행
  - `Assets/Game/UI/Hud/ToastHud.cs`: 토스트 한 개
- 방향: UI Toolkit에서 프리팹 역할은 UXML 템플릿(`VisualTreeAsset`)이다. 요소 하나를 `ToastItem.uxml`처럼 따로 저장하고, 코드는 템플릿을 `Instantiate()`해서 값만 채운다.
  - 템플릿 파일은 UI Builder에서 따로 열어 보고 고칠 수 있다.
  - 부모 UXML에 `<ui:Template>`/`<ui:Instance>`로 예시를 넣어 전체 배치도 볼 수 있다. 예시는 런타임에 지운다.
  - 반복해서 쓰는 요소는 대안으로 `[UxmlElement]` 커스텀 컨트롤도 있다.
- 기대 효과: 요소별 시각 확인·수정, 다른 UI 스크립트도 같은 기준으로 리팩토링할 수 있다.
- 정할 것: 적용 범위(UI Toolkit HUD만, 또는 Canvas UI 전환과 함께), 순서.
- 참고: GameObject 프리팹(`Assets/Game/Prefabs/UI/Hud.prefab`)은 HUD 묶음을 씬에 놓는 용도이고, 요소 단위 분리는 UXML 템플릿으로 한다.

### UI-03. 물류센터 행 ↻ 기호를 SVG 아이콘으로 교체
- 출처: Story 008 마무리 때 남은 질문. 아이콘은 Bootstrap Icons SVG를 쓴다.

### UI-04. 배송지 아이콘(Canvas)의 Fresh 색
- 출처: Story 008 마무리 때 남은 질문. Canvas → UI Toolkit 전환 때 처리한다.

### UI-05. 물류센터 창이 플레이어 상호작용 패널 채움을 가림 (2026-11-18)
- 문제: 물류센터 창이 열린 채 행을 홀드해 상차하는 동안 플레이어 오른쪽 `Space` 패널이 창 뒤에 가려서 채움 진행이 보이지 않는다. 창의 행 테두리 진행은 보인다.
- 출처: Story 022 실행 확인. `PlayerInteractionHud`와 물류센터 창은 서로 다른 `PanelRenderer`라 창이 위에 그려진다.
- 정할 것: 창이 열려 있을 때 플레이어 패널을 숨길지, 창 위로 올릴지, 행 테두리 진행만으로 충분한지.

### UI-06. 정산 화면에서 Space 패널이 보임 (2026-11-18)
- 문제: 하루가 끝나 정산 화면이 떠도 상호작용 대상 위에 있으면 플레이어 오른쪽 `Space` 패널이 계속 보인다.
- 출처: Story 022 실행 확인. 표시 조건은 예전 Canvas 아이콘과 같다(`PlayerInteractionController.InteractionContextChanged`). 운영이 끝났는지는 보지 않는다.
- 정할 것: 하루 종료 뒤 상호작용 패널을 숨길지, 아니면 상호작용 가능 판정 자체에 운영 중 조건을 넣을지(게임 규칙 쪽 변경).

## 닫힌 항목

- ~~UI-01. 상호작용 진행 바가 속도계와 겹침 (2026-11-17)~~ → `production/epics/over-load/story-022-player-interaction-ui-toolkit.md` (명세 `design/quick-specs/player-interaction-ui-toolkit-2026-11-17.md`)
- ~~UI-02. 동적 UI 요소 템플릿 분리~~ → `production/epics/over-load/story-024-uxml-element-templates.md`
- ~~UI-03. 물류센터 행 ↻ SVG~~ → `production/epics/over-load/story-023-logistics-refresh-icon-svg.md`
- ~~UI-04. 배송지 아이콘 Fresh 색~~ → `production/epics/over-load/story-026-world-parcel-icons-ui-toolkit.md` (Canvas 전면 전환 025~027과 함께)
- ~~UI-05. 물류센터 창이 플레이어 패널 채움을 가림~~ → 의도된 동작으로 닫음 (2026-11-18 사용자 결정: 창이 위에 있으니 가려지는 게 맞고, 진행은 행 테두리로 보인다)
- ~~UI-06. 정산 화면에서 Space 패널이 보임~~ → `production/epics/over-load/story-025-settlement-ui-toolkit.md` (결정: 정산 화면을 가장 앞에 그린다)
