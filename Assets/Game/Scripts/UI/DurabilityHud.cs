using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

using Game.Logistics;
using Game.Player.Interaction;
using Game.Systems;

namespace Game.UI
{
    /// <summary>
    /// 충돌 내구도 피드백 HUD(Story 006, 기획서 13장): 차량 위 감소량 −N, 적재 물류 내구도 패널. 폐기 알림은 ToastHud가 표시한다(Story 021).
    /// UI Toolkit 시범 화면이며 내구도는 계산하지 않고 이벤트로 받은 값만 표시한다.
    /// </summary>
    [RequireComponent(typeof(PanelRenderer))]
    public class DurabilityHud : MonoBehaviour
    {
        private const string SpoiledGroupTitle = "처분 장소";
        private const string LogisticsCenterTitle = "물류센터";

        // 유지 시간 비율 중 이 지점부터 선형으로 사라진다
        private const float PopupFadeStart = 0.55f;

        // 키보드 상단 숫자열 1~9 = 패널 위에서부터 그룹 순번(숫자패드 제외). 기획: quick-specs/cargo-panel-fold-scroll
        private static readonly Key[] GroupToggleKeys =
        {
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
            Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9,
        };

        // 아래 세 참조는 비우면 Awake에서 씬의 Player를 찾아 채운다
        [Tooltip("감소량 이벤트를 받을 차량의 충돌 컨트롤러")]
        [SerializeField] private PlayerCargoImpactController _impactController;
        [Tooltip("적재 물류 목록·폐기 이벤트를 받을 차량의 상호작용 컨트롤러")]
        [SerializeField] private PlayerInteractionController _interactionController;
        [Tooltip("감소량 표시가 따라갈 차량 Transform")]
        [SerializeField] private Transform _vehicle;
        [Tooltip("월드→화면 변환 카메라. 비우면 Camera.main")]
        [SerializeField] private Camera _camera;

        [Header("Damage Popup")]
        [Tooltip("차량 기준 감소량 표시 위치(월드 m)")]
        [SerializeField] private Vector3 _popupOffset = new(0f, 1f, 0f);
        [Tooltip("감소량 표시 유지 시간(초). 마지막 45% 동안 사라진다")]
        [SerializeField, Min(0.1f)] private float _popupDuration = 0.9f;
        [Tooltip("유지 시간 동안 위로 떠오르는 거리(패널 px)")]
        [SerializeField, Min(0f)] private float _popupRise = 60f;

        [Header("Cargo Panel")]
        [Tooltip("적재 물류 목록 최대 높이(패널 px). 넘치면 스크롤")]
        [SerializeField, Min(100f)] private float _listMaxHeight = 480f;
        [Tooltip("마우스 휠 한 칸·PageUp/PageDown 한 번의 스크롤 거리(패널 px)")]
        [SerializeField, Min(1f)] private float _scrollStep = 120f;

        [Header("Templates")]
        [Tooltip("배송지 그룹 헤더 템플릿(CargoGroupHeader.uxml)")]
        [SerializeField] private VisualTreeAsset _groupHeaderTemplate;
        [Tooltip("물류센터 행 템플릿(CenterNavRow.uxml)")]
        [SerializeField] private VisualTreeAsset _centerRowTemplate;
        [Tooltip("물류 행 템플릿(ParcelRow.uxml)")]
        [SerializeField] private VisualTreeAsset _parcelRowTemplate;
        [Tooltip("감소량 −N 팝업 템플릿(DamagePopup.uxml)")]
        [SerializeField] private VisualTreeAsset _damagePopupTemplate;

        private readonly List<(Label label, float startTime)> _popups = new();
        // 펼친 그룹 키(배송지 이름 또는 처분 장소). 없는 키는 접힘
        private readonly HashSet<string> _expandedGroups = new();
        // 패널 위에서부터의 그룹 순서. 숫자키 번호와 같다
        private readonly List<string> _groupOrder = new();
        // 펼친 그룹에 그려진 제한 시간 물류(Express·Fresh)의 남은 시간 바와, 행을 그릴 때의 시간 초과 여부
        private readonly Dictionary<Parcel, (VisualElement fill, bool overdue)> _deadlineFills = new();
        // 그룹 헤더의 방향 표시(화살표·거리)와 그 목적지. 패널을 다시 그릴 때 채우고 회전·거리만 매 프레임 바꾼다
        private readonly List<(VisualElement arrow, Label distance, Transform destination)> _groupNavs = new();
        private LogisticsCenter _logisticsCenter;
        private (VisualElement arrow, Label distance, Transform destination) _centerNav;
        private VisualElement _root;
        private VisualElement _hudRoot;
        // 우상단 세로 열의 세 패널: 물류센터(항상), 배송 현황(배송지 그룹), 폐기(처분 장소 그룹)
        private VisualElement _centerPanel;
        private VisualElement _cargoPanel;
        private VisualElement _disposalPanel;
        private ScrollView _cargoList;

        private void Awake()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            // Hud 프리팹은 씬의 차량을 참조할 수 없으므로, 비어 있으면 씬에서 찾는다(세 컴포넌트 모두 Player 오브젝트에 있다)
            if (_interactionController == null)
            {
                _interactionController = FindAnyObjectByType<PlayerInteractionController>();
            }

            if (_interactionController != null)
            {
                if (_impactController == null)
                {
                    _impactController = _interactionController.GetComponent<PlayerCargoImpactController>();
                }

                if (_vehicle == null)
                {
                    _vehicle = _interactionController.transform;
                }
            }

            if (_groupHeaderTemplate == null || _centerRowTemplate == null || _parcelRowTemplate == null || _damagePopupTemplate == null)
            {
                Debug.LogError($"DurabilityHud UXML templates are not assigned in {gameObject.name}");
                enabled = false;
                return;
            }

            if (_impactController == null || _interactionController == null || _vehicle == null || _camera == null)
            {
                Debug.LogError($"DurabilityHud references are missing in {gameObject.name}");
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _impactController.ImpactDamageApplied += OnImpactDamageApplied;
            _interactionController.LoadedParcelsChanged += OnLoadedParcelsChanged;
            GetComponent<PanelRenderer>().RegisterUIReloadCallback(OnUIReload);
        }

        private void OnDisable()
        {
            GetComponent<PanelRenderer>().UnregisterUIReloadCallback(OnUIReload);
            _impactController.ImpactDamageApplied -= OnImpactDamageApplied;
            _interactionController.LoadedParcelsChanged -= OnLoadedParcelsChanged;
            ClearPopups();
            _cargoList = null;
        }

        // PanelRenderer는 트리를 만들거나 다시 불러올 때마다 루트를 넘겨준다. 트리가 오기 전에는 표시를 건너뛴다(_cargoList == null)
        private void OnUIReload(PanelRenderer _, VisualElement root, int version)
        {
            ClearPopups();
            _root = root;
            _hudRoot = _root.Q("hudRoot");
            _centerPanel = _root.Q("centerPanel");
            _cargoPanel = _root.Q("cargoPanel");
            _disposalPanel = _root.Q("disposalPanel");
            _cargoList = _root.Q<ScrollView>("cargoList");
            if (_hudRoot == null || _centerPanel == null || _cargoPanel == null || _disposalPanel == null || _cargoList == null)
            {
                Debug.LogError($"DurabilityHud.uxml is not assigned to the PanelRenderer in {gameObject.name}");
                enabled = false;
                return;
            }

            // UI Builder 미리보기용 예시 요소는 런타임에 지운다
            foreach (VisualElement sample in _root.Query("sample").ToList())
            {
                sample.RemoveFromHierarchy();
            }

            // 휠은 포인터 위치와 무관하게 Update에서 직접 처리하므로 ScrollView 자체 휠 처리를 막는다
            _cargoList.contentViewport.pickingMode = PickingMode.Ignore;
            _cargoList.style.maxHeight = _listMaxHeight;
            BuildLogisticsCenterRow();
            RefreshCargoPanel();
        }

        private void ClearPopups()
        {
            foreach ((Label label, float _) in _popups)
            {
                label.RemoveFromHierarchy();
            }

            _popups.Clear();
        }

        // shortcut: 키를 InputActionReference 대신 Keyboard.current로 직접 읽는다. 키 재설정·게임패드가 필요해지면 액션 에셋으로 옮긴다
        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || _cargoList == null || IsOperating() == false)
            {
                return;
            }

