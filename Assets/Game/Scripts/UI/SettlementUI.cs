using System.Text;

using UnityEngine;

using TMPro;

using Game.Logistics;

namespace Game.UI
{
    /// <summary>하루 정산 화면. 요약과 항목별 상세 내역을 한 화면에 표시한다(기획서 12.2).</summary>
    public class SettlementUI : MonoBehaviour
    {
        private const string AmountColumnEm = "10";

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
            _finalLabel.text = $"최종  {result.FinalAmount:0}";
            _finalLabel.color = result.FinalAmount < 0f ? _lossColor : Color.white;
            // 별점은 SettlementSystem이 판정한다. 여기서는 표시만 한다
            string starText = result.Stars.HasValue
                ? $"별점 {result.Stars.Value}"
                : "평가할 물류 없음";
            // 요약 라벨 높이에 맞춰 2줄로 둔다(3줄이면 최종 라벨과 겹친다)
            _summaryLabel.text = $"{starText}      성공 {result.SuccessCount} / 전체 {result.TotalCount}\n배송 완료 {result.DeliveredCount}      실패 {result.FailedCount}";

            _builder.Clear();

            AppendLine("기본 요금", result.BaseFeeTotal);
            AppendLine("거리 요금", result.DistanceFeeTotal);
            AppendLine("시간 보너스", result.TimeBonusEarned);
            AppendLine("시간 보너스 놓침", -result.TimeBonusMissed);
            AppendLine("특성 보너스", result.TraitBonusTotal);
            AppendLine("신선 보너스", result.FreshBonusTotal);
            AppendLine("지각 손실", -result.LateLoss);
            AppendLine("실패 페널티", -result.FailurePenalty);
            _detailsLabel.text = _builder.ToString();

            _panel.SetActive(true);
        }

        private void AppendLine(string label, float amount)
        {
            // 폭이 다른 한글·숫자가 섞여도 금액 열이 맞도록 <pos>로 열 시작 위치를 고정한다(가장 긴 항목명 약 8.5em)
            _builder.Append(label).Append("<pos=").Append(AmountColumnEm).Append("em>").Append(amount.ToString("+0;-0;0")).Append('\n');
        }
    }
}
