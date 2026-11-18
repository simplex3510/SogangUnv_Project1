# Test Cases: Story 005 — 폐기 물류 전환과 처분 장소

- **Story Type**: Integration
- **Output Location**: `production/qa/test-cases/` (자동 테스트 WAIVED, 수동 플레이로 모든 AC 확인)
- **Gate Level**: BLOCKING (문서화된 플레이 테스트로 대체)
- **씬**: `Release/MainGameScene.unity`, 처분 장소는 물류센터에서 약 10 m
- **폐기 만드는 법**: Fragile 적재 후 고속 정면 충돌 2회(≈126). 또는 Fresh를 상차 후 신선도 0까지 대기

| AC | 케이스 |
|---|---|
| 1 내구도 0 → Spoiled | TC-005-01 |
| 2 즉시 실패·페널티, 시간 초과 손실 없음 | TC-005-01, 07 |
| 3 처분 장소 그룹·아이콘·나침반 | TC-005-02 |
| 4 원래 배송지에서 제외 | TC-005-02 |
| 5 적재·무게 한도 유지 | TC-005-03 |
| 6 하차 중 폐기 → 중단·초기화 | TC-005-04 |
| 7 홀드 처분, 1개씩, 한도 회복 | TC-005-05 |
| 8 "처분할 물류 없음" | TC-005-06 |
| 9 종료 시 중복 페널티 없음, 정리, 별점 실패 | TC-005-07 |
| 10 처분 장소 1곳 배치 | TC-005-08 |

---

## Test Case: TC-005-01 — 내구도 0 시 폐기 전환과 페널티 (세션 A)

**Precondition**: 출근 직후. Fragile 1개 + Express 아닌 일반 물류 1개 상차. 상차 직후 적재 UI·HUD 금액 메모.
**Steps**:
1. 고속 정면 충돌로 Fragile 내구도를 0으로 만든다.
2. 적재 UI에서 그 물류 상태를 본다.
3. Console의 실패/페널티 로그(있으면)를 복사한다.

**Expected Result**: 물류가 폐기(Spoiled)로 표시되고, 즉시 배송 실패와 페널티 `P × M_type`(현재 120 × 1.0)이 기록된다. 시간 초과 손실은 붙지 않는다.
**Pass Criteria**: 폐기 표시 = 예, 페널티 1회 기록, 시간 초과 손실 0.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-005-02 — 목적지가 처분 장소로 재설정 (세션 A 계속)

**Precondition**: TC-005-01 직후, 폐기 물류 1개가 적재된 상태.
**Steps**:
1. 적재 UI에서 폐기 물류가 처분 장소 그룹에 묶였는지 본다.
2. 나침반이 처분 장소를 가리키는지 본다.
3. 처분 장소의 물류 아이콘에 폐기 물류가 보이는지 본다.
4. 원래 배송지로 가서 그 배송지 아이콘·하차 대상에 폐기 물류가 없는지 확인한다(Space로 하차 시도).
5. 물류센터로 돌아가 상차 UI를 열어 처분 장소 그룹 표시를 확인한다.

**Expected Result**: 상차·적재 UI 모두 처분 장소 그룹에 표시, 아이콘·나침반은 처분 장소를 목적지로. 원래 배송지에서는 하차 대상·아이콘·나침반에서 빠진다.
**Pass Criteria**: 5개 단계 모두 기대와 일치.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-005-03 — 폐기 물류가 한도를 계속 차지

**Precondition**: 폐기 물류 1개 적재 상태.
**Steps**:
1. 물류센터 상차 UI 머리의 적재·무게 배지를 기록한다.
2. 폐기 전 값(같은 물류가 정상이었을 때)과 비교한다.
3. 적재 한도까지 상차를 시도한다.

**Expected Result**: 폐기 물류가 적재 1칸(Large면 2칸)과 무게를 그대로 차지해 그만큼 덜 실린다.
**Pass Criteria**: 배지 값이 폐기 전후 같음.
**Actual Result**: (적재 __/6, 무게 __/6)
**Pass/Fail**:

## Test Case: TC-005-04 — 하차 중 폐기 전환 시 하차 중단

**Precondition**: Fresh 물류를 상차해 신선도가 거의 0이 될 때 배송지 도착(또는 Fragile 내구도를 거의 바닥으로 만든 상태).
**Steps**:
1. 배송지에서 Space 홀드로 그 물류 하차를 시작한다.
2. 진행 중에 폐기 전환이 일어나게 한다(Fresh 신선도 0 / 하차 중 다른 차량 등에 밀림은 어려우므로 Fresh 권장).
3. 진행 바와 물류 상태를 본다.

**Expected Result**: 하차가 중단되고 진행도가 0으로 초기화되며 물류는 폐기로 남는다(배송 완료되지 않음).
**Pass Criteria**: 배송 완료 기록 없음, 진행 바 사라짐/0.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-005-05 — 처분 장소 홀드 처분

**Precondition**: 폐기 물류 2개 적재(배치 3 항목 11과 같은 판에서 진행 가능).
**Steps**:
1. 처분 장소로 이동한다(출발부터 도착까지 현실 시간 기록).
2. 상호작용 키(Space)를 누르고 있는다. 1개 처분까지 걸린 시간을 잰다.
3. 계속 눌러 2개 모두 처분한다.
4. 적재·무게 표시와 정산 금액 변화를 본다.

**Expected Result**: 1개씩 순서대로 처분, 1개당 설정 시간(`DisposalConfig`, 1초). 처분할 때마다 적재·무게가 회복되고 금액은 변하지 않는다.
**Pass Criteria**: 처분 개수 = 2, 1개당 시간 ≈ 1초(±0.3), 금액 변화 0.
**Actual Result**: (이동 __초 / 처분 1개 __초)
**Pass/Fail**:

## Test Case: TC-005-06 — 폐기 물류 없을 때 "처분할 물류 없음"

**Precondition**: 폐기 물류 없음(정상 물류는 있어도 됨).
**Steps**:
1. 처분 장소에서 Space를 누른다.

**Expected Result**: "처분할 물류 없음" 표시, 정상 물류는 처분되지 않는다.
**Pass Criteria**: 문구 표시 = 예, 적재 변화 없음.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-005-07 — 영업 종료 시 처리와 별점

**Precondition**: 폐기 물류 1개를 처분하지 않고 적재한 채, 다른 물류 1개 배송 완료.
**Steps**:
1. 영업 종료까지 기다린다.
2. 정산 상세 내역의 실패/페널티 줄을 본다.
3. 별점 요약의 S/N을 본다.
4. 다음 하루를 시작해 차량 적재가 비었는지 본다.

**Expected Result**: 폐기 물류 페널티는 1회만(종료 시 추가 없음), 실패로 집계, S에 미포함. 남은 폐기 물류는 정리된다.
**Pass Criteria**: 해당 물류 페널티 줄 = 1, S에 미포함, 다음 날 적재 0.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-005-08 — 처분 장소 배치

**Precondition**: Editor에서 `Release/MainGameScene.unity` 열림.
**Steps**:
1. Hierarchy에서 처분 장소 오브젝트를 검색한다.
2. 개수와 물류센터까지 거리를 본다.

**Expected Result**: 처분 장소 1곳, 물류센터에서 떨어져 있음(약 10 m).
**Pass Criteria**: 개수 = 1, 거리 > 5 m.
**Actual Result**:
**Pass/Fail**:
