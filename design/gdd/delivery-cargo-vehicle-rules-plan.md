# 배송·물류·차량 적재 규칙 개정안 적용 계획 및 진행 추적

> **목적:** `design/gdd/delivery-cargo-vehicle-rules.md`(이하 "개정안")를 현재 프로젝트(기획서·코드·씬/에셋)에 반영하기 위한 작업 순서, 수정 범위, 선행 결정, 진행 상황을 한곳에서 추적합니다.
> 새 세션에서는 이 문서의 "현재 진행 상태"와 "6. 진행 기록"을 먼저 확인합니다.

- 문서 버전: 1.0
- 최초 작성: 2026-11-14
- 최종 갱신: 2026-11-14 (R0 완료)
- **현재 진행 상태: 7.6단계 전체 완료(R0~R6). 남은 결정: `DeliveryDeadlineConfig._baseSeconds` 값(현재 90000, 8단계), GameScene 시스템 배선(7.5c)**
- 관련 문서: `design/gdd/delivery-cargo-vehicle-rules.md`(개정안), `design/gdd/game-concept.md`(기획 정본), `design/dev-plan.md`(마일스톤 진행 추적)

### 운영 규칙

- 단계 착수 전: 해당 단계의 선행 결정이 확정되었는지 확인하고, 변경 파일 목록을 제시해 승인을 받습니다.
- 단계 완료 시: 체크박스, "현재 진행 상태", "6. 진행 기록", 최종 갱신일을 갱신합니다.
- `dev-plan.md`와의 관계: 이 작업은 `dev-plan.md`의 **7.6단계(규칙 개정 반영)** 로 취급합니다. 7.5c(게임플레이 배선)와는 파일이 거의 겹치지 않아 병행 가능하며, 8단계(밸런싱)보다 먼저 끝내야 합니다(새 튜닝 값이 생기기 때문). 각 단계 완료 시 `dev-plan.md` 진행 기록에도 한 줄 요약을 남깁니다.

---

## 1. 개정안 vs 현행 구현 차이 분석

| # | 개정안 규칙 | 현행 구현 | 차이 | 영향 규모 |
|---|---|---|---|---|
| G1 | Normal은 제한 시간 없음, Express만 제한 시간 | 모든 물류가 `DeliveryDeadlineSystem.CreateParcel`에서 제한 시간을 받고 감소·초과 판정됨 | **규칙 충돌.** 제한 시간 유무를 타입별로 분기해야 함 | 중 (로직 3곳 + UI 3곳) |
| G2 | Normal은 시간 추가 보상·시간 초과 손실 없음 | `RewardSystem.CreateReward`가 타입 무관하게 `TimeBonus`·`LateLoss` 스냅샷, `SettlementSystem`은 초과가 아니면 `TimeBonus` 지급 | Normal도 시간 보상을 받는 상태 | 소 |
| G3 | 같은 배송지 물류는 남은 제한 시간 짧은 순 처리(기획서 7.4·9.3) | `PlayerInteractionController`(하차), `LogisticsCenterUI`(그룹 내 정렬), `DeliveryNavigationCompass`(배송지 순서)가 `RemainingDeadline`으로 정렬 | 제한 시간 없는 Normal의 정렬 기준이 없음 | 소 (정렬 비교 함수 3곳) |
| G4 | 생성 방식(Static/Dynamic)을 물류 데이터로 구분 | `DeliveryGenerationSystem`이 정적/동적을 구분해 생성하지만 `Parcel`에 기록하지 않음 | 데이터 필드 누락 | 소 |
| G5 | 물류 특징 `Fragile`/`Heavy`/`Large`, 복수 부여 가능 | 없음 | 신규 enum·설정 SO·생성 시 추첨 필요 | 중 |
| G6 | 물류 `Durability` 프로퍼티 | 없음 | 신규 필드(값 보관만, 감소 규칙은 미정) | 소 |
| G7 | 차량은 적재 한도(용량) + 무게 한도 이중 판정, 둘 중 하나라도 초과 시 상차 불가 | `PlayerInteractionController._maxLoadedParcels`(개수 6) 단일 판정 | **판정 구조 교체.** "가득 참"이 차량 상태가 아니라 "이 물류가 들어가는가"로 바뀜 | 대 (상호작용·상차 UI) |
| G8 | 특징이 많을수록 보상 강화 | 없음 | `DeliveryReward`에 특징 보상 항목 추가, 정산·정산 UI 반영 | 소~중 |
| G9 | 기획서 본문 갱신(개정안 9장, 10.1장) | 기획서 6.2·7.2·7.4·8.5·9.3·10.1·11장·14.2·15장·16.4·17장이 개정 전 내용 | 문서 충돌 | 문서만 |

