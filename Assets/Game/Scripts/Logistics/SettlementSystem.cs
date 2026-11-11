using System;
using System.Collections.Generic;

using UnityEngine;

namespace Game.Logistics
{
    /// <summary>배송 결과 금액을 집계하고 하루 정산을 확정한다(기획서 11~12장). 영업 종료·조기 종료 시 DailyOperationSystem이 FinalizeDay를 호출한다.</summary>
    public class SettlementSystem : MonoBehaviour
    {
        public static SettlementSystem Instance { get; private set; }

        private readonly List<Parcel> _deliveredParcels = new();

        private DeliveryDeadlineSystem _deadlineSystem;
        private bool _hasSettled;

        /// <summary>하루 정산이 확정되면 1회 발생한다.</summary>
        public event Action<SettlementResult> Settled;

        public SettlementResult? LastResult { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            _deadlineSystem = DeliveryDeadlineSystem.Instance;
            if (_deadlineSystem == null)
            {
                Debug.LogError($"SettlementSystem requires DeliveryDeadlineSystem in {gameObject.name}");
                enabled = false;
                return;
            }

            _deadlineSystem.ParcelDelivered += OnParcelDelivered;
        }

        private void OnDestroy()
        {
            if (_deadlineSystem != null)
            {
                _deadlineSystem.ParcelDelivered -= OnParcelDelivered;
            }
        }

        private void OnParcelDelivered(Parcel parcel)
        {
            if (parcel != null)
            {
                _deliveredParcels.Add(parcel);
            }
        }

        /// <summary>미배송 물류를 실패 처리하고 하루 정산을 확정한다. 영업 종료와 조기 종료(7단계)가 호출한다.</summary>
        public void FinalizeDay()
        {
            if (_hasSettled || _deadlineSystem == null)
            {
                return;
            }

            _hasSettled = true;

            float baseFeeTotal = 0f;
            float distanceFeeTotal = 0f;
            float timeBonusEarned = 0f;
            float timeBonusMissed = 0f;
            float lateLossTotal = 0f;

            for (int i = 0; i < _deliveredParcels.Count; i++)
            {
                Parcel parcel = _deliveredParcels[i];
                DeliveryReward reward = parcel.Reward;

                baseFeeTotal += reward.BaseFee;
                distanceFeeTotal += reward.DistanceFee;

                // 초과 배송은 시간 추가 보상을 받지 못하고 손실만 부과한다(기획서 11.3)
                if (parcel.IsOverdue)
                {
                    timeBonusMissed += reward.TimeBonus;
                    lateLossTotal += reward.LateLoss;
                }
                else
                {
                    timeBonusEarned += reward.TimeBonus;
                }
            }

            int failedCount = 0;
            float failurePenaltyTotal = 0f;

            // 미배송은 실패 페널티만 부과한다. 시간 초과 손실과 중복 적용하지 않는다(기획서 16.1-4)
            List<Parcel> pending = new(_deadlineSystem.TrackedParcels);
            for (int i = 0; i < pending.Count; i++)
            {
                Parcel parcel = pending[i];

                failedCount++;
                failurePenaltyTotal += parcel.Reward.FailurePenalty;

                parcel.Status = ParcelStatus.Failed;
                parcel.Target.NotifyParcelChanged(parcel);
                _deadlineSystem.Unregister(parcel);
            }

            SettlementResult result = new(
                baseFeeTotal,
                distanceFeeTotal,
                timeBonusEarned,
                timeBonusMissed,
                lateLossTotal,
                failurePenaltyTotal,
                _deliveredParcels.Count,
                failedCount);

            LastResult = result;
            Debug.Log($"[정산] 최종 {result.FinalAmount:0} (배송 {result.DeliveredCount} · 미배송 {result.FailedCount}), 수익 {result.Income:0}, 손실 {result.Loss:0}");
            Settled?.Invoke(result);
        }

        /// <summary>하루 정산 결과. 항목별 합계를 그대로 보관해 정산 화면이 다시 계산하지 않게 한다.</summary>
        public readonly struct SettlementResult
        {
            public readonly float BaseFeeTotal;
            public readonly float DistanceFeeTotal;
            public readonly float TimeBonusEarned;
            public readonly float TimeBonusMissed;
            public readonly float LateLoss;
            public readonly float FailurePenalty;
            public readonly int DeliveredCount;
            public readonly int FailedCount;

            public SettlementResult(
                float baseFeeTotal,
                float distanceFeeTotal,
                float timeBonusEarned,
                float timeBonusMissed,
                float lateLoss,
                float failurePenalty,
                int deliveredCount,
                int failedCount)
            {
                BaseFeeTotal = baseFeeTotal;
                DistanceFeeTotal = distanceFeeTotal;
                TimeBonusEarned = timeBonusEarned;
                TimeBonusMissed = timeBonusMissed;
                LateLoss = lateLoss;
                FailurePenalty = failurePenalty;
                DeliveredCount = deliveredCount;
                FailedCount = failedCount;
            }

            public float Income => BaseFeeTotal + DistanceFeeTotal + TimeBonusEarned;
            public float Loss => LateLoss + FailurePenalty;

            // 하한을 두지 않고 음수를 그대로 표시한다(기획서 16.1-6)
            public float FinalAmount => Income - Loss;
        }
    }
}
