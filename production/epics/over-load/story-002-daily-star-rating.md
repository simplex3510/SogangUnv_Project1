# Story 002: 하루 별점 피드백(3구간)

> **Epic**: over-load
> **Status**: Complete
> **Layer**: Feature
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-16

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature "하루 별점 피드백 (0~3개)" (Features 8번째 항목)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating for 6.6)
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/gdd/game-concept.md` 12.4 (2026-11-16 확정): 하루 결과는 별 0~3개, 발생 물류 대비 조건부 성공 비율 `S / N`으로 판정.*

- [ ] 발생 물류 `N`은 하루에 생성된 전체 물류(정적 + 동적)다
- [ ] 조건부 성공 `S`: Normal은 배송 완료, Express는 제한 시간 내 배송 완료다. Express 초과 완료와 미배송은 실패다
- [ ] 폐기 물류(11.6)는 실패로 센다. 폐기 전환 자체의 구현은 이 스토리 범위가 아니다
- [ ] 판정 순서: `N = 0`이면 별점 미표시("판정할 물류 없음") → 최종 정산 금액 < 0이면 별 0개 → `R = 1`이면 3개, `R ≥ T2`면 2개, `R ≥ T1`이면 1개, 그 밖은 0개
- [ ] `T2`(초기 0.7)·`T1`(초기 0.4)은 설정 데이터에 `TODO(tuning)`으로 둔다. 코드에 하드코딩하지 않는다
- [ ] 정산 화면(임시 UI) 상단 요약에 별점과 `성공 S / 발생 N`이 표시되고, 스크린샷을 `production/qa/evidence/`에 보관한다

---

## Implementation Notes

- 현행 정산은 `SettlementSystem`(`FinalSettlement = DeliveryIncome - DeliveryFailurePenalty - LateDeliveryLoss`)이고 음수·하한 없음. 별점은 이 결과 위에 얹는다.
- 구간 산식과 형태별 성공 조건은 `game-concept.md` 12.4에서 확정되었다(2026-11-16). 경계값 수치만 Story 003에서 조정한다.
- 판정 로직(N, S, 정산 금액 → 별 개수)은 순수 함수로 분리해 테스트하기 쉽게 둔다.
- 첫 릴리즈 전 개발 단계: GUI는 임시이고 기획 최종 업데이트로 이 스토리가 바뀔 수 있다.

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Story 003: 경계값 `T2`·`T1` 수치 조정
- 폐기 물류 전환(내구도 0)과 처분 장소 구현 (11.6, 별도 스토리 필요)
- 일일 목표 수익 기준 (두지 않기로 확정)

---

## Files

- `Assets/Game/Scripts/Logistics/DailyStarRating.cs` (신규: 별점 판정 순수 함수)
- `Assets/Game/Scripts/Logistics/StarRatingConfig.cs` (신규: T1·T2 설정 데이터)
- `Assets/Game/Configs/StarRatingConfig.asset` (신규: T1 0.4 · T2 0.7)
- `Assets/Game/Scripts/Logistics/SettlementSystem.cs` (S·N·별점 집계, 설정 에셋 참조)
- `Assets/Game/Scripts/UI/SettlementUI.cs` (요약에 별점·성공 S / 발생 N 표시)
- `Assets/Game/Scenes/_DevOnly/SampleMapDesign.unity` (SettlementSystem에 설정 에셋 연결, 1줄)
- [test root: Assets/Tests/EditMode/ — 면제]

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: Test: waived at qa.level minimal. 정산 화면의 retained screenshot은 `production/qa/evidence/`에 필수.

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None (작업 씬 `Assets/Game/Scenes/_DevOnly/SampleMapDesign.unity`에서 구현. Story 001은 Deferred)
- Unlocks: `production/epics/over-load/story-003-balancing.md`

---

## Completion Notes
**Completed**: 2026-11-16
**Criteria**: 5/6 passing (AC-3 폐기 물류 실패 집계는 DEFERRED: Story 005 이후 확인)
**Deviations**: None. 세션 전부터 수정되어 있던 `DeliveryDeadlineSystem.cs`·`PlayerInteractionController.cs`·`GameTimeSystem.cs`는 이 스토리의 범위 밖(OUT OF SCOPE)이다.
**Untested**: AC-2의 성공·Express 초과 경로는 실제 배송으로 실행해 보지 못했다(사용자 확인으로 통과 처리).
**Test Evidence**: waived at qa.level minimal. 정산 화면 스크린샷 `production/qa/evidence/story-002-daily-star-rating/settlement-summary.png`
**Reviewed by**: self, no independent review

## Calls beyond the design
- 별점 표시는 ★ 대신 `STARS n` 텍스트, N=0 문구는 `NO PARCELS TO RATE` (`Assets/Game/Scripts/UI/SettlementUI.cs`) · kept 2026-11-16
- 정산 요약 2줄 배치, R=1 판정은 `success >= total` 정수 비교 (`SettlementUI.cs`, `DailyStarRating.cs`) · kept 2026-11-16
- T1·T2는 `StarRatingConfig` 에셋(`Assets/Game/Configs/StarRatingConfig.asset`)에 둔다. 사용자 요청으로 Inspector 필드에서 이전 · 2026-11-16
- 설정 에셋이 없으면 `SettlementSystem`이 오류 로그를 남기고 정산하지 않는다 (`SettlementSystem.cs`) · kept 2026-11-16
- T1≥T2 경고(`OnValidate`)와 `Range(0,1)` 제한 (`StarRatingConfig.cs`) · kept 2026-11-16
