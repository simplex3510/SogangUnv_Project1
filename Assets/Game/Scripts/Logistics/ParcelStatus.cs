namespace Game.Logistics
{
    /// <summary>물류 하나의 배송 진행 상태.</summary>
    public enum ParcelStatus : int
    {
        Pending = 0,
        Loaded = 1,
        Delivered = 2,
        Failed = 3,

        /// <summary>더 이상 배송할 수 없어 폐기 대상이 된 상태(기획서 11.6). 처분 장소에서 처분되기 전까지 차량에 남는다.</summary>
        Spoiled = 4,
    }
}
