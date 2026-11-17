using System;
using System.Collections.Generic;

using UnityEngine;

namespace Game.Logistics
{
    /// <summary>배송 결과 금액을 집계하고 하루 정산을 확정한다(기획서 11~12장). 영업 종료·조기 종료 시 DailyOperationSystem이 FinalizeDay를 호출한다.</summary>
    public class SettlementSystem : MonoBehaviour
    {
        public static SettlementSystem Instance { get; private set; }

        [Tooltip("별점 경계값 T1·T2 설정(기획서 12.4)")]
        [SerializeField] private StarRatingConfig _starRatingConfig;

        private readonly List<Parcel> _deliveredParcels = new();

        // 폐기 전환으로 배송 실패가 확정된 물류. 영업 종료 때 페널티를 한 번만 매긴다(기획서 11.6)
        private readonly List<Parcel> _spoiledParcels = new();

        private DeliveryDeadlineSystem _deadlineSystem;
        private bool _hasSettled;

        /// <summary>하루 정산이 확정되면 1회 발생한다.</summary>
        public event Action<SettlementResult> Settled;

        public SettlementResult? LastResult { get; private set; }

        /// <summary>오늘 배송 완료된 물류의 보상 합계에서 시간 초과 손실을 뺀 값. 실패 페널티(폐기·미배송)는 정산 때만 반영한다.</summary>
        public float TodayIncome { get; private set; }

        /// <summary>배송 완료로 오늘 수익이 바뀔 때마다 새 값과 함께 발생한다.</summary>
        public event Action<float> TodayIncomeChanged;

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
            _deadlineSystem.ParcelSpoiled += OnParcelSpoiled;

            if (_starRatingConfig == null)
            {
                Debug.LogError($"SettlementSystem requires StarRatingConfig in {gameObject.name}");
            }
        }

        private void OnDestroy()
        {
            if (_deadlineSystem != null)
            {
                _deadlineSystem.ParcelDelivered -= OnParcelDelivered;
                _deadlineSystem.ParcelSpoiled -= OnParcelSpoiled;
            }
        }

        private void OnParcelSpoiled(Parcel parcel)
        {
            if (parcel != null)
            {
                _spoiledParcels.Add(parcel);
            }
        }

        private void OnParcelDelivered(Parcel parcel)
        {
            if (parcel != null)
            {
                _deliveredParcels.Add(parcel);

                // FinalizeDay 배송 완료 루프와 같은 항목 정의다. 한쪽을 바꾸면 다른 쪽도 맞춘다. 실패 페널티는 넣지 않는다
                DeliveryReward reward = parcel.Reward;
                TodayIncome += reward.BaseFee + reward.DistanceFee + reward.TraitBonus + parcel.FreshBonusEarned
                    + (parcel.IsOverdue ? -reward.LateLoss : reward.TimeBonus);
                TodayIncomeChanged?.Invoke(TodayIncome);
            }
        }

