# Story 020: 표시 문구 한글 통일

> **Epic**: over-load
> **Status**: Complete
> **Layer**: Presentation
> **Type**: UI
> **Estimate**: S
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-17

## Context

**GDD**: `design/gdd/game-concept.md` 13장 (표시 문구 한국어 통일)
**Requirement**: 상세 규칙: `design/quick-specs/hud-layout-2026-11-17.md` ("문구 언어" 절)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.6.8f1 | **Risk**: NOT ASSESSED (no VERSION.md risk rating for 6.6)
**Engine Notes**: TMP 폰트는 `LiberationSans SDF`뿐이라 한글 글리프가 없다. 한글 TMP FontAsset(Dynamic)을 만들어 기본 폰트 또는 폴백으로 연결한다.

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

- [ ] uGUI(TMP) 화면에서 한글이 네모(□) 없이 표시된다
- [ ] 코드의 영문 표시 문구를 한글로 바꾼다: 차단 사유(`CARGO FULL`, `WEIGHT LIMIT`, `NO PARCEL FOR THIS TARGET`, `NO PARCEL TO DISPOSE`, `NO PARCELS TO LOAD`), 물류센터 사용량(`CARGO`/`WEIGHT`), 처분 행(`spoiled`), 정산(`FINAL`, `STARS`, `NO PARCELS TO RATE`, `SUCCESS/TOTAL`, `DELIVERED/FAILED`, 상세 항목 7개)
- [ ] 숫자·단위(`km/h`, `₩`)와 키 이름은 그대로 둔다
- [ ] 정산 상세 내역의 금액 열이 한글 항목명에서도 맞춰 보인다
- [ ] 문구 비교에 쓰던 코드(`LogisticsCenterUI`의 `WeightLimitReason` 비교 등)가 바뀐 문구에서도 동작한다
- [ ] 바뀐 화면(물류센터, 정산, 차단 문구, 처분 행) 스크린샷을 `production/qa/evidence/`에 보관한다

---

## Implementation Notes

- 폰트는 Story 006의 UI Toolkit과 같은 맑은 고딕 OS 동적 폰트를 쓴다(Windows 전용 — 기존 결정과 같은 제약).
- 문구 비교는 문자열 대신 같은 상수를 공유하도록 고친다(한 곳에서만 정의).
- 정산 열 정렬은 `PadRight` 대신 TMP 태그(`<pos=…>`)나 두 열 라벨 중 작은 쪽을 고른다.
- `DrivingModeHUD`는 Story 018에서 제거되므로 번역하지 않는다.

---

## Out of Scope

- 다국어(로컬라이제이션) 시스템
- 비 Windows 플랫폼 폰트
- 최종 GUI 디자인 (첫 릴리즈 전 임시 단계)
- 물류센터 상차 창 문구(`CARGO`/`WEIGHT`/`WEIGHT LIMIT` 표시) — Story 021 개편에서 한글로 바뀌었다. 차단 사유 문자열 자체(`GetLoadBlockReason`)는 이 스토리 범위다

---

## Files

- `Assets/Game/Scripts/Player/PlayerInteractionController.cs` — 차단 사유 한글 공유 상수 5개
- `Assets/Game/Scripts/UI/LogisticsCenterUI.cs` — 무게 초과 비교를 공유 상수로
- `Assets/Game/Scripts/UI/SettlementUI.cs` — 정산 문구 한글화, `<pos=10em>` 열 정렬
- `Assets/Game/UI/MalgunGothicTMP.asset` — 신규, 맑은 고딕 DynamicOS TMP FontAsset
- `Assets/TextMesh Pro/Resources/TMP Settings.asset` — 위 폰트를 TMP 전역 폴백에 추가
- `Assets/Game/Scenes/_DevOnly/SampleMapDesign.unity`, `DevScene.unity` — 정산 제목 "일일 정산"

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: UI
**Required evidence**: 물류센터·정산·차단 문구·처분 행의 retained screenshot (`production/qa/evidence/`)

**Status**: [x] production/qa/evidence/story-020-korean-ui-text/ (settlement·blocked-message·logistics-center·logistics-center-blocked-chips·disposal-row.png)

---

## Dependencies

- Depends on: `production/epics/over-load/story-018-hud-layout-audit.md` (과적 표시 방식이 먼저 바뀐다). 차단 사유 문구는 Story 021 토스트가 UI Toolkit 한글 폰트로 표시하므로, 020에서는 문구 문자열만 한글로 바꾼다
- Unlocks: None

## Completion Notes
**Completed**: 2026-11-17
**Criteria**: 6/6 passing
**Deviations**: ADVISORY — TMP 차단 문구가 속도계 패널 뒤로 겹쳐 일부 가려짐(위치는 이 스토리 범위 밖). OUT OF SCOPE — 작업 트리의 Fresh 배송 관련 미커밋 변경은 다른 작업
**Test Evidence**: production/qa/evidence/story-020-korean-ui-text/ (UI 스크린샷 5장; 테스트는 qa.level minimal로 면제)
**Reviewed by**: self, no independent review

## Calls beyond the design
- 차단 사유 문구: 적재 초과 / 무게 초과 / 이 배송지로 갈 물류 없음 / 처분할 물류 없음 / 실을 물류 없음 (`PlayerInteractionController.cs` 공유 상수) · kept 2026-11-17
- 정산 요약: 최종 / 별점 / 평가할 물류 없음 / 성공·전체 / 배송 완료 / 실패, 상세 8개 항목명 (`SettlementUI.cs`) · kept 2026-11-17
- 정산 제목 "일일 정산" (`SampleMapDesign.unity`, `DevScene.unity`) · kept 2026-11-17
- 금액 열 위치 `<pos=10em>` (`SettlementUI.cs` `AmountColumnEm`) · kept 2026-11-17
- 한글 TMP 폰트 맑은 고딕 90pt DynamicOS를 TMP 전역 폴백으로 연결 (`MalgunGothicTMP.asset`, `TMP Settings.asset`) · kept 2026-11-17
