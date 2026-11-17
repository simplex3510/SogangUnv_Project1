using UnityEngine;
using UnityEngine.UIElements;

using Game.Logistics;

namespace Game.UI
{
    /// <summary>
    /// 하루 정산 화면. 요약과 항목별 상세 내역을 한 화면에 표시한다(기획서 12.2). 표시만 하고 게임 상태는 바꾸지 않는다.
    /// 순서 규칙: 이 패널의 PanelSettings(SettlementPanelSettings)는 Sort Order 100으로, HUD(0)·물류센터 창(10)·상호작용 패널·토스트보다 항상 앞에 그린다.
    /// </summary>
    [RequireComponent(typeof(PanelRenderer))]
    public class SettlementUI : MonoBehaviour
    {
        private const string HiddenClass = "settlement-root--hidden";
        private const string LossClass = "settlement-final--loss";

        [Tooltip("항목 한 줄 템플릿(SettlementRow.uxml)")]
        [SerializeField] private VisualTreeAsset _rowTemplate;

        private SettlementSystem _settlementSystem;
        private VisualElement _root;
        private Label _finalLabel;
        private Label _summaryLabel;
        private VisualElement _detailList;
        private SettlementSystem.SettlementResult? _lastResult;

        private void Awake()
        {
            if (_rowTemplate == null)
            {
                Debug.LogError($"SettlementUI row template is not assigned in {gameObject.name}");
                enabled = false;
            }
        }

        private void OnEnable()
        {
            GetComponent<PanelRenderer>().RegisterUIReloadCallback(OnUIReload);
        }

        private void OnDisable()
        {
            GetComponent<PanelRenderer>().UnregisterUIReloadCallback(OnUIReload);
            _root = null;
        }

        // 트리가 다시 로드되면 이전 요소는 해제되므로 열려 있던 결과를 다시 그린다
        private void OnUIReload(PanelRenderer _, VisualElement root, int version)
        {
            _root = root.Q("settlementRoot");
            _finalLabel = root.Q<Label>("finalLabel");
            _summaryLabel = root.Q<Label>("summaryLabel");
            _detailList = root.Q("detailList");
            if (_root == null || _finalLabel == null || _summaryLabel == null || _detailList == null)
            {
                Debug.LogError($"SettlementWindow.uxml is not assigned to the PanelRenderer in {gameObject.name}");
                _root = null;
                enabled = false;
                return;
            }

            // UI Builder 미리보기용 예시 행은 런타임에 지운다
            _detailList.Clear();
            if (_lastResult.HasValue)
            {
                Show(_lastResult.Value);
            }
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
            _lastResult = result;
            if (_root == null)
            {
                return;
            }

            _finalLabel.text = $"최종  {result.FinalAmount:0}";
            _finalLabel.EnableInClassList(LossClass, result.FinalAmount < 0f);
            // 별점은 SettlementSystem이 판정한다. 여기서는 표시만 한다
            string starText = result.Stars.HasValue
                ? $"별점 {result.Stars.Value}"
                : "평가할 물류 없음";
            _summaryLabel.text = $"{starText}      성공 {result.SuccessCount} / 전체 {result.TotalCount}\n배송 완료 {result.DeliveredCount}      실패 {result.FailedCount}";

            _detailList.Clear();
            AddRow("기본 요금", result.BaseFeeTotal);
            AddRow("거리 요금", result.DistanceFeeTotal);
            AddRow("시간 보너스", result.TimeBonusEarned);
            AddRow("시간 보너스 놓침", -result.TimeBonusMissed);
            AddRow("특성 보너스", result.TraitBonusTotal);
            AddRow("신선 보너스", result.FreshBonusTotal);
            AddRow("지각 손실", -result.LateLoss);
            AddRow("실패 페널티", -result.FailurePenalty);

            _root.RemoveFromClassList(HiddenClass);
        }

        private void AddRow(string label, float amount)
        {
            VisualElement row = _rowTemplate.Instantiate()[0];
            row.Q<Label>("nameLabel").text = label;
            row.Q<Label>("amountLabel").text = amount.ToString("+0;-0;0");
            _detailList.Add(row);
        }
    }
}