---

## 2. 수정 범위

### 2.1. 코드 (예상)

| 파일 | 구분 | 변경 내용 | 단계 |
|---|---|---|---|
| `Logistics/DeliveryTypeConfig.cs` | 수정 | `DeliveryTypeEntry`에 `_hasDeadline` 추가, `HasDeadline(type)` 조회 | R1 |
| `Logistics/DeliveryDeadlineSystem.cs` | 수정 | 제한 시간 없는 물류는 `Deadline` 미산정·감소 루프 제외 | R1 |
| `Logistics/Parcel.cs` | 수정 | `HasDeadline`(R1), `GenerationType`·`Traits`·`Durability`·`CapacityUsage`·`WeightUsage`(R2), `DeliveryReward.TraitBonus`(R4) | R1, R2, R4 |
| `Logistics/RewardSystem.cs` | 수정 | 제한 시간 없는 타입은 `TimeBonus`·`LateLoss` 0(R1), 특징 보상 계산(R4) | R1, R4 |
| `Logistics/SettlementSystem.cs` | 수정 | 특징 보상 집계(R4). R1은 스냅샷이 0이라 수정 불필요할 가능성 높음 | R4 |
| `Player/PlayerInteractionController.cs` | 수정 | 하차 정렬(R1), `_maxLoadedParcels` → 용량·무게 이중 판정(R3) | R1, R3 |
| `UI/LogisticsCenterUI.cs` | 수정 | Normal 행 표시·정렬(R1), 행 단위 적재 가능 판정·용량/무게 표시(R3), 타입·특징 태그(R5) | R1, R3, R5 |
| `UI/DeliveryNavigationCompass.cs` | 수정 | 배송지 정렬 기준에 Normal 처리 | R1 |
| `UI/SettlementUI.cs` | 수정 | `TRAIT BONUS` 줄 추가 | R4 |
| `Logistics/DeliveryGenerationSystem.cs` | 수정 | 생성 방식 전달, 특징 추첨·사용량 스냅샷 | R2 |
| `Logistics/DeliveryGenerationType.cs` | **신규** | `Static`/`Dynamic` enum | R2 |
| `Logistics/CargoTrait.cs` | **신규** | `[Flags]` enum `None`/`Fragile`/`Heavy`/`Large` | R2 |
| `Logistics/CargoTraitConfig.cs` | **신규** | 특징별 생성 확률·내구도·사용량·보상 설정 SO, 일반 물류 기준값 | R2 |
| `Player/VehicleCargoConfig.cs` | **신규** | 차량 적재 설정 SO: `_cargoCapacity`(적재 한도), `_weightCapacity`(무게 한도). `PlayerInteractionController`가 참조(결정 D2) | R3 |

`DeliveryAssignmentSystem`, `DeliveryTarget`, `LogisticsCenter`, `DailyOperationSystem`, `GameTimeSystem`, 이동·카메라 관련 코드, **`VehicleConfig`(주행 전용)** 는 **수정하지 않습니다.**

### 2.2. 에셋·씬 (Unity CLI로 처리)

| 대상 | 작업 | 단계 |
|---|---|---|
| `Configs/DeliveryTypeConfig.asset` | Express `_hasDeadline = true`, Normal `false` 설정 | R1 |
| `Configs/CargoTraitConfig.asset` | 신규 생성, `DeliveryGenerationSystem`·`RewardSystem` 참조 연결(GameScene, `_DevOnly` 씬) | R2 |
| `Configs/VehicleCargoConfig.asset` | 신규 생성, 적재 한도·무게 한도 값 입력 | R3 |
| 플레이어 프리팹(`Prefabs/Delivery.prefab`) | `PlayerInteractionController._cargoConfig`에 `VehicleCargoConfig.asset` 연결, `_maxLoadedParcels: 6` 직렬화 값 소멸 확인 | R3 |
| `/Canvas/LogisticsPanel` | 용량·무게 사용량 표시 텍스트 추가(필요 시) | R3 |
| `/Canvas/SettlementPanel` | 변경 없음(문자열 줄 추가만) | R4 |

### 2.3. 직렬화 주의 사항

- **`DeliveryTypeEntry._hasDeadline` 추가:** 기존 에셋은 기본값으로 채워집니다. 기본값을 `true`로 두면 기존 동작이 유지되고, Normal만 `false`로 바꾸면 됩니다(실수로 Express 제한 시간이 사라지는 것을 방지).
- **`PlayerInteractionController._maxLoadedParcels` 제거:** 프리팹에 저장된 값 `6`이 버려집니다. 새 한도 값은 `VehicleCargoConfig.asset`에서 다시 입력합니다. 이름 변경이 아니라 의미가 바뀌는 제거이므로 `[FormerlySerializedAs]`는 쓰지 않습니다.
- **`DeliveryReward` 생성자 변경:** 호출처는 `RewardSystem.CreateReward` 한 곳뿐입니다.

### 2.4. 문서

- `design/gdd/game-concept.md`: 개정안 9장 표 + 아래 항목을 반영해 버전 1.2로 갱신(R0).
  - 6.2 "파손 주의, 대형 화물 … 제외" 문구 삭제, 배송 유형/생성 방식/물류 특징 3축 분리 서술
  - 7.2·7.4·9.3: 제한 시간 감소·우선순위를 Express 한정으로 수정, Normal 정렬 기준(결정 D1) 추가
  - 8.5·10.1: 개수 기반 적재 한도 → 적재 한도 + 무게 한도 이중 판정
  - 11장: Normal은 시간 추가 보상·시간 초과 손실 없음, 특징 보상 추가
  - 14.2: `CargoTraitConfig`, `VehicleCargoConfig`(차량 적재 한도·무게 한도) 추가. `VehicleConfig`는 주행 전용으로 유지
  - 15장: "무게·부피 기반 적재 제외" → "물리 시뮬레이션은 제외, 추상 사용량 판정은 포함"으로 교체. "물류 파손 및 손상 제외"는 "내구도 프로퍼티만 보유, 감소 규칙은 미정"으로 교체
  - 16.4: 새 밸런싱 항목(개정안 10장 표) 추가
  - 17장 핵심 규칙 요약: "모든 물류는 생성 즉시 제한 시간 감소" → Express 한정
- `design/dev-plan.md`: 7.6단계 항목과 진행 기록 추가(R0), 각 단계 완료 시 갱신.

---

## 3. 작업 순서

각 단계는 단독으로 컴파일·Play 검증이 가능하도록 나누었습니다. 의존 관계는 다음과 같습니다.

```
R0 문서 정합화
 └─ R1 배송 유형(Normal 제한 시간 제거)      ← 기존 규칙과 충돌하므로 가장 먼저
     └─ R2 물류 데이터 확장(생성 방식·특징·내구도·사용량)
         ├─ R3 차량 이중 적재 한도            ← 가장 큰 변경
         └─ R4 특징 보상
             └─ R5 상차 UI 타입·특징 표시
                 └─ R6 통합 검증 및 마무리
```

R3과 R4는 서로 독립이지만, 둘 다 `Parcel`·`LogisticsCenterUI`를 건드리므로 한 번에 하나씩 진행합니다.

### R0 — 문서 정합화 `[x]`
- **목표:** 기획서 본문을 개정안 기준으로 갱신해 구현의 근거 문서를 하나로 맞춘다.
- **작업 항목**
  - [x] 4장 선행 결정 D1~D6 확정(2026-11-14)
  - [x] `game-design.md` 2.4절 목록 반영, 버전 1.2·변경 이력 추가
  - [x] `dev-plan.md`에 7.6단계와 이 문서 링크 추가
- **변경 파일:** `design/gdd/game-concept.md`, `design/dev-plan.md`, 이 문서
- **완료 기준:** 기획서 안에 개정안과 충돌하는 문장이 없음(`제한 시간`, `적재 한도`, `무게`, `부피`, `파손` 검색으로 확인)

