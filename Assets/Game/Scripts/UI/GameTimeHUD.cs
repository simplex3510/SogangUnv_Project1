using TMPro;

using UnityEngine;

using Game.Systems;

namespace Game.UI
{
    /// <summary>게임 내 시각을 HH:mm으로 표시하는 HUD.</summary>
    public class GameTimeHUD : MonoBehaviour
    {
        [SerializeField] private TMP_Text _timeText;

        private void Awake()
        {
            if (_timeText == null)
            {
                _timeText = GetComponent<TMP_Text>();
                if (_timeText == null)
                {
                    Debug.LogError($"TMP_Text is missing in {gameObject.name}");
                    enabled = false;
                }
            }
        }

        private void Update()
        {
            if (GameTimeSystem.Instance == null)
            {
                return;
            }

            _timeText.text = GameTimeSystem.Instance.CurrentTimeText;
        }
    }
}