            bool changed = false;
            for (int i = 0; i < GroupToggleKeys.Length && i < _groupOrder.Count; i++)
            {
                if (keyboard[GroupToggleKeys[i]].wasPressedThisFrame)
                {
                    string group = _groupOrder[i];
                    if (_expandedGroups.Remove(group) == false)
                    {
                        _expandedGroups.Add(group);
                    }

                    changed = true;
                }
            }

            if (keyboard.digit0Key.wasPressedThisFrame)
            {
                _expandedGroups.UnionWith(_groupOrder);
                changed = true;
            }

            if (keyboard.backquoteKey.wasPressedThisFrame)
            {
                _expandedGroups.Clear();
                changed = true;
            }

            if (changed)
            {
                RefreshCargoPanel();
            }

            // 휠 값의 크기는 플랫폼마다 달라 방향만 쓴다. 위로 굴리면 목록 위쪽을 본다
            float steps = 0f;
            float wheel = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
            if (wheel != 0f)
            {
                steps -= Mathf.Sign(wheel);
            }

            if (keyboard.pageUpKey.wasPressedThisFrame)
            {
                steps -= 1f;
            }

            if (keyboard.pageDownKey.wasPressedThisFrame)
            {
                steps += 1f;
            }

            if (steps != 0f)
            {
                // 범위 밖 값은 ScrollView의 스크롤러가 잘라 낸다
                _cargoList.scrollOffset += new Vector2(0f, steps * _scrollStep);
            }
        }

        private void LateUpdate()
        {
            if (_cargoList == null)
            {
                return;
            }

            UpdateArrows();

            for (int i = _popups.Count - 1; i >= 0; i--)
            {
                float t = (Time.time - _popups[i].startTime) / _popupDuration;
                if (t >= 1f)
                {
                    _popups[i].label.RemoveFromHierarchy();
                    _popups.RemoveAt(i);
                    continue;
                }

                PlacePopup(_popups[i].label, t);
            }

            // 행은 적재 변화 때만 다시 그리므로 바 너비만 매 프레임 줄인다. 시간 초과가 새로 생기면 #지연 태그를 위해 한 번 다시 그린다
            bool overdueChanged = false;
            foreach (KeyValuePair<Parcel, (VisualElement fill, bool overdue)> pair in _deadlineFills)
            {
                pair.Value.fill.style.width = Length.Percent(DeadlineRatio(pair.Key) * 100f);
                overdueChanged |= pair.Key.IsOverdue != pair.Value.overdue;
            }

            if (overdueChanged)
            {
                RefreshCargoPanel();
            }
        }

        // 물류센터 패널은 적재와 무관하게 항상 보이므로 트리를 받을 때 한 번만 만든다. 번호 키는 붙이지 않는다
        private void BuildLogisticsCenterRow()
        {
            _centerPanel.Clear();
            VisualElement row = Create(_centerRowTemplate);
            _centerPanel.Add(row);
            if (_logisticsCenter == null)
            {
                _logisticsCenter = FindAnyObjectByType<LogisticsCenter>();
            }

            row.Q<Label>("titleLabel").text = LogisticsCenterTitle;
            Label distance = row.Q<Label>("distanceLabel");
            distance.text = string.Empty;
            _centerNav = (row.Q("navArrow"), distance, _logisticsCenter != null ? _logisticsCenter.transform : null);
        }

        // 화살표 위쪽 = 차량 전방. 수평면에서 전방과 목적지 방향의 부호 있는 각도(시계 방향 +)로 돌린다
        private void UpdateArrows()
        {
            PointArrow(_centerNav);
            for (int i = 0; i < _groupNavs.Count; i++)
            {
                PointArrow(_groupNavs[i]);
            }
        }

        private void PointArrow((VisualElement arrow, Label distance, Transform destination) nav)
        {
            (VisualElement arrow, Label distance, Transform destination) = nav;
            if (destination == null)
            {
                return;
            }

            Vector3 forward = Vector3.ProjectOnPlane(_vehicle.forward, Vector3.up);
            Vector3 toDestination = Vector3.ProjectOnPlane(destination.position - _vehicle.position, Vector3.up);
            // 월드 1유닛 = 1m
            distance.text = $"{toDestination.magnitude:0}m";
            // 목적지와 수평으로 겹치면 방향이 정의되지 않으므로 직전 각도를 유지한다
            if (forward.sqrMagnitude < 0.0001f || toDestination.sqrMagnitude < 0.0001f)
            {
                return;
            }

            arrow.style.rotate = new Rotate(Vector3.SignedAngle(forward, toDestination, Vector3.up));
        }

        // 하루 종료(조작 잠금) 뒤에는 새 표시를 만들지 않는다(AC5). 운영 시스템이 없는 테스트 씬에서는 표시한다
        private static bool IsOperating()
        {
            return DailyOperationSystem.Instance == null || DailyOperationSystem.Instance.IsOperating;
        }

        private void OnImpactDamageApplied(float damage)
        {
            if (_cargoList == null)
            {
                return;
            }

            RefreshCargoPanel();

            if (IsOperating() == false)
            {
                return;
            }

            // 0.x 감소도 0으로 보이지 않게 올림한다
            var label = (Label)Create(_damagePopupTemplate);
            label.text = $"−{Mathf.CeilToInt(damage)}";
            // hudRoot는 패널 전체를 채우므로 좌표계가 패널과 같고, 폰트·색을 상속받는다
            _hudRoot.Add(label);
            PlacePopup(label, 0f);
            _popups.Add((label, Time.time));
        }

        private void OnLoadedParcelsChanged(Parcel _)
        {
            if (_cargoList != null)
            {
                RefreshCargoPanel();
            }
        }

        private void PlacePopup(Label label, float t)
        {
            Vector2 position = RuntimePanelUtils.CameraTransformWorldToPanel(_root.panel, _vehicle.position + _popupOffset, _camera);
            label.style.left = position.x;
            label.style.top = position.y - _popupRise * t;
            // 생성 직후 USS 전환은 첫 스타일 해석 전에 끝나 버려 투명도를 t로 직접 구동한다
            label.style.opacity = t < PopupFadeStart ? 1f : 1f - (t - PopupFadeStart) / (1f - PopupFadeStart);
        }

        /// <summary>
        /// 적재 물류를 배송지별로 묶어 배송 현황 패널에 다시 그린다. 폐기 물류는 목적지가 처분 장소라 따로 묶어 폐기 패널에 그린다.
        /// 접힌 그룹은 헤더(번호·이름·개수)만, 펼친 그룹은 물류 행까지 그린다. 빈 패널은 숨긴다.
        /// </summary>
        private void RefreshCargoPanel()
        {
            _cargoList.Clear();
            _disposalPanel.Clear();
            _groupOrder.Clear();
            _deadlineFills.Clear();
            _groupNavs.Clear();

            var groups = new Dictionary<string, List<Parcel>>();
            IReadOnlyList<Parcel> parcels = _interactionController.LoadedParcels;
            for (int i = 0; i < parcels.Count; i++)
            {
                Parcel parcel = parcels[i];
                string title = parcel.IsSpoiled ? SpoiledGroupTitle : parcel.Target.name;
                if (groups.TryGetValue(title, out List<Parcel> members) == false)
                {
                    members = new List<Parcel>();
                    groups.Add(title, members);
                    _groupOrder.Add(title);
                }

                members.Add(parcel);
            }

            // 폐기 그룹은 화면에서 맨 아래 패널이므로 숫자키 번호도 마지막으로 둔다
            if (_groupOrder.Remove(SpoiledGroupTitle))
            {
                _groupOrder.Add(SpoiledGroupTitle);
            }

            // 사라진 그룹의 펼침 상태는 버린다. 같은 이름으로 다시 생기면 접힘부터 시작한다
            _expandedGroups.RemoveWhere(title => groups.ContainsKey(title) == false);

            for (int i = 0; i < _groupOrder.Count; i++)
            {
                string title = _groupOrder[i];
                List<Parcel> members = groups[title];
                bool expanded = _expandedGroups.Contains(title);

                VisualElement group = Create(_groupHeaderTemplate);
                (title == SpoiledGroupTitle ? _disposalPanel : _cargoList).Add(group);
                // 폐기 그룹은 처분 장소, 그 외는 배송지를 가리킨다
                Transform destination = members[0].IsSpoiled ? DisposalSite.Instance?.transform : members[0].Target.transform;
                // 숫자키가 닿는 1~9번째 그룹만 키 번호를 붙인다
                if (i < GroupToggleKeys.Length)
                {
                    group.Q<Label>("keyLabel").text = $"{i + 1}";
                }
                else
                {
                    group.Q("keyLabel").RemoveFromHierarchy();
                }

                group.Q<Label>("titleLabel").text = title;
                group.Q<Label>("countLabel").text = $"×{members.Count}";
                Label distance = group.Q<Label>("distanceLabel");
                distance.text = string.Empty;
                _groupNavs.Add((group.Q("navArrow"), distance, destination));
                if (expanded)
                {
                    for (int j = 0; j < members.Count; j++)
                    {
                        AddParcelRow(group, members[j]);
                    }
                }
            }

            bool hasSpoiled = groups.ContainsKey(SpoiledGroupTitle);
            _cargoPanel.EnableInClassList("hud-panel--hidden", _groupOrder.Count == (hasSpoiled ? 1 : 0));
            _disposalPanel.EnableInClassList("hud-panel--hidden", hasSpoiled == false);
        }

        private void AddParcelRow(VisualElement group, Parcel parcel)
        {
            VisualElement row = Create(_parcelRowTemplate);
            group.Add(row);
            row.EnableInClassList("parcel-row--spoiled", parcel.IsSpoiled);

            // 상자 그림 대신 배송 유형 색 블록(Normal 파랑, Express 빨강, Fresh 초록)
            VisualElement icon = row.Q("parcelIcon");
            icon.EnableInClassList("parcel-icon--express", parcel.Type == DeliveryType.Express);
            icon.EnableInClassList("parcel-icon--fresh", parcel.IsFresh);
            // 폐기 물류는 원래 유형과 관계없이 폐기 색으로 덮는다(USS에서 뒤에 선언)
            icon.EnableInClassList("parcel-icon--spoiled", parcel.IsSpoiled);

            DeliveryReward reward = parcel.Reward;
            // 제한 시간 안에(Fresh는 신선도가 가득일 때) 배송했을 때 받는 금액
            row.Q<Label>("priceLabel").text = $"{reward.BaseFee + reward.DistanceFee + reward.TimeBonus + reward.TraitBonus + reward.FreshBonusMax:#,0}₩";

            // 폐기 행에는 바가 없다. Normal은 제한 시간이 없어 바가 없다
            if (parcel.HasDeadline && parcel.IsSpoiled == false)
            {
                VisualElement fill = row.Q("deadlineFill");
                fill.EnableInClassList("parcel-deadline-fill--fresh", parcel.IsFresh);
                fill.style.width = Length.Percent(DeadlineRatio(parcel) * 100f);
                _deadlineFills[parcel] = (fill, parcel.IsOverdue);
            }
            else
            {
                row.Q("deadline").RemoveFromHierarchy();
            }

            CargoTrait traits = parcel.Cargo.Traits;
            Keep(row, "tagSpoiled", parcel.IsSpoiled);
            Keep(row, "tagOverdue", parcel.IsOverdue && parcel.IsSpoiled == false);
            Keep(row, "tagFragile", (traits & CargoTrait.Fragile) != 0);
            Keep(row, "tagHeavy", (traits & CargoTrait.Heavy) != 0);
            Keep(row, "tagLarge", (traits & CargoTrait.Large) != 0);

            // 0이 아닌 내구도가 0으로 보이지 않게 올림한다
            row.Q<Label>("durabilityLabel").text = $"내구도 {Mathf.CeilToInt(parcel.CurrentDurability)}/{parcel.Cargo.Durability}";
        }

        private static float DeadlineRatio(Parcel parcel)
        {
            return parcel.Deadline > 0f ? Mathf.Clamp01(parcel.RemainingDeadline / parcel.Deadline) : 0f;
        }

        // 템플릿 한 개를 만들어 TemplateContainer 래퍼 없이 루트 요소만 꺼낸다(래퍼가 끼면 절대 위치·행 간격 기준이 달라진다)
        private static VisualElement Create(VisualTreeAsset template)
        {
            return template.Instantiate()[0];
        }

        // 템플릿에 고정된 조건부 요소를 조건이 거짓이면 뺀다
        private static void Keep(VisualElement root, string name, bool keep)
        {
            if (keep == false)
            {
                root.Q(name).RemoveFromHierarchy();
            }
        }
    }
}
