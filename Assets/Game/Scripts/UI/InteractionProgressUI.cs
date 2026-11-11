using UnityEngine;
using UnityEngine.UI;

using Game.Player.Interaction;

namespace Game.UI
{
    /// <summary>현재 상차·하차 중인 물류 하나의 진행도를 표시한다.</summary>
    public class InteractionProgressUI : MonoBehaviour
    {
        [SerializeField] private PlayerInteractionController _controller;
        [SerializeField] private GameObject _barRoot;
        [SerializeField] private Image _fill;

        private void Awake()
        {
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
        }

        private void OnDisable()
        {
            if (_controller != null)
            {
                _controller.InteractionProgressChanged -= OnProgressChanged;
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
