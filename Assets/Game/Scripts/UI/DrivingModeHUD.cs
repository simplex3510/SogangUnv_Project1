using TMPro;

using UnityEngine;

using Game.Player.Movement;

namespace Game.UI
{
    /// <summary>현재 주행 모드를 표시하는 HUD.</summary>
    public class DrivingModeHUD : MonoBehaviour
    {
        [SerializeField] private TMP_Text _modeText;

        private PlayerMovementController _movementController;

        private void Awake()
        {
            if (_modeText == null)
            {
                _modeText = GetComponent<TMP_Text>();
            }

            if (_modeText == null)
            {
                Debug.LogError($"TMP_Text is missing in {gameObject.name}");
                enabled = false;
            }
        }

        private void Start()
        {
            // HUD는 씬 Canvas에 있으므로 플레이어는 씬에서 찾는다
            _movementController = FindAnyObjectByType<PlayerMovementController>();
            if (_movementController == null)
            {
                Debug.LogError($"PlayerMovementController is missing in the scene");
                return;
            }

            _movementController.DrivingModeChanged += Refresh;
            Refresh(_movementController.DrivingMode);
        }

        private void OnDestroy()
        {
            if (_movementController != null)
            {
                _movementController.DrivingModeChanged -= Refresh;
            }
        }

        private void Refresh(VehicleDrivingMode mode)
        {
            // TODO: 한글 TMP 폰트 자산이 추가되면 고속/일반으로 교체한다
            _modeText.text = mode == VehicleDrivingMode.Fast ? "FAST" : "NORMAL";
        }
    }
}