### R1 — 배송 유형: Normal 제한 시간 제거 `[x]`
- **목표:** Normal은 제한 시간·시간 보상·시간 초과 손실이 없고, Express만 기존 규칙을 따르게 한다.
- **작업 항목**
  - [x] `DeliveryTypeEntry._hasDeadline`(기본 `true`) + `DeliveryTypeConfig.HasDeadline(type)`
  - [x] `Parcel.HasDeadline` 추가. 제한 시간이 없으면 `Deadline`·`RemainingDeadline`은 사용하지 않음
  - [x] `DeliveryDeadlineSystem`: 제한 시간 없는 물류는 산정 생략, `Update` 감소 루프에서 제외(추적 목록에는 유지 — 미배송 실패 판정에 필요)
  - [x] `RewardSystem`: 제한 시간 없는 타입은 `TimeBonus = 0`, `LateLoss = 0`
  - [x] 정렬 3곳(하차·상차 UI 그룹 내부·나침반)에 결정 D1 적용
  - [x] `LogisticsCenterUI`: Normal 행은 만료 시각 대신 `--:--`(또는 D6 결정), 제한 시간 바 숨김
  - [x] `DeliveryTypeConfig.asset`의 Normal `_hasDeadline = false`
- **선행 결정:** D1, D6
- **완료 기준:** Normal/Express 혼합 생성 로그 확인, Normal은 초과 로그가 나오지 않음, 정산에서 Normal 배송은 `TIME BONUS`·`LATE LOSS`가 0, Express는 기존과 동일

### R2 — 물류 데이터 확장 `[x]`
- **목표:** 생성 방식·특징·내구도·적재 사용량을 물류 생성 시점 스냅샷으로 저장한다(기존 "생성 시점 값 고정" 원칙 유지).
- **작업 항목**
  - [x] `DeliveryGenerationType` enum, `CargoTrait` `[Flags]` enum
  - [x] `CargoTraitConfig` SO: 일반 물류 기준값(내구도·용량 사용량·무게 사용량), 특징별 생성 확률·내구도/사용량 값·보상 값(보상 필드는 R4에서 사용)
  - [x] `Parcel`에 `GenerationType`, `Traits`, `Durability`, `CapacityUsage`, `WeightUsage` 추가
  - [x] `DeliveryGenerationSystem`: 정적/동적 구분 전달, 특징 추첨(결정 D3), 사용량·내구도 계산 후 `Parcel`에 전달
  - [x] 생성 로그에 생성 방식·특징·사용량 추가
  - [x] `CargoTraitConfig.asset` 생성 및 씬 참조 연결(GameScene, `_DevOnly`)
- **선행 결정:** D3, D4
- **완료 기준:** 생성 로그에서 `Static`/`Dynamic` 구분, 특징 조합(없음/단일/복수), Heavy·Large의 사용량 증가, Fragile의 내구도 감소 확인. 기존 상차·하차·정산 동작 불변

### R3 — 차량 이중 적재 한도 `[x]`
- **목표:** 개수 기반 한도를 적재 한도(용량) + 무게 한도 이중 판정으로 교체한다(개정안 6장).
- **작업 항목**
  - [x] 신규 `VehicleCargoConfig` SO(`_cargoCapacity`, `_weightCapacity`) 작성 — 주행 데이터(`VehicleConfig`)와 분리(D2)
  - [x] `PlayerInteractionController`: `_maxLoadedParcels` 제거, `[SerializeField] private VehicleCargoConfig _cargoConfig` 참조, 현재 사용량 합계(`UsedCapacity`, `UsedWeight`)와 `CanLoad(Parcel)` 판정 도입
    - 상호작용 가능 판정: "대기열에 실을 수 있는 물류가 하나라도 있는가"
    - 그룹·선택 상차: 다음 물류가 들어가지 않으면 그 물류에서 상차 종료(결정 D5)
    - 차단 문구: `CARGO FULL`(용량 초과) / `WEIGHT LIMIT`(무게 초과) 구분
  - [x] `LogisticsCenterUI`: 행 단위 비활성(해당 물류가 안 들어가면 비활성), 패널에 `용량 n/N · 무게 n/N` 표시
  - [x] `VehicleCargoConfig.asset` 생성·값 입력, 플레이어 프리팹에 참조 연결, 옛 필드 소멸 확인
