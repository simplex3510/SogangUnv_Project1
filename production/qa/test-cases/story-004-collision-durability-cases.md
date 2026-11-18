# Test Cases: Story 004 — 충돌 내구도 감소

- **Story Type**: Logic
- **Output Location**: `production/qa/test-cases/` (자동 테스트 WAIVED, 수동으로 모든 AC 확인)
- **Gate Level**: BLOCKING (수동 증거로 대체)
- **씬**: `Release/MainGameScene.unity`
- **공식**: `damage = min((Δv − 1.5) × 12, 60)`, `Δv < 1.5` → 0, Fragile ×1.5. 참고 기대값: 일반 3 m/s 정면 ≈ 18, 고속 5 m/s 정면 ≈ 42 (Fragile 63)
- **측정 수단**: Console `[CargoImpact]` 로그(Δv, damage, 물류별 내구도). 로그에 값이 없으면 Inspector의 Parcel 현재 내구도로 대신 확인

| AC | 케이스 |
|---|---|
| 1 현재/초기 내구도 | TC-004-01 |
| 2 Δv·damage 공식, 하한선 | TC-004-02, 03 |
| 3 전 물류 동일, Fragile ×1.5만 | TC-004-02, 04 |
| 4 0.5초 쿨다운, 밀어붙이기 무감소 | TC-004-05 |
| 5 지면·착지 무감소 | TC-004-06 |
| 6 0 하한, 폐기 이벤트 | TC-004-07 |
| 7 CargoTraitConfig Fragile 값 | TC-004-08 |
| 8 설정 데이터 | TC-004-08 |
| 9 로그 + 벽 충돌 확인 | TC-004-02, 03 |

---

## Test Case: TC-004-01 — 생성 시 내구도와 초기 스냅샷 (세션 A)

**Precondition**: 출근 직후.
**Steps**:
1. 물류센터에서 물류 3개(가능하면 Fragile 1개 포함)를 상차한다.
2. Inspector(또는 적재 UI)에서 각 물류의 초기 내구도와 현재 내구도를 기록한다.
3. 벽에 한 번 충돌한 뒤 다시 기록한다.

**Expected Result**: 생성 직후 현재 = 초기 = `CargoSpec.Durability`. 충돌 후 현재만 줄고 초기 값은 그대로.
**Pass Criteria**: 충돌 전후 초기 내구도 값 불변.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-004-02 — 일반·고속 정면 충돌 감소량, Fragile 배율 (세션 A)

**Precondition**: TC-004-01 상태(일반 1개 이상 + Fragile 1개 적재). 물류센터 근처 건물 벽 하나를 정해 둔다.
**Steps**:
1. 일반 모드 최고 속도(3 m/s)로 벽에 수직 정면 충돌한다. `[CargoImpact]` 로그를 복사한다.
2. 0.5초 이상 떨어졌다가 다시 가속한다.
3. 고속 모드 최고 속도(5 m/s)로 같은 벽에 정면 충돌한다. 로그를 복사한다.

**Expected Result**: 로그의 damage = `min((Δv−1.5)×12, 60)`(±1). 같은 충돌에서 모든 일반 물류가 같은 감소량, Fragile은 그 ×1.5. 일반 정면 ≈ 18, 고속 정면 ≈ 42 근처.
**Pass Criteria**: 각 충돌에서 로그 Δv로 계산한 값과 로그 damage 차이 ≤ 1, Fragile 감소 = 일반 × 1.5 (±1).
**Actual Result**:

| 충돌 | Δv | damage(일반) | damage(Fragile) | 계산값 |
|---|---|---|---|---|
| 일반 정면 | | | | |
| 고속 정면 | | | | |

**Pass/Fail**:

## Test Case: TC-004-03 — Δv < 1.5 이면 감소 없음

**Precondition**: 물류 적재 상태.
**Steps**:
1. 아주 천천히(가속 키를 짧게 톡) 벽에 닿는다.
2. 로그와 내구도를 확인한다.

**Expected Result**: Δv < 1.5로 기록되고 damage 0, 내구도 변화 없음.
**Pass Criteria**: 내구도 변화 = 0.
**Actual Result**: (Δv __)
**Pass/Fail**:

## Test Case: TC-004-04 — 특징이 여럿인 물류는 Fragile 배율만

**Precondition**: Fragile + 다른 특징(Heavy/Large)을 함께 가진 물류가 생성된 판. 없으면 몇 판 반복하거나 N/A 기록.
**Steps**:
1. 그 물류와 일반 물류를 함께 상차한다.
2. 고속 정면 충돌 1회.
3. 두 물류의 감소량을 비교한다.

**Expected Result**: 복합 특징 물류 감소 = 일반 × 1.5 (추가 배율 없음).
**Pass Criteria**: 비율 = 1.5 (±1 반올림).
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-004-05 — 0.5초 쿨다운과 밀어붙이기

**Precondition**: 물류 적재 상태.
**Steps**:
1. 벽에 충돌한 뒤 가속 키를 계속 눌러 벽에 3초간 밀어붙인다.
2. 로그 수와 내구도 변화를 본다.
3. 벽 모서리 등에서 0.5초 안에 두 번 연속 부딪히는 상황을 만든다(튕겨서 다시 맞기).

**Expected Result**: 밀어붙이는 동안 추가 감소 없음. 0.5초 안의 두 번째 충돌은 감소 없음.
**Pass Criteria**: 1단계 이후 `[CargoImpact]` 감소 로그 = 첫 충돌 1건뿐, 3단계 두 번째 충돌 감소 = 0.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-004-06 — 지면·착지 충격 무감소

**Precondition**: 물류 적재 상태. 턱·경사·연석 등 차가 살짝 뜨거나 떨어지는 곳.
**Steps**:
1. 고속으로 턱/경사를 넘어 착지한다.
2. 평지 주행 10초.
3. 로그와 내구도를 본다.

**Expected Result**: 착지·지면 접촉으로는 감소 없음.
**Pass Criteria**: 내구도 변화 = 0.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-004-07 — 0 하한과 폐기 이벤트

**Precondition**: Fragile 물류 1개 적재(고속 정면 2회 ≈ 126으로 0 도달 기대).
**Steps**:
1. 고속 정면 충돌을 내구도 0이 될 때까지 반복한다(충돌 간 0.5초 이상).
2. 0 이후 한 번 더 충돌한다.
3. 내구도 값과 폐기 표시(Story 005)를 확인한다.

**Expected Result**: 내구도가 음수가 되지 않고 0에서 멈춘다. 0 도달 시 폐기 전환 이벤트가 나 물류가 폐기 상태로 바뀐다.
**Pass Criteria**: 최솟값 = 0, 폐기 전환 = 예.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-004-08 — 설정 데이터 확인(Inspector)

**Precondition**: Editor, Play 전.
**Steps**:
1. `CargoTraitConfig` 에셋을 열어 Fragile 내구도 감산값과 Fragile 감소 배율 필드를 본다.
2. 충돌 설정 에셋에서 하한선(1.5)·계수(12)·1회 상한(60)·쿨다운(0.5)·배율 필드를 찾는다.
3. 계수를 24로 임시 변경해 일반 정면 충돌 1회 → damage가 약 2배인지 보고, 원래대로 되돌린다.

**Expected Result**: Fragile 감산값 = 0, 배율 = 1.5. 다섯 값 모두 에셋에 있고 변경이 결과에 반영된다.
**Pass Criteria**: 값 확인 = 예, 계수 변경 반영 = 예.
**Actual Result**: (에셋 경로·값 __)
**Pass/Fail**:
