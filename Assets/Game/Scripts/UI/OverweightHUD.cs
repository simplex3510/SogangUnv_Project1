using TMPro;

using UnityEngine;

using Game.Player.Interaction;

namespace Game.UI
{
    /// <summary>무게 한도 W를 넘겼을 때 과적 상태와 속도 감소율을 표시하는 HUD(기획서 8.6). 한도 이내면 숨긴다.</summary>
    public class OverweightHUD : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;

        private PlayerInteractionController _controller;

        private void Awake()
        {
            if (_text == null)
            {
                _text = GetComponent<TMP_Text>();
            }

            if (_text == null)
            {
                Debug.LogError($"TMP_Text is missing in {gameObject.name}");
                enabled = false;
            }
        }

        private void Start()
        {
            _controller = FindAnyObjectByType<PlayerInteractionController>();
            if (_controller == null)
            {
                Debug.LogError("PlayerInteractionController is missing in the scene");
                return;
            }

            _controller.SpeedMultiplierChanged += Refresh;
            Refresh(_controller.SpeedMultiplier);
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.SpeedMultiplierChanged -= Refresh;
            }
        }

        private void Refresh(float speedMultiplier)
        {
            _text.enabled = _controller.IsOverweight;
            _text.text = $"OVERWEIGHT -{Mathf.RoundToInt((1f - speedMultiplier) * 100f)}%";
        }
    }
}