- **선행 결정:** D2, D5
- **완료 기준:** ①용량만 초과 ②무게만 초과 ③둘 다 이내 ④둘 다 초과의 4가지 상차 결과가 개정안 6.3 표와 일치. 그룹 상차 도중 들어가지 않는 물류에서 정지, 하차 시 사용량 감소

### R4 — 물류 특징 보상 `[x]`
- **목표:** 특징이 많을수록 보상이 커지도록 스냅샷·정산에 반영한다(개정안 4.4·7.3).
- **작업 항목**
  - [x] `DeliveryReward.TraitBonus` 추가, `RewardSystem.CreateReward`가 특징 보상 계산(결정 D4)
  - [x] `SettlementSystem`: 배송 완료 물류의 `TraitBonus` 집계, `Income`에 포함. 미배송은 지급하지 않음
  - [x] `SettlementUI`: `TRAIT BONUS` 줄 추가
- **선행 결정:** D4
- **완료 기준:** 특징 0/1/2개 물류의 보상 차이가 생성 로그·정산 화면에서 일치

### R5 — 상차 UI 타입·특징 표시 `[x]`
- **목표:** 플레이어가 상차 전에 배송 유형·특징을 보고 계획을 세울 수 있게 한다.
- **작업 항목**
  - [x] `LogisticsCenterUI` 물류 행 라벨에 유형·특징 표기(예: `#7 EXP F/H  10:44`) — 한글 폰트가 없으므로 영문 약어
  - [x] 필요 시 행 오른쪽에 용량·무게 사용량 표기
- **선행 결정:** D6
- **완료 기준:** 상차 패널에서 Normal/Express와 특징 조합이 구분됨(사용자 시각 확인)

### R6 — 통합 검증 및 마무리 `[x]`
- **작업 항목**
  - [x] 09:00~18:00 1회 완주, 오류·경고 0 — GameScene은 시스템 미배선(7.5c)이라 SampleMapDesign에서 수행. 조기 종료 경로는 이번 변경과 무관해 7단계 검증 결과를 유지
  - [x] 새 수치의 `// TODO(tuning)` 표시 확인(`CargoTraitConfig`·`VehicleCargoConfig`), 기획서 16.4에 항목 추가됨(R0)
  - [x] `dev-plan.md` 7.6단계 완료 처리
- **완료 기준:** `dev-plan.md`의 마일스톤 완료 정의를 계속 만족

---

## 4. 선행 결정 목록

개정안 10장의 미결정 항목 중 **구현 구조에 영향을 주는 것**만 착수 전에 확정합니다. 순수 수치는 `// TODO(tuning)` 초기값으로 진행하고 8단계(밸런싱)에서 확정합니다.

| ID | 결정 항목 | 권장안(근거) | 관련 단계 | 상태 |
|---|---|---|---|---|
| D1 | 제한 시간이 없는 Normal의 처리 순서(상차 UI 그룹 내·하차·나침반) | Express(남은 시간 짧은 순) 먼저, 그다음 Normal(생성 순 = `Id` 오름차순). 급한 물류를 먼저 처리한다는 7.4의 의도를 유지하고, Normal끼리는 결정적인 순서가 필요함 | R1 | 확정(2026-11-14): 권장안 채택 |
| D2 | 차량 적재 한도·무게 한도 값의 위치 | 신규 `VehicleCargoConfig` SO로 분리하고 `PlayerInteractionController`가 참조. `VehicleConfig`는 주행 데이터 전용이므로 배송·물류 프로퍼티를 섞지 않음 | R3 | 확정(2026-11-14): 별도 SO `VehicleCargoConfig`, 컨트롤러에서 참조 |
| D3 | 특징 부여 방식 | 특징마다 독립 확률로 추첨(복수 부여 자연 발생). 조합 제한은 두지 않음. 개정안 4장 "여러 특징 함께 부여 가능"과 가장 단순하게 일치 | R2 | 확정(2026-11-14): 권장안 채택 |
| D4 | 사용량·보상의 산식 형태 | 사용량: 일반 기준값 + 특징별 **고정 가산**(예: 일반 용량 1·무게 1, Large 용량 +1, Heavy 무게 +1). 보상: 특징 1개당 **고정 가산**(합산). 배율보다 플레이어가 예측하기 쉽고, 개수가 많을수록 강화 규칙을 그대로 만족 | R2, R4 | 확정(2026-11-14): 권장안 채택 |
| D5 | 그룹·선택 상차 중 다음 물류가 한도를 넘을 때 | 상차를 종료(뒤의 작은 물류로 건너뛰지 않음). 기존 8.5 "한도 도달 시 자동 종료"와 동작이 같고, 처리 순서(D1)가 바뀌지 않음 | R3 | 확정(2026-11-14): 권장안 채택 |
| D6 | Normal 물류의 상차 UI 표시 | 만료 시각 대신 `--:--`, 제한 시간 바 숨김. 행 위치·크기는 그대로 유지 | R1, R5 | 확정(2026-11-14): 권장안 채택 |

