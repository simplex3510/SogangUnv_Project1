using UnityEngine;

using Game.Logistics;
using Game.Player.Interaction;
using Game.Player.Movement;

namespace Game.Systems
{
    /// <summary>하루 시작·종료를 묶는 운영 루프(기획서 3.4~3.6). 종료 시 조작을 멈추고 정산을 확정한다.</summary>
    // 물류 생성 시스템(100)이 참조 검증을 마친 뒤 하루 시작을 지시하도록 실행 순서를 늦춘다
    [DefaultExecutionOrder(200)]
    public class DailyOperationSystem : MonoBehaviour
    {
        public static DailyOperationSystem Instance { get; private set; }

        [SerializeField] private DeliveryGenerationSystem _generationSystem;
        [SerializeField] private SettlementSystem _settlementSystem;
        [SerializeField] private PlayerMovementController _movementController;
        [SerializeField] private PlayerInteractionController _interactionController;

        // 영업 중인지. 종료 후에는 조기 종료 판정과 조작을 다시 켜지 않는다
        public bool IsOperating { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            GameTimeSystem gameTime = GameTimeSystem.Instance;

            if (gameTime == null || _generationSystem == null || _settlementSystem == null
                || _movementController == null || _interactionController == null)
            {
                Debug.LogError($"DailyOperationSystem references are missing in {gameObject.name}");
                enabled = false;
                return;
            }

            gameTime.BusinessEndReached += OnBusinessEndReached;

            // 정적 물류가 준비되기 전에는 플레이어가 움직이지 못한다(기획서 3.4-7)
            SetControlEnabled(false);
            StartDay();
        }

        private void OnDestroy()
        {
            if (GameTimeSystem.Instance != null)
            {
                GameTimeSystem.Instance.BusinessEndReached -= OnBusinessEndReached;
            }
        }

        /// <summary>하루 시작: 할당량 초기화 → 물류 생성 → 조작 활성 순서로 진행한다.</summary>
        public void StartDay()
        {
            Debug.Log("[하루 운영] 시작");

            DeliveryAssignmentSystem.ResetDailyAssignment();
            _generationSystem.StartDay();
            SetControlEnabled(true);
            IsOperating = true;
        }

        private void Update()
        {
            if (IsOperating == false)
            {
                return;
            }

            // 조기 종료: 동적 생성이 끝났고 아직 배송되지 않은 물류가 없을 때(기획서 16.1-1)
            if (_generationSystem.IsFinished == false
                || DeliveryDeadlineSystem.Instance == null
                || DeliveryDeadlineSystem.Instance.TrackedParcels.Count > 0)
            {
                return;
            }

            EndDay("모든 배송 완료");
        }

        private void OnBusinessEndReached()
        {
            EndDay("영업 종료 시각 도달");
        }

        /// <summary>하루 종료: 조작·상호작용을 즉시 멈추고 물류 생성 이후 미배송 처리와 정산을 맡긴다.</summary>
        public void EndDay(string reason)
        {
            if (IsOperating == false)
            {
                return;
            }

            IsOperating = false;
            SetControlEnabled(false);
            _generationSystem.StopDay();

            Debug.Log($"[하루 운영] 종료: {reason}");

            // 정산은 미배송 물류를 실패 처리하고 결과 화면을 띄운다
            _settlementSystem.FinalizeDay();
        }

        private void SetControlEnabled(bool isEnabled)
        {
            _movementController.SetControlEnabled(isEnabled);
            _interactionController.SetControlEnabled(isEnabled);
        }
    }
}
