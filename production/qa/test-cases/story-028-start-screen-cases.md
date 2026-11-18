# Test Cases: Story 028 — 시작 화면

- **Story Type**: UI
- **Output Location**: `production/qa/test-cases/`, 스크린샷 `production/qa/evidence/story-028-start-screen/`
- **Gate Level**: BLOCKING (스크린샷)
- **씬**: `Release/MainGameScene.unity`
- **중점(배치 4 항목 13)**: 실제 마우스 클릭, ↑/↓ 포커스, Enter로 출근·퇴근. TC-028-01~03이 핵심
- **명칭 주의**: AC 문구의 "시작·조작법·종료"는 현재 UI의 `출근`·`게임 설명`·`퇴근` 이다(Story 029에서 조작법이 게임 설명으로 바뀜)

| AC | 케이스 |
|---|---|
| 1 MenuCam 구도·사선 메뉴 | TC-028-04 |
| 2 메뉴 중 시계 정지 | TC-028-05 |
| 3 메뉴 중 생성·이동·상호작용 없음 | TC-028-05 |
| 4 메뉴 중 HUD 숨김 | TC-028-04 |
| 5 출근 → 블렌드 → StartDay·조작·HUD | TC-028-01 |
| 6 연타해도 StartDay 1회 | TC-028-06 |
| 7 퇴근 → 플레이 중지, Esc 같은 Quit | TC-028-03 |
| 8 로고 + 버튼 하나의 사선 | TC-028-04 |
| 9 메뉴 BGM 없음, 출근 후 효과음·BGM | TC-028-01 |
| 10 조작법 버튼 흐림·무반응 | TC-028-07 |
| 11 ↑/↓·Enter, 키보드 전 강조 없음 | TC-028-02 |
| 12 StartScreenConfig | TC-028-08 |
| 13 회귀 없음 | TC-028-09 |
| 14 스크린샷 3종 | TC-028-01, 04 |

---

## Test Case: TC-028-01 — 실제 마우스 클릭으로 출근 (중점)

**Precondition**: Play 직후 시작 메뉴. Game 뷰 사운드 켬. Console Clear.
**Steps**:
1. 메뉴 동안 BGM이 나오지 않는지 5초 듣는다.
2. 마우스로 `출근` 을 한 번 클릭한다.
3. 블렌드 중간 장면을 스크린샷한다.
4. 블렌드가 끝난 뒤: 하루 시작 효과음·BGM, HUD 표시, W로 차량 이동을 확인하고 인게임 스크린샷.

**Expected Result**: 클릭 즉시 메뉴 사라짐, 블렌드 후 `StartDay()` 실행(시계 진행·물류 생성 로그), 조작 가능, HUD 표시, 효과음·BGM 재생.
**Pass Criteria**: 6개 항목(메뉴 사라짐·시계·로그·조작·HUD·사운드) 모두 예, 스크린샷 2장.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-028-02 — ↑/↓ 포커스와 Enter (중점)

**Precondition**: 새 Play, 시작 메뉴. 키보드를 아직 누르지 않음.
**Steps**:
1. 출근 버튼에 강조가 없는지 본다.
2. ↓ 를 누른다. 포커스 강조가 어디로 가는지 본다. ↓ 를 더 눌러 끝까지, ↑ 로 되돌아온다(순서 기록).
3. 포커스를 `출근` 에 두고 Enter.

**Expected Result**: 키보드 전 강조 없음. ↑/↓로 출근 ↔ 게임 설명 ↔ 퇴근 이동. Enter로 출근 실행(TC-028-01과 같은 결과).
**Pass Criteria**: 1단계 강조 없음, 2단계 이동 = 예, 3단계 하루 시작 = 예.
**Actual Result**: (포커스 순서 __)
**Pass/Fail**:

## Test Case: TC-028-03 — 퇴근(클릭·Enter)과 Esc (중점)

