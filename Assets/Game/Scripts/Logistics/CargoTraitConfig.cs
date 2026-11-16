using System;

using UnityEngine;

namespace Game.Logistics
{
    /// <summary>특징 하나의 생성 확률과 일반 물류 대비 가감값(기획서 6.3, 결정 D3·D4).</summary>
    [Serializable]
    public class CargoTraitEntry
    {
        [Tooltip("특징 하나만 지정한다(복수 지정 금지)")]
        [SerializeField] private CargoTrait _trait;
        [Tooltip("이 특징이 붙을 확률(0~1). 특징마다 독립적으로 추첨한다")]
        [SerializeField, Range(0f, 1f)] private float _spawnProbability = 0.2f; // TODO(tuning)
        [Tooltip("일반 물류 내구도에서 뺄 값")]
        [SerializeField, Min(0)] private int _durabilityReduction; // TODO(tuning)
        [Tooltip("일반 물류 적재 용량 사용량에 더할 값")]
        [SerializeField, Min(0)] private int _capacityUsageBonus; // TODO(tuning)
        [Tooltip("일반 물류 무게 적재량에 더할 값")]
        [SerializeField, Min(0)] private int _weightUsageBonus; // TODO(tuning)
        [Tooltip("이 특징이 붙은 물류를 배송 완료하면 더하는 보상(특징 1개당 고정, 기획서 11.1)")]
        [SerializeField, Min(0f)] private float _rewardBonus; // TODO(tuning)

        public CargoTrait Trait => _trait;
        public float RewardBonus => _rewardBonus;
        public float SpawnProbability => _spawnProbability;
        public int DurabilityReduction => _durabilityReduction;
        public int CapacityUsageBonus => _capacityUsageBonus;
        public int WeightUsageBonus => _weightUsageBonus;
    }

    /// <summary>일반 물류 기준값과 특징별 설정. 물류 생성 시 CreateSpec으로 스냅샷을 만든다.</summary>
    [CreateAssetMenu(menuName = "Game/Logistics/CargoTraitConfig", fileName = "CargoTraitConfig")]
    public class CargoTraitConfig : ScriptableObject
    {
        [Header("일반 물류 기준값")]
        [SerializeField, Min(1)] private int _baseDurability = 100; // TODO(tuning)
        [SerializeField, Min(1)] private int _baseCapacityUsage = 1; // TODO(tuning)
        [SerializeField, Min(1)] private int _baseWeightUsage = 1; // TODO(tuning)

        [Header("충돌")]
        [Tooltip("Fragile 물류의 충돌 내구도 감소 배율. 특징이 여럿이어도 이 배율 하나만 적용한다(기획서 6.4·10.2)")]
        [SerializeField, Min(1f)] private float _fragileDamageMultiplier = 1.5f; // TODO(tuning)

        [Header("특징")]
        [SerializeField] private CargoTraitEntry[] _entries = Array.Empty<CargoTraitEntry>();

        /// <summary>충돌 내구도 감소 배율. Fragile이면 Fragile 배율, 그 밖에는 1.</summary>
        public float GetDamageMultiplier(CargoTrait traits)
        {
            return (traits & CargoTrait.Fragile) != 0 ? _fragileDamageMultiplier : 1f;
        }

        /// <summary>특징을 독립 추첨하고, 기준값에 특징별 가감값을 합산한 스냅샷을 만든다.</summary>
        public CargoSpec CreateSpec()
        {
            CargoTrait traits = CargoTrait.None;
            int durability = _baseDurability;
            int capacityUsage = _baseCapacityUsage;
            int weightUsage = _baseWeightUsage;

            for (int i = 0; i < _entries.Length; i++)
            {
                CargoTraitEntry entry = _entries[i];
                if (UnityEngine.Random.value >= entry.SpawnProbability)
                {
                    continue;
                }

                traits |= entry.Trait;
                durability -= entry.DurabilityReduction;
                capacityUsage += entry.CapacityUsageBonus;
                weightUsage += entry.WeightUsageBonus;
            }

            // 내구도 0 처리는 미정(기획서 16.6)이므로 생성 시점에는 1 이상으로 둔다
            return new CargoSpec(traits, Mathf.Max(durability, 1), capacityUsage, weightUsage);
        }

        /// <summary>부여된 특징마다 고정 보상을 합산한다. 특징이 많을수록 커진다(결정 D4).</summary>
        public float GetRewardBonus(CargoTrait traits)
        {
            float bonus = 0f;
            for (int i = 0; i < _entries.Length; i++)
            {
                if ((traits & _entries[i].Trait) != 0)
                {
                    bonus += _entries[i].RewardBonus;
                }
            }

            return bonus;
        }
    }
}
