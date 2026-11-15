namespace Game.Logistics
{
    /// <summary>물류 생성 시점에 고정되는 특징·내구도·적재 사용량 묶음(스냅샷). 사용량은 적재 판정용 추상 수치다(기획서 8.5).</summary>
    public readonly struct CargoSpec
    {
        public readonly CargoTrait Traits;
        public readonly int Durability;
        public readonly int CapacityUsage;
        public readonly int WeightUsage;

        public CargoSpec(CargoTrait traits, int durability, int capacityUsage, int weightUsage)
        {
            Traits = traits;
            Durability = durability;
            CapacityUsage = capacityUsage;
            WeightUsage = weightUsage;
        }
    }
}
