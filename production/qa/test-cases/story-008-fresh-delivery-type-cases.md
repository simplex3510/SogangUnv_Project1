# Test Cases: Story 008 — Fresh 배송 유형

- **Story Type**: Logic (+ UI 증거)
- **Output Location**: `production/qa/test-cases/` (자동 테스트 WAIVED), 스크린샷 `production/qa/evidence/`
- **Gate Level**: BLOCKING (수동 증거로 대체)
- **씬**: `Release/MainGameScene.unity`. Fresh 확률이 약 13%이므로 Fresh가 안 나오면 Play 전 `DeliveryTypeConfig`의 Fresh 확률을 임시로 올리고(원래 값 기록·복구) 진행
- **참고(정적 모델)**: (릴리즈 씬 2.5~26.3 m 기준) Fresh 총량 현실 7.7~14.0초, Express 15.0~21.0초

| AC | 케이스 |
|---|---|
| 1 유형·설정·생성 로그 | TC-008-01, 09 |
| 2 T_fresh < Express 제한 시간 | TC-008-01 |
| 3 대기 중 불변, 상차 후 감소 | TC-008-02 |
| 4 0 → 폐기, Fresh 계수 페널티 1회 | TC-008-04 |
| 5 신선도 보상 비율 | TC-008-03 |
| 6 처리 순서 | TC-008-05 |
| 7 정산 합계, 별점 성공 | TC-008-06 |
| 8 UI·스크린샷 | TC-008-02, 07 |
| 9 설정 데이터 | TC-008-09 |
| 10 Normal·Express 회귀 없음 | TC-008-08 |

---

## Test Case: TC-008-01 — 생성 로그와 총량 비교 (세션 A)

**Precondition**: Console Clear 후 출근.
**Steps**:
1. `[Parcel #n]` 로그에서 `Fresh` 항목을 찾는다. 같은 배송지(거리)의 Express 로그도 찾는다.
2. 각 제한 시간/총량과 거리를 복사한다.
3. 동적 생성 물류가 Fresh로 나오면 그것도 기록한다(정적·동적 같은 확률).

**Expected Result**: 로그에 `Fresh` 유형이 표시되고, 같은 거리에서 Fresh 총량 < Express 제한 시간.
**Pass Criteria**: Fresh 로그 ≥ 1, 같은 거리 비교에서 Fresh < Express.
**Actual Result**:

| 유형 | 배송지 | 거리 | 제한 시간/총량(게임초) |
|---|---|---|---|
| | | | |

**Pass/Fail**:

## Test Case: TC-008-02 — 대기 중 신선도 고정, 상차 후 감소 (세션 A)

**Precondition**: Fresh 물류가 물류센터에 대기 중.
**Steps**:
1. 상차 창에서 Fresh 행의 `FRS` 태그·총량 표시·가득 찬 바를 확인하고 스크린샷.
2. 창을 닫고 현실 5초 기다린 뒤 다시 연다. 바와 값이 같은지 본다.
3. Fresh를 상차한다. 적재 UI와 배송지 아이콘의 신선도 바가 줄어드는지 보고 각각 스크린샷.

**Expected Result**: 대기 중 바 가득·값 불변. 상차 완료 순간부터 게임 시간 기준으로 감소.
**Pass Criteria**: 5초 전후 값 동일, 상차 후 감소 = 예, 스크린샷 3장.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-008-03 — 신선도 보상 비율 (배치 2 항목 5와 공유)

**Precondition**: Fresh 상차 완료.
**Steps**:
1. 하차 완료 순간 남은 신선도 바 비율을 눈으로/로그로 기록한다(가능하면 약 50%일 때 하차).
2. 정산 상세의 신선도 보상 금액을 기록한다.
3. 보상 ÷ `B_fresh` 를 계산한다.

**Expected Result**: 보상 = `B_fresh × clamp(남은 ÷ 총량, 0, 1)`. 50%면 `B_fresh × 0.5`(±1).
**Pass Criteria**: 계산 보상과 실제 차이 ≤ 1 (비율은 로그 값 기준).
**Actual Result**: (남은 비율 __ / B_fresh __ / 보상 __)
**Pass/Fail**:

## Test Case: TC-008-04 — 신선도 0 → 폐기, 페널티 1회

**Precondition**: Fresh 상차 후 정지.
**Steps**:
1. 바가 0이 될 때까지 기다린다(현실 약 9~12초).
2. 물류 상태(폐기), 처분 장소 그룹 이동을 본다.
3. 영업 종료 후 정산 상세에서 그 물류 페널티 줄 수와 금액을 본다.

**Expected Result**: 폐기 전환, 페널티 = `P × Fresh 실패 계수` 1회, 시간 초과 손실 없음.
**Pass Criteria**: 페널티 줄 1개, 금액 = P × 계수, 시간 초과 손실 0.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-008-05 — 처리 순서

**Precondition**: 물류센터에 Fresh 2개 이상, Express, Normal이 섞인 상태(필요하면 Fresh 확률 임시 상향).
**Steps**:
1. 상차 창의 행 순서를 기록한다(대기 Fresh는 총량 짧은 순, Express보다 뒤).
2. Fresh 1개, Express 1개, Normal 1개 상차 후 적재 UI·나침반·배송지 하차 순서를 본다.

**Expected Result**: 상차 Fresh(남은 짧은 순) → Express → 대기 Fresh(총량 짧은 순) → Normal. 상차 UI·하차·나침반 동일 순서.
**Pass Criteria**: 세 곳 모두 순서 일치.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-008-06 — 정산 합계와 별점 성공

**Precondition**: Fresh 1개 기한 내 배송 완료한 판.
**Steps**:
1. 영업 종료 후 정산 상세의 `신선도 보상 합계` 줄을 본다.
2. S/N에 Fresh가 성공으로 들어갔는지 본다.

**Expected Result**: 합계 표시, Fresh 배송은 S에 포함.
**Pass Criteria**: 합계 = TC-008-03 보상 합, S 포함 = 예.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-008-07 — UI 스크린샷 보관 확인

**Precondition**: TC-008-02 진행 후.
**Steps**:
1. `production/qa/evidence/` 에 물류센터(FRS·가득 바), 배송지 아이콘(감소 바), 적재 UI(감소 바) 스크린샷이 있는지 확인한다.

**Expected Result**: 3장 존재.
**Pass Criteria**: 파일 3개 확인.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-008-08 — Normal·Express 기존 동작 유지

**Precondition**: 새 하루.
**Steps**:
1. Express·Normal 로그의 제한 시간 공식(기본 6,300 + 120×D 근처)을 TC-008-01 표로 확인한다.
2. Express 1개 기한 내, Normal 1개 배송 후 정산의 기본·거리·특징·시간 보상 줄을 본다.

**Expected Result**: Fresh 추가 전과 같은 생성·제한 시간·보상·정산.
**Pass Criteria**: Express 제한 시간 = 6,300 + 120×D ± 편차(120), 보상 줄 구성 변화 없음.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-008-09 — 설정 데이터 확인

**Precondition**: Editor, Play 전.
**Steps**:
1. `DeliveryTypeConfig` 의 Fresh 항목에서 생성 확률·신선도 계수(`M_fresh`)·감소 시작 방식(상차 완료)·`B_fresh`·실패 페널티 계수를 기록한다.

**Expected Result**: 모두 에셋에 존재. 제안값(0.15, Express×0.5, Express 시간 보상×2, Express 실패 계수와 같음)과 비교해 기록.
**Pass Criteria**: 5개 필드 존재 = 예.
**Actual Result**:
**Pass/Fail**:
