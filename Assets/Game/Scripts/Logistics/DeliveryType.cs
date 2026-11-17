namespace Game.Logistics
{
    /// <summary>배송 타입 식별자. 타입별 수치(확률·계수)는 이 enum에 넣지 않고 DeliveryTypeConfig에서 관리한다.</summary>
    public enum DeliveryType : int
    {
        Normal = 0,
        Express = 1,

        /// <summary>신선 배송. 제한 시간(신선도)이 상차 완료 시점부터 줄고, 0이 되면 폐기된다(기획서 6.2).</summary>
        Fresh = 2,
    }
}
