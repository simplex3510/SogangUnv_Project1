# Test Cases: Story 013 — 고속 모드 게이지와 속도계 HUD

- **Story Type**: Logic + UI
- **Output Location**: `production/qa/test-cases/` (자동 테스트 WAIVED), 스크린샷 `production/qa/evidence/story-013-fast-gauge-speedometer/`
- **Gate Level**: BLOCKING (UI 스크린샷 + 수동 증거)
- **씬**: `Release/MainGameScene.unity`. 스톱워치 사용
- **값**: T_drain 8초, T_delay 1.5초, T_recharge 12초, T_cooldown 1초, g_reenter 0.3, 속도 표시 배율 4, 아크 최대 80 km/h

| AC | 케이스 |
|---|---|
| 1 8초 소진 → 강제 전환 | TC-013-01 |
| 2 1.5초 대기 후 12초 충전 | TC-013-02 |
| 3 1초 쿨다운(강제 포함) | TC-013-03 |
| 4 소진 후 0.3 이상 재진입 | TC-013-04 |
| 5 기존 조건 유지, 하루 종료 시 정지 | TC-013-05 |
| 6 설정 데이터 | TC-013-06 |
| 7 속도계·게이지 UI, 소진 경고색 | TC-013-07 |
| 8 스크린샷 | TC-013-07 |

---

## Test Case: TC-013-01 — 8초 소진 후 일반 모드 강제 전환 (세션 A)

**Precondition**: 운영 중, 게이지 가득, 일반 모드, 직선 도로.
**Steps**:
1. Ctrl로 고속 모드 진입과 동시에 스톱워치 시작, 계속 주행한다.
2. 게이지가 0이 되는 순간 스톱워치를 멈춘다.
3. 모드 표시와 최고 속도가 일반으로 돌아왔는지 본다. Console에 모드 변경 로그가 있으면 복사.

**Expected Result**: 약 8초에 게이지 0, 일반 모드로 강제 전환.
**Pass Criteria**: 소진 시간 8초(±0.5), 강제 전환 = 예.
**Actual Result**: (__초)
**Pass/Fail**:

## Test Case: TC-013-02 — 1.5초 대기 후 12초 충전

**Precondition**: TC-013-01 직후(게이지 0, 일반 모드).
**Steps**:
1. 강제 전환 순간부터 게이지가 차기 시작할 때까지 시간을 잰다.
2. 차기 시작부터 가득 찰 때까지 시간을 잰다.

**Expected Result**: 대기 1.5초, 충전 12초.
**Pass Criteria**: 대기 1.5(±0.3), 충전 12(±0.5).
**Actual Result**: (대기 __ / 충전 __)
**Pass/Fail**:

## Test Case: TC-013-03 — 1초 쿨다운

**Precondition**: 게이지 가득, 일반 모드.
**Steps**:
1. Ctrl로 고속 진입 직후 0.5초 안에 Ctrl을 다시 누른다.
2. 1초 넘게 기다린 뒤 Ctrl을 누른다.
3. TC-013-01처럼 강제 전환이 일어난 직후 0.5초 안에 Ctrl(게이지는 0이므로 진입 불가 확인 겸용)을 누른다.

**Expected Result**: 1단계 무시(고속 유지), 2단계 일반 전환, 3단계 무시.
**Pass Criteria**: 세 단계 모두 기대와 일치.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-013-04 — 소진 후 재진입 조건 0.3

**Precondition**: 강제 전환 직후.
**Steps**:
1. 게이지 0에서 Ctrl → 진입 불가 확인.
2. 게이지가 약 0.2일 때(대기 1.5초 + 충전 약 2.4초 후) Ctrl.
3. 게이지가 0.3을 넘은 뒤(충전 약 3.6초 이후) Ctrl.
4. 비교: 소진이 아닌 수동 전환 직후에는 게이지 0.3 미만이어도 진입되는지 기록(AC는 강제 전환 뒤만 규정).

**Expected Result**: 0, 0.2에서는 불가, 0.3 이상에서 진입.
**Pass Criteria**: 1·2단계 불가, 3단계 진입.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-013-05 — 기존 조건과 하루 종료 정지

**Precondition**: 운영 중.
**Steps**:
1. 공중(턱 넘는 순간)에서 Ctrl → 전환되지 않음.
2. 상차/하차 홀드 중 Ctrl → 전환되지 않음.
3. 고속 모드 중 영업 종료를 맞는다. 정산 화면 동안 게이지 값이 그대로인지 5초 관찰.

**Expected Result**: 1·2 전환 불가. 종료 후 게이지·쿨다운 정지.
**Pass Criteria**: 기대와 일치.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-013-06 — 설정 데이터

**Precondition**: Editor, Play 전.
**Steps**:
1. `VehicleConfig` 에서 T_drain, T_delay, T_recharge, T_cooldown, g_reenter, 속도 표시 배율, 아크 최대값을 기록한다.

**Expected Result**: 8 / 1.5 / 12 / 1 / 0.3 / 4 / 80.
**Pass Criteria**: 7개 값 존재·일치.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-013-07 — 속도계·게이지 바 UI와 스크린샷

**Precondition**: 운영 중.
**Steps**:
1. 정지 상태: 속도계가 화면 중앙 하단에 반원 아크 + 정수 속도 + `km/h`, 그 아래 게이지 바인지 본다.
2. 일반 최고속에서 표시 값 기록(3 m/s × 4 = 12 기대), 고속 최고속에서 기록(20 기대). 스크린샷.
3. 소진 직후 바 색이 경고색인지 보고 스크린샷.

**Expected Result**: 배치·구성 일치, 표시 속도 = m/s × 4(정수), 소진 시 경고색.
**Pass Criteria**: 일반 12(±1), 고속 20(±1), 경고색 = 예, 스크린샷 2장 이상 저장.
**Actual Result**:
**Pass/Fail**:
