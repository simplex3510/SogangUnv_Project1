# Test Cases: Story 011 — 카탈로그 정적 오브젝트 프리팹화

- **Story Type**: Config/Data (+ Visual 증거)
- **Output Location**: `production/qa/test-cases/`, 스크린샷 `production/qa/evidence/`
- **Gate Level**: 프리팹 정의 ADVISORY / 스크린샷 BLOCKING
- **씬**: `Release/MainGameScene.unity`. 씬에 없는 프리팹은 Play 중 임시로 끌어다 놓고 씬은 저장하지 않는다
- **기준 문서**: 오브젝트 카탈로그 2장 표(물리 형태·트리거·효과·수명·질량·내구도 열)

| AC | 케이스 |
|---|---|
| 1 7종 프리팹 = 카탈로그 표 | TC-011-01 |
| 2 CONE 동작 | TC-011-02 |
| 3 DEBRIS-S/L 동작 | TC-011-03 |
| 4 ROADWORK 3초 개방 | TC-011-04 |
| 5 머리 위 모델 무콜라이더, 기타 고정 콜라이더 | TC-011-05 |
| 6 Props 6종 일치 | TC-011-06 |
| 7 고속 정면 내구도 ±20% | TC-011-07 |
| 8 스크린샷 | TC-011-02, 03, 04 |

---

## Test Case: TC-011-01 — 7종 프리팹 정의 대조

**Precondition**: Editor, Play 전. 카탈로그 2장 표 열어 둠.
**Steps**:
1. OBJ-CONE, BOX, DUMPSTER, DEBRIS-S, DEBRIS-L, ROADWORK, FIXTURE 프리팹을 하나씩 연다.
2. 콜라이더 종류, 트리거·효과·수명 컴포넌트, Rigidbody 질량을 표와 대조한다.
3. 붙은 컴포넌트가 Story 010 모듈뿐인지(전용 스크립트 없음) 본다.

**Expected Result**: 7종 모두 표와 일치, Story 010 컴포넌트 조합만 사용.
**Pass Criteria**: 불일치 0건.
**Actual Result**:

| 프리팹 | 물리 | 트리거 | 효과 | 수명 | 질량 | 일치 |
|---|---|---|---|---|---|---|
| CONE | | | | | | |
| BOX | | | | | | |
| DUMPSTER | | | | | | |
| DEBRIS-S | | | | | | |
| DEBRIS-L | | | | | | |
| ROADWORK | | | | | | |
| FIXTURE | | | | | | |

**Pass/Fail**:

## Test Case: TC-011-02 — CONE 충돌 (세션 A)

**Precondition**: 운영 중, CONE 앞 직선 도로.
**Steps**:
1. 일반 속도로 콘에 충돌한다. 직후 최고 속도와 감속 지속 시간을 본다.
2. 쓰러진 `cone-flat` 모습을 스크린샷.
3. 충돌 시점부터 사라질 때까지 시간을 잰다.

**Expected Result**: `cone-flat` 메시로 바뀜, 최고 속도 ×0.8 1초, 10초 후 사라짐.
**Pass Criteria**: 메시 교체 = 예, 감속 ≈ 1초, 제거 10초(±1).
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-011-03 — DEBRIS-S·DEBRIS-L 진입

**Precondition**: 물류 적재(내구도 기록), DEBRIS-S·L 배치.
**Steps**:
1. DEBRIS-S를 직진으로 지난다. 진행 방향 변화 각도를 본다(같은 동작 3회, 좌/우 기록).
2. DEBRIS-L을 지난다. 최고 속도와 지속 시간을 본다.
3. 두 잔해가 즉시 사라지는지, 내구도 변화를 본다. 각 장면 스크린샷.

**Expected Result**: DEBRIS-S ±15° 회전, DEBRIS-L ×0.6 2초. 둘 다 즉시 제거, 내구도 불변.
**Pass Criteria**: 각도 약 15°, 감속 ≈ 2초, 즉시 제거, 내구도 변화 0.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-011-04 — ROADWORK 3초 개방

**Precondition**: ROADWORK 그룹 앞.
**Steps**:
1. `construction-barrier` 에 접근해 상호작용 키를 1.5초 누르고 뗀다(취소 확인).
2. 다시 3초 누르고 있는다.
3. 같은 그룹 차단물이 모두 사라지는지 보고 스크린샷.

**Expected Result**: 3초 미만은 실패, 3초 완료 시 그룹 전체 제거.
**Pass Criteria**: 1단계 제거 0, 2단계 후 그룹 차단물 0개.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-011-05 — 머리 위 모델과 기타 정적 후보

**Precondition**: 씬에 `electricity-wires*`, `road-sign-empty-hanging` 있음.
**Steps**:
1. Hierarchy에서 해당 오브젝트를 선택해 Collider 컴포넌트 유무를 본다.
2. 그 아래를 지나간다.
3. 표에 없는 정적 후보 하나에 충돌해 효과 없이 막히는지 본다.

**Expected Result**: 머리 위 모델은 콜라이더 없음·통과. 기타 후보는 고정 콜라이더(움직이지 않음, 효과 없음).
**Pass Criteria**: 기대와 일치.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-011-06 — Props 6종 대조

**Precondition**: Editor.
**Steps**:
1. `Assets/Game/Prefabs/Props/` 의 Cone, Box, Dumpster, Fence, StreetLight, Tree를 열어 카탈로그 정의와 대조한다.

**Expected Result**: 6종 일치.
**Pass Criteria**: 불일치 0.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-011-07 — 고속 정면 충돌 내구도 ±20% (배치 3 항목 8과 공유)

**Precondition**: 일반·Fragile 물류 적재. 대상: BOX, DUMPSTER, FIXTURE 등 카탈로그 "내구도" 열에 값이 있는 오브젝트.
**Steps**:
1. 각 오브젝트에 고속(5 m/s) 정면 충돌하고 `[CargoImpact]` 의 일반 감소량을 기록한다(충돌 사이 0.5초 이상).
2. 카탈로그 값과 비교해 오차를 계산한다.

**Expected Result**: 실측이 카탈로그 값 ±20% 안. 벗어나면 카탈로그를 실측값으로 갱신해야 함(기록만, 수정은 담당자).
**Pass Criteria**: 모든 대상 |실측 − 카탈로그| ÷ 카탈로그 ≤ 0.2.
**Actual Result**:

| 오브젝트 | 카탈로그 내구도 | 실측 | 오차 % |
|---|---|---|---|
| | | | |

**Pass/Fail**:
