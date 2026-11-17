using UnityEngine;
using UnityEngine.UI;

using Game.Player.Interaction;
using Game.Player.Recovery;

namespace Game.UI
{
    /// <summary>현재 상차·하차 중인 물류 하나의 진행도를 표시한다.</summary>
    public class InteractionProgressUI : MonoBehaviour
    {
        [SerializeField] private PlayerInteractionController _controller;
        [Tooltip("복구 홀드 진행도도 같은 바에 표시한다(Story 007)")]
        [SerializeField] private VehicleRecoveryController _recoveryController;
        [SerializeField] private GameObject _barRoot;
        [SerializeField] private Image _fill;

        private void Awake()
        {
            if (_recoveryController == null)
            {
                _recoveryController = FindAnyObjectByType<VehicleRecoveryController>();
            }

            if (_controller == null)
            {
                _controller = FindAnyObjectByType<PlayerInteractionController>();
            }

            if (_controller == null || _barRoot == null || _fill == null)
            {
                Debug.LogError($"InteractionProgressUI references are missing in {gameObject.name}");
                enabled = false;
                return;
            }

            _barRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (_controller != null)
            {
                _controller.InteractionProgressChanged += OnProgressChanged;
            }

            if (_recoveryController != null)
            {
                _recoveryController.RecoveryProgressChanged += OnProgressChanged;
            }
        }

        private void OnDisable()
        {
            if (_controller != null)
            {
                _controller.InteractionProgressChanged -= OnProgressChanged;
            }

            if (_recoveryController != null)
            {
                _recoveryController.RecoveryProgressChanged -= OnProgressChanged;
            }
        }

        private void OnProgressChanged(float progress01, bool isActive)
        {
            _barRoot.SetActive(isActive);

            if (isActive)
            {
                _fill.fillAmount = progress01;
            }
        }
    }
}
