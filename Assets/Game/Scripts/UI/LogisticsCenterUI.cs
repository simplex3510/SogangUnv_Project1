using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

using Game.Logistics;
using Game.Player.Interaction;

namespace Game.UI
{
    /// <summary>물류센터 상차 UI. 배송지 그룹을 거리순으로, 그룹 안 물류를 처리 순서(기획서 7.4)로 늘어놓고 선택 결과를 플레이어에게 넘긴다.</summary>
    public class LogisticsCenterUI : MonoBehaviour
    {
        private const string FillPath = "Background/Fill";
        private const string NoDeadlineText = "--:--";

        [SerializeField] private LogisticsCenter _center;
        [SerializeField] private GameObject _panel;
        [SerializeField] private Transform _content;
        [SerializeField] private Button _groupTemplate;
        [SerializeField] private Button _parcelTemplate;
        [SerializeField] private Button _closeButton;
        [Tooltip("차량 적재 용량·무게 사용량 표시(기획서 8.1)")]
        [SerializeField] private TMP_Text _usageLabel;
        [Tooltip("적재 한도에 도달했거나 이미 실은 항목의 색")]
        [SerializeField] private Color _disabledColor = new(0.5f, 0.5f, 0.5f, 1f);

        [Tooltip("무게 한도 W를 넘는 사용량·상차 시 W를 넘게 되는 행의 경고색")]
        [SerializeField] private Color _warningColor = new(1f, 0.6f, 0.1f, 1f);

        private const string WeightLimitReason = "WEIGHT LIMIT";

        private PlayerInteractionController _controller;
        private readonly List<GameObject> _spawned = new();
        private readonly List<ParcelGroup> _groups = new();
        private readonly Dictionary<Parcel, GameObject> _parcelRows = new();
        private readonly Dictionary<Parcel, Image> _parcelFills = new();
        private readonly Dictionary<DeliveryTarget, TMP_Text> _groupLabels = new();
        private readonly Dictionary<DeliveryTarget, float> _groupDistances = new();
        private bool _isHolding;
        private bool _needsRefresh;
        private List<Parcel> _heldParcels;
        private HoldInteractButton _heldButton;

        public bool IsOpen { get; private set; }

        private sealed class ParcelGroup
        {
            public DeliveryTarget Target;
            public float Distance;
            public readonly List<Parcel> Remaining = new();
            public int LoadedCount;
        }

        private void Awake()
        {
            // UI를 화면 캔버스에 두는 구성을 허용하기 위해 씬 참조를 우선 사용한다
            if (_center == null)
            {
                _center = GetComponentInParent<LogisticsCenter>();
            }

            if (_center == null || _panel == null || _content == null
                || _groupTemplate == null || _parcelTemplate == null)
            {
                Debug.LogError($"LogisticsCenterUI references are missing in {gameObject.name}");
                enabled = false;
                return;
            }

            _groupTemplate.gameObject.SetActive(false);
            _parcelTemplate.gameObject.SetActive(false);
            _panel.SetActive(false);

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Close);
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

            // 제한 시간은 게임 시간에 따라 줄어들므로 행의 프로그레스 바를 매 프레임 갱신한다
            foreach (KeyValuePair<Parcel, Image> pair in _parcelFills)
            {
                if (pair.Value == null || pair.Key == null)
                {
                    continue;
                }

                pair.Value.fillAmount = pair.Key.Deadline > 0f
                    ? Mathf.Clamp01(pair.Key.RemainingDeadline / pair.Key.Deadline)
                    : 0f;
            }
        }

