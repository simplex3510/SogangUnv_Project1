namespace Game.Logistics
{
    /// <summary>배송 제한 시간(신선도 포함)이 줄기 시작하는 시점(기획서 7.2).</summary>
    public enum DeadlineStart : int
    {
        /// <summary>물류 생성 시점부터 감소한다(Express).</summary>
        Creation = 0,

        /// <summary>물류마다 상차가 완료된 시점부터 감소한다(Fresh). 대기 중에는 줄지 않는다.</summary>
        LoadCompletion = 1,
    }
}
