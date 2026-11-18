# Test Cases: Story 029 — 게임 설명 패널 + Esc 단계 처리와 퇴근 확인창

- **Story Type**: UI
- **Output Location**: `production/qa/test-cases/`, 스크린샷 `production/qa/evidence/story-029-guide-panel-esc/`
- **Gate Level**: BLOCKING (스크린샷)
- **씬**: `Release/MainGameScene.unity`
- **중점(배치 4 항목 14)**: 실제 Esc 키, 마우스, ↑/↓/PageUp/PageDown, 21:9. TC-029-01~04가 핵심
- **21:9 설정**: Game 뷰 해상도 드롭다운에 `2560x1080` (또는 `21:9` Aspect) 추가

| AC | 케이스 |
|---|---|
| 1 열면 메뉴·로고 사라지고 오른쪽 패널 | TC-029-01 |
| 2 16:9·21:9 차량 미가림 | TC-029-03 |
| 3 휠·키 스크롤 끝까지, 잘림 없음 | TC-029-02 |
| 4 X 닫기, 재오픈 시 맨 위 | TC-029-01 |
| 5 열린 동안 하루 미시작·카메라/HUD 불변 | TC-029-01 |
| 6 키보드만으로, 닫으면 포커스 복귀 | TC-029-04 |
| 7 패널 열림 Esc = 패널만 닫기 | TC-029-04 |
| 8 물류센터 창 Esc = 창만 닫기 | TC-029-05 |
| 9 운영 중·메뉴·정산 Esc = 확인창 | TC-029-06 |
| 10 확인창 중 정지·입력 무시 | TC-029-07 |
| 11 확인창 Esc/아니오 재개, 예 종료, 포커스 | TC-029-08 |
| 12 Esc 한 번에 동작 하나 | TC-029-04, 05, 08 |
| 13 회귀 없음 | TC-029-09 |
| 14 스크린샷 3종 | TC-029-01, 06 |

---

## Test Case: TC-029-01 — 마우스로 열기·닫기 (중점, 16:9)

**Precondition**: Game 뷰 16:9(1920x1080), 시작 메뉴.
**Steps**:
1. 시작 메뉴 스크린샷.
2. 마우스로 `게임 설명` 클릭. 로고·버튼 3개가 모두 사라지고 패널이 오른쪽에 뜨는지 본다. 패널 스크린샷.
3. 패널을 연 채 10초 기다린다. 시계·카메라·HUD가 변하지 않는지 본다.
4. 휠로 중간까지 내린 뒤 X 클릭 → 메뉴·로고 복귀 확인.
5. 다시 `게임 설명` 클릭 → 맨 위부터 보이는지 본다.

**Expected Result**: 모두 기대대로, 하루 미시작.
**Pass Criteria**: 5단계 모두 예, 스크린샷 2장.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-029-02 — 휠·↑/↓·PageUp/PageDown 스크롤 (중점)

**Precondition**: 패널 열림, 맨 위.
**Steps**:
1. 마우스 휠로 끝까지 내린다. 마지막 줄이 잘리지 않고 다 보이는지 본다. 휠로 맨 위로 올린다.
2. ↓ 를 반복해 끝까지, ↑ 로 맨 위까지.
3. PageDown 반복으로 끝까지, PageUp 으로 맨 위까지.

**Expected Result**: 세 방법 모두 끝까지 스크롤, 본문 잘림 없음.
**Pass Criteria**: 3가지 입력 모두 양 끝 도달 = 예.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-029-03 — 21:9에서 차량 미가림 (중점)

**Precondition**: Game 뷰 21:9(2560x1080).
**Steps**:
1. Play → 시작 메뉴 → `게임 설명` 열기.
2. 패널이 차량을 가리는지, 패널이 화면 밖으로 잘리는지 보고 스크린샷.
3. 16:9로 바꿔 같은 확인을 한다.

**Expected Result**: 두 비율 모두 패널이 차량을 가리지 않고 잘리지 않음.
**Pass Criteria**: 2개 비율 모두 예, 스크린샷 저장.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-029-04 — 키보드만으로 열기→스크롤→닫기, Esc는 패널만 (중점)

**Precondition**: 새 Play, 시작 메뉴. 마우스 사용 안 함.
**Steps**:
1. ↓ 로 `게임 설명` 포커스 → Enter로 연다.
2. PageDown 으로 스크롤한다.
3. Esc 를 한 번 누른다.
4. 포커스가 `게임 설명` 버튼에 있는지, 퇴근 확인창이 뜨지 않았는지 본다.

**Expected Result**: Esc 한 번에 패널만 닫힘, 확인창 없음, 포커스 = 게임 설명.
**Pass Criteria**: 4단계 기대와 일치.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-029-05 — 물류센터 창 열림 Esc

**Precondition**: 출근 후 물류센터 상차 창 열림.
**Steps**:
1. Esc 한 번.
2. 창 닫힘과 확인창 없음을 본다.
3. 다시 Esc 한 번 → 이번에는 확인창이 뜨는지 본다(운영 중이므로).

**Expected Result**: 1번째 Esc = 창만 닫기, 2번째 = 확인창.
**Pass Criteria**: 기대와 일치.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-029-06 — 운영 중·메뉴·정산 Esc 확인창

**Precondition**: 각 상황을 차례로 만든다.
**Steps**:
1. 시작 메뉴에서 Esc → 확인창 문구 확인 → `아니오`. 확인창 스크린샷.
2. 운영 중 주행하며 Esc → 확인 → `아니오`.
3. 정산 화면에서 Esc → 확인 → `아니오`.

**Expected Result**: 세 상황 모두 `게임을 정지하고 퇴근하시겠습니까?` 확인창.
**Pass Criteria**: 3/3 표시.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-029-07 — 확인창 중 시간·차량 정지, 입력 무시

**Precondition**: 운영 중, Express 1개와 Fresh 1개 상차, 주행 중.
**Steps**:
1. 영업 시각과 Express·Fresh 남은 바를 기록하고 Esc.
2. 확인창이 열린 채 10초 동안 W·Space·R·Ctrl을 누른다.
3. `아니오` 를 눌러 재개한 직후 시각과 바를 다시 기록한다.

**Expected Result**: 시각·제한 시간·신선도 그대로, 차량 정지, 키 무시.
**Pass Criteria**: 시각 차 0분, 바 변화 없음, 차량 이동 0.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-029-08 — 확인창 키 조작

**Precondition**: 운영 중.
**Steps**:
1. Esc로 확인창 → 처음 포커스가 `아니오` 인지 본다.
2. Esc 한 번 → 재개되고 확인창이 다시 뜨지 않는지 본다.
3. Esc로 확인창 → Enter → 재개(아니오).
4. Esc로 확인창 → → 로 `예` → ← 로 `아니오` → → 로 `예` → Enter → 플레이 중지.

**Expected Result**: 처음 포커스 아니오, Esc/아니오 재개, 예 종료, Esc 한 번에 동작 하나.
**Pass Criteria**: 4단계 모두 기대와 일치.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-029-09 — 기존 기능 유지

**Precondition**: 새 Play.
**Steps**:
1. 출근 클릭 → 카메라 블렌드 정상.
2. 키보드 ↑/↓ 포커스 강조 정상.
3. 물류센터 창 X 닫기, 창 열린 채 Space(상호작용)로 닫기.
4. 메뉴에서 `퇴근` → 확인창 → `예` 로 종료.

**Expected Result**: 이전과 같다.
**Pass Criteria**: 이상 0.
**Actual Result**:
**Pass/Fail**:
