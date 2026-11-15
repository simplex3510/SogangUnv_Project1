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

        public int CargoCapacity => _cargoCapacity;
        public int WeightCapacity => _weightCapacity;
    }
}
