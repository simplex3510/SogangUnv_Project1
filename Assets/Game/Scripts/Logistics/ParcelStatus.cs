namespace Game.Logistics
{
    /// <summary>물류 하나의 배송 진행 상태.</summary>
    public enum ParcelStatus : int
    {
        Pending = 0,
        Loaded = 1,
        Delivered = 2,
        Failed = 3,
    }
}
