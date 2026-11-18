# Accessibility Requirements

> **Status**: Accepted
> **Author**: 사용자 + ux-designer
> **Last Updated**: 2026-11-17
> **Template**: Accessibility Requirements

## Accessibility Tier Definition

**Tier: Basic** (2026-11-17 확정)

첫 릴리즈 범위에 맞춘 최소 기준이다. 정보는 색만으로 전달하지 않고, 화면 글자는 읽을 수 있는 크기를 지킨다. 키 재지정, UI 크기 조절, 동작 줄이기 옵션은 이 등급의 범위가 아니다.

---

## Visual Accessibility

- 색만으로 정보를 전달하지 않는다. 색으로 구분하는 상태에는 글자·모양·위치 중 하나를 함께 둔다.
- 게임 정보를 담은 글자는 1920×1080 기준 16px 이상이다.
- 화면 문구는 한국어로 통일한다(`design/gdd/game-concept.md` 13장). 숫자·단위·키 이름은 예외다.

---

## Motor Accessibility

- 홀드 조작(상·하차, 처분, 방해물, 복구)은 연타가 아니라 누르고 있기만 하면 된다.
- 키 재지정은 범위 밖이다(Known Intentional Limitations).

---

## Cognitive Accessibility

- 지금 할 수 있는 조작만 키 안내로 보인다(`design/ux/hud.md` HUD Philosophy).
- 알림은 동시에 3개까지만 보인다.

---

## Auditory Accessibility

- 게임 진행에 필요한 정보는 소리로만 전달하지 않는다. 현재 오디오 피드백은 없다.

---

## Platform Accessibility API Integration

범위 밖(Basic).

---

## Per-Feature Accessibility Matrix

| 기능 | 색 외 대체 | 최소 글자 크기 | 상태 |
|---|---|---|---|
| 주행 HUD | `design/ux/hud.md` Accessibility 절 | 미달 일부(13·14px) | 미해결 2건 |
| 물류센터 창 | 무게 경고는 행 경고 표시·`무게 초과` 글자 | 미달 일부(13~15px) | 확인 필요 |
| 정산 화면 | 확인 안 됨 | 확인 안 됨 | 확인 필요 |

---

## Accessibility Test Plan

- 흑백 스크린샷으로 색 구분 요소가 구분되는지 확인한다.
- 1280×720에서 16px 기준 글자가 읽히는지 확인한다.

---

## Known Intentional Limitations

- 키 재지정 없음
- UI 크기·투명도 조절 없음
- 동작 줄이기 옵션 없음
- 화면 낭독기 지원 없음

---

## Audit History

| 날짜 | 범위 | 결과 |
|---|---|---|
| 2026-11-17 | 주행 HUD 문서 (`/ux-review`) | 등급 확정 전 NOT ASSESSED → 이 문서로 등급 확정 |

---

## External Resources

- Game Accessibility Guidelines: https://gameaccessibilityguidelines.com/

---

## Open Questions

- 고속 모드 게이지의 색 외 대체 (`design/ux/hud.md` Open Questions 1)
- 16px 미만 글자 정리 (주행 HUD, 물류센터 창)
- 정산 화면 점검
