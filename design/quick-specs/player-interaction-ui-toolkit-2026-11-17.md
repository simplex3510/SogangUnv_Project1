# Quick Design Spec: 플레이어 상호작용 UI의 UI Toolkit 전환

**Type**: Addition
**System**: 플레이어 주변 UI (상호작용 키 안내, 진행 표시, 복구 키)
**GDD Reference**: `design/gdd/game-concept.md` 10.6·13장, `design/quick-specs/hud-layout-2026-11-17.md`
**Date**: 2026-11-17
**Backlog**: `production/ui-backlog.md` UI-01

## Change Summary

플레이어 프리팹의 Canvas UI(상호작용 아이콘, 진행 바)를 모두 UI Toolkit으로 옮긴다. 플레이어 왼쪽에는 복구 키, 오른쪽에는 상호작용 아이콘을 두고, 각각 실행할 수 있을 때만 보여 준다. 진행도는 별도 바 없이 패널 안이 아래에서 위로 차오르는 것으로 표시한다.

## Motivation

진행 바가 하단 중앙 속도계와 겹쳤다(UI-01). 또 R 아이콘이 운영 중 항상 떠 있어서 지금 필요한 조작이 무엇인지 흐려졌다. 실행 가능한 조작만 보여 주고, 진행도를 그 아이콘 안에서 보여 주면 별도 바가 필요 없다.

## Design Delta

`game-concept.md` 13장 "복구 진행 표시" 기존 내용:

> 플레이어 오른쪽 상호작용 키 안내에 R키 아이콘(임시)을 하루 운영 중 조작 가능할 때 항상 표시한다. 리셋 키를 누르는 동안 상·하차와 같은 진행 바에 복구 진행도를 표시하고, 복구 후 조작 잠금 중에는 R키 아이콘을 흐리게 표시한다

`hud-layout-2026-11-17.md` 화면 영역 표 기존 내용:

> 플레이어 오른쪽 (스크린 공간, 플레이어 화면 위치 기준) | 상호작용 키 안내 아이콘(Space, R)과 진행 바(상·하차·복구 공용)

이 명세에서 아래 New Rules로 바꾼다. 복구 실행 규칙(기획서 10.6)은 바뀌지 않는다.

## New Rules / Values

### 배치

스크린 공간 UI Toolkit. 매 프레임 플레이어의 화면 위치를 따라간다.

- 왼쪽 패널: 복구 키. 둥근 사각 키캡에 `R`.
- 오른쪽 패널: 상호작용. 둥근 사각 패널에 `Space` 글자와 종류 아이콘.
  - 상차 `box-arrow-in-down`, 하차 `box-arrow-up`, 폐기(처분 장소) `trash3-fill`, 방해물 `hand-index-thumb-fill` (Bootstrap Icons, MIT, `Assets/Game/UI/Icons/`)
  - 처분 장소는 지금 코드에서 상호작용 종류가 `Unload`라서, 대상이 처분 장소인지로 하차와 폐기 아이콘을 구분한다.
- 패널 크기와 플레이어 기준 좌우 오프셋은 USS/`[SerializeField]`(`TODO(tuning)`). px 값은 기준 해상도 1920×1080(`HudPanelSettings` Scale With Screen Size, Match 0.5) 기준이다.

### 오른쪽 상호작용 패널

- 지금 Canvas 아이콘이 켜지는 조건과 같은 조건에서 보인다(가능한 대상이 있을 때 종류 하나만). 대상이 없으면 숨긴다.
- 상차·하차·폐기·방해물 홀드 중에는 패널 안이 진행도만큼 아래에서 위로 찬다. 떼거나 끝나면 비워진다.
- 하루 종료(조작 잠금)부터 정산 화면까지는 대상 위에 있어도 숨긴다. 복구 패널과 같은 조건이다(2026-11-17 추가).
- 물류센터 창이 열려 있으면 이 패널이 창에 가려질 수 있다. 창에서 행을 홀드해 상차하는 진행은 창 안 그 행의 테두리 진행이 대신 보여 준다(2026-11-17 명시).

### 왼쪽 복구 패널

- **갇힘** 상태이면서 복구할 수 있을 때(하루 운영 중, 복구 후 잠금 아님)만 보인다.
- R은 갇힘 여부와 관계없이 지금처럼 언제든 동작한다. 갇힘이 아닐 때 R을 누르면 누르는 동안 패널이 나타난다.
- R 홀드 중에는 패널 안이 진행도만큼 아래에서 위로 찬다. 떼거나 복구가 끝나면 비워지고, 갇힘이 아니면 숨는다.
- 복구 후 잠금 중과 하루 종료 뒤에는 보이지 않는다.

### 갇힘 판정

둘 중 하나가 `T_stuck` 동안 계속되면 갇힘이다.

- 뒤집힘: 차체 위 방향과 월드 위 방향의 내적이 `minBodyUpDot`(0.5, 안전 지점 기울기 기준과 같은 값)보다 작다
- 끼임: 전진·후진 입력이 있는데 수평 속도가 `v_stuck`보다 작다

두 조건이 모두 풀리면 갇힘이 해제된다.

| 값 | 기본 | 위치 |
|---|---|---|
| `T_stuck` | 1.5초 | `VehicleRecoveryConfig`, `TODO(tuning)` |
| `v_stuck` | 0.5 m/s | `VehicleRecoveryConfig`, `TODO(tuning)` |

### 제거

플레이어 프리팹의 Canvas(종류별 아이콘 이미지, `InteractionProgressUI` 진행 바)를 없앤다. 하단 중앙에는 진행 바를 두지 않는다.

## Affected Systems

| System | Impact | Action Required |
|---|---|---|
| `PlayerUIController`, `PlayerObjectUI`, `GameObjectUI` | Canvas 아이콘을 UI Toolkit 패널로 대체 | 코드 수정, 프리팹 Canvas 제거(Editor로) |
| `InteractionProgressUI` | 진행 바를 패널 채움으로 대체 | 코드 수정 또는 제거 |
| `VehicleRecoveryController` | 갇힘 판정 추가, `KeyIconStateChanged`의 visible 의미 변경 | 코드 수정 |
| `VehicleRecoveryConfig` | `T_stuck`, `v_stuck` 추가 | 설정 추가 |
| 속도계, 물류센터 창 | 없음 | 없음 |

## Acceptance Criteria

- [ ] 상차·하차·폐기·방해물 대상이 있을 때 플레이어 오른쪽에 `Space` + 해당 종류 아이콘 패널이 하나만 보이고, 없으면 숨는다. 처분 장소에서는 하차 아이콘이 아니라 `trash3-fill`이다
- [ ] 홀드 중 그 패널 안이 진행도만큼 아래에서 위로 차고, 떼거나 끝나면 비워진다
- [ ] 정상 주행 중에는 복구 패널이 보이지 않는다
- [ ] 차량이 뒤집히거나 끼인 채 `T_stuck`이 지나면 플레이어 왼쪽에 `R` 패널이 보이고, R 홀드 중 아래에서 위로 찬다
- [ ] 정상 주행 중 R을 누르면 복구 패널이 나타나 차오르고, 떼면 숨으며, 복구 동작은 이전과 같다
- [ ] 복구 후 잠금 중과 하루 종료 뒤에는 복구 패널이 보이지 않는다
- [ ] 하루 종료 뒤 정산 화면에서 상호작용 대상 위에 있어도 Space 패널이 보이지 않는다
- [ ] 플레이어 프리팹에 Canvas UI가 남아 있지 않고, 하단 중앙 속도계와 겹치는 진행 바가 없다
- [ ] No regression: 상·하차·복구·방해물 동작과 홀드 시간, 물류센터 창 행 테두리 진행
- [ ] GUI는 임시다. 상차 대상 근처, 상차 중 채움, 갇힘 상태 R 패널, R 홀드 중 스크린샷을 `production/qa/evidence/`에 보관한다

## GDD Update Required?

Yes.

- `design/gdd/game-concept.md` 13장 "복구 진행 표시" 행을 이 명세의 복구 패널 규칙으로 바꾼다.
- `design/quick-specs/hud-layout-2026-11-17.md` 화면 영역 표의 "플레이어 오른쪽" 행과 "상호작용 키 안내" 절을 이 명세의 배치·표시 규칙으로 바꾼다.
