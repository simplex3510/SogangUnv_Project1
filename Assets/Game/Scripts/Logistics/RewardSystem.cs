using UnityEngine;

namespace Game.Logistics
{
    /// <summary>물류 생성 시점의 보상·손실 스냅샷을 계산한다(기획서 6.1, 11장). 하루 집계는 SettlementSystem이 맡는다.</summary>
    public class RewardSystem : MonoBehaviour
    {
        public static RewardSystem Instance { get; private set; }

        [SerializeField] private DeliveryRewardConfig _config;
        [SerializeField] private DeliveryTypeConfig _typeConfig;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (_config == null || _typeConfig == null)
            {
                Debug.LogError($"RewardSystem references are missing in {gameObject.name}");
                enabled = false;
            }
        }

        /// <summary>생성 시점 값으로 고정되는 보상·손실 묶음을 만든다. 근거 값이 없으면 아무것도 지급·부과하지 않는다.</summary>
        public DeliveryReward CreateReward(Vector3 origin, Vector3 targetPosition, DeliveryType type, float traitBonus)
        {
            if (_config == null || _typeConfig == null)
            {
                return default;
            }

            float distance = CalculateXZDistance(origin, targetPosition);

            // 제한 시간이 없는 유형(Normal)은 시간 추가 보상·시간 초과 손실 판정 자체가 없다(기획서 11.1)
            bool hasDeadline = _typeConfig.HasDeadline(type);

            // Fresh는 고정 시간 보상·시간 초과 손실 대신 신선도 보상을 쓴다(기획서 11.1)
            bool isFresh = type == DeliveryType.Fresh;

            return new DeliveryReward(
                _config.BaseFee,
                _config.DistanceFeePerMeter * distance,
                hasDeadline && isFresh == false ? _config.TimeBonus : 0f,
                traitBonus,
                hasDeadline && isFresh == false ? _config.LateLoss * _typeConfig.GetLateLossMultiplier(type) : 0f,
                _config.FailurePenalty * _typeConfig.GetFailurePenaltyMultiplier(type),
                isFresh ? _config.FreshBonus : 0f);
        }

        /// <summary>기획서 7.1과 같은 XZ 평면 직선거리 기준.</summary>
        private static float CalculateXZDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
