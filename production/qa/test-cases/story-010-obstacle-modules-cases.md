# Test Cases: Story 010 — 정적 방해물 모듈 컴포넌트

- **Story Type**: Logic
- **Output Location**: `production/qa/test-cases/` (자동 테스트 WAIVED)
- **Gate Level**: BLOCKING (수동 증거로 대체)
- **씬**: `Release/MainGameScene.unity`. Story 011 프리팹(CONE, DEBRIS-S/L, ROADWORK)을 대상으로 쓴다
- **주의**: AC 2·7은 코드 검색 항목이다. 수동 플레이로는 확인할 수 없어 IDE 검색 절차로 적었다

| AC | 케이스 |
|---|---|
| 1 트리거·효과·수명 조합, 코드 수정 없이 | TC-010-01 |
| 2 IVehicleEffectTarget 경유 | TC-010-07 (코드 검색) |
| 3 1회 실행 | TC-010-02 |
| 4 같은 효과 최강값 + 지속 갱신 | TC-010-03 |
| 5 하루 종료 중 비실행, 효과 해제 | TC-010-04 |
| 6 대상 제거 후 오류 없음 | TC-010-05 |
| 7 내구도 심볼 미참조, 효과는 내구도 무변 | TC-010-06, 07 |
| 8 SO 데이터 | TC-010-08 |
| 9 기존 충돌·상하차 회귀 없음 | TC-010-09 |

---

## Test Case: TC-010-01 — 새 조합 프리팹을 코드 수정 없이 만들기

**Precondition**: Editor, Play 전. 스크립트 파일 수정 금지.
**Steps**:
1. 빈 GameObject + BoxCollider를 만들고 트리거 "진입", 효과 "최고 속도 배율", 수명 "잔류 N초 후 제거(N=3)" 컴포넌트를 붙인다(설정 SO 연결).
2. 도로 위에 놓고 Play → 출근 → 지나간다.
3. 3초 뒤 사라지는지 본다. 테스트 후 오브젝트를 삭제하고 씬을 저장하지 않는다.

**Expected Result**: 붙인 조합대로 감속이 걸리고 3초 뒤 제거. 컴파일 에러·코드 수정 없음.
**Pass Criteria**: 효과 발생 = 예, 3초 제거 = 예, 스크립트 변경 0.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-010-02 — 오브젝트는 한 번만 실행

**Precondition**: 운영 중, CONE(충돌·잔류 10초) 1개.
**Steps**:
1. 콘에 충돌한다.
2. 쓰러진 콘에 10초 안에 다시 부딪힌다.
3. 감속이 다시 걸리는지 본다.

**Expected Result**: 첫 접촉만 효과, 두 번째 접촉은 효과 없음.
**Pass Criteria**: 감속 발생 횟수 = 1.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-010-03 — 같은 효과 겹침: 최강값, 지속 갱신

**Precondition**: CONE(×0.8, 1초)과 DEBRIS-L(×0.6, 2초)을 1초 안에 연달아 지날 수 있게 배치(임시, 씬 저장 안 함).
**Steps**:
1. CONE → DEBRIS-L 순으로 지난다. 최고 속도를 본다.
2. DEBRIS-L → CONE 순으로 다시 한다(새 Play).

**Expected Result**: 둘 다 ×0.6(더 강한 값) 하나만 적용, 곱해서 ×0.48이 되지 않는다. 지속 시간은 마지막 효과 기준으로 갱신.
**Pass Criteria**: 최고 속도 ≈ 기준 × 0.6 (±0.05), ×0.48 아님.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-010-04 — 하루 종료 중 트리거 비실행, 효과 해제

**Precondition**: 영업 종료 약 2초 전.
**Steps**:
1. DEBRIS-L을 지나 감속을 건 상태로 영업 종료를 맞는다.
2. 종료 후 차량 속도 효과 상태를 본다(Inspector에서 효과 목록이 있으면).
3. 종료 후 관성으로 다른 콘에 닿게 한다.

**Expected Result**: 종료 시 지속 효과 해제, 종료 후 접촉한 오브젝트는 실행되지 않음(콘 그대로).
**Pass Criteria**: 효과 0, 종료 후 실행 0.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-010-05 — 대상이 이미 제거됐을 때 오류 없음

**Precondition**: ROADWORK 그룹.
**Steps**:
1. 차단물 하나를 Hierarchy에서 Play 중 수동 삭제한다.
2. `construction-barrier`를 3초 상호작용해 그룹 제거를 실행한다.
3. Console을 본다.

**Expected Result**: 남은 차단물만 제거, 예외 없음.
**Pass Criteria**: Error/Exception = 0.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-010-06 — 효과 실행만으로 내구도 불변

**Precondition**: 물류 2개 적재, 내구도 기록.
**Steps**:
1. 일반 속도로 DEBRIS-S, DEBRIS-L 트리거를 지난다(충돌 아님).
2. 내구도와 `[CargoImpact]` 로그를 본다.

**Expected Result**: 내구도 변화 없음.
**Pass Criteria**: 내구도 변화 0.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-010-07 — 코드 검색: 결합 금지·Rigidbody 직접 쓰기 금지

**Precondition**: IDE 또는 Grep 사용 가능.
**Steps**:
1. 방해물(오브젝트) 계열 스크립트 폴더에서 `Parcel`, `CargoSpec`, `Durability` 를 검색한다.
2. 같은 폴더에서 `velocity`, `angularVelocity`, `AddForce`, `Rigidbody` 를 검색한다.
3. 효과 컴포넌트가 `IVehicleEffectTarget` 를 통해 적용하는지 확인한다.

**Expected Result**: 1·2 검색 결과 0건, 효과는 `IVehicleEffectTarget` 호출로만.
**Pass Criteria**: 검색 결과 0건(물리 형태용 Rigidbody 컴포넌트 참조는 예외로 기록).
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-010-08 — 효과 파라미터와 잔류 시간이 SO 데이터

**Precondition**: Editor.
**Steps**:
1. CONE의 효과·수명 컴포넌트에서 연결된 ScriptableObject를 연다.
2. 배율·지속·잔류 시간 값을 기록하고, 잔류 시간을 3으로 바꿔 콘이 3초 뒤 사라지는지 본다. 원래대로 복구.

**Expected Result**: 값이 SO에 있고 변경이 반영된다.
**Pass Criteria**: 반영 = 예.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-010-09 — 기존 건물·울타리 충돌, 상·하차 그대로

**Precondition**: 운영 중.
**Steps**:
1. 건물과 울타리(Fence)에 각각 충돌한다. 차가 막히고 `[CargoImpact]` 로그가 이전처럼 나오는지 본다.
2. 물류센터 상차 1건, 배송지 하차 1건을 한다.

**Expected Result**: 충돌·막힘·감소 로그와 상·하차가 이전과 같다.
**Pass Criteria**: 이상 동작 0, Error 0.
**Actual Result**:
**Pass/Fail**:
