## Performance Profile: Assets/Game/Scripts
Generated: 2026-11-16

방식: 정적 분석(코드 읽기). 런타임 프로파일러 측정값 없음. 후보를 찾는 단계이며 영향 추정은 모두 미확인이다.

### Performance Budgets
`project.yaml`의 `performance.*` 4개 키가 모두 비어 있다. 설계 문서·CLAUDE.md에도 예산이 없다.
`performance.enforce`는 `warn`(기본값)이라 위반이 생겨도 차단하지 않는다.

| Metric | Budget | Estimated Current | Status |
|--------|--------|-------------------|--------|
| Frame time | 미설정 | 측정 없음 | NOT ASSESSED |
| Memory | 미설정 | 측정 없음 | NOT ASSESSED |
| Load time | 미설정 | 측정 없음 | NOT ASSESSED |
| Draw calls | 미설정 | 측정 없음 | NOT ASSESSED |

### Find 계열 호출 (7곳)
전부 `Awake`/`Start` 1회성이라 프레임 비용은 없다.

| 위치 | 호출 | 실행 시점 | 판단 |
|------|------|-----------|------|
| DeliveryNavigationCompass.cs:59, 67 | FindAnyObjectByType | Start | 1회, 문제 없음 |
| InteractionProgressUI.cs:19 | FindAnyObjectByType | Start | 1회, 문제 없음 |
| DrivingModeHUD.cs:33 | FindAnyObjectByType | Start | 1회, 문제 없음 |
| LogisticsCenterUI.cs:80 | FindAnyObjectByType | Start | 1회, 문제 없음 |
| ManagerBase.cs:18 | GameObject.Find(typeof(T).Name) | `Instance` getter, 인스턴스가 없을 때만 | 첫 접근 때만 실행. 이름 문자열 의존이라 취약하지만 비용 문제는 아님 |
| MapColliderBaker.cs | (에디터 전용) | 에디터 | 런타임 무관 |

Find 자체의 성능 위험은 낮다. 남는 문제는 구조(씬 안 유일 객체를 이름·타입 검색으로 연결)와 `ManagerBase` 이름 규약 의존이다.

### Hotspots Identified
| # | Location | Issue | Estimated Impact | Fix Effort |
|---|----------|-------|------------------|------------|
| 1 | DeliveryNavigationCompass.cs:74-140 | `LateUpdate`가 매 프레임 `CollectTargets`를 다시 계산한다. 적재 물류 n개에 대해 선택 반복 × `_targets.Contains`가 겹쳐 O(n·k²) | 미확인(적재 수가 작으면 무시 가능). 값이 바뀔 때만 계산하면 0으로 줄어든다 | S |
| 2 | LogisticsCenterUI.cs:266-297, 428, 508 | 패널 갱신마다 행을 Instantiate/Destroy한다. GC 할당과 스파이크 후보 | 미확인. 패널이 열려 있고 물류가 바뀔 때만 발생 | M |
| 3 | LogisticsCenterUI.cs:270, 297 | 행마다 `GetComponentInChildren<TMP_Text>(true)` | 낮음. 템플릿에 직렬화 참조를 두면 제거 | S |
| 4 | PlayerMovementController.cs:238 | FixedUpdate마다 SphereCast 4회 | 차량 서스펜션에 필요한 비용. 물리 틱 수 대비 정상 범위로 보이나 미확인 | 조치 불필요 |
| 5 | PlayerMovementController.cs:192-193 | FixedUpdate마다 `GetModeSettings` 2회 구조체 복사 | 낮음 | S |

### Optimization Recommendations (Priority Order)
1. **내비 화살표 대상을 이벤트로 갱신** — `PlayerInteractionController`에 이미 `LoadedParcelsChanged` 이벤트가 있으므로 그 이벤트에서만 `CollectTargets`를 호출하고 `LateUpdate`는 위치 갱신만 한다.
   - Location: DeliveryNavigationCompass.cs:74-85
   - Expected gain: 미확인(후보)
   - Risk: Low. 이벤트가 물류 제거(배송 완료, 시간 초과)에도 발생하는지 확인해야 한다.
   - Approach: OnEnable/OnDisable에서 구독 해제를 짝지어 처리.
2. **Find를 직접 참조로 교체** — 인스펙터 직렬화 필드로 연결한다. 이미 `DeliveryNavigationCompass`는 직렬화 필드를 먼저 확인하고 null일 때만 Find를 쓰는 형태라, 씬에서 필드만 채우면 Find 경로는 자연히 쓰이지 않는다.
   - Expected gain: 성능이 아닌 안정성(이름·타입 검색 실패 가능성 제거)
   - Risk: Low. 씬 편집은 Unity Editor로 해야 한다(`.unity` 직접 수정 금지).
3. **LogisticsCenterUI 행 풀링** — 행 재생성이 실제 스파이크로 나오는지 측정한 뒤 결정한다.

### Quick Wins (< 1 hour each)
- `LogisticsCenterUI`의 `GetComponentInChildren<TMP_Text>(true)`를 템플릿 직렬화 참조로 대체
- `DeliveryNavigationCompass.CollectTargets`를 이벤트 기반으로 전환

### Requires Investigation
- `PlayerInteractionController.RefreshInteractionState()`(Update에서 매 프레임 호출) — 이번에 본문을 읽지 않았다.
- `PlayerInteractionController.LoadedParcels` 프로퍼티가 호출마다 새 컬렉션을 만드는지 여부.
- `VehicleConfig.WheelOffsets`, `GetModeSettings`가 호출마다 복사·할당하는지 여부.
- `DeliveryDeadlineSystem`, `DailyOperationSystem`, `GameTimeSystem`, `GameTimeHUD`, `PlayerUIController`의 Update 본문(이번에는 일부만 읽음).
- 실제 프레임 시간·GC 할당은 Unity Profiler로 측정해야 확정된다.

### Deferred to Polish
없음.
