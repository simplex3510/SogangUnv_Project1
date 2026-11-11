using UnityEngine;

namespace Game.Logistics
{
    /// <summary>배송 보상·손실 산정 설정. 값은 생성 시점 스냅샷에 복사되므로 이후 변경은 이미 생성된 물류에 영향을 주지 않는다.</summary>
    [CreateAssetMenu(menuName = "Game/Logistics/DeliveryRewardConfig", fileName = "DeliveryRewardConfig")]
    public class DeliveryRewardConfig : ScriptableObject
    {
        [Header("보상")]
        [Tooltip("물류 1개당 기본 배송료. 정상 배송이면 항상 지급한다(기획서 11.1)")]
        [SerializeField, Min(0f)] private float _baseFee = 100f; // TODO(tuning)
        [Tooltip("물류센터~배송지 직선거리 1m당 거리 추가 배송료")]
        [SerializeField, Min(0f)] private float _distanceFeePerMeter = 10f; // TODO(tuning)
        [Tooltip("제한 시간 내 배송 완료 시 추가 보상")]
        [SerializeField, Min(0f)] private float _timeBonus = 50f; // TODO(tuning)

        [Header("손실")]
        [Tooltip("제한 시간 초과 후 배송 완료 시 손실 기준값. 배송 타입별 계수를 곱한다")]
        [SerializeField, Min(0f)] private float _lateLoss = 80f; // TODO(tuning)
        [Tooltip("영업 종료 시 미배송 물류의 실패 페널티 기준값. 배송 타입별 계수를 곱한다(기획서 11.4)")]
        [SerializeField, Min(0f)] private float _failurePenalty = 120f; // TODO(tuning)

        public float BaseFee => _baseFee;
        public float DistanceFeePerMeter => _distanceFeePerMeter;
        public float TimeBonus => _timeBonus;
        public float LateLoss => _lateLoss;
        public float FailurePenalty => _failurePenalty;
    }
}
