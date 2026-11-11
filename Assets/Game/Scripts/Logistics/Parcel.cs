namespace Game.Logistics
{
    /// <summary>물류 생성 시점에 고정되는 배송 보상·손실 값 묶음(스냅샷).</summary>
    public readonly struct DeliveryReward
    {
        public readonly float BaseFee;
        public readonly float DistanceFee;
        public readonly float TimeBonus;
        public readonly float LateLoss;
        public readonly float FailurePenalty;

        public DeliveryReward(float baseFee, float distanceFee, float timeBonus, float lateLoss, float failurePenalty)
        {
            BaseFee = baseFee;
            DistanceFee = distanceFee;
            TimeBonus = timeBonus;
            LateLoss = lateLoss;
            FailurePenalty = failurePenalty;
        }
    }

    /// <summary>배송할 물류 하나를 나타내는 순수 데이터 클래스. 제한 시간·보상은 생성 시점 값으로 고정하며, 시간은 게임 시간(초) 단위이다.</summary>
    public class Parcel
    {
        public int Id { get; }
        public DeliveryTarget Target { get; }
        public DeliveryType Type { get; }
        public float CreationTime { get; }
        public float Deadline { get; }
        public float RemainingDeadline { get; set; }
        public ParcelStatus Status { get; set; }
        public DeliveryReward Reward { get; }
        public bool IsOverdue { get; set; }

        public Parcel(
            int id,
            DeliveryTarget target,
            DeliveryType type,
            float creationTime,
            float deadline,
            DeliveryReward reward)
        {
            Id = id;
            Target = target;
            Type = type;
            CreationTime = creationTime;
            Deadline = deadline;
            Reward = reward;

            RemainingDeadline = deadline;
            Status = ParcelStatus.Pending;
        }
    }
}
