using UnityEngine;

namespace Game.Player.Interaction
{
    /// <summary>차량의 적재 한도·무게 한도(기획서 8.5). 주행 성능(VehicleConfig)과 분리한 배송·물류용 차량 설정이다.</summary>
    [CreateAssetMenu(menuName = "Game/Player/VehicleCargoConfig", fileName = "VehicleCargoConfig")]
    public class VehicleCargoConfig : ScriptableObject
    {
        [Header("적재 및 하중 제한")]
        [Tooltip("적재 용량 사용량 합계의 상한(Cargo Capacity)")]
        [SerializeField, Min(1)] private int _cargoCapacity = 6; // TODO(tuning)
        [Tooltip("무게 적재량 합계의 상한(Weight Capacity)")]
        [SerializeField, Min(1)] private int _weightCapacity = 6; // TODO(tuning)

        [Header("초과 적재(기획서 8.6)")]
        [Tooltip("개별 상차가 허용되는 무게 상한 배율. W_max = W × r_max")]
        [SerializeField, Min(1f)] private float _maxWeightRatio = 1.5f; // TODO(tuning)
        [Tooltip("무게 한도 초과 비율당 속도 감소 계수(k)")]
        [SerializeField, Min(0f)] private float _overweightSlowdown = 1.0f; // TODO(tuning)
        [Tooltip("속도 배율의 하한(m_min)")]
        [SerializeField, Range(0f, 1f)] private float _minSpeedMultiplier = 0.5f; // TODO(tuning)

        public int CargoCapacity => _cargoCapacity;
        public int WeightCapacity => _weightCapacity;

        /// <summary>개별 상차가 허용되는 무게 상한 W_max.</summary>
        public float MaxWeight => _weightCapacity * _maxWeightRatio;

        /// <summary>현재 적재 무게의 속도 배율 m = max(m_min, 1 - k × max(0, w - W) / W). W 이하면 1.</summary>
        public float GetSpeedMultiplier(int weight)
        {
            float overRatio = Mathf.Max(0, weight - _weightCapacity) / (float)_weightCapacity;
            return Mathf.Max(_minSpeedMultiplier, 1f - _overweightSlowdown * overRatio);
        }
    }
}
