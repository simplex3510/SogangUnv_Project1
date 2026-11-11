using System.Text;

using UnityEngine;

using TMPro;

using Game.Logistics;

namespace Game.UI
{
    /// <summary>하루 정산 화면. 요약과 항목별 상세 내역을 한 화면에 표시한다(기획서 12.2).</summary>
    public class SettlementUI : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _finalLabel;
        [SerializeField] private TMP_Text _summaryLabel;
        [SerializeField] private TMP_Text _detailsLabel;
        [Tooltip("최종 정산 금액이 음수일 때의 색")]
        [SerializeField] private Color _lossColor = new(0.95f, 0.40f, 0.35f, 1f);

        private SettlementSystem _settlementSystem;
        private readonly StringBuilder _builder = new();

        private void Awake()
        {
            if (_panel == null || _finalLabel == null || _summaryLabel == null || _detailsLabel == null)
            {
                Debug.LogError($"SettlementUI references are missing in {gameObject.name}");
                enabled = false;
                return;
            }

            _panel.SetActive(false);
        }

        private void Start()
        {
            _settlementSystem = SettlementSystem.Instance;
            if (_settlementSystem == null)
            {
                Debug.LogError($"SettlementSystem is missing for {gameObject.name}");
                return;
            }

            _settlementSystem.Settled += Show;
        }

        private void OnDestroy()
        {
            if (_settlementSystem != null)
            {
                _settlementSystem.Settled -= Show;
            }
        }

        private void Show(SettlementSystem.SettlementResult result)
        {
            _finalLabel.text = $"FINAL  {result.FinalAmount:0}";
            _finalLabel.color = result.FinalAmount < 0f ? _lossColor : Color.white;
            _summaryLabel.text = $"DELIVERED {result.DeliveredCount}      FAILED {result.FailedCount}";

            _builder.Clear();

            // 기본 폰트는 비례폭이므로 고정폭 태그로 감싸 항목명과 금액의 열을 맞춘다
            _builder.Append("<mspace=0.62em>");
            AppendLine("BASE FEE", result.BaseFeeTotal);
            AppendLine("DISTANCE FEE", result.DistanceFeeTotal);
            AppendLine("TIME BONUS", result.TimeBonusEarned);
            AppendLine("TIME BONUS MISSED", -result.TimeBonusMissed);
            AppendLine("LATE LOSS", -result.LateLoss);
            AppendLine("FAILURE PENALTY", -result.FailurePenalty);
            _builder.Append("</mspace>");
            _detailsLabel.text = _builder.ToString();

            _panel.SetActive(true);

            // 정산 화면은 조작이 없으므로 커서를 풀어 둔다
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void AppendLine(string label, float amount)
        {
            _builder.Append(label.PadRight(22)).Append(amount.ToString("+0;-0;0")).Append('\n');
        }
    }
}
