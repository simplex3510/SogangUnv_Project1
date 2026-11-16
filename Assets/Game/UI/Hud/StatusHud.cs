using UnityEngine;
using UnityEngine.UIElements;

using Game.Logistics;
using Game.Systems;

namespace Game.UI.Hud
{
    /// <summary>화면 좌상단 일차·시각 HUD. 정산이 열리면 숨는다. 표시만 하고 상태는 바꾸지 않는다.</summary>
    [RequireComponent(typeof(PanelRenderer))]
    public class StatusHud : MonoBehaviour
    {
        private const string HiddenClass = "status-hud--hidden";

        private SettlementSystem _settlementSystem;
        private VisualElement _statusHud;
        private Label _clockTime;
        private Label _dayLabel;
        private string _shownTime;
        private int _shownDay = -1;
        private bool _settled;

        private void OnEnable()
        {
            GetComponent<PanelRenderer>().RegisterUIReloadCallback(OnUIReload);
        }

        private void OnDisable()
        {
            GetComponent<PanelRenderer>().UnregisterUIReloadCallback(OnUIReload);
            _clockTime = null;
        }

        // PanelRenderer는 트리를 만들거나 다시 불러올 때마다 루트를 넘겨준다
        private void OnUIReload(PanelRenderer _, VisualElement root, int version)
        {
            _statusHud = root.Q("statusHud");
            _clockTime = root.Q<Label>("clockTime");
            _dayLabel = root.Q<Label>("dayLabel");
            _shownTime = null;
            _shownDay = -1;

            if (_statusHud == null || _clockTime == null || _dayLabel == null)
            {
                Debug.LogError($"StatusHud UXML elements are missing in {gameObject.name}");
                enabled = false;
                return;
            }

            // 정산 뒤 UI를 다시 불러와도 숨김을 유지한다
            _statusHud.EnableInClassList(HiddenClass, _settled);
        }

        private void Start()
        {
            _settlementSystem = SettlementSystem.Instance;
            if (_settlementSystem != null)
            {
                _settlementSystem.Settled += OnSettled;
            }
        }

        private void OnDestroy()
        {
            if (_settlementSystem != null)
            {
                _settlementSystem.Settled -= OnSettled;
            }
        }

        private void OnSettled(SettlementSystem.SettlementResult _)
        {
            _settled = true;
            _statusHud?.AddToClassList(HiddenClass);
        }

        private void LateUpdate()
        {
            GameTimeSystem gameTime = GameTimeSystem.Instance;
            if (_clockTime == null || _settled || gameTime == null)
            {
                return;
            }

            string time = gameTime.CurrentTimeText;
            if (time != _shownTime)
            {
                _shownTime = time;
                _clockTime.text = time;
            }

            int day = DailyOperationSystem.Instance != null ? DailyOperationSystem.Instance.CurrentDay : 1;
            if (day != _shownDay)
            {
                _shownDay = day;
                _dayLabel.text = $"DAY {day}";
            }
        }
    }
}