        public void Open()
        {
            if (IsOpen || enabled == false)
            {
                return;
            }

            IsOpen = true;
            _isHolding = false;
            _needsRefresh = false;
            _panel.SetActive(true);

            // 상차 패널은 마우스로 고르므로 게임 중 숨겨 둔 커서를 풀어 준다
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (_controller != null)
            {
                _controller.LoadedParcelsChanged += OnLoadedParcelsChanged;
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
            ClearRows();
            _panel.SetActive(false);

            if (_controller != null)
            {
                _controller.LoadedParcelsChanged -= OnLoadedParcelsChanged;
                _controller.EndHoldLoad();
            }

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Build()
        {
            ClearRows();
            CollectGroups();
            RefreshUsageLabel();

            for (int i = 0; i < _groups.Count; i++)
            {
                ParcelGroup group = _groups[i];
                AddGroupRow(group);

                // 현재 사용량으로 실을 수 없는 물류는 행 단위로 비활성 표시한다(기획서 8.5, 16.2-5)
                for (int j = 0; j < group.Remaining.Count; j++)
                {
                    Parcel parcel = group.Remaining[j];
                    AddParcelRow(parcel, CanLoad(parcel) == false);
                }
            }
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

        private void RefreshUsageLabel()
        {
            if (_usageLabel == null || _controller == null)
            {
                return;
            }

            _usageLabel.text = $"CARGO {_controller.UsedCapacity}/{_controller.CargoCapacity}   WEIGHT {_controller.UsedWeight}/{_controller.WeightCapacity}";
            _usageLabel.color = _controller.IsOverweight ? _warningColor : Color.white;
        }

        /// <summary>대기열 물류를 배송지별로 묶고, 그룹은 물류센터에서의 거리순·그룹 내부는 남은 시간순으로 정렬한다(기획서 8.1).</summary>
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
                if (parcel == null || parcel.Target == null)
                {
                    continue;
                }

                FindGroup(parcel.Target).Remaining.Add(parcel);
            }

            if (_controller != null)
            {
                IReadOnlyList<Parcel> loaded = _controller.LoadedParcels;
                for (int i = 0; i < loaded.Count; i++)
                {
                    Parcel parcel = loaded[i];
                    if (parcel == null || parcel.Target == null)
                    {
                        continue;
                    }

                    // 실린 물류가 있어도 그룹 머리글은 목록에 남긴다
                    FindGroup(parcel.Target).LoadedCount++;
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

        private void AddGroupRow(ParcelGroup group)
        {
            Button button = Instantiate(_groupTemplate, _content);
            button.gameObject.SetActive(true);
            _spawned.Add(button.gameObject);

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                // 미적재 수량은 차량 적재량과 분리해 표시한다(기획서 8.1)
                label.text = BuildGroupText(group.Target, group.Distance, group.Remaining.Count);
                label.color = CanHoldGroup(group) ? Color.white : _disabledColor;
                _groupLabels[group.Target] = label;
            }

            _groupDistances[group.Target] = group.Distance;

            // 적재 한도에 도달해도 그룹은 숨기지 않고 비활성으로 남긴다(기획서 16.2-5)
            bool canHold = CanHoldGroup(group);
            ConfigureButton(button, canHold == false);

            if (canHold)
            {
                SetHold(button, group.Remaining);
            }
        }

        private void AddParcelRow(Parcel parcel, bool isDisabled)
        {
            Button button = Instantiate(_parcelTemplate, _content);
            button.gameObject.SetActive(true);
            _spawned.Add(button.gameObject);

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                bool isWeightBlocked = isDisabled && _controller != null && _controller.GetLoadBlockReason(parcel) == WeightLimitReason;
                label.text = isWeightBlocked ? $"{BuildParcelText(parcel)} {WeightLimitReason}" : BuildParcelText(parcel);

                // 상차하면 W를 넘지만 W_max 이내인 행은 경고색으로 두되 활성 상태를 유지한다
                bool isWarning = isDisabled == false && _controller != null && _controller.WouldExceedWeightCapacity(parcel);
                label.color = isDisabled ? _disabledColor : isWarning ? _warningColor : Color.white;
            }

            _parcelRows[parcel] = button.gameObject;

            Transform fill = button.transform.Find(FillPath);
            if (fill != null && fill.TryGetComponent(out Image fillImage))
            {
                // 제한 시간이 없는 물류는 바를 숨기고 매 프레임 갱신 대상에도 넣지 않는다(기획서 8.2)
                fillImage.enabled = parcel.HasDeadline;
                if (parcel.HasDeadline)
                {
                    _parcelFills[parcel] = fillImage;
                    fillImage.fillAmount = parcel.Deadline > 0f
                        ? Mathf.Clamp01(parcel.RemainingDeadline / parcel.Deadline)
                        : 0f;
                }
            }

            ConfigureButton(button, isDisabled);

            SetHold(button, new List<Parcel> { parcel });
        }

        /// <summary>행 생성 시 색 전환 애니메이션이 재생되면 목록이 깜빡이므로 즉시 전환으로 맞춘다.</summary>
        private void ConfigureButton(Button button, bool isDisabled)
        {
            ColorBlock colors = button.colors;
            colors.fadeDuration = 0f;
            colors.disabledColor = _disabledColor;
            button.colors = colors;

            button.interactable = isDisabled == false;
        }

        /// <summary>행을 누르고 있는 동안만 상차가 진행되도록 연결한다(기획서 8.3).</summary>
        private void SetHold(Button button, IReadOnlyList<Parcel> parcels)
        {
            if (button.TryGetComponent(out HoldInteractButton hold))
            {
                hold.Initialize(() => BeginHold(parcels, hold), EndHold);
            }
        }

        private void BeginHold(IReadOnlyList<Parcel> parcels, HoldInteractButton hold)
        {
            _isHolding = true;
            _heldButton = hold;
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
            _heldParcels = null;
            _heldButton = null;
            _controller?.EndHoldLoad();

            // 홀드 중에는 행을 다시 만들지 않았으므로 손을 뗀 시점에 한 번 갱신한다
            if (_needsRefresh)
            {
                _needsRefresh = false;
                if (IsOpen)
                {
                    Build();
                }
            }
        }

        private void OnLoadedParcelsChanged(Parcel parcel)
        {
            if (IsOpen == false || parcel == null)
            {
                return;
            }

            // 홀드 중에도 방금 실린 물류의 행을 하나씩 없애고 그룹 수량을 갱신해 바로 반영한다
            RemoveParcelRow(parcel);
            RefreshGroupLabel(parcel.Target);

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
            RefreshUsageLabel();

            // 처리 순서상 다음 물류가 한도를 넘으면 컨트롤러가 상차를 끝냈으므로 홀드도 끝낸다(결정 D5)
            if (_heldParcels.Count == 0 || _controller == null || _controller.CanContinueGroupLoad(_heldParcels[0]) == false)
            {
                _heldButton?.EndHold();
            }
        }

        /// <summary>실린 물류의 행을 지운다. 그룹 머리글은 남겨 그룹 단위 정보를 유지한다.</summary>
        private void RemoveParcelRow(Parcel parcel)
        {
            _parcelFills.Remove(parcel);

            if (_parcelRows.TryGetValue(parcel, out GameObject row) == false)
            {
                return;
            }

            _parcelRows.Remove(parcel);
            _spawned.Remove(row);

            if (row != null)
            {
                // 파괴는 프레임 끝에 처리되므로 먼저 숨겨 새 행과 겹쳐 보이지 않게 한다
                row.SetActive(false);
                Destroy(row);
            }
        }

        private void RefreshGroupLabel(DeliveryTarget target)
        {
            if (target == null || _groupLabels.TryGetValue(target, out TMP_Text label) == false)
            {
                return;
            }

            float distance = _groupDistances.TryGetValue(target, out float stored) ? stored : 0f;
            label.text = BuildGroupText(target, distance, CountAvailable(target));
        }

        /// <summary>물류 행 표기: 번호, 배송 유형, 특징(F/H/L 자리), 적재 용량·무게 사용량, 만료 시각(기획서 8.1).</summary>
        private static string BuildParcelText(Parcel parcel)
        {
            string type = parcel.Type == DeliveryType.Express ? "EXP" : "NRM";

            CargoTrait traits = parcel.Cargo.Traits;
            char fragile = (traits & CargoTrait.Fragile) != 0 ? 'F' : '-';
            char heavy = (traits & CargoTrait.Heavy) != 0 ? 'H' : '-';
            char large = (traits & CargoTrait.Large) != 0 ? 'L' : '-';

            // 남은 시간이 아니라 생성 시점에 확정된 만료 시각(게임 시계)을 보여 준다. 제한 시간이 없으면 --:--
            string expiry = parcel.HasDeadline ? FormatClock(parcel.CreationTime + parcel.Deadline) : NoDeadlineText;

            // 비례폭 폰트에서도 행끼리 열이 맞도록 고정폭 태그로 감싼다
            return $"<mspace=0.62em>#{parcel.Id,-3} {type} {fragile}{heavy}{large} C{parcel.Cargo.CapacityUsage} W{parcel.Cargo.WeightUsage}  {expiry}</mspace>";
        }

        private string BuildGroupText(DeliveryTarget target, float distance, int remaining)
        {
            return $"{target.name}  {distance:0}m  x{remaining}  loaded {CountLoaded(target)}";
        }

        private int CountAvailable(DeliveryTarget target)
        {
            int count = 0;
            IReadOnlyList<Parcel> parcels = _center.AvailableParcels;
            for (int i = 0; i < parcels.Count; i++)
            {
                if (parcels[i] != null && parcels[i].Target == target)
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
                if (loaded[i].Target == target)
                {
                    count++;
                }
            }

            return count;
        }

        private void ClearRows()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                {
                    // 파괴는 프레임 끝에 처리되므로 먼저 숨겨 새 행과 겹쳐 보이지 않게 한다
                    _spawned[i].SetActive(false);
                    Destroy(_spawned[i]);
                }
            }

            _spawned.Clear();
            _parcelRows.Clear();
            _parcelFills.Clear();
            _groupLabels.Clear();
            _groupDistances.Clear();
        }

        private static float CalculateXZDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>게임 시간(자정 기준 초)을 시계 표기로 바꾼다.</summary>
        private static string FormatClock(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{totalSeconds / 3600:00}:{totalSeconds % 3600 / 60:00}";
        }
    }
}
