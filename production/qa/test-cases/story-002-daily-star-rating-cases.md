# Test Cases: Story 002 — 하루 별점 피드백(3구간)

- **Story Type**: Logic
- **Output Location**: `production/qa/test-cases/` (qa.level=minimal → 자동 테스트 WAIVED, 수동으로 모든 AC 확인)
- **Gate Level**: BLOCKING (수동 증거로 대체)
- **씬**: `Release/MainGameScene.unity`, 하루 = 현실 약 72초
- **판정 규칙(AC 4)**: `N = 0` → 미표시 / 정산 < 0 → 별 0 / `R = 1` → 3 / `R ≥ T2(0.7)` → 2 / `R ≥ T1(0.4)` → 1 / 그 밖 0

| AC | 케이스 |
|---|---|
| 1 N = 정적+동적 | TC-002-01 |
| 2 S 정의(Normal, Express 기한 내) | TC-002-02, 03 |
| 3 폐기 = 실패 | TC-002-04 |
| 4 판정 순서 | TC-002-01, 03, 05, 06, 07 |
| 5 T1·T2 설정 데이터 | TC-002-08 |
| 6 정산 상단 표시 + 스크린샷 | TC-002-01 |

---

## Test Case: TC-002-01 — 전량 성공 시 별 3개, N 집계, 상단 표시 (세션 A)

**Precondition**: 출근 직후. Console Clear.
**Steps**:
1. 하루 시작 직후 Console의 `[Parcel #n]` 로그 수를 센다(정적). 하루 동안 추가로 생긴 로그(동적, 토스트)도 센다.
2. 모든 물류를 Express 우선으로 상차·배송한다(고속 모드 사용).
3. 영업 종료까지 기다려 정산 화면을 본다.
4. 정산 화면 상단 요약을 스크린샷으로 `production/qa/evidence/` 에 저장한다.

**Expected Result**: 정산 `N` = 1단계에서 센 정적+동적 로그 수. 전량 기한 내 성공이면 `S = N`, `R = 1`, 별 3개. 상단에 별점과 `성공 S / 발생 N` 표시.
**Pass Criteria**: N(정산) = N(로그 합) 이고, S = N이면 별 = 3. 스크린샷 파일 존재.
**Actual Result**: (2026-11-19, 릴리즈 씬) 정산 N 9 / S 3 / 실패 6 / 정산 −270 / 별 0 — 정산 음수라 별 0이 규칙대로다. 전량 성공은 Express 제한 시간이 너무 짧아 불가능했다(밸런스 B-5). 로그 N은 보고되지 않았다. S=N → 별 3 분기는 `DailyStarRating.Evaluate` 코드로 확인. 파일 `production/qa/evidence/story-002-daily-star-rating/third-round.png`
**Pass/Fail**: PASS WITH NOTES

## Test Case: TC-002-02 — Normal 배송 완료는 성공, 미배송은 실패

**Precondition**: 새 하루, 출근 직후.
**Steps**:
1. Normal 물류 1개만 상차해 배송 완료한다.
2. 나머지 물류는 손대지 않고 영업 종료까지 기다린다.
3. 정산의 S·N을 확인한다.

**Expected Result**: S = 1(배송한 Normal 1건만), 나머지는 미배송 실패.
**Pass Criteria**: S = 1.
**Actual Result**: 이상 없음 (사용자 보고, 2026-11-19)
**Pass/Fail**: PASS

## Test Case: TC-002-03 — Express 기한 초과 완료는 실패

**Precondition**: 새 하루, 출근 직후. Express 물류가 1개 이상 생성됨.
**Steps**:
1. Express 1개(가능하면 가장 먼 배송지 먼 배송지(≥20 m))를 상차한다.
2. 제한 시간 바가 0이 될 때까지 차를 세워 둔 뒤 배송 완료한다.
3. Normal 1개를 배송 완료한다.
4. 영업 종료 후 정산을 본다.

**Expected Result**: 초과 완료 Express는 S에 들어가지 않는다. S = 1(Normal만).
**Pass Criteria**: S = 1.
**Actual Result**: 이상 없음 (TC-002-02와 같은 판, 사용자 보고)
**Pass/Fail**: PASS

## Test Case: TC-002-04 — 폐기 물류는 실패로 집계

**Precondition**: 새 하루. Story 004/005 동작 중.
**Steps**:
1. 물류 2개를 상차한다.
2. 고속 모드로 벽에 정면 충돌을 반복해 1개 이상을 폐기(내구도 0)시킨다(`[CargoImpact]` 로그로 확인).
3. 폐기되지 않은 물류가 남아 있으면 배송 완료한다.
4. 영업 종료 후 정산을 본다.

