# Story 011 Play mode 관찰 — 2026-11-17

> **[PRE-FIX]** 아래 "환경"부터 "5. 확인하지 못한 것 / 한계"까지는 프리팹 스케일 수정 **전**(pre-fix) 관찰이다. 스크린샷 01·02-before·02·02a·03·03b·04는 문서 끝 "재측정" 때 덮어썼으므로, 4장 표의 설명은 지금 파일과 맞지 않는다(00-initial.png만 이전 그대로).

> **Story Type**: Integration | **Evidence 위치**: `production/qa/evidence/story-011-catalog-obstacle-prefabs/` | **Gate**: BLOCKING (스크린샷 보관), 자동 테스트는 qa.level minimal로 면제

## 환경

- 씬: `Assets/Game/Scenes/_DevOnly/SampleMapDesign.unity` (Unity 6000.6.8f1, Play mode). 씬은 저장하지 않았다. 종료 후 `isDirty=False`.
- 시작 시 Editor 상태가 `playing`으로 보고됐지만 `Time.frameCount=1`, `isPlaying=False`였다(멈춘 세션이 곧 정지됨). 그래서 내가 새로 Play mode에 들어갔다(`editor set_auto_tick`, `set_run_in_background` 켜고 `job wait`로 frameCount 증가 확인). 끝난 뒤 Play mode 종료, 두 설정 모두 원래대로 끔.
- 하루 영업이 약 55초 후 끝나면 `DailyOperationSystem.IsOperating=false`가 되어 `ObstacleModule.CanExecute`와 차량 효과 요청이 모두 막힌다. 첫 콘 시도는 이 때문에 효과가 실행되지 않았다(아래 "무효 시도"). 이후에는 eval마다 `GameTimeSystem._currentSeconds`를 영업 시작 시각으로 되돌려 하루를 유지했다(Play mode 런타임 값만, 에셋 변경 없음).

## 방법

- `unity command script eval`로 진행.
  - Player(`PlayerMovementController`, 질량 100, 콜라이더 약 0.40×0.44×0.96 m)를 (-0.88, 0, -26.24), 회전 0으로 되돌린다.
  - `_loadedParcels`를 비우고 시험 물류 1개(Normal, 특징 None, 내구도 100, ID 9xxx)를 적재한다.
  - `AssetDatabase.LoadAssetAtPath` + `Object.Instantiate`로 프리팹을 차량 정면에 놓는다. 가장 가까운 면이 차량 앞면에서 0.1 m 떨어진 위치다.
  - 차량 `Rigidbody.linearVelocity = forward × 5`. 입력이 없어서 관성 감속(4 m/s²)이 걸리므로 접촉 순간 속도는 약 4.9 m/s다.
- 효과·수명 타이밍은 `EditorApplication.update` 콜백 모니터로 측정했다. 매 프레임 `Time.time`, 메시 이름, 오브젝트 존재 여부, `_obstacleSpeedEffect`(SpeedEffectState) 상태를 읽어 `[QA]` 로그로 남겼다.
- ROADWORK 상호작용: 차량을 Barrier 트리거 구역 안으로 옮겼다. 플레이어 `PlayerInteractionController`가 Barrier를 현재 상호작용 대상으로 잡은 것을 확인한 뒤, 키 유지 대신 공개 API `ic.TickInteraction(deltaTime)`을 매 프레임 호출해 3초 유지를 흉내 냈다(실제 플레이어 경로 `TickObstacle → Interact`를 탄다).
- 프리팹 구성은 Edit/Play mode에서 `AssetDatabase`로 로드해 컴포넌트·콜라이더·Rigidbody·Config 값을 읽었다(읽기만 함).

## 1. 프리팹 구성 (AC-1, AC-5, AC-6)

| 프리팹 | 관찰한 구성 | 카탈로그 2장 대비 |
|---|---|---|
| Cone | 루트 Rigidbody m=5 비키네마틱, ObstacleModule, CollisionObstacleTrigger, SpeedMultiplierEffect(ObstacleCone ×0.8 / 1초), SwapMeshEffect(→`cone-flat`), ObstacleLifetime RemoveAfterLinger(10초). Model: MeshFilter `cone`, MeshCollider convex, 재질 ObstacleNoBounce(b=0) | 일치 |
| Box | Rigidbody m=15, CollisionObstacleTrigger, 효과 없음, Lifetime RemoveAfterLinger(ObstacleBox 15초), BoxCollider ObstacleNoBounce | 일치 |
| Dumpster | Rigidbody m=80, 트리거·효과·Lifetime 없음(유지), MeshCollider 3개 convex ObstacleNoBounce | 일치 |
| DebrisS | BoxCollider isTrigger, TriggerEnterObstacleTrigger, HeadingRotationEffect(yaw 15, 무작위 부호), Lifetime RemoveImmediately, 메시 `debris-bolt` | 일치 |
| DebrisL | BoxCollider isTrigger, TriggerEnterObstacleTrigger, SpeedMultiplierEffect(×0.6 / 2초), RemoveImmediately, 메시 `debris-tire` | 일치 |
| Roadwork | 루트 아래 Barrier / Fence / Light. Barrier: isTrigger 구역 BoxCollider, InteractionObstacleTrigger(duration 3), RemoveGroupEffect, RemoveImmediately. 각 Model은 고정 MeshCollider(비convex, Rigidbody 없음) | 일치. 프리팹 직렬화 값은 `_interactionType=None`이지만 Awake에서 Obstacle로 고정되고, 런타임에 `type=Obstacle`을 확인했다 |
| RoadSign | 고정 MeshCollider(`road-sign-stop`), 효과 없음 | 일치(OBJ-FIXTURE) |
| OverheadWires | 콜라이더 0개, 렌더러 높이 약 0.82~0.94 m(차량 지붕 약 0.50 m보다 높음) | 일치 |
| Fence | `fence` BoxCollider 고정, 효과 없음, 물리 재질 없음 | 일치(효과 없는 고정 콜라이더) |
| StreetLight | `light-square` MeshCollider 고정, 효과 없음, 물리 재질 없음 | 일치(OBJ-FIXTURE) |
| Tree | `tree-large` BoxCollider 고정, 효과 없음, 물리 재질 없음 | 일치 |

## 2. 원본 콘솔 로그 (발췌, 순서대로)

### 무효 시도 (하루 종료 후, 효과 미실행)
```
[하루 운영] 종료: 영업 종료 시각 도달
[CargoImpact] impulse=27.93 mass=100 → deltaVelocity=0.28
[CargoImpact] deltaVelocity=0.28 < 하한선 1.5 → 무시
```
→ 콘은 밀렸지만 메시 `cone` 유지, 속도 효과 없음. `IsOperating=false` 잠금이 정상 동작한 결과다. 판정에는 쓰지 않았다.

### CONE (하루 운영 중)
```
[QA] t=55.37 launch v=5 toward Cone, durability before=100
[CargoImpact] impulse=27.93 mass=100 → deltaVelocity=0.28
[CargoImpact] deltaVelocity=0.28 < 하한선 1.5 → 무시
[QA] t=55.53 Cone mesh=cone-flat
[QA] t=55.53 speedFx active=True mul=0.8 maxFwd=2.4
[QA] t=56.42 speedFx active=False mul=1 maxFwd=3
[QA] t=65.40 Cone destroyed
```
첫 시도(같은 세션, 수동 폴링)에서도 `meshBefore=cone` → 충돌 후 `cone-flat`, `fxMul=0.8 maxFwd=2.4`를 확인했다.

### DEBRIS-S
```
[QA] t=97.99 launch v=5 toward DebrisS, yaw before=0.00 durability before=100
[QA] t=98.14 DebrisS destroyed, vehicle yaw=345.00 dur=100
```
추가 3회 반복: 차량 yaw=15.0 (3회 모두). 두 부호(−15°, +15°)가 모두 나왔다.

### DEBRIS-L
```
[QA] t=114.16 launch v=5 toward DebrisL, durability before=100
[QA] t=114.28 DebrisL destroyed, vehicle yaw=0.00 dur=100
[QA] t=114.28 speedFx active=True mul=0.6 maxFwd=1.8
[QA] t=116.28 speedFx active=False mul=1 maxFwd=3
```

### ROADWORK 상호작용
```
barrier.CanInteract=True type=Obstacle duration=3 ic.current=Barrier isBarrier=True detectedCount=1 ic.CanInteract(bar)=True children=3
[QA] t=203.16 start simulated hold (ic.TickInteraction per frame), children=3
[QA] t=203.41 hold elapsed=0.25 progress=0.08
[QA] t=204.16 hold elapsed=1.00 progress=0.33
[QA] t=204.92 hold elapsed=1.76 progress=0.59
[QA] t=205.67 hold elapsed=2.51 progress=0.84
[QA] t=206.17 hold elapsed=3.01 Roadwork children=0 []
[QA] t=206.17 Barrier destroyed, elapsed=3.01
```

