# Test Cases: Session 0 — 스모크 (시작 메뉴 → 하루 → 정산 → 메뉴 복귀)

- **Story Type**: Integration (빌드 건강 점검)
- **Output Location**: `production/qa/test-cases/`
- **Gate Level**: BLOCKING (이 세션이 실패하면 배치 1~5를 진행하지 않고 그 자리에서 보고)
- **씬**: `Assets/Game/Scenes/Release/MainGameScene.unity` 을 열고 Play(자동으로 `Boot.unity`부터 시작)
- **공통 준비**: Console 창을 열고 `Clear on Play` 켜기, `Error Pause` 끄기. 하루 = 현실 약 72초.

---

## Test Case: SMK-01 — Play 시 시작 메뉴 표시

**Precondition**: Editor에서 `Release/MainGameScene.unity` 를 연 상태, Play 전.
**Steps**:
1. Play 버튼을 누른다.
2. 3초 기다린다.
3. Console의 Error/Exception 개수를 본다.

**Expected Result**: 시작 메뉴(타이틀 로고 + 출근·게임 설명·퇴근 버튼)가 보이고 HUD는 안 보인다. 하루 시계가 시작 시각에서 멈춰 있다.
**Pass Criteria**: 메뉴 표시 = 예, Console Error/Exception = 0건.
**Actual Result**:
**Pass/Fail**:

## Test Case: SMK-02 — 출근으로 하루 시작

**Precondition**: SMK-01 직후, 시작 메뉴 표시 중.
**Steps**:
1. 마우스로 `출근` 을 클릭한다.
2. 카메라 블렌드가 끝날 때까지 기다린다.
3. W 키로 차량을 1초간 움직인다.
4. Console에서 `[Parcel #` 로그를 찾는다.

**Expected Result**: 메뉴가 사라지고 카메라가 인게임 구도로 이동한 뒤 HUD가 보이고 시계가 흐른다. 차량이 움직인다. 물류 생성 로그가 1개 이상 있다.
**Pass Criteria**: HUD 표시 = 예, 차량 이동 = 예, `[Parcel #n]` 로그 ≥ 1건, Error/Exception = 0건.
**Actual Result**:
**Pass/Fail**:

## Test Case: SMK-03 — 하루 종료와 정산 화면

**Precondition**: SMK-02 직후, 하루 운영 중.
**Steps**:
1. 물류센터에서 물류 1개 이상을 상차하고 아무 배송지에 하차한다(가능한 만큼).
2. 영업 종료까지 기다린다(시작 후 현실 약 72초).
3. 정산 화면을 확인한다.

**Expected Result**: 영업 종료 시 조작이 잠기고 정산 화면이 뜬다. 상단에 별점(또는 "판정할 물류 없음")과 `성공 S / 발생 N`, 최종 정산 금액이 보인다.
**Pass Criteria**: 정산 화면 표시 = 예, S·N·금액이 숫자로 표시됨, Error/Exception = 0건.
**Actual Result**:
**Pass/Fail**:

## Test Case: SMK-04 — 정산 후 메뉴 복귀(재시작)

**Precondition**: SMK-03 직후, 정산 화면 표시 중.
**Steps**:
1. 정산 화면의 재시작(메뉴 복귀) 버튼을 누른다.
2. 시작 메뉴가 나오는지 본다.
3. 다시 `출근` 을 눌러 하루가 시작되는지 확인한다(10초 진행 후 Play 종료).

**Expected Result**: 시작 메뉴로 돌아오고 시계가 시작 시각으로 초기화되며, 두 번째 출근에서도 하루가 정상 시작된다. 이전 하루의 물류·적재가 남아 있지 않다.
**Pass Criteria**: 메뉴 복귀 = 예, 2회차 하루 시작 = 예, 시작 시각 초기화 = 예, Error/Exception = 0건.
**Actual Result**:
**Pass/Fail**:

## Test Case: SMK-05 — 세션 전체 콘솔 점검

**Precondition**: SMK-01~04를 한 번의 Play에서 진행함.
**Steps**:
1. Play를 멈추기 전에 Console의 Error·Exception·Warning 개수를 기록한다.
2. Error/Exception이 있으면 첫 줄과 스택 상단을 복사한다.

**Expected Result**: Error/Exception이 없다. Warning은 기록만 한다.
**Pass Criteria**: Error = 0, Exception = 0.
**Actual Result**: (Error __ / Exception __ / Warning __)
**Pass/Fail**:
