# Test Cases: Story 009 — 무게 한도 초과 적재와 속도 감소

- **Story Type**: Logic (+ UI 증거)
- **Output Location**: `production/qa/test-cases/` (자동 테스트 WAIVED), 스크린샷 `production/qa/evidence/`
- **Gate Level**: BLOCKING (수동 증거로 대체)
- **씬**: `Release/MainGameScene.unity`
- **값**: W = 6, r_max = 1.5 → W_max = 9, k = 1.0, m_min = 0.5. `m = max(0.5, 1 − (w−6)/6)` → w 7: 0.833, 8: 0.667, 9: 0.5
- **속도 측정**: 속도계 HUD(Story 013) 값 또는 Inspector Rigidbody 속도. 일반 3 m/s, 고속 5 m/s 기준

| AC | 케이스 |
|---|---|
| 1 W_max까지 개별 상차, 초과 시 WEIGHT LIMIT, 적재 한도 불가 | TC-009-01, 02 |
| 2 그룹 상차 W 초과 물류에서 종료 | TC-009-03 |
| 3 m을 전진 최고 속도·가속에만 | TC-009-04, 05 |
| 4 상차·하차·처분 시 재계산, 폐기 포함 | TC-009-06 |
| 5 수치 예시 | TC-009-04 |
| 6 상차 UI 경고색·행 상태 | TC-009-01, 02 |
| 7 HUD OVERWEIGHT | TC-009-04, 06 |
| 8 보상·내구도·별점 무영향 | TC-009-07 |
| 9 설정 데이터 | TC-009-08 |
| 10 스크린샷 | TC-009-01, 04 |

---

## Test Case: TC-009-01 — W 초과 개별 상차와 경고 행 (세션 A, 배치 3 항목 10과 공유)

**Precondition**: 출근 직후, Heavy 물류 2개 이상 존재(없으면 판 반복). 적재 0.
**Steps**:
1. 일반 물류 4개 + Heavy 1개를 개별(행 홀드) 상차해 무게 6을 만든다.
2. 상차 창에서 남은 Heavy 행이 경고 표시지만 활성인지 본다(상차 시 7 ≤ 9). 스크린샷.
3. Heavy 1개를 더 상차한다(무게 > W). 무게 배지가 경고색인지 본다.

**Expected Result**: W 초과지만 W_max 이하인 행은 경고 + 활성, 상차 성공. 무게 사용량이 W를 넘으면 경고색.
**Pass Criteria**: 상차 성공 = 예, 경고색 = 예, 스크린샷 저장.
**Actual Result**: (무게 __/6)
**Pass/Fail**:

## Test Case: TC-009-02 — W_max 초과 행 비활성, 적재 한도 초과 불가

**Precondition**: 무게 8 근처까지 적재(Heavy 포함).
**Steps**:
1. 상차하면 무게가 9를 넘게 되는 행을 찾는다. `WEIGHT LIMIT` 표시와 비활성인지 보고 홀드해 본다.
2. 적재 6칸을 채운 뒤 아무 행이나 홀드해 본다.

**Expected Result**: W_max 초과 행은 비활성 + `WEIGHT LIMIT`, 상차 안 됨. 적재 한도 초과 상차도 안 됨.
**Pass Criteria**: 두 경우 모두 상차 0.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-009-03 — 그룹 상차의 W 정지

**Precondition**: 적재 0. 한 배송지 그룹에 Heavy가 앞쪽, 가벼운 물류가 뒤쪽에 있는 상태(정렬 순서 확인).
**Steps**:
1. 무게 5까지 다른 물류를 실어 둔다.
2. 그룹 헤더를 홀드해 그룹 상차한다.
3. 어느 물류에서 멈췄는지, 뒤의 가벼운 물류가 실렸는지 본다.

**Expected Result**: 상차 후 무게가 W(6)를 넘는 물류에서 자동 종료, 뒤 물류로 건너뛰지 않는다.
**Pass Criteria**: 종료 후 무게 ≤ 6, 건너뛴 상차 0.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-009-04 — 속도 배율 수치와 HUD 표시

**Precondition**: 무게 7, 8, 9를 차례로 만들 수 있는 물류(Heavy 2~3개).
**Steps**:
1. 무게 ≤ 6일 때 일반·고속 최고 속도를 기록하고 HUD에 `OVERWEIGHT` 가 없는지 본다.
2. 무게 8로 만든 뒤 직선 도로에서 일반·고속 최고 속도와 HUD 문구를 기록한다. HUD 스크린샷.
3. 무게 9에서 반복한다.

**Expected Result**: m = 1 / 0.667 / 0.5. 최고 속도 = 기준 × m. HUD는 `OVERWEIGHT −33%`, `−50%`, 이내일 때 숨김.
**Pass Criteria**: 실측 속도 ÷ 기준 속도가 m과 ±0.05 이내, HUD 감소율 일치.
**Actual Result**:

| w | 기대 m | 일반 최고속(측정) | 고속 최고속(측정) | HUD 문구 |
|---|---|---|---|---|
| ≤6 | 1.0 | | | |
| 8 | 0.667 | | | |
| 9 | 0.5 | | | |

**Pass/Fail**:

## Test Case: TC-009-05 — 후진·제동·회전·드리프트는 불변

**Precondition**: 같은 위치에서 w ≤ 6 판과 w = 9 판을 비교.
**Steps**:
1. 각 판에서 후진 최고 속도를 기록한다.
2. 같은 속도에서 제동 정지 거리, 제자리 회전 감각을 비교한다.
3. 가속(0 → 최고속 도달 시간)을 비교한다.

**Expected Result**: 후진·제동·회전·드리프트 같음. 전진 가속만 줄어든다.
**Pass Criteria**: 후진 최고속 차이 ≤ 5%, 가속 시간은 w=9에서 더 김.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-009-06 — 하차·처분 시 재계산, 폐기 물류 포함

**Precondition**: 무게 8(Heavy 포함) 적재.
**Steps**:
1. Heavy가 아닌 물류 하나를 하차한다 → HUD·속도 변화 확인.
2. 남은 물류 중 하나를 충돌로 폐기시킨다 → HUD가 그대로인지 확인.
3. 처분 장소에서 처분한다 → HUD 감소율 변화 확인.

**Expected Result**: 하차·처분 완료 직후 m과 HUD 갱신. 폐기 전환만으로는 무게가 빠지지 않는다.
**Pass Criteria**: 3단계 각각 HUD 값이 기대 무게의 m과 일치.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-009-07 — 보상·내구도·별점 무영향

**Precondition**: 과적(w = 8) 상태로 물류 1개 배송.
**Steps**:
1. 그 물류의 정산 보상 줄을 과적 없이 배송한 같은 조건 물류와 비교한다.
2. 과적 상태로 충돌해 내구도 감소량이 Story 004 공식과 같은지 본다.

**Expected Result**: 과적 관련 보상 증감·추가 피해 없음.
**Pass Criteria**: 보상 차이 0(거리·특징 차 제외), 감소량 = 공식값.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-009-08 — 설정 데이터

**Precondition**: Editor, Play 전.
**Steps**:
1. `VehicleCargoConfig` 에서 `r_max`·`k`·`m_min` 값을 기록한다.

**Expected Result**: 1.5 · 1.0 · 0.5.
**Pass Criteria**: 세 값 일치.
**Actual Result**:
**Pass/Fail**:
