using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using Game.Logistics;
using Game.Player.Interaction;
using Game.Systems;

namespace Game.UI.Hud
{
    /// <summary>
    /// 상단 중앙 토스트 알림(Story 021): 동적 물류 생성, 상호작용 차단 사유, 폐기 전환.
    /// 하루 종료 뒤 새 알림은 무시한다. 남은 알림은 정산 화면과 상관없이 유지 시간이 지나면 사라진다.
    /// 표시만 하고 상태는 바꾸지 않는다.
    /// </summary>
    [RequireComponent(typeof(PanelRenderer))]
    public class ToastHud : MonoBehaviour
    {
        /// <summary>알림 종류. 종류마다 USS 클래스(색 등, ToastHud.uss)가 다르다.</summary>
        public enum ToastKind
        {
            Parcel,
            Blocked,
            Spoiled,
        }

        private const string ToastClass = "toast";
        private const string ShownClass = "toast--shown";
        private const string SpoiledText = "물류가 폐기되었습니다 — 처분 장소로 이동";

        // 값은 이 컴포넌트에서, 색·모양·연출 시간은 ToastHud.uss 맨 위 변수에서 고친다
        [Tooltip("동시에 보이는 최대 알림 수. 넘치면 가장 오래된 알림이 즉시 퇴장한다")]
        [SerializeField, Min(1)] private int _maxCount = 3; // TODO(tuning)
        [Tooltip("모든 알림의 유지 시간(초). 등장·퇴장 연출 시간은 ToastHud.uss")]
        [UnityEngine.Serialization.FormerlySerializedAs("_parcelHoldSeconds")]
        [SerializeField, Min(0.1f)] private float _holdSeconds = 2f; // TODO(tuning)

        private readonly ToastQueue _queue = new();
        private readonly Dictionary<ToastQueue.Entry, Label> _labels = new();
        private readonly List<ToastQueue.Entry> _expired = new();
        private VisualElement _stack;
        private DeliveryGenerationSystem _generationSystem;
        private DeliveryDeadlineSystem _deadlineSystem;
        private PlayerInteractionController _interactionController;

        private void OnEnable()
        {
            GetComponent<PanelRenderer>().RegisterUIReloadCallback(OnUIReload);
        }

        private void OnDisable()
        {
            GetComponent<PanelRenderer>().UnregisterUIReloadCallback(OnUIReload);
            ClearAll();
            _stack = null;
        }

        // PanelRenderer는 트리를 만들거나 다시 불러올 때마다 루트를 넘겨준다
        private void OnUIReload(PanelRenderer _, VisualElement root, int version)
        {
            ClearAll();
            _stack = root.Q("toastStack");
            if (_stack == null)
            {
                Debug.LogError($"ToastHud UXML elements are missing in {gameObject.name}");
                enabled = false;
            }
        }

        private void Start()
        {
            _generationSystem = DeliveryGenerationSystem.Instance;
            _deadlineSystem = DeliveryDeadlineSystem.Instance;
            // HUD는 씬 UI에 있으므로 플레이어는 씬에서 찾는다
            _interactionController = FindAnyObjectByType<PlayerInteractionController>();
            if (_generationSystem == null || _deadlineSystem == null || _interactionController == null)
            {
                Debug.LogError("ToastHud: DeliveryGenerationSystem, DeliveryDeadlineSystem or PlayerInteractionController is missing in the scene");
                enabled = false;
                return;
            }

            _generationSystem.DynamicParcelGenerated += OnDynamicParcelGenerated;
            _deadlineSystem.ParcelSpoiled += OnParcelSpoiled;
            _interactionController.InteractionBlocked += OnInteractionBlocked;
        }

        private void OnDestroy()
        {
            if (_generationSystem != null)
            {
                _generationSystem.DynamicParcelGenerated -= OnDynamicParcelGenerated;
            }

            if (_deadlineSystem != null)
            {
                _deadlineSystem.ParcelSpoiled -= OnParcelSpoiled;
            }

            if (_interactionController != null)
            {
                _interactionController.InteractionBlocked -= OnInteractionBlocked;
            }
        }

        private void Update()
        {
            _queue.CollectExpired(Time.time, _expired);
            foreach (ToastQueue.Entry entry in _expired)
            {
                Dismiss(entry);
            }

            _expired.Clear();
        }

        private void OnDynamicParcelGenerated(Parcel parcel)
        {
            Show($"새 물류 — {parcel.Target.name} · {GetTypeName(parcel.Type)}", ToastKind.Parcel);
        }

        private void OnParcelSpoiled(Parcel _)
        {
            Show(SpoiledText, ToastKind.Spoiled);
        }

        private void OnInteractionBlocked(string reason)
        {
            Show(reason, ToastKind.Blocked);
        }

        // 배송 유형 한글 표시명(기획서 6.2 표의 이름)
        private static string GetTypeName(DeliveryType type)
        {
            return type switch
            {
                DeliveryType.Express => "신속 배송",
                DeliveryType.Fresh => "신선 배송",
                _ => "일반 배송",
            };
        }

        // ToastHud.uss의 종류별 클래스(toast--parcel 등)
        private static string GetKindClass(ToastKind kind)
        {
            return kind switch
            {
                ToastKind.Blocked => "toast--blocked",
                ToastKind.Spoiled => "toast--spoiled",
                _ => "toast--parcel",
            };
        }

        private void Show(string text, ToastKind kind)
        {
            // 하루 종료(조작 잠금) 뒤 새 알림은 띄우지 않는다
            if (_stack == null || (DailyOperationSystem.Instance != null && DailyOperationSystem.Instance.IsOperating == false))
            {
                return;
            }

            ToastQueue.Entry entry = _queue.Push(text, Time.time + _holdSeconds, _maxCount, out ToastQueue.Entry evicted);
            if (evicted != null)
            {
                Dismiss(evicted);
            }

            if (entry == null)
            {
                return;
            }

            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList(ToastClass);
            label.AddToClassList(GetKindClass(kind));
            _stack.Insert(0, label);
            _labels[entry] = label;
            // 기본 상태(위·투명)가 스타일·레이아웃까지 한 번 계산된 뒤 붙여야 등장 transition이 걸린다.
            // schedule.Execute는 첫 스타일 계산보다 먼저 돌 수 있어 연출 없이 바로 나타났다
            label.RegisterCallback<GeometryChangedEvent>(OnFirstLayout);
        }

        private static void OnFirstLayout(GeometryChangedEvent evt)
        {
            var label = (VisualElement)evt.target;
            label.UnregisterCallback<GeometryChangedEvent>(OnFirstLayout);
            // 그 사이 퇴장했으면 건너뛴다
            if (label.parent != null)
            {
                label.AddToClassList(ShownClass);
            }
        }

        // 기본 상태로 되돌려 위로 올라가며 사라지게 하고, transition이 끝나면 요소를 지운다
        private void Dismiss(ToastQueue.Entry entry)
        {
            if (_labels.Remove(entry, out Label label) == false)
            {
                return;
            }

            // 아직 등장 전이면 연출할 것이 없으므로 바로 지운다
            if (label.ClassListContains(ShownClass) == false)
            {
                label.RemoveFromHierarchy();
                return;
            }

            label.RegisterCallback<TransitionEndEvent>(_ => label.RemoveFromHierarchy());
            label.RemoveFromClassList(ShownClass);
        }

        private void ClearAll()
        {
            _stack?.Clear();
            _labels.Clear();
            _queue.Clear();
        }
    }

    /// <summary>토스트 큐 규칙: 최신이 앞, 최대 개수 초과 시 가장 오래된 것 퇴장, 표시 중인 같은 문구는 유지 시간만 다시 시작.</summary>
    public sealed class ToastQueue
    {
        /// <summary>표시 중인 알림 하나.</summary>
        public sealed class Entry
        {
            /// <summary>표시 문구. 중복 판정 기준이다.</summary>
            public string Text { get; }

            /// <summary>퇴장을 시작할 시각(Time.time 기준).</summary>
            public float HideTime { get; set; }

            /// <summary>문구와 퇴장 시각으로 알림을 만든다.</summary>
            public Entry(string text, float hideTime)
            {
                Text = text;
                HideTime = hideTime;
            }
        }

        // 0번이 최신
        private readonly List<Entry> _entries = new();

        /// <summary>
        /// 알림을 넣는다. 같은 문구가 표시 중이면 퇴장 시각만 갱신하고 null을 돌려준다.
        /// 새 알림이면 맨 앞에 넣어 돌려주고, maxCount를 넘으면 가장 오래된 알림을 빼서 evicted로 돌려준다.
        /// </summary>
        public Entry Push(string text, float hideTime, int maxCount, out Entry evicted)
        {
            evicted = null;
            foreach (Entry existing in _entries)
            {
                if (existing.Text == text)
                {
                    existing.HideTime = hideTime;
                    return null;
                }
            }

            var entry = new Entry(text, hideTime);
            _entries.Insert(0, entry);
            if (_entries.Count > maxCount)
            {
                evicted = _entries[_entries.Count - 1];
                _entries.RemoveAt(_entries.Count - 1);
            }

            return entry;
        }

        /// <summary>퇴장 시각이 지난 알림을 빼서 expired에 담는다.</summary>
        public void CollectExpired(float now, List<Entry> expired)
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (_entries[i].HideTime <= now)
                {
                    expired.Add(_entries[i]);
                    _entries.RemoveAt(i);
                }
            }
        }

        /// <summary>모든 알림을 지운다.</summary>
        public void Clear()
        {
            _entries.Clear();
        }
    }
}
