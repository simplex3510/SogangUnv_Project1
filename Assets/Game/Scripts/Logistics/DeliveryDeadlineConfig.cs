using UnityEngine;

namespace Game.Logistics
{
    /// <summary>배송 제한 시간 산정 설정. T_base(D) = 고정 시간 + 미터당 시간 * 거리, R은 무작위 편차(초)이며 게임 시간 기준이다.</summary>
    [CreateAssetMenu(menuName = "Game/Logistics/DeliveryDeadlineConfig", fileName = "DeliveryDeadlineConfig")]
    public class DeliveryDeadlineConfig : ScriptableObject
    {
        [Header("기본 제한 시간 T_base(D)")]
        [Tooltip("거리와 무관한 고정 제한 시간(게임 초)")]
        [SerializeField, Min(0f)] private float _baseSeconds = 900f; // TODO(tuning)
        [Tooltip("1m당 추가 제한 시간(게임 초)")]
        [SerializeField, Min(0f)] private float _secondsPerMeter = 10f; // TODO(tuning)

        [Header("무작위 편차 R (게임 초)")]
        [SerializeField] private float _variationMinSeconds = -120f; // TODO(tuning)
        [SerializeField] private float _variationMaxSeconds = 120f; // TODO(tuning)

        public float BaseSeconds => _baseSeconds;
        public float SecondsPerMeter => _secondsPerMeter;
        public float VariationMinSeconds => _variationMinSeconds;
        public float VariationMaxSeconds => _variationMaxSeconds;
    }
}
