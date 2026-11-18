# Test Cases: Story 007 — 차량 리셋·복구

- **Story Type**: Integration (+ UI 증거)
- **Output Location**: `production/qa/test-cases/`, 스크린샷은 `production/qa/evidence/`
- **Gate Level**: BLOCKING (수동 플레이 + 스크린샷)
- **씬**: `Release/MainGameScene.unity`
- **기본값**: 홀드 1초, 기록 간격 1초, 보관 5개, 최소 경과 3초, 잠금 2초

| AC | 케이스 |
|---|---|
| 1 R 1초 홀드, 진행 바, 중간 해제 취소 | TC-007-01, 02 |
| 2 R 아이콘 표시·숨김 | TC-007-01, 08 |
| 3 운영 중에만, 상·하차 중단 | TC-007-03, 08 |
| 4 안전 지점 기록 조건 | TC-007-04 |
| 5 3초 이상 지난 지점, 폴백 | TC-007-04, 05 |
| 6 수평·방향·속도 0 | TC-007-04 |
| 7 2초 잠금, 흐린 아이콘, 시간 계속 | TC-007-06 |
| 8 적재·내구도 불변 | TC-007-07 |
| 9 설정 데이터 | TC-007-09 |
| 10 스크린샷 3장 | TC-007-01, 02, 06 |

---

## Test Case: TC-007-01 — R 아이콘 표시와 홀드 복구 (세션 A)

**Precondition**: 출근 직후, 차량 조작 가능.
**Steps**:
1. 플레이어 오른쪽 상호작용 키 안내에 R 아이콘이 보이는지 확인하고 스크린샷 저장.
2. 물류센터 앞에서 R 아이콘과 Space 아이콘이 함께 보이는지 본다.
3. 도로를 5초 이상 달린 뒤 R을 1초 이상 누르고 있는다(진행 바 스크린샷).

**Expected Result**: R 아이콘이 항상 보이고 대상 근처에서 Space와 함께 보인다. 홀드 중 `InteractionProgressUI` 진행 바가 차고 1초 뒤 복구된다.
**Pass Criteria**: 아이콘 표시 = 예, 진행 바 표시 = 예, 복구 발생 = 예, 스크린샷 2장 저장.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-007-02 — 중간에 떼면 취소

**Precondition**: 운영 중, 주행 가능.
**Steps**:
1. R을 약 0.5초 누르고 뗀다.
2. 진행 바와 차량 위치를 본다.

**Expected Result**: 바가 사라지고 복구되지 않는다.
**Pass Criteria**: 차량 위치 변화 없음(이동 없음), 바 사라짐.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-007-03 — 상·하차 중 복구 시 작업 중단

**Precondition**: 물류센터 상차 가능 상태.
**Steps**:
1. 상차 창에서 물류 행을 홀드해 진행 중일 때 R을 1초 누른다(창이 키 입력을 막으면 배송지 Space 하차 중에 시도).
2. 작업 상태와 진행도를 본다.

**Expected Result**: 작업이 중단되고 현재 물류 진행도가 0으로 초기화된다. 그 물류는 상차/하차되지 않는다.
**Pass Criteria**: 작업 미완료, 진행도 0.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-007-04 — 복구 위치·자세 (배치 4 항목 15)

**Precondition**: 운영 중.
**Steps**:
1. 직선 도로를 한 방향으로 5초 이상 달린다(안전 지점 기록).
2. 연석/건물에 걸리거나 차를 뒤집히게 만든다(경사·고속 충돌).
3. 곧바로 R 1초 홀드.
4. 복구된 위치·방향·자세를 본다. 직후 1초간 차가 움직이는지 본다.

**Expected Result**: 3초 이상 지난 가장 최근 안전 지점(도로 위, 바퀴 4개 접지)으로 이동, 차체 수평, 기록 당시 진행 방향, 속도·회전 0.
**Pass Criteria**: 도로 위 복구 = 예, 수평 = 예, 방향 일치 = 예, 미끄러짐 없음.
**Actual Result**: (복구 위치 설명 __)
**Pass/Fail**:

## Test Case: TC-007-05 — 쓸 지점이 없으면 물류센터 앞

**Precondition**: 새 하루, 출근 직후.
**Steps**:
1. 출근 후 3초 안에(안전 지점이 3초 이상 지나지 않은 상태) R 1초 홀드.

**Expected Result**: 물류센터 앞 지정 위치로 복구.
**Pass Criteria**: 물류센터 앞 위치 = 예.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-007-06 — 2초 잠금과 흐린 아이콘, 시간 계속 흐름

**Precondition**: Express 물류 1개 적재(제한 시간 바 보이는 상태).
**Steps**:
1. R 1초 홀드로 복구한다. 복구 직후 시계와 Express 남은 시간을 기록.
2. 즉시 W·방향키를 누른다.
3. 잠금 중 흐린 R 아이콘 스크린샷 저장.
4. 2초 뒤 다시 W를 눌러 움직이는지 본다.

**Expected Result**: 약 2초간 조작 불가, R 아이콘 반투명. 그동안 게임 시계와 Express 제한 시간은 계속 줄어든다. 2초 후 조작 가능.
**Pass Criteria**: 잠금 시간 2초(±0.3), 시계 진행 = 예, 스크린샷 저장.
**Actual Result**: (잠금 __초)
**Pass/Fail**:

## Test Case: TC-007-07 — 적재·내구도 불변, 충돌 판정 제외

**Precondition**: 물류 3개 적재, 각 내구도 기록.
**Steps**:
1. R 복구를 실행한다.
2. 적재 목록과 내구도를 다시 본다. Console에 `[CargoImpact]` 로그가 생겼는지 본다.

**Expected Result**: 적재 물류 수·내구도 동일, 복구 이동으로 인한 `[CargoImpact]` 감소 없음.
**Pass Criteria**: 내구도 변화 0, 감소 로그 0.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-007-08 — 하루 종료 후 R 비활성·아이콘 숨김 / 메뉴 중 비활성

**Precondition**: 시작 메뉴 화면, 그 뒤 하루 종료 직후.
**Steps**:
1. 시작 메뉴에서 R을 1초 누른다.
2. 하루가 끝난(정산 화면) 뒤 R 아이콘이 보이는지 보고 R을 1초 누른다.

**Expected Result**: 메뉴와 하루 종료 후에는 진행 바·복구가 없고 R 아이콘은 숨겨진다.
**Pass Criteria**: 두 상황 모두 복구 없음, 종료 후 아이콘 숨김.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-007-09 — 설정 데이터 확인

**Precondition**: Editor, Play 전.
**Steps**:
1. 복구 설정 에셋을 Inspector로 열어 홀드 시간·기록 간격·보관 개수·최소 경과 시간·최대 기울기·잠금 시간을 기록한다.
2. 잠금 시간을 4로 임시 변경해 TC-007-06을 한 번 재현하고 원래대로 되돌린다.

**Expected Result**: 6개 값이 에셋에 있고 변경이 반영된다.
**Pass Criteria**: 6개 값 확인 = 예, 잠금 4초 반영 = 예.
**Actual Result**:
**Pass/Fail**:
