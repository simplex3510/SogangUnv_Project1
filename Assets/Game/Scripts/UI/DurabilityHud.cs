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
    /// 충돌 내구도 피드백 HUD(Story 006, 기획서 13장): 차량 위 감소량 −N, 적재 물류 내구도 패널, 폐기 알림.
    /// UI Toolkit 시범 화면이며 내구도는 계산하지 않고 이벤트로 받은 값만 표시한다.
    /// </summary>
    [RequireComponent(typeof(PanelRenderer))]
    public class DurabilityHud : MonoBehaviour
    {
        private const string SpoiledGroupTitle = "처분 장소";
        private const string DiscardNoticeText = "물류가 폐기되었습니다 — 처분 장소로 이동";

        // 유지 시간 비율 중 이 지점부터 선형으로 사라진다
        private const float PopupFadeStart = 0.55f;

        // 키보드 상단 숫자열 1~9 = 패널 위에서부터 그룹 순번(숫자패드 제외). 기획: quick-specs/cargo-panel-fold-scroll
        private static readonly Key[] GroupToggleKeys =
        {
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
            Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9,
        };

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

        [Header("Discard Notice")]
        [Tooltip("폐기 알림 표시 시간(초)")]
        [SerializeField, Min(0f)] private float _noticeDuration = 2.5f;

        [Header("Cargo Panel")]
        [Tooltip("적재 물류 목록 최대 높이(패널 px). 넘치면 스크롤")]
        [SerializeField, Min(100f)] private float _listMaxHeight = 480f;
        [Tooltip("마우스 휠 한 칸·PageUp/PageDown 한 번의 스크롤 거리(패널 px)")]
        [SerializeField, Min(1f)] private float _scrollStep = 120f;

        private readonly List<(Label label, float startTime)> _popups = new();
        // 펼친 그룹 키(배송지 이름 또는 처분 장소). 없는 키는 접힘
        private readonly HashSet<string> _expandedGroups = new();
        // 패널 위에서부터의 그룹 순서. 숫자키 번호와 같다
        private readonly List<string> _groupOrder = new();
        private VisualElement _root;
        private VisualElement _hudRoot;
        private VisualElement _cargoPanel;
        private ScrollView _cargoList;
        private Label _notice;
        private float _noticeHideTime;

        private void Awake()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
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
            _interactionController.ParcelSpoiled += OnParcelSpoiled;
            GetComponent<PanelRenderer>().RegisterUIReloadCallback(OnUIReload);
        }

        private void OnDisable()
        {
            GetComponent<PanelRenderer>().UnregisterUIReloadCallback(OnUIReload);
            _impactController.ImpactDamageApplied -= OnImpactDamageApplied;
            _interactionController.LoadedParcelsChanged -= OnLoadedParcelsChanged;
            _interactionController.ParcelSpoiled -= OnParcelSpoiled;
            ClearPopups();
            _cargoList = null;
        }

        // PanelRenderer는 트리를 만들거나 다시 불러올 때마다 루트를 넘겨준다. 트리가 오기 전에는 표시를 건너뛴다(_cargoList == null)
        private void OnUIReload(PanelRenderer _, VisualElement root, int version)
        {
            ClearPopups();
            _root = root;
            _hudRoot = _root.Q("hudRoot");
            _cargoPanel = _root.Q("cargoPanel");
            _cargoList = _root.Q<ScrollView>("cargoList");
            _notice = _root.Q<Label>("discardNotice");
            if (_hudRoot == null || _cargoPanel == null || _cargoList == null || _notice == null)
            {
                Debug.LogError($"DurabilityHud.uxml is not assigned to the PanelRenderer in {gameObject.name}");
                enabled = false;
                return;
            }

            // 휠은 포인터 위치와 무관하게 Update에서 직접 처리하므로 ScrollView 자체 휠 처리를 막는다
            _cargoList.contentViewport.pickingMode = PickingMode.Ignore;
            _cargoList.style.maxHeight = _listMaxHeight;
            _notice.text = DiscardNoticeText;
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

            if (_noticeHideTime > 0f && Time.time >= _noticeHideTime)
            {
                _notice.RemoveFromClassList("discard-notice--visible");
                _noticeHideTime = 0f;
            }
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
            var label = new Label($"−{Mathf.CeilToInt(damage)}") { pickingMode = PickingMode.Ignore };
            label.AddToClassList("damage-popup");
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

        private void OnParcelSpoiled(Parcel _)
        {
            if (_cargoList == null || IsOperating() == false)
            {
                return;
            }

            _notice.AddToClassList("discard-notice--visible");
            _noticeHideTime = Time.time + _noticeDuration;
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
        /// 적재 물류를 배송지별로 묶어 다시 그린다. 폐기 물류는 목적지가 처분 장소라 따로 묶는다.
        /// 접힌 그룹은 헤더(번호·이름·개수)만, 펼친 그룹은 물류 행까지 그린다.
        /// </summary>
        private void RefreshCargoPanel()
        {
            _cargoList.Clear();
            _groupOrder.Clear();

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

            // 사라진 그룹의 펼침 상태는 버린다. 같은 이름으로 다시 생기면 접힘부터 시작한다
            _expandedGroups.RemoveWhere(title => groups.ContainsKey(title) == false);

            for (int i = 0; i < _groupOrder.Count; i++)
            {
                string title = _groupOrder[i];
                List<Parcel> members = groups[title];
                bool expanded = _expandedGroups.Contains(title);

                VisualElement group = AddElement(_cargoList, "cargo-group");
                // 숫자키가 닿는 1~9번째 그룹만 번호를 붙인다
                string badge = i < GroupToggleKeys.Length ? $"[{i + 1}] " : string.Empty;
                // 긴 배송지 이름은 말줄임하고 개수는 항상 보이게 따로 둔다
                VisualElement header = AddElement(group, "cargo-group-header");
                AddLabel(header, "cargo-group-title", $"{(expanded ? "▼" : "▶")} {badge}{title}");
                AddLabel(header, "cargo-group-count", $"×{members.Count}");
                if (expanded)
                {
                    for (int j = 0; j < members.Count; j++)
                    {
                        AddParcelRow(group, members[j]);
                    }
                }
            }

            _cargoPanel.EnableInClassList("cargo-panel--empty", parcels.Count == 0);
        }

        private static void AddParcelRow(VisualElement group, Parcel parcel)
        {
            VisualElement row = AddElement(group, "parcel-row");
            row.EnableInClassList("parcel-row--spoiled", parcel.IsSpoiled);

            // 상자 그림 대신 배송 유형 색 블록(Normal 파랑, Express 빨강)
            AddElement(row, "parcel-icon").EnableInClassList("parcel-icon--express", parcel.Type == DeliveryType.Express);

            VisualElement info = AddElement(row, "parcel-info");
            DeliveryReward reward = parcel.Reward;
            // 제한 시간 안에 배송했을 때 받는 금액
            AddLabel(info, "parcel-price", $"{reward.BaseFee + reward.DistanceFee + reward.TimeBonus + reward.TraitBonus:#,0}₩");

            VisualElement tags = AddElement(info, "parcel-tags");
            if (parcel.IsSpoiled)
            {
                AddLabel(tags, "tag-chip", "#폐기").AddToClassList("tag-chip--spoiled");
            }

            CargoTrait traits = parcel.Cargo.Traits;
            if ((traits & CargoTrait.Fragile) != 0)
            {
                AddLabel(tags, "tag-chip", "#취급주의");
            }

            if ((traits & CargoTrait.Heavy) != 0)
            {
                AddLabel(tags, "tag-chip", "#고중량");
            }

            if ((traits & CargoTrait.Large) != 0)
            {
                AddLabel(tags, "tag-chip", "#대형");
            }

            // 0이 아닌 내구도가 0으로 보이지 않게 올림한다
            AddLabel(row, "parcel-durability", $"내구도\n{Mathf.CeilToInt(parcel.CurrentDurability)}/{parcel.Cargo.Durability}");
        }

        private static VisualElement AddElement(VisualElement parent, string className)
        {
            var element = new VisualElement { pickingMode = PickingMode.Ignore };
            element.AddToClassList(className);
            parent.Add(element);
            return element;
        }

        private static Label AddLabel(VisualElement parent, string className, string text)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList(className);
            parent.Add(label);
            return label;
        }
    }
}