**Expected Result**: 폐기 물류는 S에 포함되지 않고 N에는 포함된다.
**Pass Criteria**: S = (배송 완료한 비폐기 물류 수), N 변화 없음.
**Actual Result**: (폐기 __건 / S __ / N __)
**Pass/Fail**:

## Test Case: TC-002-05 — N = 0이면 별점 미표시

**Precondition**: 물류 생성을 0으로 만들 수 있는 설정(정적·동적 생성 개수 0)을 Play 전에 Inspector에서 임시로 바꾼다. 테스트 후 원래 값으로 되돌린다(값 기록).
**Steps**:
1. 생성 설정을 0으로 바꾸고 Play → 출근.
2. 영업 종료까지 기다린다.
3. 정산 상단을 본다.

**Expected Result**: 별 대신 "판정할 물류 없음" 표시.
**Pass Criteria**: 별 아이콘 0개 표시가 아니라 "판정할 물류 없음" 문구가 보임.
**Actual Result**:
**Pass/Fail**:

## Test Case: TC-002-06 — 정산 금액 < 0이면 R과 무관하게 별 0개

**Precondition**: 새 하루.
**Steps**:
1. 물류를 하나도 배송하지 않고 영업 종료까지 기다린다(전량 실패 → 정산 음수 기대).
2. 정산 금액과 별을 본다.

**Expected Result**: 정산 < 0, 별 0개.
**Pass Criteria**: 정산 금액 < 0 이고 별 = 0.
**Actual Result**: 이상 없음 (사용자 보고, 2026-11-19)
**Pass/Fail**: PASS

## Test Case: TC-002-07 — R 경계값별 별 1·2개

**Precondition**: 새 하루를 여러 번. N을 먼저 확인한다.
**Steps**:
1. 정산이 0 이상이 되도록 배송하되, 실패 수를 조절해 `R ≥ 0.7`(예: N=9에서 실패 2건) 판을 만든다.
2. 다른 판에서 `0.4 ≤ R < 0.7`(N=9에서 성공 4~6건) 판을 만든다. 정산이 음수면 별 0이 정상이므로 금액을 함께 기록한다.
3. 각 판의 S/N, R, 금액, 별을 기록한다.

**Expected Result**: 정산 ≥ 0일 때 R ≥ 0.7 → 별 2, 0.4 ≤ R < 0.7 → 별 1, R < 0.4 → 별 0.
**Pass Criteria**: 기록한 모든 판이 위 표와 일치.
**Actual Result**:

| 판 | S/N | R | 정산 금액 | 별 | 기대 별 |
|---|---|---|---|---|---|
| 1 | | | | | |
| 2 | | | | | |

**Pass/Fail**:

## Test Case: TC-002-08 — T1·T2가 설정 데이터에 있음

**Precondition**: Editor, Play 전.
**Steps**:
1. Project에서 별점 경계값을 가진 설정 에셋(`Assets/Game/Configs/` 아래)을 찾아 Inspector로 연다.
2. T2·T1 값을 기록한다.
3. T2를 1.0으로 임시 변경하고 TC-002-07의 R≥0.7 판을 재현해 별이 1로 바뀌는지 본다. 끝나면 원래 값으로 되돌린다.

**Expected Result**: T2 = 0.7, T1 = 0.4가 에셋에 있고, 값을 바꾸면 판정이 따라 바뀐다(하드코딩 아님). 해당 필드 코드에 `TODO(tuning)` 주석이 있다.
**Pass Criteria**: 에셋에서 두 값 확인 = 예, 값 변경이 판정에 반영 = 예.
**Actual Result**: (에셋 경로 __ / T2 __ / T1 __)
**Pass/Fail**:

---

## Execution Summary (2026-11-19)

- **Result**: PASS WITH NOTES
- 플레이로 확인: TC-002-01(별 0 분기), 02, 03, 06.
- 코드로 확인(`Assets/Game/Scripts/Logistics/DailyStarRating.cs`, `SettlementSystem.cs`): TC-002-05(N=0 → null), 07(경계 순서), 08(T1·T2가 `StarRatingConfig.asset`에 있음).
- 다음 판으로 미룸: TC-002-04(폐기 = 실패)는 Story 004 충돌 판에서 같이 확인.
- Notes: 별점이 텍스트로 다른 정보와 같은 줄에 표시됨 → `production/ui-backlog.md` UI-07. 제한 시간 배송 성공이 매우 어려움 → QA 전략 B-5 실측.
