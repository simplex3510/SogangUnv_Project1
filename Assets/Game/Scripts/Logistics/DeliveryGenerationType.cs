namespace Game.Logistics
{
    /// <summary>물류가 생성된 시점에 따른 생성 방식(기획서 4장). 배송 유형·물류 특징과 독립된 분류다.</summary>
    public enum DeliveryGenerationType : int
    {
        Static = 0,
        Dynamic = 1,
    }
}