**Precondition**: 시작 메뉴.
**Steps**:
1. 마우스로 `퇴근` 클릭 → Editor 플레이가 멈추는지 본다. (Story 029 이후 확인창이 뜨면 `예` 선택 후 멈춤인지 기록)
2. 다시 Play → ↓ 로 `퇴근` 포커스 → Enter.
3. 다시 Play → 메뉴에서 Esc.

**Expected Result**: 1·2단계 플레이 중지. Esc는 같은 종료 경로(현재는 퇴근 확인창 → `예` 로 중지).
**Pass Criteria**: 세 경로 모두 최종적으로 플레이 중지, Error 0.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-028-04 — 메뉴 구도·로고·HUD 숨김

**Precondition**: Play 직후 시작 메뉴.
**Steps**:
1. 카메라가 `MenuCam` 구도인지, 차량이 화면 왼쪽을 채우는지 본다.
2. 로고 두 줄과 버튼 3개가 오른쪽에서 하나의 `\` 사선으로 이어지는지 본다.
3. 속도계·상태·적재 패널 등 HUD가 하나도 안 보이는지 본다.
4. 메뉴 화면을 스크린샷하고 첨부 기준 사진과 비교한다.

**Expected Result**: 모두 일치.
**Pass Criteria**: 4개 항목 예, 스크린샷 저장.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-028-05 — 메뉴 중 정지 상태

**Precondition**: Play 직후 시작 메뉴. Console Clear.
**Steps**:
1. 10초 동안 W/방향키/Space/R/Ctrl을 누른다.
2. Console에 `[Parcel #` 로그가 생겼는지 본다.
3. 출근 후 영업 시각이 시작 시각(09:00) 그대로인지 본다.

**Expected Result**: 차량 정지, 상호작용 없음, 물류 생성 0, 시작 시각 그대로.
**Pass Criteria**: 이동 0, 메뉴 중 로그 0, 시작 시각 = 09:00.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-028-06 — 출근 연타

**Precondition**: 시작 메뉴. Console Clear.
**Steps**:
1. `출근` 을 빠르게 5번 클릭한다(또는 Enter 5번).
2. 하루 시작 효과음 횟수와 Console의 하루 시작 로그·`[Parcel #` 첫 묶음 수를 본다.

**Expected Result**: `StartDay()` 1회만. 물류 생성 묶음 1회.
**Pass Criteria**: 하루 시작 1회, 물류 중복 생성 없음.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-028-07 — 조작법(현 게임 설명) 버튼

**Precondition**: 시작 메뉴.
**Steps**:
1. 두 번째 버튼의 표시와 클릭 결과를 본다.

**Expected Result**: AC 원문은 "흐림·무반응"이나 Story 029로 `게임 설명` 패널이 열리도록 바뀜. 현 동작을 기록하고 029 기준으로 판정한다.
**Pass Criteria**: 029 기준으로 패널이 열림(흐림·무반응 AC는 029가 대체).
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-028-08 — StartScreenConfig

**Precondition**: Editor, Play 전.
**Steps**:
1. `StartScreenConfig` 에서 블렌드 시간·곡선·사선 행 간격·메뉴 영역 폭을 기록한다.
2. 블렌드 시간을 2배로 바꿔 출근 블렌드가 길어지는지 본다. 원래대로 복구.

**Expected Result**: 4개 값 존재, 변경 반영.
**Pass Criteria**: 반영 = 예.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-028-09 — 시작 후 기존 기능 유지

**Precondition**: 출근 완료.
**Steps**:
1. 상차 1건, 하차 1건, HUD 갱신을 본다.
2. 조기 종료(있으면) 또는 영업 종료로 정산 화면까지 간다.

**Expected Result**: 하루 운영·조기 종료·정산·HUD가 이전과 같다.
**Pass Criteria**: 이상 0, Error 0.
**Actual Result**:
**Pass/Fail**:
