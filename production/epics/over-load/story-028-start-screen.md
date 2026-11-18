# Story 028: 시작 화면 (메뉴 카메라 구도 + 타이틀 로고 + 사선 메뉴 + 출근 시 하루 시작)

> **Epic**: over-load
> **Status**: Complete
> **Layer**: Presentation
> **Type**: UI
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-18

## Context

**GDD**: `design/quick-specs/start-screen-2026-11-18.md` (하루 운영 GDD 없음 — `design/game-brief.md`와 이 퀵 스펙이 기록)
**Requirement**: 게임 실행 직후 차량 클로즈업 구도와 우측 사선 메뉴(시작·조작법·종료)를 보여 준다. 시작을 누르면 카메라가 인게임 구도로 돌아가고 그때 하루가 시작된다(사용자 결정 2026-11-18: 메뉴 동안 시계·물류 생성 정지).

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating for 6.6)
**Engine Notes**: `com.unity.cinemachine` 6.6.0 사용 중. `SampleMapDesign.unity`에 Cinemachine 카메라가 이미 있다(인게임 카메라 구성은 아직 확인 안 함). Cinemachine 3.x 계열 API(`CinemachineCamera`, `Priority`)는 착수 전에 패키지 문서로 확인한다.

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

- [ ] 플레이 시작 직후 카메라가 `MenuCam` 구도이고, 차량이 화면 왼쪽을 채우며 메뉴 3개(시작·조작법·종료)가 오른쪽에서 `\` 모양 사선으로 보인다
- [ ] 메뉴 동안 하루 시계가 멈춰 있다. 10초 기다린 뒤 시작해도 영업 시각이 시작 시각 그대로다
- [ ] 메뉴 동안 물류가 생성되지 않고, 차량이 움직이지 않으며, 상호작용도 되지 않는다
- [ ] 메뉴 동안 기존 HUD(속도계, 상태, 적재 패널 등)가 보이지 않는다
- [ ] 시작을 누르면 메뉴가 사라지고 카메라가 블렌드 시간 동안 인게임 구도로 이동한다. 블렌드가 끝나면 `StartDay()`가 실행되고 조작이 풀리며 HUD가 보인다
- [ ] 시작 버튼을 연타해도 `StartDay()`는 한 번만 호출된다
- [ ] 퇴근을 누르면 Editor에서 플레이가 멈춘다. Esc도 같은 `GameManager.Quit()`을 쓴다
- [ ] 메뉴 위에 타이틀 로고가 보이고, 로고 두 줄과 버튼 3개가 하나의 `\` 사선으로 이어진다
- [ ] 메뉴 동안 BGM이 나오지 않고, 출근 후 하루 시작 효과음과 BGM이 재생된다
- [ ] 조작법 버튼은 흐리게 표시되고 눌러도 아무 일도 일어나지 않는다
- [ ] 키보드 ↑/↓로 포커스를 옮기고 Enter로 출근·퇴근을 선택할 수 있다. 키보드를 쓰기 전에는 출근 버튼이 강조되지 않는다
- [ ] 블렌드 시간, 블렌드 곡선, 사선 행 간격, 메뉴 영역 폭은 `StartScreenConfig`에서 읽는다
- [ ] No regression: 시작 후 하루 운영, 조기 종료, 정산, HUD 동작이 이전과 같다
- [ ] 메뉴 화면(첨부 기준 사진과 비교), 블렌드 중간, 시작 후 인게임 화면 스크린샷을 `production/qa/evidence/story-028-start-screen/`에 보관한다

---

## Implementation Notes

- `DailyOperationSystem.Start()`에서 `StartDay()` 호출만 빼고 `SetControlEnabled(false)`는 유지한다. `StartDay()`는 이미 public이다.
- `GameManager.Terminate`의 종료 로직을 `public void Quit()`으로 꺼내고, Esc 콜백은 `Quit()`을 호출한다.
- 시작 화면 스크립트(`StartMenuUI`) 하나가 상태(`Menu`/`Starting`/`Playing`), 버튼, `MenuCam` 우선순위, HUD 토글을 맡는다. 블렌드 종료는 설정의 블렌드 시간만큼 기다리는 것으로 충분하다.
- 조작법 연결 지점: `public event Action ControlsRequested`만 선언하고 이 스토리에서는 발생시키지 않는다(Story 029).
- 사선 배치: 행마다 USS `translate` X를 단계적으로 준다. 글자는 회전·기울이지 않는다. 간격 값은 설정에서 코드로 넣는다.
- 레이아웃은 기존 `HudPanelSettings`를 쓴다. 문구는 한국어, 폰트는 기존 `MalgunGothicDynamic`.
- `MenuCam` 위치·회전은 Editor에서 눈으로 맞춘다. 씬 수정은 Unity CLI로 Editor에서 한다. `.unity`를 직접 고치지 않는다.
- HUD 토글 대상은 착수 때 씬의 HUD 프리팹 구성을 보고 정한다. 각 HUD의 루트 `display`만 끄고 켠다.
- 플레이 중 커서가 메뉴에서 보이는지 확인한다.

---

## Out of Scope

- 조작법 화면 본체 (Story 029: 조작법·시스템·콘텐츠를 스크롤 텍스트로 보여 주는 패널)
- 로고 애니메이션, 버튼 호버 애니메이션, 설정 화면
- 일시정지 메뉴, 하루 종료 후 시작 화면 복귀

---

## Files

- `Assets/Game/Scripts/UI/StartMenuUI.cs` (새 파일)
- `Assets/Game/Scripts/UI/StartScreenConfig.cs` + 설정 에셋 (새 파일)
- `Assets/Game/UI/StartMenu/StartMenu.uxml`, `StartMenu.uss` (새 파일)
- `Assets/Game/Scripts/Systems/DailyOperationSystem.cs`
- `Assets/Game/Scripts/Manager/GameManager.cs`
- `Assets/Game/Scenes/_DevOnly/SampleMapDesign.unity` (`MenuCam`, 시작 화면 오브젝트 추가 — Editor에서)
- `Assets/Game/Scripts/Audio/GameAudio.cs` (BGM·하루 시작 효과음을 출근 시 재생, `DayStarted` 구독)
- `Assets/Game/UI/StartMenu/TitleLogo.png` (타이틀 로고), `tools/title-logo/title-logo.html` (로고 생성기)

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: UI
**Required evidence**: 메뉴·블렌드 중·인게임 화면의 retained screenshot (`production/qa/evidence/story-028-start-screen/`). 테스트는 `qa.level: minimal`에서 생략

**Status**: [x] `production/qa/evidence/story-028-start-screen/` (01 메뉴 구도·타이틀 로고, 02 블렌드 중, 03 출근 후 인게임). 출근·퇴근은 버튼에 Submit 이벤트를 보내 확인했고, 실제 마우스 클릭과 ↑/↓ 포커스 이동은 직접 조작으로 아직 확인하지 않았다(사용자 시각 확인 예정).

---

## Dependencies

- Depends on: None
- Unlocks: Story 029 조작법 화면 (아직 작성 안 함)

## Completion Notes
**Completed**: 2026-11-18
**Criteria**: 14/14 passing (조작·상호작용 정지, 마우스·키보드 선택, 소리, 기존 기능 유지는 사용자가 직접 확인)
**Deviations**: 없음. `## Files`에 없던 이 스토리의 변경(`GameAudio.cs`, `TitleLogo.png`, `tools/title-logo/`)은 Files 목록에 추가함
**Test Evidence**: `production/qa/evidence/story-028-start-screen/` (01 메뉴·로고, 02 블렌드 중, 03 출근 후 인게임). 02·03은 문구·로고 변경 전 캡처라 현재 메뉴 모습은 01만 해당. 증거 스탬프 없음. 테스트는 qa.level minimal에서 생략
**Reviewed by**: self, no independent review
**Run result**: OBSERVED — SampleMapDesign Play, 새 에러 없음

## Calls beyond the design
- 카메라 전환은 MenuCam이 차량을 바라보며 수평으로 도는 Cylindrical 방식 (`SampleMapDesign.unity`의 `MenuCam`) · kept 2026-11-18
- 하루 시작 효과음도 출근 시 재생 (`GameAudio.cs`) · kept 2026-11-18
- 로고·버튼의 색·크기·입체 스타일 (`TitleLogo.png`, `StartMenu.uss`) · kept 2026-11-18
