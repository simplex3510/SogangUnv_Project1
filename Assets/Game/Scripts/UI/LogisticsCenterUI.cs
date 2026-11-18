using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using Game.Logistics;
using Game.Manager;
using Game.Player.Interaction;

namespace Game.UI
{
    /// <summary>
    /// 물류센터 상차 창(기획서 8.1·8.2, quick-specs/logistics-center-window-layout). 배송지 카드를 거리순 1열로,
    /// 카드 안 물류를 처리 순서(7.4)대로 늘어놓고 누르고 있는 항목을 플레이어에게 넘긴다. 표시만 하고 상차는 컨트롤러가 한다.
    /// </summary>
    [RequireComponent(typeof(PanelRenderer))]
    public class LogisticsCenterUI : MonoBehaviour
    {
        private const string HiddenClass = "lc-root--hidden";
        private const string HoldableClass = "lc-holdable";
        private const string HoldingClass = "lc-holding";
        private const string DisabledClass = "lc-disabled";
        private const string TargetClass = "lc-row--target";
        private const string DisposalTitle = "처분 장소";

        [SerializeField] private LogisticsCenter _center;
        [Tooltip("배송지 카드 템플릿(CenterCard.uxml)")]
        [SerializeField] private VisualTreeAsset _cardTemplate;
        [Tooltip("물류 행 템플릿(CenterRow.uxml)")]
        [SerializeField] private VisualTreeAsset _rowTemplate;

        private PlayerInteractionController _controller;
        private readonly List<ParcelGroup> _groups = new();
        private readonly Dictionary<Parcel, VisualElement> _parcelRows = new();
        private readonly Dictionary<Parcel, VisualElement> _parcelFills = new();
        private readonly Dictionary<DeliveryTarget, Label> _groupCounts = new();
        private VisualElement _windowRoot;
        private ScrollView _cardList;
        private Label _totalLabel;
        private Label _capacityBadge;
        private Label _weightBadge;
        private bool _isHolding;
        private bool _needsRefresh;
        private List<Parcel> _heldParcels;
        private VisualElement _heldElement;
        // 지금 상차 중인 물류 행 테두리를 따라 차오르는 진행 표시. 한 번에 한 행에만 붙는다
        // PanelRenderer가 다시 불러오면 이전 요소는 해제되므로 트리를 받을 때마다 새로 만든다
        private BorderProgress _trace;

        private const int EscapePriority = 100;

        public bool IsOpen { get; private set; }

        private sealed class ParcelGroup
        {
            public DeliveryTarget Target;
            public float Distance;
            public readonly List<Parcel> Remaining = new();
        }

        private void Awake()
        {
            if (_center == null)
            {
                _center = GetComponentInParent<LogisticsCenter>();
            }

            // UI 씬이 맵 씬과 분리되면 인스펙터 참조가 끊기므로 씬 전체에서 찾는다
            if (_center == null)
            {
                _center = FindAnyObjectByType<LogisticsCenter>();
            }

            if (_center == null)
            {
                Debug.LogError($"LogisticsCenterUI references are missing in {gameObject.name}");
                enabled = false;
            }
            else if (_cardTemplate == null || _rowTemplate == null)
            {
                Debug.LogError($"LogisticsCenterUI UXML templates are not assigned in {gameObject.name}");
                enabled = false;
            }
        }

        private void OnEnable()
        {
            GetComponent<PanelRenderer>().RegisterUIReloadCallback(OnUIReload);
            GameManager.AddEscapeHandler(EscapePriority, TryCloseOnEscape);
        }

        private void OnDisable()
        {
            GetComponent<PanelRenderer>().UnregisterUIReloadCallback(OnUIReload);
            GameManager.RemoveEscapeHandler(TryCloseOnEscape);
            _cardList = null;
        }

        // Esc가 열린 창을 먼저 닫는다. 닫을 창이 없으면 다음 핸들러(퇴근 확인)로 넘긴다
        private bool TryCloseOnEscape()
        {
            if (IsOpen == false)
            {
                return false;
            }

            Close();
            return true;
        }

        // PanelRenderer는 트리를 만들거나 다시 불러올 때마다 루트를 넘겨준다. 트리가 오기 전에는 표시를 건너뛴다(_cardList == null)
        private void OnUIReload(PanelRenderer _, VisualElement root, int version)
        {
            _windowRoot = root.Q("windowRoot");
            _cardList = root.Q<ScrollView>("cardList");
            _totalLabel = root.Q<Label>("totalLabel");
            _capacityBadge = root.Q<Label>("capacityBadge");
            _weightBadge = root.Q<Label>("weightBadge");
            Button closeButton = root.Q<Button>("closeButton");
            if (_windowRoot == null || _cardList == null || _totalLabel == null
                || _capacityBadge == null || _weightBadge == null || closeButton == null)
            {
                Debug.LogError($"LogisticsCenterWindow.uxml is not assigned to the PanelRenderer in {gameObject.name}");
                _cardList = null;
                enabled = false;
                return;
            }

            // UI Builder 미리보기용 예시 카드·행은 런타임에 지운다
            _cardList.Clear();
            _trace = new BorderProgress();
            closeButton.clicked += Close;
            _windowRoot.EnableInClassList(HiddenClass, IsOpen == false);
            if (IsOpen)
            {
                Build();
            }
        }

        private void Start()
        {
            _controller = FindAnyObjectByType<PlayerInteractionController>();
            if (_controller == null)
            {
                Debug.LogError($"PlayerInteractionController is missing for {gameObject.name}");
            }
        }

        private void Update()
        {
            if (IsOpen == false)
            {
                return;
            }

            // 제한 시간은 게임 시간에 따라 줄어들므로 바를 매 프레임 갱신한다
            foreach (KeyValuePair<Parcel, VisualElement> pair in _parcelFills)
            {
                if (pair.Key != null && pair.Value != null)
                {
                    pair.Value.style.width = Length.Percent(DeadlineRatio(pair.Key) * 100f);
                }
            }
        }

        public void Open()
        {
            if (IsOpen || enabled == false)
            {
                return;
            }

            IsOpen = true;
            Game.Audio.GameAudio.PlayMenuOpen();
            _isHolding = false;
            _needsRefresh = false;
            _windowRoot?.RemoveFromClassList(HiddenClass);

            if (_controller != null)
            {
                _controller.LoadedParcelsChanged += OnLoadedParcelsChanged;
                _controller.InteractionProgressChanged += OnProgressChanged;
            }

            if (_cardList != null)
            {
                _cardList.scrollOffset = Vector2.zero;
            }

            Build();
        }

        public void Close()
        {
            if (IsOpen == false)
            {
                return;
            }

            IsOpen = false;
            Game.Audio.GameAudio.PlayMenuClose();
            ClearRows();
            _windowRoot?.AddToClassList(HiddenClass);

            if (_controller != null)
            {
                _controller.LoadedParcelsChanged -= OnLoadedParcelsChanged;
                _controller.InteractionProgressChanged -= OnProgressChanged;
            }

            EndHold();
            _controller?.EndHoldLoad();
        }

        private void Build()
        {
            if (_cardList == null)
            {
                return;
            }

            // 다시 그려도 보던 위치를 유지한다
            Vector2 offset = _cardList.scrollOffset;
            ClearRows();
            CollectGroups();
            RefreshSummary();

            for (int i = 0; i < _groups.Count; i++)
            {
                ParcelGroup group = _groups[i];
                VisualElement card = AddGroupCard(group);

                // 현재 사용량으로 실을 수 없는 물류는 행 단위로 비활성 표시한다(기획서 8.5, 16.2-5)
                for (int j = 0; j < group.Remaining.Count; j++)
                {
                    AddParcelRow(card, group.Remaining[j]);
                }
            }

            AddDisposalCard();
            _cardList.scrollOffset = offset;
        }

        /// <summary>폐기 물류는 목적지가 처분 장소이므로 마지막에 처분 장소 카드로 묶어 보여 준다(기획서 11.6). 상차 대상이 아니라 누를 수 없다.</summary>
        private void AddDisposalCard()
        {
            if (_controller == null || DisposalSite.Instance == null)
            {
                return;
            }

            int spoiled = 0;
            IReadOnlyList<Parcel> loaded = _controller.LoadedParcels;
            for (int i = 0; i < loaded.Count; i++)
            {
                if (loaded[i].IsSpoiled)
                {
                    spoiled++;
                }
            }

            if (spoiled == 0)
            {
                return;
            }

            float distance = CalculateXZDistance(_center.transform.position, DisposalSite.Instance.transform.position);
            AddCard(DisposalTitle, distance, $"폐기 {spoiled}").AddToClassList("lc-card--warning");
        }

        /// <summary>그룹 상차는 처리 순서 첫 물류부터 시작하므로, 그 물류가 실려야 그룹을 누를 수 있다(결정 D5).</summary>
        private bool CanHoldGroup(ParcelGroup group)
        {
            return group.Remaining.Count > 0 && CanLoad(group.Remaining[0]);
        }

        private bool CanLoad(Parcel parcel)
        {
            return _controller != null && _controller.CanLoad(parcel);
        }

        private void RefreshSummary()
        {
            if (_totalLabel == null)
            {
                return;
            }

            _totalLabel.text = $"총 물류 개수: {CountAvailable(null)}";

            if (_controller == null)
            {
                return;
            }

            _capacityBadge.text = $"적재 한도: {_controller.UsedCapacity}/{_controller.CargoCapacity}";
            _weightBadge.text = $"무게 한도: {_controller.UsedWeight}/{_controller.WeightCapacity}";
            _weightBadge.EnableInClassList("lc-badge--warning", _controller.IsOverweight);
        }

        /// <summary>대기열 물류를 배송지별로 묶고, 그룹은 물류센터에서의 거리순·그룹 내부는 처리 순서로 정렬한다(기획서 8.1).</summary>
        private void CollectGroups()
        {
            _groups.Clear();

            // 물류가 없어도 모든 배송지 그룹을 목록에 남긴다
            List<DeliveryTarget> targets = LogisticsCenter.DeliveryTargets;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] != null)
                {
                    FindGroup(targets[i]);
                }
            }

            IReadOnlyList<Parcel> available = _center.AvailableParcels;
            for (int i = 0; i < available.Count; i++)
            {
                Parcel parcel = available[i];
                if (parcel != null && parcel.Target != null)
                {
                    FindGroup(parcel.Target).Remaining.Add(parcel);
                }
            }

            for (int i = 0; i < _groups.Count; i++)
            {
                _groups[i].Remaining.Sort(Parcel.CompareProcessingOrder);
            }

            _groups.Sort((a, b) => a.Distance.CompareTo(b.Distance));
        }

        private ParcelGroup FindGroup(DeliveryTarget target)
        {
            for (int i = 0; i < _groups.Count; i++)
            {
                if (_groups[i].Target == target)
                {
                    return _groups[i];
                }
            }

            ParcelGroup group = new()
            {
                Target = target,
                Distance = CalculateXZDistance(_center.transform.position, target.transform.position),
            };
            _groups.Add(group);
            return group;
        }

        private VisualElement AddGroupCard(ParcelGroup group)
        {
            VisualElement card = AddCard(group.Target.name, group.Distance, BuildCountText(group.Target));
            VisualElement header = card.Q("header");
            _groupCounts[group.Target] = card.Q<Label>("countLabel");

            // 적재 한도에 도달해도 그룹은 숨기지 않고 비활성으로 남긴다(기획서 16.2-5)
            if (CanHoldGroup(group))
            {
                MakeHoldable(header, group.Remaining);
                // 헤더에 올리면 그룹 상차로 실릴 물류 행을 함께 강조한다
                header.RegisterCallback<PointerEnterEvent>(_ => SetGroupTarget(group, true));
                header.RegisterCallback<PointerLeaveEvent>(_ => SetGroupTarget(group, false));
            }
            else
            {
                header.AddToClassList(DisabledClass);
            }

            return card;
        }

        private void SetGroupTarget(ParcelGroup group, bool isTarget)
        {
            for (int i = 0; i < group.Remaining.Count; i++)
            {
                if (_parcelRows.TryGetValue(group.Remaining[i], out VisualElement row) && row.ClassListContains(DisabledClass) == false)
                {
                    row.EnableInClassList(TargetClass, isTarget);
                }
            }
        }

        /// <summary>상차 진행도를 지금 실리는 물류 행(그룹 상차면 처리 순서 첫 행) 테두리에 그린다.</summary>
        private void OnProgressChanged(float progress01, bool isActive)
        {
            if (_isHolding == false || isActive == false || _heldParcels == null || _heldParcels.Count == 0)
            {
                HideTrace();
                return;
            }

            VisualElement target = _parcelRows.TryGetValue(_heldParcels[0], out VisualElement row) ? row : _heldElement;
            if (target == null || _trace == null)
            {
                HideTrace();
                return;
            }

            _trace.AttachTo(target);
            _trace.Value = progress01;
        }

        private void HideTrace()
        {
            if (_trace != null)
            {
                _trace.Value = 0f;
                _trace.RemoveFromHierarchy();
            }
        }

        private VisualElement AddCard(string title, float distance, string count)
        {
            VisualElement card = Create(_cardTemplate);
            _cardList.Add(card);
            card.Q<Label>("titleLabel").text = title;
            card.Q<Label>("distanceLabel").text = $"{distance:0}m";
            // 미적재 수량은 차량 적재량과 분리해 표시한다(기획서 8.1)
            card.Q<Label>("countLabel").text = count;
            return card;
        }

        /// <summary>물류 행: 적재 HUD와 같은 유형 아이콘·예상 보상·해시태그, Express만 제한 시간 바와 만료 시각.</summary>
        private void AddParcelRow(VisualElement card, Parcel parcel)
        {
            bool isDisabled = CanLoad(parcel) == false;

            VisualElement row = Create(_rowTemplate);
            card.Add(row);
            VisualElement icon = row.Q("parcelIcon");
            icon.EnableInClassList("parcel-icon--express", parcel.Type == DeliveryType.Express);
            icon.EnableInClassList("parcel-icon--fresh", parcel.IsFresh);

            DeliveryReward reward = parcel.Reward;
            // 제한 시간 안에 배송했을 때 받는 금액(적재 HUD와 같은 식)
            Label price = row.Q<Label>("priceLabel");
            price.text = $"{reward.BaseFee + reward.DistanceFee + reward.TimeBonus + reward.TraitBonus + reward.FreshBonusMax:#,0}₩";
            // 상차하면 W를 넘지만 W_max 이내인 행은 경고색으로 두되 누를 수 있게 둔다(6.4)
            price.EnableInClassList("lc-price--warning", isDisabled == false && _controller != null && _controller.WouldExceedWeightCapacity(parcel));

            CargoTrait traits = parcel.Cargo.Traits;
            Keep(row, "tagFragile", (traits & CargoTrait.Fragile) != 0);
            Keep(row, "tagHeavy", (traits & CargoTrait.Heavy) != 0);
            Keep(row, "tagLarge", (traits & CargoTrait.Large) != 0);
            if (isDisabled)
            {
                bool isWeight = _controller != null && _controller.GetLoadBlockReason(parcel) == PlayerInteractionController.WeightLimitReason;
                row.Q<Label>("tagBlocked").text = isWeight ? "#무게 초과" : "#적재 초과";
            }
            else
            {
                row.Q("tagBlocked").RemoveFromHierarchy();
            }

            // 제한 시간이 없는 물류는 바와 시각을 두지 않는다(기획서 8.2)
            if (parcel.HasDeadline)
            {
                VisualElement fill = row.Q("deadlineFill");
                fill.style.width = Length.Percent(DeadlineRatio(parcel) * 100f);
                fill.EnableInClassList("lc-bar-fill--fresh", parcel.IsFresh);
                _parcelFills[parcel] = fill;
                // 남은 시간이 아니라 생성 시점에 확정된 만료 시각(게임 시계)을 보여 준다. Fresh는 대기 중 줄지 않으므로 신선도 총량을 보여 준다
                row.Q("deadlineIcon").EnableInClassList("lc-deadline-icon--fresh", parcel.IsFresh);
                row.Q<Label>("deadlineTime").text = FormatDeadline(parcel, parcel.Deadline);
            }
            else
            {
                row.Q("deadline").RemoveFromHierarchy();
            }

            _parcelRows[parcel] = row;

            if (isDisabled)
            {
                row.AddToClassList(DisabledClass);
            }
            else
            {
                MakeHoldable(row, new List<Parcel> { parcel });
            }
        }

        /// <summary>항목을 누르고 있는 동안만 상차가 진행되도록 연결한다. 떼거나 벗어나면 진행도를 잃는다(기획서 8.2, 16.2-3).</summary>
        private void MakeHoldable(VisualElement target, IReadOnlyList<Parcel> parcels)
        {
            target.pickingMode = PickingMode.Position;
            target.AddToClassList(HoldableClass);
            target.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == 0 && _isHolding == false)
                {
                    BeginHold(parcels, target);
                }
            });
            target.RegisterCallback<PointerUpEvent>(_ => EndHold());
            target.RegisterCallback<PointerLeaveEvent>(_ => EndHold());
        }

        private void BeginHold(IReadOnlyList<Parcel> parcels, VisualElement element)
        {
            _isHolding = true;
            _heldElement = element;
            _heldElement.AddToClassList(HoldingClass);
            _heldParcels = new List<Parcel>(parcels);
            _controller?.BeginHoldLoad(parcels);
        }

        private void EndHold()
        {
            if (_isHolding == false)
            {
                return;
            }

            _isHolding = false;
            HideTrace();
            _heldElement?.RemoveFromClassList(HoldingClass);
            _heldElement = null;
            _heldParcels = null;
            _controller?.EndHoldLoad();

            // 홀드 중에는 목록을 다시 만들지 않았으므로 손을 뗀 시점에 한 번 갱신한다
            if (_needsRefresh)
            {
                _needsRefresh = false;
                if (IsOpen)
                {
                    Build();
                }
            }
        }

        // 이 물류센터에 같은 배송지로 갈 물류(그룹)가 남아 있는지
        private bool HasAvailableParcelFor(DeliveryTarget target)
        {
            IReadOnlyList<Parcel> available = _center.AvailableParcels;
            for (int i = 0; i < available.Count; i++)
            {
                if (available[i].Target == target)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnLoadedParcelsChanged(Parcel parcel)
        {
            if (IsOpen == false || parcel == null)
            {
                return;
            }

            // 홀드 중에도 방금 실린 물류의 행을 하나씩 없애고 수량을 갱신해 바로 반영한다
            RemoveParcelRow(parcel);
            RefreshGroupCount(parcel.Target);
            RefreshSummary();

            if (parcel.Status == ParcelStatus.Loaded && HasAvailableParcelFor(parcel.Target) == false)
            {
                Game.Audio.GameAudio.PlayGroupLoaded();
            }

            if (_isHolding == false)
            {
                Build();
                return;
            }

            _needsRefresh = true;

            // 고른 물류를 모두 실었으면 마우스를 계속 누르고 있어도 상호작용을 끝낸다
            if (_heldParcels == null)
            {
                return;
            }

            _heldParcels.Remove(parcel);

            // 처리 순서상 다음 물류가 한도를 넘으면 컨트롤러가 상차를 끝냈으므로 홀드도 끝낸다(결정 D5)
            if (_heldParcels.Count == 0 || _controller == null || _controller.CanContinueGroupLoad(_heldParcels[0]) == false)
            {
                EndHold();
            }
        }

        /// <summary>실린 물류의 행을 지운다. 카드 헤더는 남겨 그룹 단위 정보를 유지한다.</summary>
        private void RemoveParcelRow(Parcel parcel)
        {
            _parcelFills.Remove(parcel);
            if (_parcelRows.Remove(parcel, out VisualElement row))
            {
                row.RemoveFromHierarchy();
            }
        }

        private void RefreshGroupCount(DeliveryTarget target)
        {
            if (target != null && _groupCounts.TryGetValue(target, out Label label))
            {
                label.text = BuildCountText(target);
            }
        }

        private string BuildCountText(DeliveryTarget target)
        {
            return $"남은 {CountAvailable(target)} · 적재 {CountLoaded(target)}";
        }

        /// <summary>물류센터에 남은 물류 수. target이 null이면 전체.</summary>
        private int CountAvailable(DeliveryTarget target)
        {
            int count = 0;
            IReadOnlyList<Parcel> parcels = _center.AvailableParcels;
            for (int i = 0; i < parcels.Count; i++)
            {
                if (parcels[i] != null && (target == null || parcels[i].Target == target))
                {
                    count++;
                }
            }

            return count;
        }

        private int CountLoaded(DeliveryTarget target)
        {
            if (_controller == null)
            {
                return 0;
            }

            int count = 0;
            IReadOnlyList<Parcel> loaded = _controller.LoadedParcels;
            for (int i = 0; i < loaded.Count; i++)
            {
                if (loaded[i].Target == target && loaded[i].IsSpoiled == false)
                {
                    count++;
                }
            }

            return count;
        }

        private void ClearRows()
        {
            _cardList?.Clear();
            _parcelRows.Clear();
            _parcelFills.Clear();
            _groupCounts.Clear();
        }

        private static float DeadlineRatio(Parcel parcel)
        {
            return parcel.Deadline > 0f ? Mathf.Clamp01(parcel.RemainingDeadline / parcel.Deadline) : 0f;
        }

        // 템플릿 한 개를 만들어 TemplateContainer 래퍼 없이 루트 요소만 꺼낸다(래퍼가 끼면 카드·행 간격 기준이 달라진다)
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

        private static float CalculateXZDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>제한 시간 표기. Express는 만료 시각 `hh:mm까지`, Fresh는 신선도 `freshSeconds`를 분 단위로 올림한 `hh:mm 남음`.</summary>
        internal static string FormatDeadline(Parcel parcel, float freshSeconds)
        {
            if (parcel.IsFresh)
            {
                // 올림해서 0이 아닌 값이 00:00으로 보이지 않게 한다
                int minutes = Mathf.CeilToInt(Mathf.Max(0f, freshSeconds) / 60f);
                return $"{minutes / 60:00}:{minutes % 60:00} 남음";
            }

            return $"{FormatClock(parcel.CreationTime + parcel.Deadline)}까지";
        }

        /// <summary>게임 시간(자정 기준 초)을 시계 표기로 바꾼다.</summary>
        private static string FormatClock(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{totalSeconds / 3600:00}:{totalSeconds % 3600 / 60:00}";
        }
    }
}
