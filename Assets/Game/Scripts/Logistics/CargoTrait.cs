using System;

namespace Game.Logistics
{
    /// <summary>물류 특징(기획서 6.3). 하나의 물류에 여러 특징이 함께 붙을 수 있어 비트 플래그로 둔다. 특징별 수치는 CargoTraitConfig에서 관리한다.</summary>
    [Flags]
    public enum CargoTrait : int
    {
        None = 0,
        Fragile = 1 << 0,
        Heavy = 1 << 1,
        Large = 1 << 2,
    }
}