        /// <summary>미배송 물류를 실패 처리하고 하루 정산을 확정한다. 영업 종료와 조기 종료(7단계)가 호출한다.</summary>
        public void FinalizeDay()
        {
            // 별점 설정이 없으면 정산하지 않는다. 시작 시 오류를 이미 남겼다
            if (_hasSettled || _deadlineSystem == null || _starRatingConfig == null)
            {
                return;
            }

            _hasSettled = true;

            float baseFeeTotal = 0f;
            float distanceFeeTotal = 0f;
            float timeBonusEarned = 0f;
            float timeBonusMissed = 0f;
            float traitBonusTotal = 0f;
            float freshBonusTotal = 0f;
            float lateLossTotal = 0f;

            for (int i = 0; i < _deliveredParcels.Count; i++)
            {
                Parcel parcel = _deliveredParcels[i];
                DeliveryReward reward = parcel.Reward;

                baseFeeTotal += reward.BaseFee;
                distanceFeeTotal += reward.DistanceFee;

                // 특징 보상은 시간 조건과 무관하게 배송 완료 시 지급한다(기획서 11.1)
                traitBonusTotal += reward.TraitBonus;

                // 신선도 보상은 하차 시점에 확정한 값을 합산한다(기획서 14.2)
                freshBonusTotal += parcel.FreshBonusEarned;

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

            // 조건부 성공 S: 배송 완료 중 Express 제한 시간 초과 완료는 별점상 실패(기획서 12.4)
            int successCount = 0;
            for (int i = 0; i < _deliveredParcels.Count; i++)
            {
                Parcel delivered = _deliveredParcels[i];
                if (!(delivered.Type == DeliveryType.Express && delivered.IsOverdue))
                {
                    successCount++;
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

            // 폐기 물류는 전환 시점에 실패가 확정되어 추적에서 빠졌으므로 여기서 실패 페널티만 매긴다. 시간 초과 손실은 없다
            for (int i = 0; i < _spoiledParcels.Count; i++)
            {
                failedCount++;
                failurePenaltyTotal += _spoiledParcels[i].Reward.FailurePenalty;
            }

            // N = 배송 완료 + 미배송 + 폐기(실패로 센다)
            int totalCount = _deliveredParcels.Count + failedCount;

            // 최종 금액 산식을 중복하지 않으려고 별점 없는 결과로 먼저 구한다
            float finalAmount = new SettlementResult(
                baseFeeTotal, distanceFeeTotal, timeBonusEarned, timeBonusMissed, traitBonusTotal,
                lateLossTotal, failurePenaltyTotal, _deliveredParcels.Count, failedCount, successCount, totalCount, null, freshBonusTotal).FinalAmount;

            SettlementResult result = new(
                baseFeeTotal,
                distanceFeeTotal,
                timeBonusEarned,
                timeBonusMissed,
                traitBonusTotal,
                lateLossTotal,
                failurePenaltyTotal,
                _deliveredParcels.Count,
                failedCount,
                successCount,
                totalCount,
                DailyStarRating.Evaluate(totalCount, successCount, finalAmount, _starRatingConfig.Threshold2, _starRatingConfig.Threshold1),
                freshBonusTotal);

            LastResult = result;
            string starText = result.Stars.HasValue ? $"별 {result.Stars.Value}" : "별점 없음";
            Debug.Log($"[정산] 최종 {result.FinalAmount:0} (배송 {result.DeliveredCount} · 미배송 {result.FailedCount}), 수익 {result.Income:0}, 손실 {result.Loss:0}, {starText} (성공 {result.SuccessCount}/발생 {result.TotalCount})");
            Settled?.Invoke(result);
        }

        /// <summary>하루 정산 결과. 항목별 합계를 그대로 보관해 정산 화면이 다시 계산하지 않게 한다.</summary>
        public readonly struct SettlementResult
        {
            public readonly float BaseFeeTotal;
            public readonly float DistanceFeeTotal;
            public readonly float TimeBonusEarned;
            public readonly float TimeBonusMissed;
            public readonly float TraitBonusTotal;

            /// <summary>Fresh 신선도 보상 합계(기획서 12.2).</summary>
            public readonly float FreshBonusTotal;
            public readonly float LateLoss;
            public readonly float FailurePenalty;
            public readonly int DeliveredCount;
            public readonly int FailedCount;

            /// <summary>조건부 성공 수 S(기획서 12.4).</summary>
            public readonly int SuccessCount;

            /// <summary>발생 물류 수 N(기획서 12.4).</summary>
            public readonly int TotalCount;

            /// <summary>별 0~3개. 판정할 물류가 없으면 null.</summary>
            public readonly int? Stars;

            public SettlementResult(
                float baseFeeTotal,
                float distanceFeeTotal,
                float timeBonusEarned,
                float timeBonusMissed,
                float traitBonusTotal,
                float lateLoss,
                float failurePenalty,
                int deliveredCount,
                int failedCount,
                int successCount,
                int totalCount,
                int? stars,
                float freshBonusTotal = 0f)
            {
                BaseFeeTotal = baseFeeTotal;
                DistanceFeeTotal = distanceFeeTotal;
                TimeBonusEarned = timeBonusEarned;
                TimeBonusMissed = timeBonusMissed;
                TraitBonusTotal = traitBonusTotal;
                LateLoss = lateLoss;
                FailurePenalty = failurePenalty;
                DeliveredCount = deliveredCount;
                FailedCount = failedCount;
                SuccessCount = successCount;
                TotalCount = totalCount;
                Stars = stars;
                FreshBonusTotal = freshBonusTotal;
            }

            public float Income => BaseFeeTotal + DistanceFeeTotal + TimeBonusEarned + TraitBonusTotal + FreshBonusTotal;
            public float Loss => LateLoss + FailurePenalty;

            // 하한을 두지 않고 음수를 그대로 표시한다(기획서 16.1-6)
            public float FinalAmount => Income - Loss;
        }
    }
}
