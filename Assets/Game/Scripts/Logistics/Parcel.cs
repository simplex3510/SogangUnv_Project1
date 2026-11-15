namespace Game.Logistics
{
    /// <summary>물류 생성 시점에 고정되는 배송 보상·손실 값 묶음(스냅샷).</summary>
    public readonly struct DeliveryReward
    {
        public readonly float BaseFee;
        public readonly float DistanceFee;
        public readonly float TimeBonus;
        public readonly float TraitBonus;
        public readonly float LateLoss;
        public readonly float FailurePenalty;

        public DeliveryReward(float baseFee, float distanceFee, float timeBonus, float traitBonus, float lateLoss, float failurePenalty)
        {
            BaseFee = baseFee;
            DistanceFee = distanceFee;
            TimeBonus = timeBonus;
            TraitBonus = traitBonus;
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
        public DeliveryGenerationType GenerationType { get; }
        public CargoSpec Cargo { get; }
        public float CreationTime { get; }
        // 제한 시간이 없는 물류(Normal)는 Deadline·RemainingDeadline·IsOverdue를 사용하지 않는다
        public bool HasDeadline { get; }
        public float Deadline { get; }
        public float RemainingDeadline { get; set; }
        public ParcelStatus Status { get; set; }
        public DeliveryReward Reward { get; }
        public bool IsOverdue { get; set; }

        public Parcel(
            int id,
            DeliveryTarget target,
            DeliveryType type,
            DeliveryGenerationType generationType,
            CargoSpec cargo,
            float creationTime,
            bool hasDeadline,
            float deadline,
            DeliveryReward reward)
        {
            Id = id;
            Target = target;
            Type = type;
            GenerationType = generationType;
            Cargo = cargo;
            CreationTime = creationTime;
            HasDeadline = hasDeadline;
            Deadline = hasDeadline ? deadline : 0f;
            Reward = reward;

            RemainingDeadline = Deadline;
            Status = ParcelStatus.Pending;
        }

        /// <summary>같은 배송지 물류의 처리 순서(기획서 7.4): 제한 시간 있는 물류를 남은 시간 짧은 순으로 먼저, 그다음 생성 순.</summary>
        public static int CompareProcessingOrder(Parcel a, Parcel b)
        {
            if (a.HasDeadline != b.HasDeadline)
            {
                return a.HasDeadline ? -1 : 1;
            }

            if (a.HasDeadline)
            {
                int byRemaining = a.RemainingDeadline.CompareTo(b.RemainingDeadline);
                if (byRemaining != 0)
                {
                    return byRemaining;
                }
            }

            return a.Id.CompareTo(b.Id);
        }
    }
}
