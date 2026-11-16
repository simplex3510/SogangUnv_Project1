using UnityEngine;
using UnityEngine.UIElements;

using Game.Player.Movement;

namespace Game.UI.Hud
{
    /// <summary>화면 중앙 하단 속도계와 고속 모드 게이지 바. 표시만 하고 상태는 바꾸지 않는다.</summary>
    [RequireComponent(typeof(PanelRenderer))]
    public class SpeedometerHUD : MonoBehaviour
    {
        private const string ExhaustedClass = "fast-gauge--exhausted";

        private PlayerMovementController _movementController;
        private SpeedArc _speedArc;
        private Label _speedValue;
        private VisualElement _fastGauge;
        private VisualElement _fastGaugeFill;
        private int _shownSpeed = -1;

        private void OnEnable()
        {
            GetComponent<PanelRenderer>().RegisterUIReloadCallback(OnUIReload);
        }

        private void OnDisable()
        {
            GetComponent<PanelRenderer>().UnregisterUIReloadCallback(OnUIReload);
            _speedValue = null;
        }

        // PanelRenderer는 트리를 만들거나 다시 불러올 때마다 루트를 넘겨준다
        private void OnUIReload(PanelRenderer _, VisualElement root, int version)
        {
            _speedArc = root.Q<SpeedArc>("speedArc");
            _speedValue = root.Q<Label>("speedValue");
            _fastGauge = root.Q("fastGauge");
            _fastGaugeFill = root.Q("fastGaugeFill");
            _shownSpeed = -1;

            if (_speedArc == null || _speedValue == null || _fastGauge == null || _fastGaugeFill == null)
            {
                Debug.LogError($"Speedometer UXML elements are missing in {gameObject.name}");
                enabled = false;
            }
        }

        private void Start()
        {
            // HUD는 씬 UI에 있으므로 플레이어는 씬에서 찾는다
            _movementController = FindAnyObjectByType<PlayerMovementController>();
            if (_movementController == null)
            {
                Debug.LogError("PlayerMovementController is missing in the scene");
                enabled = false;
            }
        }

        private void LateUpdate()
        {
            VehicleConfig config = _movementController.Config;
            FastModeGauge gauge = _movementController.FastGauge;
            if (config == null || gauge == null || _speedValue == null)
            {
                return;
            }

            float kmh = Mathf.Abs(_movementController.ForwardSpeed) * 3.6f * config.SpeedometerDisplayScale;
            int speed = Mathf.RoundToInt(kmh);
            if (speed != _shownSpeed)
            {
                _shownSpeed = speed;
                _speedValue.text = speed.ToString();
            }

            _speedArc.Value = kmh / config.SpeedometerMaxKmh;
            _fastGaugeFill.style.width = Length.Percent(gauge.Value * 100f);
            _fastGauge.EnableInClassList(ExhaustedClass, gauge.IsExhausted);
        }
    }
}