### 내구도 (고속 5 m/s 정면 충돌, 시험 물류 Normal 100)
```
Dumpster #9201  impulse=241.38 → Δv=2.41  damage=10.97  100 → 89.03
Dumpster #9311  impulse=253.49 → Δv=2.53  damage=12.42  100 → 87.58
Dumpster #9301  impulse=188    → Δv=1.88  damage=4.56   100 → 95.44
Dumpster #9321  impulse=188    → Δv=1.88  damage=4.56   100 → 95.44
Dumpster #9331  impulse=185.54 → Δv=1.86  damage=4.27   100 → 95.73
RoadSign #9202  impulse=483.18 → Δv=4.83  damage=39.98  100 → 60.02
RoadSign #9302  impulse=490.74 → Δv=4.91  damage=40.89  100 → 59.11
StreetLight #9304 impulse=331.43 → Δv=3.31 damage=21.77 100 → 78.23  (바로 다음 프레임 impulse=47.23 Δv=0.47 → 쿨다운 중 재접촉 무시)
Roadwork #9323  impulse=341.74 → Δv=3.42  damage=23.01  100 → 76.99  (다음 프레임 Δv=0.3 쿨다운 무시, 차량 yaw 352.9로 비껴 맞음)
Roadwork #9333  impulse=341.74 → Δv=3.42  damage=23.01  100 → 76.99  (위와 동일)
Box #9205       impulse=78.59  → Δv=0.79  < 하한선 1.5 → 무시   (100 유지)
Box #9315       impulse=79.14  → Δv=0.79  < 하한선 1.5 → 무시   (100 유지)
Cone #9206      impulse=18.87  → Δv=0.19  < 하한선 1.5 → 무시   (100 유지)
Cone #9316      impulse=30.65  → Δv=0.31  < 하한선 1.5 → 무시   (100 유지)
Fence #9207     impulse=350.45 → Δv=3.5   damage=24.05  100 → 75.95   (카탈로그 수치 없음, 참고)
Tree #9208      impulse=369.27 → Δv=3.69  damage=26.31  100 → 73.69   (카탈로그 수치 없음, 참고)
```
(무효 측정: StreetLight #9204 Δv=1.43과 Roadwork #9203/#9303/#9313은 배치 계산이 트리거 구역이나 머리 위 암을 기준으로 잡혀 정면 몸체에 맞지 않았다. 판정에서 뺐다.)

부수 오류: 피해가 적용될 때마다 `NullReferenceException at Game.UI.DurabilityHud.RefreshCargoPanel () (DurabilityHud.cs:310)`이 났다. 시험 물류의 `Target`이 null이라 `parcel.Target.name`에서 터진 것이다. 시험 방법 때문에 생긴 오류이며, 실제 생성 물류에는 배송지가 있다. 내구도 감소 로그는 예외 전에 이미 남아 판정에는 영향이 없다.

## 3. 항목별 결과

| AC | 항목 | 결과 | 근거 |
|---|---|---|---|
| AC-1 | 7종 프리팹이 Story 010 컴포넌트 조합만으로 구성되고, 물리 형태·트리거·효과·수명·질량이 2장 표와 같음 | **확인** | 1장 표. 효과 수치는 Config SO 값을 읽어서 확인 |
| AC-2 | CONE: `cone-flat` 교체, ×0.8 1초, 10초 후 제거 | **확인** | 메시 교체 t=55.53. maxFwd 3 → 2.4가 t=55.53~56.42 동안 유지(관찰 해상도 ~0.1초, 충돌 시각 ≈55.4 기준 약 1초). 제거 t=65.40(충돌 후 ≈10초). 스크린샷 01 |
| AC-3 | DEBRIS-S ±15°, 즉시 제거, 내구도 불변 | **확인** | yaw 0 → 345(−15°), 반복 시 +15°. 진입 프레임에 제거. 내구도 100 유지 |
| AC-3 | DEBRIS-L ×0.6 2초, 즉시 제거, 내구도 불변 | **확인** | maxFwd 3 → 1.8, t=114.28~116.28(2.00초). 같은 프레임에 제거, 내구도 100. 스크린샷 02-before / 02 |
| AC-4 | ROADWORK Barrier 3초 상호작용 → 같은 그룹 차단물 모두 제거 | **확인(조건부)** | 플레이어 상호작용 컨트롤러가 Barrier를 현재 대상으로 잡았고(`CanInteract=True`), 3.01초 후 Fence·Light·Barrier가 모두 사라짐(children 3 → 0). 단 실제 키 입력이 아니라 `TickInteraction`을 직접 호출했다 |
| AC-5 | 머리 위 모델 콜라이더 없음, 표에 없는 정적 후보는 효과 없는 고정 콜라이더 | **확인** | OverheadWires 콜라이더 0. Fence·Tree는 고정 콜라이더이고 효과 컴포넌트가 없으며, 충돌 시 속도 효과 없이 내구도만 줄었다. `road-sign-empty-hanging` 프리팹은 없어서 확인하지 않음 |
| AC-6 | Props 6종(Cone, Box, Dumpster, Fence, StreetLight, Tree)이 카탈로그와 일치 | **확인** | 1장 표 |
| AC-7 | 고속 정면 충돌 내구도가 카탈로그 ±20% 이내 | **부분 확인 / 일부 미충족** | 아래 표 |
| AC-8 | 동작 스크린샷 보관 | **확인** | 아래 파일 목록 |

### AC-7 상세 (카탈로그 수치는 변경하지 않음)

| 대상 | 카탈로그 (고속) | ±20% 범위 | 실측 | 판정 |
|---|---|---|---|---|
| Cone | 0 | 0 또는 하한선 미만 | Δv 0.19~0.31 → 0 | 범위 안 |
| Box | 0 | 0 또는 하한선 미만 | Δv 0.79 → 0 | 범위 안 |
| Dumpster | −9 | −7.2 ~ −10.8 | −10.97, −12.42, −4.56, −4.56, −4.27 (평균 −7.36) | **5회 모두 범위 밖**. 같은 배치에서도 4.3~12.4로 흩어지며 결과가 결정적이지 않다 |
| RoadSign (FIXTURE) | −42 | −33.6 ~ −50.4 | −39.98, −40.89 | 범위 안 |
| StreetLight (FIXTURE `light-*`) | −42 | −33.6 ~ −50.4 | −21.77 (1회) | **범위 밖**. 충격량이 두 물리 프레임으로 나뉘었고(3.31 + 0.47), 둘째 프레임은 쿨다운에 걸려 버려졌다 |
| Roadwork | −42 | −33.6 ~ −50.4 | −23.01 (2회, 동일) | **범위 밖**. 비껴 맞음: Barrier 폭 0.26 m가 차폭 0.40 m보다 좁아서 차량이 Fence 모서리에 걸리고 7° 돌았다. 깨끗한 정면 충돌을 만들지 못했다 |
| Fence / Tree | (수치 없음) | — | −24.05 / −26.31 | 참고 |

## 4. 스크린샷 (Read 도구로 직접 확인한 내용)

| 파일 | 실제로 보이는 것 |
|---|---|
| `00-initial.png` | Play mode 첫 화면. 초록 트럭, 앞쪽 도로 갈림길, 좌상단 시각 HUD, 하단 속도계 0 km/h. 빈 화면이나 정지 화면 아님 |
| `01-cone-knocked.png` | 트럭 바로 앞에 주황색 넓은 받침 위로 납작한 원뿔(`cone-flat`)이 보인다. 콘이 트럭 차체보다 크다 |
| `02-debris-before.png` | 트럭 바로 앞 도로 경계에 큰 검은 타이어(DebrisL)가 세워져 있다 |
| `02-debris.png` | 같은 위치에서 타이어가 사라지고 트럭이 도로 위로 약간 전진했다. 속도계 0 |
| `02a-debris-s-after.png` | DebrisS 통과 직후 트럭과 카메라가 왼쪽으로 15° 돌아가 도로가 기울어 보인다. 속도계 22 km/h, 잔해 없음 |
| `03-roadwork-before.png` | 트럭 앞에 작은 공사 펜스·바리케이드·경고등 3개가 나란히 서 있다. 상호작용 프롬프트 아이콘은 화면에서 확인되지 않음 |
| `03b-roadwork-holding.png` | 같은 장면에 하단 속도계 뒤로 가로 진행 막대(초록, 일부 채워짐)가 겹쳐 그려져 있다 |
| `04-roadwork-after.png` | 공사 오브젝트 3개가 모두 사라지고 도로가 비어 있다 |

## 5. 확인하지 못한 것 / 한계

- 실제 키보드 입력(상호작용 키 3초 유지, 실제 주행)으로는 해 보지 않았다. 충돌은 속도를 직접 넣어서 만들었고, 상호작용은 `TickInteraction` 호출로 대신했다.
- CONE 감속 1초는 모니터 해상도(Editor update 간격 ~0.1초) 안에서만 맞다. 충돌 프레임 시각을 직접 기록하지는 못했다(첫 모니터 프레임이 이미 `cone-flat`).
- Dumpster·StreetLight·Roadwork의 −9 / −42 수치는 범위 안에서 재현하지 못했다. 원인 가설: (a) 동적 Dumpster 접촉의 비결정성, (b) 충격량이 여러 프레임으로 나뉠 때 둘째 프레임이 쿨다운에 버려짐, (c) Roadwork 정면 폭이 좁음. 원인은 검증하지 않았다. AC-7은 "벗어나면 카탈로그 갱신"을 요구하지만 지시대로 수치는 바꾸지 않았다.
- `road-sign-empty-hanging` 프리팹은 Props에 없어서 머리 위 표지판은 확인하지 않았다(OverheadWires만 확인).
- MapPainter `Props` 카테고리 등록(Implementation Notes)은 확인하지 않았다.
- 이 스토리 범위 밖에서 눈에 띈 점: Cone(0.89×1.12 m)과 DebrisL 타이어(1.13 m)가 차량(0.40×0.44×0.96 m)보다 크고, Roadwork 부품(폭 0.14~0.26 m)은 매우 작아서 프리팹 간 축척이 맞지 않는다. 03b에서는 상호작용 진행 막대가 속도계와 겹친다.

---

## 재측정 (스케일 수정 후, 2026-11-17)

> 프리팹 스케일 수정(Vehicles 키트 소품 scale 0.25, Roadwork 부품을 로컬 Z로 이어 붙인 벽, 상호작용 구역 패딩 0.4) 이후 같은 방법으로 다시 관찰했다. Assets 파일·카탈로그 수치는 바꾸지 않았고 씬은 저장하지 않았다.

### 환경

- 시작 시 Editor는 `playMode: stopped`, `runInBackground: false`. 내가 Play mode에 들어갔다. `editor set_auto_tick`은 이미 켜져 있었다("already enabled", 간격만 16 ms로 갱신). 원래 켜져 있던 설정이라 끄지 않았다. Run In Background는 켰다가 종료 후 `--enabled false`로 프로젝트 값(off)으로 되돌렸다.
- `job wait`(frameCount changed): `met=true`, 550 → 578, framesObserved=2. 멈춤 없음.
- 하루 종료 잠금 회피: 매 eval과 모니터 콜백에서 `GameTimeSystem._currentSeconds`가 영업 시작+600초를 넘으면 시작 시각(32400)으로 되돌렸다(런타임 값만). 이번 세션에서 영업 종료는 한 번도 발생하지 않았다.
- 종료 후 `isDirty=False`, `isPlaying=False`, `Application.runInBackground=False`, 임시 카메라 `QACam` 없음을 확인했다.

### 방법 (이전과 같음, 달라진 점만)

- 차량: (-0.88, 0, -26.24), 회전 identity, 속도·각속도 0으로 되돌린 뒤 시험 물류 1개(Normal, None, 내구도 100, ID 94xx~96xx)를 `_loadedParcels`에 넣는다.
- 측정한 크기(월드 bounds, 런타임): 차량 비트리거 콜라이더 7개, 폭 0.39, 앞면 z=-25.721, 가장 낮은 면 y=0.060. 도로 표면 y=0.030(Raycast). Cone 0.12×0.15×0.12, Box 0.18 정육면체, DebrisS 트리거 0.04×0.07×0.05, DebrisL 트리거 0.09×0.15×0.15(중심이 피벗이라 절반이 도로 아래), Roadwork 부품 Barrier 0.25×0.24×0.42 / Fence 0.14×0.34×0.70 / Light 0.14×0.44×0.14, Barrier 상호작용 트리거 1.05×0.24×1.22.
- 배치: 프리팹을 도로 표면 높이(Raycast)에 놓고, 비트리거 콜라이더(없으면 트리거)의 가장 가까운 면이 차량 앞면 +0.1 m가 되게 z를 맞췄다. x는 차량 중심과 같다.
  - Roadwork는 yaw 90°: 그룹 로컬 X가 월드 −Z가 되어, 차량이 그룹 로컬 X 방향으로 넓은 면(1.27 m 벽)에 정면 충돌한다. 차량 폭 0.38 전체가 Barrier 모델(로컬 Z 폭 0.42) 안에 들어온다.
  - StreetLight는 yaw 90°: 머리 위 팔이 옆(−X)으로 가고 기둥 정면에 맞는다.
  - Cone·Box·Dumpster·RoadSign은 yaw 0.
- 속도 `linearVelocity = forward × 5`. 이전 QA 오브젝트 정리는 `DestroyImmediate`로 했다(처음엔 `Destroy`라서 Dumpster #9542 배치용 Raycast가 지워지기 전의 이전 Dumpster를 맞아 groundY=0.385로 떠 있었다. 무효 처리).
- 스크린샷: 01·02-before·02·03·04는 런타임에 추가한 비활성 카메라 `QACam`(FOV 45, 차량 앞 왼쪽 위에서 앞범퍼 앞 0.55~0.6 m를 바라봄)으로 `capture game_view --source camera --camera QACam --overlay false` 촬영(HUD 없음). 02a·03b는 게임 화면(`--source screen`, Cinemachine 카메라 거리 4, HUD 포함). Cinemachine 설정을 잠시 바꿔 봤으나 트럭이 콘을 가려서 원래 값(거리 4, RotationComposer offset (0,−1,1))으로 되돌렸다.

### 원본 콘솔 로그

#### CONE (AC-2)
```
[QA] t=99.86 launch v=5 toward Cone #9401 yaw before=0.00 dur before=100 mesh=cone gap=0.1 groundY=0.030
[CargoImpact] impulse=70.54 mass=100 → deltaVelocity=0.71
[CargoImpact] deltaVelocity=0.71 < 하한선 1.5 → 무시
[QA] t=100.07 (+0.22) obj=cone-flat fx=True mul=0.8 maxFwd=2.4 yaw=0.9 dur=100 v=3.03
...
[QA] t=100.88 (+1.02) obj=cone-flat fx=True mul=0.8 maxFwd=2.4 yaw=2.5 dur=100 v=0.01
[QA] t=100.90 (+1.04) obj=cone-flat fx=False mul=1 maxFwd=3 yaw=2.5 dur=100 v=0.01
...
[QA] t=105.96 (+6.10) obj=cone-flat fx=False mul=1 maxFwd=3 yaw=3.4 dur=100 v=0.00
[QA] t=109.88 (+10.02) obj=destroyed fx=False mul=1 maxFwd=3 yaw=3.4 dur=100 v=0.00
```
반복 #9402: 첫 모니터 프레임(+0.19)에 이미 `cone-flat`·fx=True, +1.02까지 fx=True. #9403~#9406도 같은 충격량 70.54 → 무시, `cone-flat`.
(충돌은 간격 0.1 m / 5 m/s ≈ +0.02초. 모니터 첫 프레임이 +0.19~0.22라 충돌 프레임 자체는 못 봤다.)

#### DEBRIS-S (AC-3)
```
[QA] t=276.64 launch v=5 toward DebrisS #9421 yaw before=0.00 dur before=100 mesh=debris-bolt
[QA] t=276.79 (+0.15) obj=destroyed fx=False mul=1 maxFwd=3 yaw=345.0 dur=100 v=3.96
[QA] t=280.89 launch v=5 toward DebrisS #9422 ...
[QA] t=281.02 (+0.13) obj=destroyed fx=False mul=1 maxFwd=3 yaw=15.0 dur=100 v=4.06
[QA] t=285.17 launch v=5 toward DebrisS #9423 ...
[QA] t=285.31 (+0.14) obj=destroyed fx=False mul=1 maxFwd=3 yaw=345.1 dur=100 v=3.98
[QA] t=289.41 launch v=5 toward DebrisS #9424 ...
[QA] t=289.54 (+0.13) obj=destroyed fx=False mul=1 maxFwd=3 yaw=345.0 dur=100 v=3.96
[QA] t=303.22 launch v=5 toward DebrisS #9425 ...
[QA] t=303.38 (+0.16) obj=destroyed fx=False mul=1 maxFwd=3 yaw=15.0 dur=100 v=3.86
```
5회 모두 트리거 발동. `[CargoImpact]` 로그 없음(트리거라 충격 없음). 높이: DebrisS 윗면 ≈ 0.030+0.065 = 0.095, 차량 최저면 0.060. 겹침이 약 0.035 m로 작지만 발동한다.

#### DEBRIS-L (AC-3)
```
[QA] t=256.12 launch v=0 toward DebrisL #9411 ... mesh=debris-tire gap=0.1 groundY=0.030
[QA] t=256.39 (+0.27) obj=debris-tire fx=False mul=1 maxFwd=3 yaw=0.0 dur=100 v=0.01
[QA] t=257.24 GO v=5 yaw=0.00
[QA] t=257.29 (+1.17) obj=destroyed fx=True mul=0.6 maxFwd=3 yaw=0.0 dur=100 v=4.84
[QA] t=257.30 (+1.18) obj=destroyed fx=True mul=0.6 maxFwd=1.8 yaw=0.0 dur=100 v=4.76
[QA] t=259.30 (+3.18) obj=destroyed fx=False mul=1 maxFwd=3 yaw=0.0 dur=100 v=0.00
```
(정지 상태로 배치해 02-debris-before를 찍은 뒤 출발시켰다.)

#### ROADWORK 상호작용 (AC-4)
```
barrier.CanInteract=True type=Obstacle duration=3 ic.current=Barrier isBarrier=True detectedCount=1 ic.CanInteract(bar)=True children=3 [Barrier,Fence,Light] rwRotY=90
[QA] t=352.70 start simulated hold (ic.TickInteraction per frame), children=3
[QA] t=352.95 hold elapsed=0.25 progress=0.08
[QA] t=353.70 hold elapsed=1.00 progress=0.33
[QA] t=354.45 hold elapsed=1.75 progress=0.58
[QA] t=355.20 hold elapsed=2.50 progress=0.83
[QA] t=355.70 hold elapsed=3.01 Roadwork children=0 [] barrierDestroyed=True
```
(`TickInteraction`은 게임 프레임이 바뀐 Editor update에서만 한 번씩 호출했다.)

#### 내구도 (AC-7, 5 m/s 정면, 시험 물류 Normal 100, 정리 수정 후 세트)
```
Cone #9601        impulse=70.61  → Δv=0.71 < 하한선 1.5 → 무시 (100 유지)
Cone #9602        impulse=70.54  → Δv=0.71 < 하한선 → 무시
Cone #9603        impulse=70.54  → Δv=0.71 < 하한선 → 무시
Box #9604         impulse=64.77  → Δv=0.65 < 하한선 → 무시
Box #9605         impulse=75.8   → Δv=0.76 < 하한선 → 무시
Box #9606         impulse=64.77  → Δv=0.65 < 하한선 → 무시
Dumpster #9607    impulse=221.61 → Δv=2.22  damage=min((2.22-1.5)*12, 60)=8.59  100 → 91.41
Dumpster #9608    impulse=199.17 → Δv=1.99  damage=5.9   100 → 94.1
Dumpster #9609    impulse=221.61 → Δv=2.22  damage=8.59  100 → 91.41
Dumpster #9610    impulse=199.16 → Δv=1.99  damage=5.9   100 → 94.1
Dumpster #9611    impulse=221.61 → Δv=2.22  damage=8.59  100 → 91.41
Dumpster #9541    impulse=232.38 → Δv=2.32  damage=9.89  100 → 90.11   (groundY=0.030 확인, 유효)
RoadSign #9612    impulse=485.46 → Δv=4.85  damage=40.26  100 → 59.74  (다음 접촉 Δv=0 쿨다운 무시)
RoadSign #9613    impulse=485.46 → Δv=4.85  damage=40.26  100 → 59.74
RoadSign #9614    impulse=485.46 → Δv=4.85  damage=40.26  100 → 59.74
StreetLight #9615 impulse=366.86 → Δv=3.67  damage=26.02  100 → 73.98
StreetLight #9616 impulse=366.86 → Δv=3.67  damage=26.02  100 → 73.98
StreetLight #9617 impulse=366.86 → Δv=3.67  damage=26.02  100 → 73.98
Roadwork #9618    impulse=330.92 → Δv=3.31  damage=21.71  100 → 78.29
Roadwork #9619    impulse=330.92 → Δv=3.31  damage=21.71  100 → 78.29
Roadwork #9620    impulse=330.92 → Δv=3.31  damage=21.71  100 → 78.29
```
정리 수정 전 첫 세트(#9501~#9518, 이전 오브젝트가 Raycast에 걸렸는지 확인 안 됨, 참고만): Cone Δv 0.71×3, Box 0.64 / 0.32(+0.21+0.1+0.13 여러 번 접촉) / 0.76 → 모두 무시, Dumpster −8.42 / −8.61 / −14.33, RoadSign −40.26×3, StreetLight −26.02 / −21.0 / −26.02, Roadwork −21.71×3. 무효: Dumpster #9542(groundY=0.385, 이전 Dumpster 위에 놓임), #9543(로그 잘림).

충돌 직후 차량 속도(모니터):
```
Roadwork #9531    기록 Δv=3.31 → +0.26초 v=0.15, +0.31초 v=0.08
RoadSign #9532    기록 Δv=4.85 → +0.14초 v=0.01
StreetLight #9533 기록 Δv=3.67 → +0.13초 v=0.31, +0.28초 v=0.09
```

부수 오류: 피해가 적용될 때마다 `NullReferenceException at Game.UI.DurabilityHud.RefreshCargoPanel () (DurabilityHud.cs:310)`(시험 물류 `Target`=null, 이전과 같은 시험 방법 오류). 이 외 오류·경고 없음.

### 항목별 결과 (재측정)

| AC | 항목 | 결과 | 근거 |
|---|---|---|---|
| AC-2 | CONE: `cone-flat`, ×0.8 약 1초, 약 10초 후 제거 | **확인** | 충돌(≈+0.02) 후 첫 관찰 프레임에 `cone-flat`, maxFwd 3 → 2.4. fx=True 마지막 +1.02, False 첫 +1.04 → 약 1.0초. 제거 +10.02(충돌 후 ≈10.0초). 6회 재현 |
| AC-3 | DEBRIS-S ±15°, 즉시 제거, 내구도 불변 | **확인** | 작아진 트리거(0.04×0.07×0.05)도 5/5 발동. yaw 0 → 345/15/345/345/15(두 부호 모두). 첫 관찰 프레임(+0.13~0.16)에 제거. 내구도 100, `[CargoImpact]` 없음 |
| AC-3 | DEBRIS-L ×0.6 2초, 즉시 제거, 내구도 불변 | **확인** | 출발 0.05초 후 제거와 같은 프레임에 fx=True mul=0.6, maxFwd 1.8. t=257.29~259.30(2.01초). 내구도 100 |
| AC-4 | ROADWORK Barrier 3초 → 그룹 전체 제거 | **확인(조건부)** | 플레이어 상호작용 컨트롤러 현재 대상=Barrier, `CanInteract=True`, 감지 1개. 3.01초 후 children 3 → 0. 실제 키 입력 대신 `TickInteraction` 호출(이전과 같은 한계) |
| AC-7 | 고속 정면 충돌 ±20% | **일부 미충족** | 아래 표. Cone·Box·RoadSign 범위 안, Dumpster 불안정, StreetLight·Roadwork 범위 밖 |
| AC-8 | 스크린샷 보관 | **확인** | 아래 표(7장 덮어씀) |

### AC-7 표 (카탈로그 수치는 변경하지 않음)

| 대상 | 카탈로그(고속) | ±20% 범위 | 실측(정리 수정 후 세트) | 판정 |
|---|---|---|---|---|
| Cone | 0 | 하한선 미만 | Δv 0.71 ×3 → 0 | **범위 안** |
| Box | 0 | 하한선 미만 | Δv 0.65 / 0.76 / 0.65 → 0 | **범위 안** |
| Dumpster | −9 | −7.2 ~ −10.8 | −8.59, −5.90, −8.59, −5.90, −8.59, −9.89 (평균 −7.91) | **불안정**: 6회 중 4회 범위 안, 2회(−5.90) 밖. 같은 배치에서 Δv 2.22 / 1.99 두 값이 번갈아 나온다 |
| RoadSign | −42 | −33.6 ~ −50.4 | −40.26 ×3 (첫 세트도 ×3) | **범위 안** |
| StreetLight (yaw 90, 기둥 정면) | −42 | −33.6 ~ −50.4 | −26.02 ×3 (첫 세트 −26.02 / −21.0 / −26.02) | **범위 밖** |
| Roadwork 넓은 면 | −42 | −33.6 ~ −50.4 | −21.71 ×3 (첫 세트도 ×3) | **범위 밖**. 이번엔 비껴 맞지 않음(차량 yaw 0 유지, 정면 Barrier 모델에 전폭 접촉) |

원인 추정(검증하지 않음, 수정하지 않음):
- StreetLight·Roadwork: 차량은 실제로 거의 멈춘다(+0.13~0.31초에 v 0.08~0.31). 실제 속도 변화 ≈4.6~4.8 m/s이니 공식대로라면 −37 ~ −40으로 범위 안이다. 그런데 기록된 충격량은 Δv 3.3~3.7뿐이다. `PlayerCargoImpactController`는 `OnCollisionEnter`의 `collision.impulse`만 더하고 `OnCollisionStay`는 일부러 버리므로, 충격이 여러 물리 스텝에 나뉘어 전달되면 첫 스텝분만 피해가 된다. RoadSign은 한 스텝에 4.85가 다 들어와서 맞는다. 왜 이 두 프리팹에서만 나뉘는지(비convex MeshCollider, 차량 콜라이더 7개가 서로 다른 스텝에 닿음, 낮은 Barrier 높이 0.24 등)는 확인하지 않았다.
- Dumpster: 동적 강체(m=80)라 결과가 Δv 1.99 / 2.22 두 값으로 갈린다. 완전 비탄성 충돌 이론값은 Δv ≈ 4.9×80/180 ≈ 2.18(피해 ≈8.1)로 2.22 쪽과 맞는다. 1.99가 나오는 원인은 확인하지 않았다.

### 스크린샷 (Read로 직접 확인)

| 파일 | 촬영 | 실제로 보이는 것 |
|---|---|---|
| `01-cone-knocked.png` | QACam | 초록 화물칸·흰 캡 트럭을 앞 왼쪽 위에서 본 장면. 앞범퍼 바로 앞에 주황 받침이 달린 작은 콘이 옆으로 누운 채(`cone-flat`) 범퍼에 붙어 있다. 콘은 범퍼 높이보다 낮고 바퀴보다 작다 |
| `02-debris-before.png` | QACam | 같은 시점. 트럭 앞범퍼 왼쪽 앞 도로 위에 작은 검은 타이어(DebrisL)가 반원 모양으로 보인다(아래 절반은 도로 아래). 크기는 트럭 바퀴와 비슷하다 |
| `02-debris.png` | QACam | 트럭이 조금 전진했고 타이어가 사라졌다. 트럭 앞 도로가 비어 있다 |
| `02a-debris-s-after.png` | 게임 화면 | 뒤 위에서 본 트럭이 진행 방향(화면 위) 기준으로 오른쪽으로 약 15° 틀어져 있다. 속도계 21 km/h, 좌상단 09:06 DAY 1. 잔해는 보이지 않는다 |
| `03-roadwork-before.png` | QACam | 트럭 바로 앞에 진행 방향을 가로막는 공사 벽: 왼쪽 주황·흰 경고등, 가운데 회색 콘크리트 바리케이드, 오른쪽으로 길게 뻗은 회색 철제 펜스. 벽 길이가 트럭 폭의 약 3배다 |
| `03b-roadwork-holding.png` | 게임 화면 | 뒤 위에서 본 트럭 앞에 펜스(왼쪽)와 경고등(오른쪽)이 트럭 양옆으로 튀어나와 보인다. 하단 속도계(0 km/h) 뒤로 가로 진행 막대가 겹쳐 있고 왼쪽 일부가 초록으로 채워져 있다(진행 중) |
| `04-roadwork-after.png` | QACam | 03과 같은 시점. 경고등·바리케이드·펜스가 모두 사라지고 트럭 앞 도로가 비어 있다 |

### 확인하지 못한 것 / 한계 (재측정)

- 실제 키 입력(상호작용 키 3초 유지, 실제 운전)은 하지 않았다. 충돌은 속도 직접 대입, 상호작용은 `TickInteraction` 호출로 대신했다.
- 모니터 해상도는 약 0.02~0.2초. CONE 충돌 프레임 자체는 기록하지 못했다(첫 관찰 프레임이 +0.19).
- StreetLight·Roadwork 미충족 원인과 Dumpster 결과 분기 원인은 가설만 있다. 카탈로그·코드·에셋은 지시대로 바꾸지 않았다.
- StreetLight는 yaw 90(기둥 정면)만 측정했다. 이전 측정의 방향은 기록이 없어 그대로 비교할 수 없다.
- 이 스토리 밖에서 눈에 띈 점: DebrisL 타이어 트리거·메시 중심이 피벗이라 절반이 도로 아래에 묻혀 보인다. DebrisS 트리거와 차량 바닥의 높이 겹침이 약 0.035 m뿐이라 차체가 들리거나 도로 높이가 다르면 놓칠 수 있다. 03b에서 상호작용 진행 막대가 여전히 속도계 뒤에 겹친다.

## 재측정 (판정 창 0.06초 적용 후, 2026-11-17)

> `PlayerCargoImpactController`가 `OnCollisionEnter`로 판정 창(0.06초 = 고정 스텝 3회, `fixedDeltaTime` 0.02)을 열고 창 안의 `OnCollisionEnter`·`OnCollisionStay` 충격량을 합산해 창이 닫힐 때 한 번 판정하도록 바뀐 뒤 AC-7을 다시 쟀다. Assets·기획 문서는 바꾸지 않았다. 스크린샷 없음.

### 환경

- 시작 시 Editor `playMode: stopped`, `runInBackground: false`, 씬 `SampleMapDesign.unity` `isDirty=False`. 컴파일된 형식에 `_windowStepsLeft` 필드가 있음을 확인했다(수정 코드 반영).
- `editor set_auto_tick`은 "already enabled"(원래 켜져 있음). Run In Background는 내가 켰다. Play mode 진입 후 `job wait`(frameCount changed) `met=true`, 1442 → 1470, framesObserved=2.
- 런타임 Config: `ImpactWindow=0.06`(에셋에 `_impactWindow` 직렬화 값이 없어 코드 기본값), 하한선 1.5, 1 m/s당 12, 상한 60, 쿨다운 0.5 → 판정 창 3스텝.
- 방법은 "재측정 (스케일 수정 후)"과 같다: 매 회 `GameTimeSystem._currentSeconds`를 영업 시작 시각으로 되돌림, 차량 (-0.88, 0, -26.24) 회전 0·속도 0, `_loadedParcels`를 비우고 시험 물류(Normal, None, 100, ID 97xx) 1개, 프리팹을 도로 표면(Raycast, groundY=0.030)에 놓고 가장 가까운 비트리거 면을 차량 앞면(z=-25.721) +0.1 m에, 이전 QA 오브젝트는 `DestroyImmediate`, `linearVelocity = forward × 5`. StreetLight·Roadwork는 yaw 90(기둥 정면 / 넓은 면). 각 회 1.5초 후 차량 속도·내구도를 `[QA] after`로 기록.

### 원본 콘솔 로그 (`[CargoImpact]`, 판정 창 적용 후)

```
Cone #9701/#9711/#9712/#9713  impulse=72.72 → Δv=0.73 < 하한선 1.5 → 무시, 이어서 impulse=10.09 → Δv=0.1 → 무시 (4회 동일, dur 100)
Box #9721      impulse=120.81 → Δv=1.21 < 하한선 → 무시 (+ impulse=2.93 Δv=0.03 무시)  dur 100
Box #9722      impulse=109.73 → Δv=1.1  < 하한선 → 무시                              dur 100
Box #9723      impulse=120.81 → Δv=1.21 < 하한선 → 무시 (+ Δv=0.03 무시)            dur 100
Dumpster #9731 impulse=339.37 → Δv=3.39 damage=min((3.39-1.5)*12, 60)=22.72  100 → 77.28
Dumpster #9732 impulse=337.17 → Δv=3.37 damage=22.46  100 → 77.54
Dumpster #9733 impulse=339.37 → Δv=3.39 damage=22.72  100 → 77.28
Dumpster #9734 impulse=337.18 → Δv=3.37 damage=22.46  100 → 77.54
Dumpster #9735 impulse=339.37 → Δv=3.39 damage=22.72  100 → 77.28
Dumpster #9736 impulse=337.18 → Δv=3.37 damage=22.46  100 → 77.54
Dumpster #9737 impulse=339.37 → Δv=3.39 damage=22.72  100 → 77.28
RoadSign #9741/#9742/#9743       impulse=485.92 → Δv=4.86 damage=40.31  100 → 59.69 (3회 동일)
StreetLight #9751 impulse=457.09 → Δv=4.57 damage=36.85  100 → 63.15
StreetLight #9752 impulse=459.17 → Δv=4.59 damage=37.1   100 → 62.9
StreetLight #9753 impulse=457.09 → Δv=4.57 damage=36.85  100 → 63.15
Roadwork #9761/#9762/#9763       impulse=586.2  → Δv=5.86 damage=52.34  100 → 47.66 (3회 동일)
```
`[QA] after`(충돌 약 1.8초 후): 모든 회차 차량 v=0.00~0.01, yaw 변화 Cone 3°, Box 0.8~1.8°, Dumpster −0.5°, 나머지 0°.

Dumpster #9737 모니터(`EditorApplication.update`, 첫 관찰이 충돌 후 약 +0.1초라 충돌 스텝 자체는 못 봄):
```
ft=160.820 v=0.05 vy=-0.02 objV=0.05 objVy=-0.07 objRotX=0.8
ft=160.840 v=-0.16 vy=-0.09 objV=-0.04 ...
ft=160.900 v=-0.08 ... objV=-0.01   (이후 둘 다 0으로 수렴)
```
충돌 약 0.1초 뒤 차량과 Dumpster가 모두 거의 정지해 있다(Dumpster가 밀려 나가지 않음).

부수 오류: 피해가 적용될 때마다 `NullReferenceException at Game.UI.DurabilityHud.RefreshCargoPanel`(시험 물류 `Target`=null, 알려진 시험 방법 오류). 그 밖의 오류 없음.

### AC-7 표 (판정 창 적용 후, 카탈로그 수치는 변경하지 않음)

| 대상 | 카탈로그(고속) | ±20% 범위 | 실측 | 판정 |
|---|---|---|---|---|
| Cone | 0 | 하한선 미만 | Δv 0.73 ×4 → 0 | **범위 안** |
| Box | 0 | 하한선 미만 | Δv 1.21 / 1.10 / 1.21 → 0 | **범위 안**. 단 이전(0.65~0.76)보다 커져 하한선 1.5까지 여유가 약 0.3 m/s(약 20%)뿐이다 |
| Dumpster | −9 | −7.2 ~ −10.8 | −22.72 / −22.46 ×7회(번갈아) | **범위 밖**(약 2.5배). 결과는 이제 결정적(Δv 3.37/3.39)이지만 너무 크다 |
| RoadSign | −42 | −33.6 ~ −50.4 | −40.31 ×3 | **범위 안** |
| StreetLight (yaw 90) | −42 | −33.6 ~ −50.4 | −36.85 / −37.1 / −36.85 | **범위 안**(이전 −26.02에서 개선) |
| Roadwork 넓은 면 (yaw 90) | −42 | −33.6 ~ −50.4 | −52.34 ×3 | **범위 밖**(상한 −50.4를 1.94 초과). 이전 −21.71에서 반대쪽으로 넘어감 |

원인 추정(검증하지 않음, 수정하지 않음):
- Dumpster: 판정 창이 이제 차량의 정지 전체(≈4.9 m/s 중 3.4)를 모은다. 이전 이론(완전 비탄성, Dumpster가 함께 밀려 나감 → Δv≈2.2)과 달리 실제로는 Dumpster가 거의 밀리지 않고 차량이 멈춘다(모니터 +0.1초 시점 둘 다 v≈0.05). 즉 동적 Dumpster(m=80)가 고정물처럼 작동해 카탈로그 −9 가정(밀리는 중량물)과 어긋난다. Dumpster 마찰·질량 설정이나 카탈로그 수치 쪽 판단이 필요하다.
- Roadwork: 기록 Δv 5.86이 접촉 속도(≈4.9 m/s)보다 크다. 수평 운동량만으로는 나올 수 없는 값이라, 창 안 `OnCollisionStay` 합산에 수직 성분(낮은 Barrier 0.24 m에 앞부분이 얹히는 접촉의 법선 y가 `GroundNormalMinDot` 미만이라 지면으로 걸러지지 않음)이나 되튐이 들어갔을 가능성이 있다. Roadwork 모니터 측정(#9764)은 아래 사고로 무효가 되어 확인하지 못했다.

### 회귀 확인 (판정 창이 지켜야 할 규칙)

| 항목 | 결과 |
|---|---|
| (a) 정지 상태로 벽/RoadSign에 닿은 채 풀 스로틀 약 2초 → 감소 없음 | **미실시** |
| (b) 저속(약 1.5 m/s) RoadSign 접촉 → 공식대로 작거나 0 | **미실시** |
| (c) 충돌 후 0.5초 안 되튐 재접촉 → 쿨다운 무시 | **미실시**(이번 고속 세트에서 쿨다운 로그는 한 번도 나오지 않았다. Cone·Box의 두 번째 창은 하한선 미만이라 쿨다운까지 가지 않음) |

중단 사유: Roadwork·StreetLight 모니터 측정 도중(약 02:09~02:10 UTC) 다른 작업 주체가 같은 Editor에서 `DurabilityHud.cs/.uss/.uxml`을 수정해 재컴파일하고 Play mode를 종료했다가 다시 시작했다(콘솔에 내가 보내지 않은 `gameobject delete` 실패 기록도 있다). 같은 Editor에서 계속 측정하면 그 세션과 충돌하므로 회귀 확인은 하지 않았다.

### 사고: Edit mode 씬 오염 (조치 필요)

- Play mode가 바깥에서 종료된 사이 내 측정 eval 2회(#9764 Roadwork, #9754 StreetLight)가 **Edit mode에서** 실행됐다. 그 결과 Edit 씬에 `QA_Roadwork_9764`가 생기고 Player 위치가 (-0.88, 0.03, -26.25) → (-0.88, 0, -26.24)로 바뀌었다. 직후 02:10:03 UTC에 **다른 주체가 씬을 저장**해서, 디스크의 `Assets/Game/Scenes/_DevOnly/SampleMapDesign.unity`에 `QA_Roadwork_9764`(루트, Roadwork 복제본, 프리팹 링크 없음)와 Player 위치 변경(`m_LocalPosition.y 0.03 → 0`, `.z -26.25 → -26.24`)이 들어갔다.
- 메모리의 Edit 씬에는 두 번째 eval이 만든 `QA_StreetLight_9754`가 남아 있고(`QA_Roadwork_9764`는 메모리에서는 지워짐), 지금 다른 주체의 Play 세션은 이 상태에서 시작됐다(차량 앞 0.1 m에 StreetLight가 있음).
- 되돌리기를 시도했으나 그 시점에 이미 Play mode가 다시 켜져 있어 손대지 않았다. 필요한 조치: Play mode 종료 후 루트의 `QA_*` 오브젝트 삭제, Player 프리팹 인스턴스 위치를 (-0.88, 0.03, -26.25)로 복원한 뒤 씬 저장(또는 이 두 부분만 git에서 되돌림). 씬의 다른 변경(Roadwork 배치, NavigationCompass 제거 등)은 내 것이 아니다.

## 재측정 (수평 속도 변화 방식, 2026-11-17)

> `PlayerCargoImpactController`가 충격량 합산 대신 **차량 수평 속도 변화**로 Δv를 구하도록 바뀐 뒤(충돌 스텝 직전 FixedUpdate의 속도 → `OnCollisionEnter`가 연 0.06초 창(고정 스텝 3회)이 끝날 때 속도, y 성분 제외, `OnCollisionStay` 제거) AC-7과 회귀 3항목을 다시 쟀다. Assets·기획 문서·씬은 바꾸거나 저장하지 않았다. 스크린샷 없음.

### 환경

- 시작 시 `playMode: stopped`, `EditorApplication.isPlaying=False`, 씬 `SampleMapDesign.unity` `isDirty=False`, 루트에 `QA_*` 없음, `runInBackground=False`. 컴파일된 형식에 `_lastStepVelocity` 필드가 있음을 확인했다(수정 코드 반영).
- `editor set_auto_tick`은 "already enabled"(원래 켜져 있어 그대로 둠). Run In Background는 내가 켰고 끝에 `--enabled false`로 되돌렸다(프로젝트 값 off 확인).
- Play mode 진입 후 `job wait`(frameCount changed) `met=true`, 2 → 5, framesObserved=2.
- 런타임 Config: `ImpactWindow=0.06`, 하한선 1.5, 1 m/s당 12, 상한 60, 쿨다운 0.5, `fixedDeltaTime=0.02`, 차량 질량 100.

### 방법 (달라진 점)

- 측정 절차는 `PlayerLoop`의 `FixedUpdate` 맨 앞(게임 스크립트보다 먼저)에 넣은 임시 훅(`QAHookTag`, 런타임 메모리만)이 고정 스텝 단위로 진행했다. 한 회 = 75스텝(1.5초), 밀기 = 150스텝. 훅은 매 스텝 `EditorApplication.isPlaying`을 확인하고, 설치 eval은 `isPlaying` 가드로 시작한다. 끝난 뒤 훅을 제거하고 `QA_*`를 지웠다.
- 매 회 준비(같은 스텝 안): 이전 `QA_*` 비활성화 후 `Destroy`, `GameTimeSystem._currentSeconds`를 영업 시작 시각으로, 차량 (-0.88, 0, -26.24)·회전 0·속도 0, 이동 컨트롤러 `_forwardSpeed`·`_input` 0, 충격 컨트롤러 `_windowStepsLeft=0`·`_lastStepVelocity=0`·쿨다운 해제, `_loadedParcels`를 비우고 시험 물류 1개(Normal, None, 내구도 100, ID 98xx). **이번에는 씬의 실제 `DeliveryTarget`(`DeliveryTarget-Type-B (1)`)을 넣어** 이전의 `DurabilityHud.RefreshCargoPanel` NullReferenceException이 나지 않았다(새 오류 0건).
- 배치는 이전과 같다. 도로 Raycast(groundY=0.030, 도로 타일 `model` L7)에 놓고 가장 가까운 비트리거 면을 차량 앞면(z=-25.721) +gap에 맞췄다. StreetLight·Roadwork는 yaw 90. 속도 `linearVelocity = forward × v`.
- 대조군(장애물 없음): 5 m/s와 1.75 m/s로 출발시켜 스텝마다 속도를 기록했다.

### 원본 콘솔 로그

#### 대조군 (장애물 없음)
```
[QA] setup #9850 mode=control obj=none yaw=0 v=5 gap=0.1 ft=134.92 dur=100
[QA] control step=1 veh v=(0,0.036,4.92) h=4.92 pos=(-0.88,0.001,-26.141) win=0
[QA] control step=2 veh v=(0,0.017,4.84) h=4.84 pos=(-0.88,0.001,-26.044) win=0
[QA] control step=3 veh v=(0,0.014,4.76) h=4.76 pos=(-0.88,0.001,-25.948) win=0
[QA] control step=4 veh v=(0,0.008,4.68) h=4.68 pos=(-0.88,0.002,-25.854) win=0
[QA] control step=5 veh v=(0,1.43,3.084) h=3.084 pos=(-0.88,0.03,-25.794) win=0
[QA] control step=6 veh v=(0,-0.423,3.004) h=3.004 pos=(-0.88,0.022,-25.736) win=0
...
[QA] setup #9851 mode=control obj=none yaw=0 v=1.75 gap=0.1 ft=136.42 dur=100
[QA] control step=1 veh v=(0,0.036,1.67) h=1.67 pos=(-0.88,0.001,-26.206) win=0
[QA] control step=2 veh v=(0,0.017,1.59) h=1.59 ...
[QA] control step=12 veh v=(0,-0.007,0.79) h=0.79 pos=(-0.88,0.001,-25.936) win=0
```
첫 세트의 #9800도 5 m/s 대조군이었고 값이 같다. 손을 뗀 상태의 감속(`CoastDeceleration` 4 m/s²)으로 스텝당 0.08 m/s씩 준다. 그래서 장애물이 없어도 창 3스텝 동안 약 0.24 m/s가 Δv에 섞인다.

#### AC-7 (5 m/s 정면, 매 회 시험 물류 Normal 100)
```
[QA] setup #9801 mode=hit obj=Cone yaw=0 v=5 gap=0.1 ft=10.32 dur=100 groundY=0.03 ground=model/L7 objPos=(-0.88,0.03,-25.561) objColMinY=0.03 objColMaxY=0.179 objMinZ=-25.621 vehFront=-25.721 vehMinY=0.062 vehMaxY=0.48
[QA] contact detected at step 1 ft=10.36 vVeh=(0,0.199,4)
[CargoImpact] v 4.92 → 3.82 → deltaVelocity=1.1
[CargoImpact] deltaVelocity=1.1 < 하한선 1.5 → 무시
[CargoImpact] v 3.72 → 3.32 → deltaVelocity=0.43
[CargoImpact] deltaVelocity=0.43 < 하한선 1.5 → 무시
[QA] end #9801 obj=Cone ft=11.80 veh v=(0.009,0.001,-0.004) yaw=2.9 dur=100
(#9802, #9803 같은 로그: 1.1 → 무시, 0.43 → 무시, dur=100)

[QA] setup #9804 mode=hit obj=Box ... objPos=(-0.88,0.03,-25.532) objColMinY=0.03 objColMaxY=0.209 objMinZ=-25.621
[CargoImpact] v 4.92 → 3.51 → deltaVelocity=1.41
[CargoImpact] deltaVelocity=1.41 < 하한선 1.5 → 무시
[CargoImpact] v 0.01 → 0.01 → deltaVelocity=0
[CargoImpact] deltaVelocity=0 < 하한선 1.5 → 무시
[QA] end #9804 obj=Box ... dur=100
[QA] setup #9805 mode=hit obj=Box ...
[CargoImpact] v 4.92 → 3.58 → deltaVelocity=1.34
[CargoImpact] deltaVelocity=1.34 < 하한선 1.5 → 무시
[QA] end #9805 obj=Box ... dur=100
[QA] setup #9806 mode=hit obj=Box ...
[CargoImpact] v 4.92 → 3.51 → deltaVelocity=1.41
[CargoImpact] deltaVelocity=1.41 < 하한선 1.5 → 무시
[CargoImpact] v 0.01 → 0.01 → deltaVelocity=0
[QA] end #9806 obj=Box ... dur=100

[QA] setup #9807 mode=hit obj=Dumpster ... objPos=(-0.88,0.03,-25.274) objColMinY=0.03 objColMaxY=0.423 objMinZ=-25.621
[QA] contact detected at step 1 ft=19.36 vVeh=(0,0.83,2.708)
[CargoImpact] v 4.92 → 1.13 → deltaVelocity=3.8
[CargoImpact] damage=min((3.8-1.5)*12, 60)=27.56
[CargoImpact] Parcel#9807 (Normal)  100 → 72.44
[QA] setup #9808 mode=hit obj=Dumpster ...
[QA] contact detected at step 1 ft=20.86 vVeh=(-0.008,0.823,2.588)
[CargoImpact] v 4.92 → 1.33 → deltaVelocity=3.6
[CargoImpact] damage=min((3.6-1.5)*12, 60)=25.22
[CargoImpact] Parcel#9808 (Normal)  100 → 74.78
[QA] setup #9809 mode=hit obj=Dumpster ...
[CargoImpact] v 4.92 → 1.13 → deltaVelocity=3.8
[CargoImpact] damage=min((3.8-1.5)*12, 60)=27.56
[CargoImpact] Parcel#9809 (Normal)  100 → 72.44
[QA] setup #9810 mode=probe obj=Dumpster ...   (아래 Dumpster 조사)
[CargoImpact] v 4.92 → 1.33 → deltaVelocity=3.6
[CargoImpact] damage=min((3.6-1.5)*12, 60)=25.22
[CargoImpact] Parcel#9810 (Normal)  100 → 74.78

[QA] setup #9811 mode=hit obj=RoadSign ... objPos=(-0.88,0.03,-25.491) objColMinY=0.03 objColMaxY=0.956 objMinZ=-25.621
[QA] contact detected at step 1 ft=25.36 vVeh=(0,-0.1,-0.013)
[CargoImpact] v 4.92 → 0.01 → deltaVelocity=4.92
[CargoImpact] damage=min((4.92-1.5)*12, 60)=41.08
[CargoImpact] Parcel#9811 (Normal)  100 → 58.92
(#9812, #9813 같은 로그: 4.92 → 41.08, 100 → 58.92)

[QA] setup #9814 mode=hit obj=StreetLight yaw=90 ... objPos=(-0.88,0.03,-25.509) objColMaxY=2.73 objMinZ=-25.621
[QA] contact detected at step 1 ft=29.86 vVeh=(-0.131,1.785,1.59)
[CargoImpact] v 4.92 → 0.26 → deltaVelocity=4.77
[CargoImpact] damage=min((4.77-1.5)*12, 60)=39.23
[CargoImpact] Parcel#9814 (Normal)  100 → 60.77
[QA] setup #9815 ...
[CargoImpact] v 4.92 → 0.25 → deltaVelocity=4.83
[CargoImpact] damage=min((4.83-1.5)*12, 60)=40.01
[CargoImpact] Parcel#9815 (Normal)  100 → 59.99
[QA] setup #9816 ...
[CargoImpact] v 4.92 → 0.26 → deltaVelocity=4.77
[CargoImpact] damage=min((4.77-1.5)*12, 60)=39.23
[CargoImpact] Parcel#9816 (Normal)  100 → 60.77

[QA] setup #9817 mode=hit obj=Roadwork yaw=90 ... objPos=(-0.88,0.03,-25.494) objColMaxY=0.468 objMinZ=-25.621
[QA] contact detected at step 1 ft=34.36 vVeh=(-0.239,1.72,1.53)
[CargoImpact] v 4.92 → 0.28 → deltaVelocity=4.78
[CargoImpact] damage=min((4.78-1.5)*12, 60)=39.31
[CargoImpact] Parcel#9817 (Normal)  100 → 60.69
(#9818, #9819 같은 로그: 4.78 → 39.31, 100 → 60.69)
```
모든 회차에서 접촉은 출발 1스텝 뒤에 일어났다(간격 0.1 m / 5 m/s). 1.5초 뒤 차량 속도는 0.00~0.01. 새 오류·경고는 0건.

### AC-7 표 (수평 속도 변화 방식, 카탈로그 수치는 변경하지 않음)

| 대상 | 카탈로그(고속) | ±20% 범위 | 실측 Δv → 감소량 | 판정 |
|---|---|---|---|---|
| Cone | 0 | 하한선 미만 | 1.1 ×3 → 0 (이어서 열린 0.43 창도 무시) | **범위 안** |
| Box | 0 | 하한선 미만 | 1.41 / 1.34 / 1.41 → 0 | **범위 안, 여유 매우 작음**: 하한선 1.5까지 0.09 m/s(6%). 이 중 약 0.24는 손을 뗀 상태의 감속분(대조군) |
| Dumpster | −9 | −7.2 ~ −10.8 | 3.8 / 3.6 / 3.8 / 3.6 → −27.56 / −25.22 / −27.56 / −25.22 | **범위 밖**(약 3배) |
| RoadSign | −42 | −33.6 ~ −50.4 | 4.92 ×3 → −41.08 ×3 | **범위 안** |
| StreetLight (yaw 90, 기둥 정면) | −42 | −33.6 ~ −50.4 | 4.77 / 4.83 / 4.77 → −39.23 / −40.01 / −39.23 | **범위 안** |
| Roadwork 넓은 면 (yaw 90) | −42 | −33.6 ~ −50.4 | 4.78 ×3 → −39.31 ×3 | **범위 안**. 이전의 −52.34 초과가 해소됨(Δv가 접근 속도 4.92를 넘지 않음) |

### Dumpster 조사 (#9810, 1회)

배치·설정(`[QA] setup #9810 mode=probe` 원문 발췌):
```
groundY=0.03 ground=model/L7 objPos=(-0.88,0.03,-25.274) objColMinY=0.03 objColMaxY=0.423 objMinZ=-25.621 vehFront=-25.721 vehMinY=0.062 vehMaxY=0.48
| rb mass=80 kin=False damp=0 angDamp=0.05 constr=None interp=None det=Discrete sleepThr=0.005 com=(-0.014,0.187,0) vehMass=100
| col Model MeshCollider convex=True mat=ObstacleNoBounce f0.6/0.6 b.min=(-1.152,0.03,-25.621) b.max=(-0.636,0.416,-24.927)
| col lid-left MeshCollider convex=True mat=ObstacleNoBounce f0.6/0.6 b.min=(-1.086,0.283,-25.584) b.max=(-0.651,0.423,-25.274)
| col lid-right MeshCollider convex=True mat=ObstacleNoBounce f0.6/0.6 b.min=(-1.086,0.283,-25.274) b.max=(-0.651,0.423,-24.965)
| ignoreLayer(obj L0 vs ground L7)=False
| behind1m=[model(L7,trig=False,b.min=(-3.93,0,-25.5),b.max=(0,0.06,-22.5))]
| contactOffset=0.01 solverIt=6
```
도로 타일 `model`(L7)에는 물리 재질이 없다(기본 마찰 0.6). `DynamicsManager`는 FrictionType 0(Patch), Solver 6/1.

스텝별 기록(k = 접촉 스텝 이후 완료된 고정 스텝 수, 값은 해당 스텝의 물리 계산 직후):
```
[QA] probe step=1 k=-1 veh v=(0,0.036,4.92) h=4.92 pos=(-0.88,0.001,-26.141) | dumpster v=(0,0,0) h=0 pos=(-0.88,0.03,-25.274) rot=(0,0,0) sleeping=False
[QA] probe step=2 k=0 veh v=(-0.008,0.823,2.588) h=2.588 | dumpster v=(0.041,0.001,1.369) h=1.369 w=(0.619,-0.05,-0.063) pos=(-0.879,0.03,-25.251) rot=(1.133,359.869,-0.005)
[QA] probe step=3 k=1 veh v=(-0.023,0.47,1.831) h=1.831 | dumpster v=(0.065,-0.085,1.257) h=1.258 pos=(-0.878,0.03,-25.225) behind1m=[model(L7,...,b.max=(0,0.06,-22.5))]
[QA] probe step=4 k=2 veh v=(-0.032,0.168,1.318) h=1.319 | dumpster v=(0.071,-0.079,1.053) h=1.055 pos=(-0.876,0.03,-25.204)
[QA] probe step=5 k=3 veh v=(-0.044,0.166,0.865) h=0.866 | dumpster v=(0.07,-0.062,0.77) h=0.773 pos=(-0.874,0.03,-25.19)
[QA] probe step=6 k=4 veh v=(-0.052,0.071,0.452) h=0.455 | dumpster v=(0.055,-0.073,0.515) h=0.518 pos=(-0.873,0.03,-25.182)
[QA] probe step=7 k=5 veh v=(-0.051,-0.018,0.103) h=0.115 | dumpster v=(0.035,-0.076,0.268) h=0.271 pos=(-0.872,0.03,-25.178)
[QA] probe step=8 k=6 veh v=(-0.045,-0.147,-0.106) h=0.116 | dumpster v=(0.014,-0.073,0.036) h=0.039
[QA] probe step=12 k=10 veh v=(-0.009,-0.107,-0.058) h=0.058 | dumpster v=(0,-0.012,-0.008) h=0.008 pos=(-0.872,0.03,-25.178) rot=(0.102,359.237,0.028)
```
요청 항목: 접촉 후 1스텝 Dumpster 수평 속도 1.26 m/s, 3스텝 0.77 m/s, 10스텝 0.008 m/s. 총 이동 거리 0.096 m.

관찰:
- **배치는 정상이다**: 콜라이더 최저면 y=0.030이 도로 표면 0.030과 같다(묻히지 않음). 뒤 1 m 안에는 도로 타일 말고 아무것도 없다(벽·연석·다른 오브젝트 없음). 레이어 충돌 무시 아님, 키네마틱·제약 없음.
- **고정 벽처럼 서지 않는다**: 접촉 스텝에 1.37 m/s까지 밀려 나가고 약 0.12초(6스텝)에 걸쳐 멈춘다. 지난번 "둘 다 거의 정지"는 모니터 첫 관찰이 +0.1초라서 이미 멈춘 뒤를 본 것이다.
- **그러나 운동량 대부분이 사라진다**: 접촉 스텝 하나에 차량은 수평 운동량 100×(4.92−2.59)=233을 잃는데 Dumpster는 80×1.37=110만 얻는다. 같은 스텝에 차량 수직 속도가 +0.82 m/s로 튄다(RoadSign은 −0.1, 대조군은 +0.04). 이후 Dumpster는 차량과 떨어진 뒤(k≥4)에도 약 12 m/s²로 감속한다(마찰 0.6이면 μg≈5.9).
- 이론값: 완전 비탄성이면 공동 속도 4.92×100/180=2.73, 차량 Δv≈2.19, 감소량 ≈8.3이다(카탈로그 −9와 맞음). 실측 차량 Δv는 3.6~3.8.

원인 추정(검증하지 않음, 수정하지 않음): 차량과 Dumpster의 접촉 법선이 수평이 아니라 기울어 있다. 차량이 위로 밀리고(vy +0.82) 그 반작용이 Dumpster를 도로 쪽으로 누른다. 수직 하중이 커진 만큼 도로 마찰(Patch, 0.6)이 같은 스텝 안에 수평 운동량을 흡수해서, Dumpster는 덜 밀리고 차량은 더 많이 감속한다. 차량 콜라이더 위쪽(최고 0.48)이 Dumpster 윗면·뚜껑(0.283~0.423, convex lid 2개) 모서리에 걸리는 접촉이 가장 그럴듯하다. 어느 콜라이더 쌍이 닿는지(`collision.GetContact` 법선)는 기록하지 않았다. 카탈로그 −9는 "밀려 나가는 중량물" 가정에서만 나온다. Dumpster 콜라이더 형태(앞면이 수직인 BoxCollider 등), 마찰, 카탈로그 수치 중 어느 쪽을 바꿀지는 판단이 필요하다.

### 회귀 확인

```
(a) 밀기 #9852 (간격 0.02, 0.5초 정지 후 풀 스로틀 `_input=(0,1)` 2초)
[QA] setup #9852 mode=push obj=RoadSign yaw=0 v=0 gap=0.02 ft=137.92 dur=100 ... objMinZ=-25.701 vehFront=-25.721
[QA] push step=20 input=(0.00, 0.00) veh v=(0,-0.002,0) frontZ=-25.704 signMinZ=-25.701 dur=100
[QA] push step=30 input=(0.00, 1.00) veh v=(0,0.004,0.5) frontZ=-25.675 signMinZ=-25.701 dur=100
[QA] contact detected at step 33 ft=138.60 vVeh=(0.003,0.067,0.799)
[CargoImpact] v 0.8 → 0.26 → deltaVelocity=0.64
[CargoImpact] deltaVelocity=0.64 < 하한선 1.5 → 무시
[QA] push step=40 input=(0.00, 1.00) veh v=(0.007,-0.032,-0.026) ... dur=100
[QA] push step=60~120 input=(0.00, 1.00) veh v=(0,0,0) frontZ=-25.614 dur=100
[QA] push step=130 input=(0.00, 0.00) veh v=(-0.002,-0.026,-0.021) ... dur=100
[CargoImpact] v 0.04 → 0.03 → deltaVelocity=0          (손을 뗀 뒤 미세 재접촉 3회, 모두 0)
[QA] end #9852 obj=RoadSign ft=140.90 ... dur=100
(a') 밀기 #9870 (간격 0): 정지 중 차량이 0.012 m 뒤로 가라앉아, 이번에도 스로틀 후 0.69 m/s로 첫 접촉
[QA] contact detected at step 32 ft=182.12 vVeh=(0.003,0.065,0.687)
[CargoImpact] v 0.7 → 0.23 → deltaVelocity=0.56
[CargoImpact] deltaVelocity=0.56 < 하한선 1.5 → 무시
[QA] push step=60~120 input=(0.00, 1.00) veh v=(0,0,0) dur=100
[QA] end #9870 obj=RoadSign ft=184.44 ... dur=100

(b) 저속 RoadSign
#9853/#9854 v=1.75 gap=0.1:  [CargoImpact] v 1.27 → 0.12 → deltaVelocity=1.19 < 하한선 → 무시  dur=100 (2회 동일)
#9855/#9856 v=2   gap=0.1:  [CargoImpact] v 1.6 → 0.16 → deltaVelocity=1.47 < 하한선 → 무시   dur=100 (2회 동일)
#9871/#9872 v=2   gap=0.02: [CargoImpact] v 1.84 → 0 → deltaVelocity=1.84  damage=min((1.84-1.5)*12, 60)=4.12  100 → 95.88 (2회 동일)
#9873/#9874 v=2.5 gap=0.1:  [CargoImpact] v 2.26 → 0 → deltaVelocity=2.26  damage=min((2.26-1.5)*12, 60)=9.14  100 → 90.86 (2회 동일)

(c) 재접촉 #9857 (5 m/s 충돌 → k=4~7 후진 2 m/s → k=8 다시 4 m/s)
[CargoImpact] v 4.92 → 0.01 → deltaVelocity=4.92
[CargoImpact] damage=min((4.92-1.5)*12, 60)=41.08
[CargoImpact] Parcel#9857 (Normal)  100 → 58.92
[QA] recontact k=3 ft=147.02 win=0 veh v=(0,0.008,-0.002) dur=58.92
[QA] recontact k=4 ft=147.04 win=0 veh v=(0,0,-2) dur=58.92
[QA] recontact relaunch v=4 at k=8 ft=147.12 frontZ=-25.687
[QA] recontact k=10 ft=147.16 win=3 veh v=(0,-0.099,-0.033) dur=58.92
[CargoImpact] v 3.92 → 0.01 → deltaVelocity=3.92
[CargoImpact] 쿨다운 중 재접촉 deltaVelocity=3.92 → 무시
[QA] recontact k=13 ft=147.22 win=0 ... dur=58.92
(#9858 같은 로그)
```

| 항목 | 결과 | 근거 |
|---|---|---|
| (a) 닿은 채 풀 스로틀 2초 → 감소 없음 | **통과** | 2회 모두 미는 동안 새 판정 창이 열리지 않았고(접촉 유지는 창을 열지 않음) 내구도 100. 다만 정확히 "닿은 채 정지" 상태로 시작하지는 못했다: 스로틀 직후 0.7~0.8 m/s로 첫 접촉(Δv 0.56/0.64, 하한선 미만) |
| (b) 저속 RoadSign → 공식대로 | **통과** | 4개 속도 모두 감소량 = min((Δv−1.5)×12, 60). 1.75/2 m/s 출발(간격 0.1)은 감속 탓에 접촉 속도가 1.27/1.6이라 0. 접촉 속도 1.84 → −4.12, 2.26 → −9.14 |
| (c) 0.5초 안 재접촉 → 무시 | **통과** | 첫 판정(ft 147.02) 0.18초 뒤 재접촉 Δv 3.92(쿨다운이 없었다면 −29)가 "쿨다운 중 재접촉 … 무시"로 처리되고 내구도 58.92 유지. 2회 재현 |

### 종료 상태

- `QA_*`를 정리한 뒤 Play mode를 종료했다. 종료 뒤 `isPlaying=False`, 활성 씬 `SampleMapDesign` 루트에 `QA_*` 없음, `isDirty=False`, PlayerLoop에 `QAHook*` 0개, `runInBackground=False`. 디스크의 씬 파일에도 `QA_` 문자열이 0건이다. 씬은 저장하지 않았다.

### 확인하지 못한 것 / 한계

- 실제 키 입력 운전은 하지 않았다(속도는 직접 대입, 밀기는 `_input` 직접 대입).
- Dumpster 원인은 추정이다. 접촉점·법선(`GetContact`)과 어느 콜라이더 쌍이 닿는지는 기록하지 않았다.
- Box 여유 0.09 m/s는 손을 뗀 상태 기준이다. 실제 주행처럼 스로틀을 유지하면 감속분(≈0.24)이 빠져 Δv가 더 작아질 것으로 보이나 측정하지 않았다.
- 이 스토리 밖의 관찰: 장애물 없이 5 m/s로 달리면 차량 위치 z≈−25.79(앞바퀴 오프셋 z+0.305 → 도로 타일 경계 z=−25.5 부근)에서 수평 속도가 한 스텝에 4.68 → 3.08로 1.6 m/s 줄고 vy +1.43이 생긴다(2회 재현). `[CargoImpact]` 판정 창은 열리지 않는다(지면 처리). 이번 AC-7 회차는 모두 그 지점 전에 멈춰서 결과에 영향이 없다. 원인(서스펜션 SphereCast가 타일 모서리 법선을 받는지 등)은 확인하지 않았다.

## 재측정 (Dumpster 박스 콜라이더, 2026-11-17)

> Dumpster 프리팹 콜라이더가 convex MeshCollider 3개(본체 + 기울어진 뚜껑 2개)에서 **루트 BoxCollider 1개**(center (-0.014,0.196,0), size (0.516,0.393,0.694), `ObstacleNoBounce`)로 바뀐 뒤 AC-7의 **Dumpster 행만** 다시 쟀다. Rigidbody 질량 80은 그대로. 방법·스크립트는 바로 위 "재측정 (수평 속도 변화 방식)"과 같다(`seq_template.cs`, 작업 `probe:Dumpster:0:5:0.1` ×5, ID 9900~9904). Assets·기획 문서·씬은 바꾸거나 저장하지 않았다. 스크린샷 없음.

### 환경

- 시작 시 `isPlaying=False`, 씬 `SampleMapDesign` `isDirty=False`, 루트에 `QA_*` 없음, PlayerLoop에 `QAHook*` 0개, `runInBackground=False`, 컴파일 실패 없음. 프리팹을 `AssetDatabase`로 읽어 콜라이더가 `Dumpster:BoxCollider c=(-0.01,0.20,0.00) s=(0.52,0.39,0.69) mat=ObstacleNoBounce` 1개, 질량 80임을 확인했다.
- Run In Background는 내가 켰고 끝에 `--enabled false`로 되돌렸다. `editor set_auto_tick`은 "already enabled". Play mode 진입 후 `job wait`(frameCount changed) `met=true`, 1 → 2.
- 모든 회차가 probe 모드다(hit 모드와 같은 배치·속도에 스텝별 기록만 추가). 매 회 시험 물류 Normal 100 새로 적재.

### 원본 콘솔 로그

배치(5회 모두 같음, `[QA] setup #9900` 원문 발췌):
```
groundY=0.03 ground=model/L7 objPos=(-0.88,0.03,-25.274) objColMinY=0.03 objColMaxY=0.423 objMinZ=-25.621 vehFront=-25.721 vehMinY=0.062 vehMaxY=0.48
| rb mass=80 kin=False damp=0 angDamp=0.05 constr=None interp=None det=Discrete sleepThr=0.005 com=(-0.014,0.196,0) vehMass=100
| col QA_Dumpster_9900 BoxCollider convex=- mat=ObstacleNoBounce f0.6/0.6 b.min=(-1.152,0.03,-25.621) b.max=(-0.636,0.423,-24.927)
| ignoreLayer(obj L0 vs ground L7)=False | behind1m=[model(L7,trig=False,b.min=(-3.93,0,-25.5),b.max=(0,0.06,-22.5))] | contactOffset=0.01 solverIt=6
```

`[CargoImpact]`와 접촉 스텝:
```
[QA] setup #9900 ... ft=8.26 dur=100
[QA] contact detected at step 1 ft=8.30 vVeh=(-0.081,1.001,2.524)
[CargoImpact] v 4.92 → 1.32 → deltaVelocity=3.64
[CargoImpact] damage=min((3.64-1.5)*12, 60)=25.65
[CargoImpact] Parcel#9900 (Normal)  100 → 74.35
[QA] setup #9901 ... ft=9.76 dur=100
[QA] contact detected at step 1 ft=9.80 vVeh=(-0.041,0.829,2.558)
[CargoImpact] v 4.92 → 1.22 → deltaVelocity=3.71
[CargoImpact] damage=min((3.71-1.5)*12, 60)=26.54
[CargoImpact] Parcel#9901 (Normal)  100 → 73.46
[QA] setup #9902 ... ft=11.26 dur=100
[QA] contact detected at step 1 ft=11.30 vVeh=(-0.081,1.001,2.524)
[CargoImpact] v 4.92 → 1.32 → deltaVelocity=3.64
[CargoImpact] damage=min((3.64-1.5)*12, 60)=25.65
[CargoImpact] Parcel#9902 (Normal)  100 → 74.35
[QA] setup #9903 ... ft=12.76 dur=100
[QA] contact detected at step 1 ft=12.80 vVeh=(-0.041,0.829,2.558)
[CargoImpact] v 4.92 → 1.22 → deltaVelocity=3.71
[CargoImpact] damage=min((3.71-1.5)*12, 60)=26.54
[CargoImpact] Parcel#9903 (Normal)  100 → 73.46
[QA] setup #9904 ... ft=14.26 dur=100
[QA] contact detected at step 1 ft=14.30 vVeh=(-0.081,1.001,2.524)
[CargoImpact] v 4.92 → 1.32 → deltaVelocity=3.64
[CargoImpact] damage=min((3.64-1.5)*12, 60)=25.65
[CargoImpact] Parcel#9904 (Normal)  100 → 74.35
[QA] DONE
```
결과는 두 가지 패턴이 번갈아 정확히 반복됐다: A(#9900·#9902·#9904)와 B(#9901·#9903). 1.5초 뒤 차량 속도 0.004~0.012. 이 실행 중 새 오류·경고 0건(콘솔 error 1·warn 3은 모두 시작 전 타임스탬프).

스텝별 기록(k = 접촉 스텝 이후 완료된 고정 스텝 수). 패턴 A(#9900 원문, #9902·#9904 동일):
```
[QA] probe step=1 k=-1 veh v=(0,0.036,4.92) h=4.92 pos=(-0.88,0.001,-26.141) | dumpster v=(0,0,0) h=0 w=(0,0,0) pos=(-0.88,0.03,-25.274) rot=(0,0,0) sleeping=False
[QA] probe step=2 k=0 veh v=(-0.081,1.001,2.524) h=2.526 pos=(-0.879,0.019,-26.119) | dumpster v=(0.118,0.248,0.885) h=0.893 w=(-0.295,0.098,-0.279) pos=(-0.878,0.035,-25.256) rot=(359.906,0.142,359.662)
[QA] probe step=3 k=1 veh v=(-0.1,0.586,1.789) h=1.792 pos=(-0.877,0.03,-26.106) | dumpster v=(0.121,-0.01,0.835) h=0.843 pos=(-0.875,0.036,-25.239) behind1m=[]
[QA] probe step=4 k=2 veh v=(-0.073,0.303,1.284) h=1.286 | dumpster v=(0.094,-0.221,0.668) h=0.674 pos=(-0.873,0.033,-25.228)
[QA] probe step=5 k=3 veh v=(-0.105,0.234,0.757) h=0.765 | dumpster v=(0.066,-0.114,0.178) h=0.19 pos=(-0.871,0.03,-25.225)
[QA] probe step=6 k=4 veh v=(-0.138,0.184,0.284) h=0.316 | dumpster v=(0.041,-0.096,0.022) h=0.046
[QA] probe step=12 k=10 veh v=(-0.005,-0.167,-0.174) h=0.174 | dumpster v=(0.001,-0.007,-0.01) h=0.01 pos=(-0.871,0.03,-25.225) rot=(0.038,0.863,0.041)
```
패턴 B(#9901 원문, #9903 동일):
```
[QA] probe step=1 k=-1 veh v=(0,0.036,4.92) h=4.92 pos=(-0.88,0.001,-26.141) | dumpster v=(0,0,0) h=0
[QA] probe step=2 k=0 veh v=(-0.041,0.829,2.558) h=2.559 pos=(-0.878,0.016,-26.111) | dumpster v=(0.065,-0.008,1.407) h=1.409 w=(0.652,0.088,-0.085) pos=(-0.879,0.03,-25.25) rot=(1.04,0.157,359.937)
[QA] probe step=3 k=1 veh v=(-0.085,0.702,1.66) h=1.662 | dumpster v=(0.081,-0.086,1.172) h=1.175 pos=(-0.876,0.03,-25.231)
[QA] probe step=4 k=2 veh v=(-0.094,0.124,1.209) h=1.213 | dumpster v=(0.075,-0.118,0.861) h=0.864
[QA] probe step=5 k=3 veh v=(-0.101,0.178,0.692) h=0.7 | dumpster v=(0.064,-0.078,0.544) h=0.548
[QA] probe step=6 k=4 veh v=(-0.095,-0.048,0.369) h=0.382 | dumpster v=(0.044,-0.078,0.268) h=0.272
[QA] probe step=12 k=10 veh v=(0.006,-0.086,-0.085) h=0.086 | dumpster v=(-0.001,-0.012,-0.005) h=0.005 pos=(-0.87,0.03,-25.195)
```

### AC-7 Dumpster 행 (박스 콜라이더, 카탈로그 수치는 변경하지 않음)

| 회차 | 접촉 스텝 차량 vy | 차량 Δv | 감소량 | Dumpster 수평 속도 k=1 / 3 / 10 | 총 이동 |
|---|---|---|---|---|---|
| #9900 (A) | +1.001 | 3.64 | −25.65 | 0.843 / 0.19 / 0.01 | 0.049 m |
| #9901 (B) | +0.829 | 3.71 | −26.54 | 1.175 / 0.548 / 0.005 | 0.079 m |
| #9902 (A) | +1.001 | 3.64 | −25.65 | 0.843 / 0.19 / 0.01 | 0.049 m |
| #9903 (B) | +0.829 | 3.71 | −26.54 | 1.175 / 0.548 / 0.005 | 0.079 m |
| #9904 (A) | +1.001 | 3.64 | −25.65 | 0.843 / 0.19 / 0.01 | 0.049 m |

| 대상 | 카탈로그(고속) | ±20% 범위 | 실측 Δv → 감소량 | 판정 |
|---|---|---|---|---|
| Dumpster | −9 | −7.2 ~ −10.8 | 3.64 ×3 / 3.71 ×2 → −25.65 / −26.54 | **범위 밖**(약 2.9배). 콜라이더 교체 전(3.6~3.8 → −25.22~−27.56)과 사실상 같음 |

### 관찰

- **콜라이더 교체로 결과가 바뀌지 않았다.** 앞면이 수직인 박스인데도 접촉 스텝에 차량 vy가 +0.83~+1.00(교체 전 +0.82~+0.83)으로 튀고, 차량 수평 속도는 4.92 → 2.53~2.56으로 한 스텝에 떨어진다.
- 운동량(접촉 스텝 k=0): 차량이 잃는 수평 운동량 100×(4.92−2.53)≈239(A) / 236(B), Dumpster가 얻는 양 80×0.89≈71(A) / 80×1.41≈113(B). 교체 전(233 대 110)과 같은 경향으로 절반 이상이 사라진다. 패턴 A에서는 Dumpster도 vy +0.25로 떠오른다(위치 y 0.030 → 0.035).
- 그래서 "기울어진 뚜껑 hull 위로 차량이 올라타며 Dumpster를 바닥에 누른다"는 이전 원인 추정은 **이 측정으로 지지되지 않는다**. 뚜껑 hull이 없어도 같은 현상이 난다.
- 대안 추정(검증하지 않음): 위쪽으로 튀는 힘은 Dumpster 모양이 아니라 **차량 쪽 콜라이더**(이 회차 `playerCols=7`) 중 앞쪽 아래가 둥글거나 기울어진 것(바퀴·범퍼 등)이 박스 앞 모서리와 닿으면서 생기는 것으로 보인다. 같은 조건에서 RoadSign은 vy −0.1이었는데, RoadSign의 접촉면 높이·폭이 다른 탓일 수 있다. 접촉 법선(`collision.GetContact`)과 어느 차량 콜라이더가 닿는지는 이번에도 기록하지 않았다.

### 종료 상태

- `QA_*`와 PlayerLoop 훅을 Play mode 안에서 제거한 뒤(`isPlaying` 가드 넣은 정리 스크립트) Play mode를 종료했다. 종료 뒤 `isPlaying=False`, 활성 씬 `SampleMapDesign` 루트에 `QA_*` 없음, `isDirty=False`, PlayerLoop에 `QAHook*` 0개, `runInBackground=False`(내가 켠 것을 되돌림). 디스크의 씬 파일에 `QA_` 문자열 0건. 씬은 저장하지 않았다.

### 확인하지 못한 것 / 한계

- 접촉점·법선과 차량 콜라이더 쌍을 기록하지 않아서 차량 vy가 튀는 원인은 여전히 추정이다.
- 결과가 두 패턴(A/B)으로 결정적으로 번갈아 나오는 이유(물리 상태 초기화 차이 등)는 조사하지 않았다. 두 패턴 모두 범위 밖이므로 판정에는 영향이 없다.
- 실제 키 입력 운전은 하지 않았다(속도 직접 대입).

## 재측정 (사용자 Dumpster 박스 콜라이더, 2026-11-17)

> 사용자가 Dumpster 프리팹을 직접 다시 만든 뒤 AC-7의 **Dumpster 행만** 다시 쟀다. MeshCollider는 모두 없고 자식 `Model`에 **BoxCollider 1개**(로컬 center (-0.008,0.103,0), size (0.275,0.206,0.370), `Model` 스케일 1.875 → 월드 약 0.516×0.386×0.694), 재질 `ObstacleNoBounce`, 루트 Rigidbody 질량 80, 키네마틱 아님, 제약 없음. 방법·배치·속도는 "재측정 (수평 속도 변화 방식)"과 같다(`seq_dumpster_box.cs` 복사본 `seq_dumpster_contacts.cs`, 작업 `probe:Dumpster:0:5:0.1` ×5, ID 9920~9924, 매 회 시험 물류 Normal 100 새로 적재). **이번에 추가한 것**: 접촉 기록. Assets·기획 문서·씬·ProjectSettings는 바꾸거나 저장하지 않았다. 스크린샷 없음.

### 환경

- 시작 시 `isPlaying=False`(다른 세션 사용 중 아님), 씬 `SampleMapDesign` `isDirty=False`, 루트에 `QA_*` 없음, PlayerLoop에 `QAHook*` 0개, `runInBackground=False`, 컴파일 실패 없음. 프리팹을 `AssetDatabase`로 읽어 `Model:BoxCollider c=(-0.01,0.10,0.00) s=(0.28,0.21,0.37) mat=ObstacleNoBounce`, `Model` 스케일 1.88, 질량 80, `kin=False`, `constr=None`임을 확인했다.
- Run In Background는 내가 켰고 끝에 `--enabled false`로 되돌렸다. `editor set_auto_tick`은 "already enabled".
- `ObstacleNoBounce`: 마찰 0.6/0.6, `m_FrictionCombine: 0`(Average). 도로 타일 `model`은 재질 없음(기본 0.6). `DynamicsManager`: `m_FrictionType: 0`(Patch), **`m_ImprovedPatchFriction: 0`**, `m_SolverType: 0`.

### 접촉 기록 방법

- 시퀀서 설치 eval 안에서(`isPlaying` 가드 뒤) `Physics.ContactEvent`에 임시 처리기를 구독하고, **Play mode 인스턴스** `QA_Dumpster_99xx`의 콜라이더에만 `providesContacts = true`를 켰다(프리팹은 건드리지 않음). 처리기는 Dumpster 콜라이더가 낀 접촉 쌍만 모아 두었다가 회차 끝에 접촉 스텝 기준 k=−1..2 범위를 `[QA] contact`로 출력한다. 처리기는 시퀀서가 끝날 때 스스로 구독을 해제했고, 정리 스크립트(`cleanup_contacts.cs`)가 남은 처리기 0개를 확인했다.
- 요청서의 "OnCollisionEnter/Stay를 받는 임시 MonoBehaviour" 대신 이 방식을 썼다. eval이 만든 동적 어셈블리의 MonoBehaviour는 `AddComponent`가 보장되지 않고, `ContactEvent`는 같은 물리 접촉 데이터(쌍·점·법선·분리 거리·충격량)를 컴포넌트 추가 없이 준다.
- 법선 `n(d->o)`와 충격량 `imp(d->o)`는 **Dumpster → 상대 방향**으로 부호를 맞췄다(차량 접촉이면 −z가 차량을 뒤로 미는 방향). `hRoad` = 접촉점 y − 도로 표면 y(0.030). `sep` = PhysX 분리 거리(양수 = 아직 떨어져 있음, contactOffset 0.01 안의 예측 접촉).
- 차량 비트리거 콜라이더 7개는 모두 같은 오브젝트 `Body`의 BoxCollider라 이름으로 구분이 안 된다. 아래처럼 월드 경계로 번호를 붙였다(`[QA] setup` 원문의 `veh` 항목, 출발 위치 기준):

| 번호 | y 범위 | 앞면 z | x 범위 | 추정 부위 |
|---|---|---|---|---|
| V1 | 0.090~0.480 | −26.050 | −1.075~−0.685 | 뒤쪽 화물칸 |
| V2 | 0.062~0.119 | **−25.721**(가장 앞) | −1.075~−0.685 | 앞 범퍼 아래 |
| V3 | 0.120~0.210 | −25.735 | −1.075~−0.685 | 앞 범퍼 위 |
| V4 | 0.189~0.245 | −25.736 | −1.045~−0.715 | 보닛 앞 |
| V5 | 0.225~0.389 | −25.853 | −1.045~−0.715 | 앞유리 아래 |
| V6 | 0.210~0.390 | −25.913 | −1.045~−0.715 | 운전석 |
| V7 | 0.390~0.420 | −25.945 | −0.985~−0.775 | 지붕 |

### 원본 콘솔 로그

배치(5회 모두 같음, `[QA] setup #9920` 원문 발췌):
```
groundY=0.03 ground=model/L7 objPos=(-0.88,0.03,-25.274) objColMinY=0.03 objColMaxY=0.416 objMinZ=-25.621 vehFront=-25.721 vehMinY=0.062 vehMaxY=0.48
| rb mass=80 kin=False damp=0 angDamp=0.05 constr=None interp=None det=Discrete sleepThr=0.005 com=(-0.014,0.193,0) vehMass=100
| col Model BoxCollider convex=- mat=ObstacleNoBounce f0.6/0.6 b.min=(-1.152,0.03,-25.621) b.max=(-0.636,0.416,-24.927) | dumpScale=(1.875,1.875,1.875)
| veh Body BoxCollider mat=none b.min=(-1.075,0.09,-26.665) b.max=(-0.685,0.48,-26.05) | veh Body BoxCollider mat=none b.min=(-1.075,0.062,-26.279) b.max=(-0.685,0.119,-25.721)
| veh Body BoxCollider mat=none b.min=(-1.075,0.12,-26.05) b.max=(-0.685,0.21,-25.735) | veh Body BoxCollider mat=none b.min=(-1.045,0.189,-25.881) b.max=(-0.715,0.245,-25.736)
| veh Body BoxCollider mat=none b.min=(-1.045,0.225,-25.941) b.max=(-0.715,0.389,-25.853) | veh Body BoxCollider mat=none b.min=(-1.045,0.21,-26.051) b.max=(-0.715,0.39,-25.913)
| veh Body BoxCollider mat=none b.min=(-0.985,0.39,-26.051) b.max=(-0.775,0.42,-25.945)
| ignoreLayer(obj L0 vs ground L7)=False | behind1m=[model(L7,trig=False,b.min=(-3.93,0,-25.5),b.max=(0,0.06,-22.5))] | contactOffset=0.01 solverIt=6
```
콜라이더 높이 0.386(0.030 → 0.416)으로 새 박스 크기와 맞는다. 질량 중심은 도로 위 0.193.

`[CargoImpact]`와 접촉 스텝:
```
[QA] setup #9920 ... ft=0.02 dur=100
[QA] contact detected at step 1 ft=0.06 vVeh=(-0.041,0.827,2.562)
[CargoImpact] v 4.92 → 1.22 → deltaVelocity=3.71
[CargoImpact] damage=min((3.71-1.5)*12, 60)=26.54
[CargoImpact] Parcel#9920 (Normal)  100 → 73.46
[QA] setup #9921 ... ft=1.52 dur=100
[QA] contact detected at step 1 ft=1.56 vVeh=(-0.08,0.999,2.527)
[CargoImpact] v 4.92 → 1.33 → deltaVelocity=3.63
[CargoImpact] damage=min((3.63-1.5)*12, 60)=25.58
[CargoImpact] Parcel#9921 (Normal)  100 → 74.42
[QA] setup #9922 ... ft=3.02 dur=100
[QA] contact detected at step 1 ft=3.06 vVeh=(-0.041,0.827,2.562)
[CargoImpact] v 4.92 → 1.22 → deltaVelocity=3.71
[CargoImpact] damage=min((3.71-1.5)*12, 60)=26.54
[CargoImpact] Parcel#9922 (Normal)  100 → 73.46
[QA] setup #9923 ... ft=4.52 dur=100
[QA] contact detected at step 1 ft=4.56 vVeh=(-0.08,0.999,2.527)
[CargoImpact] v 4.92 → 1.33 → deltaVelocity=3.63
[CargoImpact] damage=min((3.63-1.5)*12, 60)=25.58
[CargoImpact] Parcel#9923 (Normal)  100 → 74.42
[QA] setup #9924 ... ft=6.02 dur=100
[QA] contact detected at step 1 ft=6.06 vVeh=(-0.041,0.827,2.562)
[CargoImpact] v 4.92 → 1.22 → deltaVelocity=3.71
[CargoImpact] damage=min((3.71-1.5)*12, 60)=26.54
[CargoImpact] Parcel#9924 (Normal)  100 → 73.46
[QA] DONE
```
이번에도 두 패턴이 번갈아 정확히 반복됐다: B(#9920·#9922·#9924)와 A(#9921·#9923). 1.5초 뒤 차량 속도 0.004~0.012. 이 실행 중 새 오류·경고 0건.

스텝별 기록(k = 접촉 스텝 이후 완료된 고정 스텝 수). 패턴 B(#9924 원문, #9920·#9922 동일):
```
[QA] probe step=1 k=-1 veh v=(0,0.036,4.92) h=4.92 pos=(-0.88,0.001,-26.141) | dumpster v=(0,0,0) h=0 w=(0,0,0) pos=(-0.88,0.03,-25.274) rot=(0,0,0) sleeping=False
[QA] probe step=2 k=0 veh v=(-0.041,0.827,2.562) h=2.562 pos=(-0.878,0.015,-26.111) | dumpster v=(0.065,-0.009,1.406) h=1.408 w=(0.65,0.091,-0.08) pos=(-0.879,0.03,-25.249) rot=(1.04,0.162,359.946)
[QA] probe step=3 k=1 veh v=(-0.085,0.704,1.663) h=1.665 pos=(-0.877,0.03,-26.089) | dumpster v=(0.081,-0.085,1.17) h=1.173 w=(-0.428,-0.027,-0.107) pos=(-0.876,0.03,-25.231)
[QA] probe step=4 k=2 veh v=(-0.094,0.126,1.21) h=1.213 pos=(-0.875,0.033,-26.078) | dumpster v=(0.077,-0.116,0.858) h=0.861 pos=(-0.874,0.03,-25.211)
[QA] probe step=5 k=3 veh v=(-0.101,0.18,0.691) h=0.699 | dumpster v=(0.066,-0.077,0.541) h=0.545 pos=(-0.872,0.03,-25.202)
[QA] probe step=12 k=10 veh v=(0.007,-0.089,-0.089) h=0.089 | dumpster v=(0,-0.01,-0.005) h=0.005 pos=(-0.871,0.03,-25.196) rot=(0.116,0.799,0.031)
```
패턴 A(#9921 원문, #9923 동일):
```
[QA] probe step=2 k=0 veh v=(-0.08,0.999,2.527) h=2.528 pos=(-0.879,0.019,-26.119) | dumpster v=(0.116,0.244,0.89) h=0.898 w=(-0.285,0.102,-0.273) pos=(-0.878,0.035,-25.256) rot=(359.92,0.15,359.671)
[QA] probe step=3 k=1 veh v=(-0.1,0.585,1.793) h=1.795 | dumpster v=(0.12,-0.013,0.838) h=0.846 pos=(-0.875,0.036,-25.239)
[QA] probe step=4 k=2 veh v=(-0.073,0.303,1.289) h=1.291 | dumpster v=(0.094,-0.222,0.669) h=0.675 pos=(-0.873,0.032,-25.228)
[QA] probe step=5 k=3 veh v=(-0.104,0.235,0.759) h=0.766 | dumpster v=(0.067,-0.108,0.174) h=0.187 pos=(-0.871,0.03,-25.224)
[QA] probe step=12 k=10 veh v=(-0.005,-0.168,-0.175) h=0.175 | dumpster v=(0.001,-0.007,-0.01) h=0.01 pos=(-0.871,0.03,-25.224) rot=(0.038,0.873,0.041)
```

접촉 스텝(k=0) 차량-Dumpster 접촉 원문, #9924(패턴 B):
```
[QA] contact #9924 k=0 VEH Body(BoxCollider) x Model(BoxCollider) enter pt=(-0.715,0.211,-25.638) hRoad=0.181 vehLocal=(0.542,0.719,1.546) n(d->o)=(0,0,-1) sep=0.0167 imp(d->o)=(0,0,0)
[QA] contact #9924 k=0 VEH Body(BoxCollider) x Model(BoxCollider) enter pt=(-1.045,0.211,-25.638) hRoad=0.181 vehLocal=(-0.558,0.726,1.543) n(d->o)=(0,0,-1) sep=0.0167 imp(d->o)=(0,0,0)
[QA] contact #9924 k=0 VEH Body(BoxCollider) x Model(BoxCollider) enter pt=(-0.715,0.197,-25.641) hRoad=0.167 vehLocal=(0.541,0.675,1.537) n(d->o)=(0,0,-1) sep=0.02 imp(d->o)=(0,0,0)
[QA] contact #9924 k=0 VEH Body(BoxCollider) x Model(BoxCollider) enter pt=(-1.045,0.197,-25.641) hRoad=0.167 vehLocal=(-0.558,0.681,1.534) n(d->o)=(0,0,-1) sep=0.02 imp(d->o)=(0,0,0)
[QA] contact #9924 k=0 VEH Body(BoxCollider) x Model(BoxCollider) enter pt=(-0.685,0.211,-25.636) hRoad=0.181 vehLocal=(0.642,0.72,1.552) n(d->o)=(0,0,-1) sep=0.015 imp(d->o)=(0,0,-86.82)
[QA] contact #9924 k=0 VEH Body(BoxCollider) x Model(BoxCollider) enter pt=(-0.685,0.121,-25.636) hRoad=0.091 vehLocal=(0.64,0.42,1.566) n(d->o)=(0,0,-1) sep=0.0149 imp(d->o)=(0,0,0)
[QA] contact #9924 k=0 VEH Body(BoxCollider) x Model(BoxCollider) enter pt=(-1.075,0.121,-25.636) hRoad=0.091 vehLocal=(-0.66,0.428,1.562) n(d->o)=(0,0,-1) sep=0.0149 imp(d->o)=(0,0,0)
[QA] contact #9924 k=0 VEH Body(BoxCollider) x Model(BoxCollider) enter pt=(-1.075,0.211,-25.636) hRoad=0.181 vehLocal=(-0.658,0.727,1.548) n(d->o)=(0,0,-1) sep=0.015 imp(d->o)=(0,0,-77.648)
[QA] contact #9924 k=0 VEH Body(BoxCollider) x Model(BoxCollider) enter pt=(-0.685,0.119,-25.622) hRoad=0.089 vehLocal=(0.64,0.417,1.611) n(d->o)=(0,0,-1) sep=0.0014 imp(d->o)=(0,0,-35.976)
[QA] contact #9924 k=0 VEH Body(BoxCollider) x Model(BoxCollider) enter pt=(-0.685,0.062,-25.622) hRoad=0.032 vehLocal=(0.639,0.227,1.62) n(d->o)=(0,0,-1) sep=0.0014 imp(d->o)=(0,0,0)
[QA] contact #9924 k=0 VEH Body(BoxCollider) x Model(BoxCollider) enter pt=(-1.075,0.062,-25.622) hRoad=0.032 vehLocal=(-0.661,0.235,1.616) n(d->o)=(0,0,-1) sep=0.0014 imp(d->o)=(0,0,-8.35)
[QA] contact #9924 k=0 VEH Body(BoxCollider) x Model(BoxCollider) enter pt=(-1.075,0.119,-25.622) hRoad=0.089 vehLocal=(-0.66,0.425,1.608) n(d->o)=(0,0,-1) sep=0.0014 imp(d->o)=(0,0,-19.004)
```
같은 스텝 Dumpster-도로 접촉(#9924, 6점, 모두 `model(MeshCollider)`, 법선 (0,−1,0), sep 0): 충격량 y = −38.2(z=−25.5, x=−1.152) / −51.641(z=−25.5, x=−0.636) / −6.235(z=−24.927, x=−0.766) / 나머지 3점 0. 접촉 직전 스텝(k=−1)의 도로 수직 충격량 합은 15.7(= 80 × 9.81 × 0.02, 자중만).

접촉 스텝(k=0) 차량-Dumpster 접촉 원문, #9921(패턴 A, 충격량이 0이 아닌 점만, 나머지 9점은 위치·법선·분리 거리가 #9924와 같고 충격량 0):
```
[QA] contact #9921 k=0 VEH Body(BoxCollider) x Model(BoxCollider) enter pt=(-0.685,0.119,-25.622) hRoad=0.089 vehLocal=(0.635,0.437,1.637) n(d->o)=(0,0,-1) sep=0.0014 imp(d->o)=(0,0,-132.181)
[QA] contact #9921 k=0 VEH Body(BoxCollider) x Model(BoxCollider) enter pt=(-1.075,0.062,-25.622) hRoad=0.032 vehLocal=(-0.666,0.254,1.639) n(d->o)=(0,0,-1) sep=0.0014 imp(d->o)=(0,0,-56.702)
[QA] contact #9921 k=0 VEH Body(BoxCollider) x Model(BoxCollider) enter pt=(-1.075,0.119,-25.622) hRoad=0.089 vehLocal=(-0.665,0.443,1.627) n(d->o)=(0,0,-1) sep=0.0014 imp(d->o)=(0,0,-42.424)
```
전체 접촉 원문(5회 × k=−1..2, 약 360줄)은 이 실행의 콘솔 seq 2537~2927에 있다. 아래 표는 그 합계다.

### 접촉 표 (#9924, 패턴 B)

접촉 스텝(k=0) 차량-Dumpster 12점. 차량 콜라이더 번호는 접촉점 위치와 위 경계 표로 정했다(접촉점 z가 그 콜라이더 앞면 z와 같고, x·y가 그 콜라이더 모서리와 같음).

| 차량 콜라이더 | 접촉점(모서리) | 도로 위 높이 | 법선(Dumpster→차량) | 분리 거리 | 충격량 z |
|---|---|---|---|---|---|
| V2 앞 범퍼 아래(BoxCollider) | 윗모서리 좌/우 | 0.089 | (0,0,−1) | 0.0014 | −19.0 / −36.0 |
| V2 | 아랫모서리 좌/우 | 0.032 | (0,0,−1) | 0.0014 | −8.35 / 0 |
| V3 앞 범퍼 위(BoxCollider) | 윗모서리 좌/우 | 0.181 | (0,0,−1) | 0.0150 | −77.6 / −86.8 |
| V3 | 아랫모서리 좌/우 | 0.091 | (0,0,−1) | 0.0149 | 0 / 0 |
| V4 보닛 앞(BoxCollider) | 아래쪽 좌/우 | 0.181 / 0.167 | (0,0,−1) | 0.0167 / 0.0200 | 0 |

스텝별 합계(같은 패턴의 회차는 값이 모두 같음):

| 회차 | k | 차량-Dumpster 접촉 | 높이 범위 | 법선(Dumpster→차량) | 분리 거리 범위 | 충격량 합(Dumpster→차량) | 도로 수직 충격량 합 |
|---|---|---|---|---|---|---|---|
| B #9924 | 0 | 12점 enter (V2·V3·V4) | 0.032~0.181 | (0,0,−1) | +0.0014~+0.0200 | (0, 0, −227.8) | 96.1 |
| B #9924 | 1 | 12점 stay | 0.022~0.173 | (−0.003,0.018,−1) | −0.0083~+0.0101 | (−0.2, 1.5, −83.3) | 84.1 |
| B #9924 | 2 | 12점 stay | 0.021~0.175 | (−0.007,0.026,−1) | −0.0128~+0.0047 | (−0.3, 1.0, −38.3) | 51.9 |
| A #9921 | 0 | 12점 enter (V2만 충격량) | 0.032~0.181 | (0,0,−1) | +0.0014~+0.0200 | (0, 0, −231.3) | 133.4 |
| A #9921 | 1 | 12점 stay | 0.016~0.167 | (−0.003,−0.001,−1) | −0.0100~+0.0075 | (−0.2, −0.1, −65.3) | 57.9 |
| A #9921 | 2 | 12점 stay | 0.005~0.155 | (−0.006,−0.006,−1) | −0.0088~+0.0045 | (−0.2, −0.2, −42.1) | 46.0 |

Dumpster 윗면(0.386)이나 윗모서리에는 닿지 않는다. 모든 접촉이 Dumpster 앞면(z=−25.621 부근)에, Dumpster 질량 중심 높이(0.193)보다 아래에서 일어난다.

### 운동량 수지 (z 성분, 단위 N·s)

| 회차 | k | 차량이 Dumpster에 준 충격량 | Dumpster 운동량 변화 80×Δvz | 도로가 가져간 수평 충격량(차이) | 도로 수직 충격량 N | μN (μ=0.6) | 비율 |
|---|---|---|---|---|---|---|---|
| B | 0 | 227.8 | +112.5 (0 → 1.406) | 115.3 | 96.1 | 57.7 | **2.00** |
| B | 1 | 83.3 | −18.9 (1.406 → 1.17) | 102.2 | 84.1 | 50.5 | **2.02** |
| B | 2 | 38.3 | −25.0 (1.17 → 0.858) | 63.3 | 51.9 | 31.1 | **2.03** |
| A | 0 | 231.3 | +71.2 (0 → 0.89) | 160.1 | 133.4 | 80.0 | **2.00** |
| A | 1 | 65.3 | −4.2 (0.89 → 0.838) | 69.5 | 57.9 | 34.7 | **2.00** |
| A | 2 | 42.1 | −13.5 (0.838 → 0.669) | 55.6 | 46.0 | 27.6 | **2.01** |

검산: 차량 쪽 k=0 수평 운동량 손실 100×(4.92−2.562)=235.8 = 접촉 충격량 227.8 + 손을 뗀 감속분 8.0(대조군 스텝당 0.08 m/s). 도로 수직 충격량은 k=−1에 자중 15.7과 정확히 같다. 그래서 보고된 충격량 값 자체는 믿을 만하다.

### AC-7 Dumpster 행 (사용자 박스 콜라이더, 카탈로그 수치는 변경하지 않음)

| 회차 | 접촉 스텝 차량 vy | 차량 Δv | 감소량 | Dumpster 수평 속도 k=1 / 3 / 10 |
|---|---|---|---|---|
| #9920 (B) | +0.827 | 3.71 | −26.54 | 1.173 / 0.545 / 0.005 |
| #9921 (A) | +0.999 | 3.63 | −25.58 | 0.846 / 0.187 / 0.010 |
| #9922 (B) | +0.827 | 3.71 | −26.54 | 1.173 / 0.545 / 0.005 |
| #9923 (A) | +0.999 | 3.63 | −25.58 | 0.846 / 0.187 / 0.010 |
| #9924 (B) | +0.827 | 3.71 | −26.54 | 1.173 / 0.545 / 0.005 |

| 기준 | 카탈로그 값 | ±20% 범위 | 실측 감소량 | 판정 |
|---|---|---|---|---|
| 카탈로그 계산값 | −9 | −7.2 ~ −10.8 | −25.58 ×2 / −26.54 ×3 | **범위 밖**(약 2.9배) |
| 현재 카탈로그 값 | −26 | −20.8 ~ −31.2 | −25.58 ×2 / −26.54 ×3 | **범위 안**(5회 모두) |

이전 두 콜라이더 구성(convex Mesh 3개: −25.22~−27.56, 루트 박스: −25.65/−26.54)과 사실상 같다.

### 관찰

- **차량은 Dumpster 앞면을 수평으로 친다.** 접촉 스텝의 법선은 정확히 (0,0,−1)이고, 차량에 들어간 충격량의 y 성분은 0이다. 닿는 차량 콜라이더는 앞쪽 아래 BoxCollider 3개(V2 범퍼 아래, V3 범퍼 위, V4 보닛 앞)이고 높이는 도로 위 0.03~0.18이다. 이전 추정("기울어진 면·뚜껑 모서리에 걸려 위로 밀림")은 **틀렸다**.
- 그런데도 접촉 스텝 차량 vy가 +0.83/+1.00으로 튄다. Dumpster 접촉이 위로 미는 힘을 주지 않으므로 이 vy는 다른 곳에서 온다. 수평 충격량이 차량 질량 중심보다 낮은 앞쪽 끝에 들어가 차량 앞이 숙여지고, 그 결과 서스펜션·바퀴(또는 차량-도로 접촉)가 위로 밀어낸 것으로 보인다. 차량-도로 접촉과 차량 각속도는 기록하지 않아서 **검증하지 않았다**. 이 vy는 Δv 계산(y 제외)에 직접 들어가지 않는다.
- **운동량이 사라지는 곳은 Dumpster와 도로 사이 마찰이다.** 매 스텝 도로가 가져가는 수평 충격량이 `μ × 수직 충격량`의 **정확히 2.00~2.03배**다(6개 스텝 모두). 즉 실효 마찰계수가 0.6이 아니라 약 1.2로 작동한다. 이전 회차에서 본 "떨어진 뒤 감속 약 12 m/s²(μg≈5.9의 2배)"와도 맞는다.
- 수직 충격량도 자중보다 크다(k=0에 96~133, 자중분 15.7의 6~8배). 차량이 Dumpster 질량 중심(0.193)보다 아래를 밀어 Dumpster가 앞으로 넘어지려 하고, 그 회전을 도로가 막느라 앞쪽 모서리(z=−25.5)에서 큰 수직력이 생긴다. 큰 수직력 × 2배 마찰이 첫 스텝에 들어온 충격량의 절반(B) ~ 70%(A)를 먹는다. 그래서 Dumpster는 0.9~1.4 m/s밖에 나가지 못하고, 차량은 판정 창 3스텝 내내 멈춰 가는 Dumpster를 밀며 계속 감속한다(2.56 → 1.66 → 1.21).
- **패턴 A/B 차이도 같은 설명으로 맞는다.** A에서는 접촉 스텝 충격량이 전부 V2(높이 0.03~0.09, 더 낮음)에 실려 넘어뜨리는 힘이 더 크다 → 도로 수직 충격량 133(B는 96) → 마찰 더 큼 → Dumpster 0.89 m/s(B는 1.41). A에서는 Dumpster 뒤쪽이 들려 vy +0.24가 된다. 같은 접촉점 12개 중 솔버가 어디에 충격량을 싣느냐가 회차마다 번갈아 바뀐다.
- 카탈로그 −9는 "충돌 순간 운동량 교환만"(공동 속도 2.73, 차량 Δv≈2.19)을 가정한다. 실측 접촉 스텝 하나만 보면 차량 수평 속도 2.53~2.56으로 그 가정과 비슷하다. 차이는 **창 0.06초 동안 Dumpster가 도로에 붙잡혀 차량을 계속 막는 것**에서 나온다.
- 2배 마찰의 원인 추정(검증하지 않음): `DynamicsManager`의 Patch 마찰 + `Improved Patch Friction` 꺼짐. PhysX 패치 마찰은 이 옵션이 꺼져 있으면 정지 마찰이 해석값을 넘을 수 있는 것으로 알려져 있다(두 앵커 각각에 한도 적용). 실측 비율 2.00과 맞지만, 설정을 켜서 비교해 보지는 않았다(ProjectSettings는 바꾸지 않음). 이 설정은 전역이라 모든 장애물·차량에 영향을 준다.
- 참고: Dumpster 밑면은 z −25.621~−24.927인데 도로 접촉점의 앞쪽 끝은 z=−25.5(도로 타일 `model` 경계)다. 앞쪽 0.12 m는 이 타일 밖에 걸려 있다. 결과에 주는 영향은 확인하지 않았다.

### 종료 상태

- 시퀀서가 끝날 때 `ContactEvent` 구독을 스스로 해제했다. Play mode 안에서 `cleanup_contacts.cs`(`isPlaying` 가드)를 실행: PlayerLoop 훅 제거, `ContactEvent` 처리기 before=0 / after=0, `QA_*` 루트 없음(시퀀서가 이미 정리), `dirty=False`. 그다음 Play mode를 종료했다.
- 종료 뒤 `isPlaying=False`, 활성 씬 `SampleMapDesign` 루트에 `QA_*` 없음, `isDirty=False`, PlayerLoop에 `QAHook*` 0개, `ContactEvent` 처리기 0개, `runInBackground=False`(내가 켠 것을 되돌림). 디스크의 씬 파일에 `QA_` 문자열 0건. 컴파일 실패 없음, 콘솔 오류·경고 0. 씬은 저장하지 않았다. `providesContacts`는 Play mode 인스턴스에만 켰고 인스턴스와 함께 사라졌다.

### 확인하지 못한 것 / 한계

- 차량 vy가 튀는 원인(차량-도로 접촉, 서스펜션, 차량 각속도)은 기록하지 않았다. Dumpster 접촉이 원인이 아니라는 것만 확인했다.
- 2배 마찰의 원인은 추정이다. `Improved Patch Friction`을 켜거나 마찰을 바꾼 비교는 하지 않았다(설정·Assets 수정 금지).
- `ContactPairPoint.impulse`에는 마찰 성분이 없어서 도로 마찰 충격량은 운동량 차이로 구했다(Dumpster의 x 성분 0.07~0.12 m/s는 무시).
- 실제 키 입력 운전은 하지 않았다(속도 직접 대입, 손을 뗀 상태).
