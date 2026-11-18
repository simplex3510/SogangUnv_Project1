# Quick Design Spec: 정산 닫기 후 메인 복귀

**Type**: Addition
**System**: 정산 화면(SettlementUI), 시작 화면·Esc 흐름(StartMenuUI, GameManager)
**GDD Reference**: GDD 없음. `design/game-brief.md`, `design/quick-specs/start-screen-2026-11-18.md`, `design/quick-specs/guide-panel-addition-2026-11-18.md`
**Date**: 2026-11-18
**Estimated Implementation**: 3~4시간

## Change Summary

1. 정산 카드에 `확인` 버튼을 둔다. 누르면 정산이 닫히고, 잠시 뒤 화면이 검게 페이드 아웃된 다음 씬을 다시 불러와 시작 화면으로 돌아간다.
2. 정산 화면에서 Esc를 누르면 `게임을 정지하고 퇴근하시겠습니까?` 확인창이 정산 패널보다 앞에 뜬다.

## Motivation

하루가 끝나면 정산 화면에서 더 갈 곳이 없어 게임이 멈춘 것처럼 보인다. 닫고 시작 화면으로 돌아가야 한 판의 흐름이 이어진다.

## Design Delta

- `start-screen-2026-11-18.md` Scope: "하루 종료 후 메인 메뉴 복귀는 다루지 않는다" → 이 스펙이 대체한다.
- `guide-panel-addition-2026-11-18.md` Esc 표 4행의 "정산 화면" → 확인창이 정산보다 앞에 보이는 것을 명시한다.

## New Rules / Values

### A. 정산 닫기와 복귀

1. 정산 카드 아래에 `확인` 버튼을 둔다. 정산이 뜨면 포커스가 이 버튼에 가고, Enter와 클릭으로 누른다. 정산 화면에는 다른 닫는 방법이 없다.
2. 누르면 아래 순서로 진행한다. 모든 대기는 `Time.timeScale`의 영향을 받지 않는 시간(unscaled)으로 센다.
   1. 정산 카드를 숨긴다. 버튼은 비활성화해 연타해도 한 번만 처리한다.
   2. `ReturnDelaySeconds` 동안 대기한다. 하루는 이미 끝난 상태라 게임 화면은 그대로 보인다.
   3. 화면 전체를 덮는 검은 막이 `FadeSeconds` 동안 투명에서 불투명이 된다. 막은 정산 패널(Sort Order 100) 안의 전체 화면 요소다.
   4. 검게 된 직후 현재 활성 씬을 다시 불러온다. 씬 이름은 코드에 적지 않고 `SceneManager.GetActiveScene()`을 쓴다.
3. 다시 불러온 씬은 기존 시작 화면 흐름 그대로다(MenuCam, 하루 정지, HUD 숨김, 출근 버튼). 페이드 인은 이번 범위가 아니다.
4. 복귀 시퀀스가 시작된 뒤(2-1 이후)에는 Esc 확인창을 띄우지 않는다.
5. 상태 초기화는 씬 재로드에 맡긴다. 다만 정적 상태(`GameManager.IsPaused`, `DeliveryAssignmentSystem`의 하루 할당 정보, Esc 핸들러 목록)가 남지 않는지, `ManagerBase` 싱글턴이 씬 이동 후 중복되거나 사라지지 않는지는 구현 때 확인한다.

### B. 정산 중 Esc

- 정산 화면이 열려 있을 때 Esc는 기존 규칙대로 확인창을 띄운다(게임 정지). 확인창은 정산 패널보다 앞에 그려야 하므로, 구현 때 확인창 패널의 Sort Order가 100보다 크게 되도록 맞춘다.
- `아니오`나 Esc로 닫으면 정산 화면으로 돌아오고 포커스는 `확인` 버튼에 있다. 확인창이 열린 동안에는 `확인` 버튼이 눌리지 않는다.

## Tuning Knobs

| Knob | Default | Range | Category | Rationale |
|---|---|---|---|---|
| `ReturnDelaySeconds` | 0.5 | 0–2 | feel | 정산이 닫힌 뒤 바로 검어지면 급해 보임 |
| `FadeSeconds` | 1.0 | 0.2–3 | feel | 부드럽게 보이면서 답답하지 않은 길이 |

값은 기존 `StartScreenConfig`에 둔다.

## Affected Systems

| System | Impact | Action Required |
|---|---|---|
| `SettlementUI.cs`, `SettlementWindow.uxml/.uss` | 확인 버튼, 검은 막, 복귀 코루틴 | 코드·UI 수정 |
| `StartScreenConfig` | 튜닝 값 2개 | 수정 |
| `StartMenuUI` 확인창 패널 | Sort Order가 정산보다 앞인지 확인, 확인창 중 `확인` 입력 차단 | 확인 후 수정 |
| `GameManager` 등 정적 상태 | 씬 재로드 후 잔여 상태 | 구현 때 점검 |
| `start-screen`·`guide-panel` 스펙 | 해당 줄 보강 | 이 스펙으로 대체 |

## Acceptance Criteria

- [ ] 하루가 끝나면 정산 카드에 `확인` 버튼이 보이고 포커스가 있다. Enter와 클릭 모두 동작한다.
- [ ] 확인을 누르면 카드가 사라지고, 약 0.5초 뒤 약 1초에 걸쳐 화면이 검게 된 다음 시작 화면(MenuCam, 출근 버튼)으로 복귀한다. 페이드 중간 스크린샷을 `production/qa/evidence/`에 남긴다.
- [ ] 확인을 연타해도 씬 로드는 한 번만 일어난다.
- [ ] 복귀한 시작 화면에서 출근 → 하루 진행 → 정산 → 복귀가 두 번 연속 정상 동작한다(물류·시계·차량 위치·BGM이 처음 상태).
- [ ] 정산 화면에서 Esc를 누르면 확인창이 정산 카드보다 앞에 뜨고 게임이 정지한다. `아니오`/Esc로 닫으면 정산 화면과 `확인` 포커스로 돌아온다.
- [ ] 확인창이 떠 있는 동안 `확인` 버튼 입력이 무시된다. 복귀 시퀀스 중에는 Esc 확인창이 뜨지 않는다.
- [ ] 기존 기능 유지: 정산 내용·별점 표시, 정산 중 BGM 페이드, 퇴근 버튼과 평소 Esc 동작.

## GDD Update Required?

No. 시작 화면·정산 흐름은 GDD가 없고, 이 스펙이 위 두 스펙의 해당 부분을 대체한다.
