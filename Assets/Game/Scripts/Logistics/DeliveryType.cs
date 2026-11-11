namespace Game.Logistics
{
    /// <summary>배송 타입 식별자. 타입별 수치(확률·계수)는 이 enum에 넣지 않고 DeliveryTypeConfig에서 관리한다.</summary>
    public enum DeliveryType : int
    {
        Normal = 0,
        Express = 1,
    }
}