다음 항목은 개정안이 명시적으로 미정으로 둔 것이며 **이번 작업에서 구현하지 않습니다.**

- 내구도 감소 조건, 내구도 0 처리, 내구도와 보상 연계(개정안 5장) → `Durability` 값 보관만
- 무게 한도 초과 적재 허용과 속도 감소(개정안 6.4)

---

## 5. 검증 방법

- 각 단계: Unity CLI로 `script recompile` → `recompile_status` → `console get --level warn`.
- 런타임: `editor.playmode start` → 생성·상차·하차·정산 로그 확인 → `editor.playmode stop`. 검증용으로 바꾼 설정값(예: 특징 확률 1.0, 작은 적재 한도)은 원복 후 저장.
- 로직 단위 테스트: 현재 테스트 어셈블리가 없으므로 기본은 Play 로그 검증입니다. R3의 이중 한도 판정은 EditMode 테스트로 분리하기 좋은 지점이므로, 원하면 R3에서 함께 제안합니다.
- 화면 표시(R3 패널 문구, R5 태그)와 조작감은 사용자가 Play에서 최종 확인합니다.

---

## 6. 진행 기록

| 날짜 | 작업 | 결과 | 다음 작업 |
|---|---|---|---|
| 2026-11-14 | 개정안·기획서·현행 코드 대조, 차이 분석(G1~G9)·수정 범위·작업 순서(R0~R6)·선행 결정(D1~D6) 정리, 본 문서 작성 | 문서만 | D1~D6 확정 후 R0 착수 |
| 2026-11-14 | 선행 결정 D1~D6 확정. D2는 권장안(`VehicleConfig` 확장) 대신 주행 데이터와 분리한 신규 SO `VehicleCargoConfig`로 변경, 나머지는 권장안 채택. 2·3장 수정 범위 갱신 | 문서만 | R0 착수(`game-design.md`·`dev-plan.md` 갱신) |
| 2026-11-14 | R0: `game-design.md` 1.2 갱신 — 용어 "배송 타입/급행" → "배송 유형/신속" 통일, 4장 생성 방식 축 명시, 6장 데이터 표·6.2 배송 유형·6.3 물류 특징·6.4 내구도 신설, 7장 제한 시간 Express 한정·7.4 처리 순서(D1), 8.1·8.2 표시·정렬(D6), 8.5 이중 한도(D5), 9.3, 10.1, 11장 특징 보상·유형별 보상, 12.2 정산 항목, 13장 UI·나침반, 14.2 `CargoTraitConfig`·`VehicleCargoConfig`, 15장 제외 범위, 16.2-5 개정·16.4 수치 항목·16.6 미결정 신설, 17장 요약 25~29. `dev-plan.md`에 7.6단계 추가 | 문서만. 충돌 키워드(`배송 타입`, `남은 제한 시간이 짧은`, `무게 및 부피`, `모든 물류는 생성`) 검색 결과 0건 | R1 착수: 변경 파일 목록 제시 후 승인 |
| 2026-11-14 | R1: `DeliveryTypeEntry._hasDeadline`(기본 true)·`HasDeadline(type)`, `Parcel.HasDeadline`·`CompareProcessingOrder`(D1), `DeliveryDeadlineSystem` 산정·감소 분기(추적은 유지), `DeliveryGenerationSystem` 전달, `RewardSystem` Normal의 `TimeBonus`·`LateLoss` 0, 하차·상차 UI·나침반 정렬을 공통 비교로 교체, 상차 UI Normal 행 `--:--`·바 숨김(D6). `DeliveryTypeConfig.asset` Normal `_hasDeadline=false`(CLI) | 컴파일 성공(경고 0). Play(SampleMapDesign): 정적 10개 중 Normal 6·Express 4, Normal은 `제한 시간 없음`·보상 `/0`, 배송지별 정렬 Express(남은 시간 순)→Normal(Id 순) 확인. Normal 1·Express 2 배송 후 강제 종료 정산 `tbEarned=100`(Express 2건만), Normal 0. 오류·경고 0. **미확인:** Express 시간 초과 경로(강제 초과 시도 시 게임 시간이 진행되지 않아 미발생), 상차 UI `--:--` 화면 표시(사용자 확인 필요). **관찰:** 현재 `DeliveryDeadlineConfig` 기준 Express 제한 시간이 약 63,000초(17.5시간)로 하루보다 길어 실제 플레이에서 초과가 나지 않음 — 8단계 튜닝 대상 | R2 착수: 파일 목록 제시 후 승인 |
| 2026-11-14 | R2: 신규 `DeliveryGenerationType`, `CargoTrait`([Flags]), `CargoSpec`(특징·내구도·사용량 스냅샷), `CargoTraitConfig`(+`CargoTraitEntry`: 확률·내구도 감산·용량/무게 가산). `Parcel`에 `GenerationType`·`Cargo`, `DeliveryDeadlineSystem.CreateParcel`·`DeliveryGenerationSystem.TryGenerateParcel(generationType)`에 전달, 생성 로그 확장. `CargoTraitConfig.asset` 생성(기준 내구도 100·용량 1·무게 1, 특징별 확률 0.2, Fragile 내구도 −50, Heavy 무게 +1, Large 용량 +1, 전부 TODO(tuning)), GameScene·DevScene·SampleMapDesign의 `DeliveryGenerationSystem._traitConfig` 연결 | 컴파일 성공(경고 0). Play: 기본 확률에서 Static/Dynamic 구분·Fragile(내구도 50)·Large(용량 2) 확인, 확률 0.6 임시 상향으로 Heavy(무게 2)·`Fragile, Heavy, Large`(내구도 50·용량 2·무게 2) 등 복수 조합 확인 후 0.2로 원복·저장. 오류·경고 0. 참고: 첫 씬 배선 시 참조가 null로 저장되어 재연결 후 씬 파일에서 GUID 저장을 확인함 | R3 착수: 파일 목록 제시 후 승인 |
| 2026-11-14 | R3: 신규 `VehicleCargoConfig`(`Game.Player.Interaction`, 적재 한도 6·무게 한도 6, TODO(tuning)) + 에셋, `Delivery.prefab`의 `PlayerInteractionController._cargoConfig` 연결(`_maxLoadedParcels` 제거). 컨트롤러: `UsedCapacity`·`UsedWeight`(적재 목록 합산), `CanLoad`·`GetLoadBlockReason`(`CARGO FULL`/`WEIGHT LIMIT`, 둘 다 넘으면 용량 우선), 물류센터 상호작용은 실을 수 있는 물류가 있을 때만, `StopLoadingIfBlocked`로 다음 물류가 넘치면 선택을 비우고 종료(D5, 상차 완료 직후와 시작 시 모두 판정). `LogisticsCenterUI`: 행 단위 비활성, 그룹은 첫 물류가 실려야 활성, 홀드 중 다음 물류가 넘치면 홀드 종료, 상단 `UsageLabel`(`CARGO n/N   WEIGHT n/N`) — 3개 씬에 생성·연결 | 컴파일 성공(경고 0). Play(검증용 물류 주입, `TickInteraction` 직접 호출): 사용량 4/4에서 (1,1)=OK, (3,1)=CARGO FULL, (1,3)=WEIGHT LIMIT, (3,3)=CARGO FULL. 선택 [A(1,1), C(2,1), B(1,1)] 홀드 → A만 적재(5/5), C에서 CARGO FULL로 종료, B는 건너뛰지 않고 대기열에 남음. 오류·경고 0. **미확인:** 실제 마우스 홀드 흐름과 `UsageLabel` 화면 배치(사용자 Play 확인 필요) | R4 착수 |
| 2026-11-14 | R4: `CargoTraitEntry._rewardBonus`(특징별 50, TODO(tuning))·`CargoTraitConfig.GetRewardBonus(traits)`, `DeliveryReward.TraitBonus`, `RewardSystem.CreateReward(..., traitBonus)` — 생성 시스템이 특징 보상을 계산해 넘기므로 새 씬 배선 없음. `SettlementSystem` 배송 완료분 `TraitBonusTotal` 집계·`Income` 포함, `SettlementUI`에 `TRAIT BONUS` 줄 | 컴파일 성공(경고 0). Play(확률 0.6 임시): 특징 0개=0·1개(Large)=50·3개=150, 세 물류 배송 후 종료 정산 `TraitBonusTotal=200`(미배송분 600 제외), `Income 1690 = 300+1140+50+200`, 정산 상세 7줄 높이 176/240으로 넘침 없음. 확률 0.2 원복·저장. 오류·경고 0 | R5 착수 |
| 2026-11-14 | R5: `LogisticsCenterUI.BuildParcelText` — 행 표기를 `#번호 유형(EXP/NRM) 특징(F/H/L 자리, 없으면 -) C용량 W무게  만료시각`으로 변경, 고정폭 태그(`<mspace=0.62em>`)로 열 정렬(정산 화면과 같은 방식) | 컴파일 성공(경고 0). Play(확률 0.6 임시): 행 15개 모두 1줄·폭 278/486px로 넘침 없음, Express는 바 표시·Normal은 `--:--`·바 숨김, `UsageLabel` `CARGO 0/6   WEIGHT 0/6`. 게임 뷰 캡처(카메라+오버레이)로 패널 배치 확인 — 사용량 라벨과 CLOSE 버튼 겹침 없음. 확률 0.2 원복. 오류·경고 0. **관찰:** Express 만료 시각이 `26:34`처럼 24시를 넘음 — R1에서 기록한 `DeliveryDeadlineConfig` 과대값 때문(8단계 튜닝) | R6 통합 검증 |
| 2026-11-14 | R6 통합 검증 + 제한 시간 바 조사. **바 조사:** 사용자 보고("시작 시 제한 시간 바가 안 줄어 보임") 측정 — 바는 실제 1초당 약 1% 감소(0.971→0.927→0.883, 4.6초 간격)로 동작은 정상이나 매우 느림. 원인은 `DeliveryDeadlineConfig.asset`의 `_baseSeconds`가 커밋 7bec53f(2026-11-12)에서 9000→90000으로 바뀐 값(코드 기본값 900)으로, Express 제한 시간이 약 63,000 게임초(17.6시간)라 하루(9시간)보다 김. 코드 결함 아님 — 값 확정은 사용자 결정 대기. **R6:** GameScene에는 배송지·`DailyOperationSystem`·정산 UI가 없어(배송지 0개) 하루 운영 불가 → 7.5c 범위로 남기고 시스템이 모두 배선된 SampleMapDesign에서 수행. 검증용 `_baseSeconds` 9000으로 임시 변경 → Express 2·Normal 2 상차(용량 4/4), 시작 직후 Express #1 시간 내 배송, 12초 후 초과된 Express #4·Normal #2 배송, Normal #5는 실은 채 미배송, 18:00 자동 종료. `[하루 운영] 종료: 영업 종료 시각 도달`·정산 패널 표시. 정산 결과와 물류별 스냅샷 재계산이 전 항목 일치: 배송 3·미배송 5, 기본 300, 거리 1307.4, 시간 보상 50(#1만), 시간 미지급 50(#4), 특징 50(#4 Fragile), 초과 손실 80(#4만, Normal은 0), 실패 600, 최종 1027.4. 정산 화면 캡처로 `TRAIT BONUS +50` 표시 확인. 오류·경고 0. 검증 후 `_baseSeconds` 90000 원복·저장, 활성 씬 SampleMapDesign 복귀(씬 변경 없음). 참고: 캡처에 상차 패널이 정산 뒤에 남은 것은 검증 중 상호작용 대상을 직접 비운 탓(실제 흐름에서는 `SetControlEnabled(false)`가 닫음) | 7.6단계 완료. 남은 결정: `_baseSeconds` 값(8단계), GameScene 배선(7.5c) |
