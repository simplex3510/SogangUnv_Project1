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

        /// <summary>Fresh의 최대 신선도 보상 B_fresh. 실제 지급액은 하차 시점의 남은 비율로 정한다(기획서 14.2).</summary>
        public readonly float FreshBonusMax;

        public DeliveryReward(float baseFee, float distanceFee, float timeBonus, float traitBonus, float lateLoss, float failurePenalty, float freshBonusMax = 0f)
        {
            BaseFee = baseFee;
            DistanceFee = distanceFee;
            TimeBonus = timeBonus;
            TraitBonus = traitBonus;
            LateLoss = lateLoss;
            FailurePenalty = failurePenalty;
            FreshBonusMax = freshBonusMax;
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

        // Fresh는 Deadline을 신선도 총량 T_fresh, RemainingDeadline을 남은 신선도로 쓴다
        /// <summary>제한 시간이 상차 완료 시점부터 줄어드는지. 대기(Pending) 중에는 줄지 않는다.</summary>
        public bool DeadlineStartsOnLoad { get; }

        /// <summary>신선 배송 물류인지. 제한 시간 대신 신선도를 쓰고 0이 되면 폐기된다.</summary>
        public bool IsFresh => Type == DeliveryType.Fresh;
        public float Deadline { get; }
        public float RemainingDeadline { get; set; }

        /// <summary>상차를 완료한 게임 시각(초). 상차 전에는 0. Fresh는 남은 신선도 = 총량 − (현재 시각 − 이 값)으로 구한다.</summary>
        public float LoadCompletionTime { get; set; }
        public ParcelStatus Status { get; set; }
        public DeliveryReward Reward { get; }
        public bool IsOverdue { get; set; }

        /// <summary>하차 완료 시점에 확정한 신선도 보상. Fresh가 아니면 0.</summary>
        public float FreshBonusEarned { get; private set; }

        /// <summary>현재 내구도. 초기 내구도(Cargo.Durability)에서 시작해 충돌로만 감소하며 0 아래로 내려가지 않는다.</summary>
        public float CurrentDurability { get; private set; }

        /// <summary>내구도가 0이 되어 폐기 대상이 된 물류인지.</summary>
        public bool IsDepleted => CurrentDurability <= 0f;

        /// <summary>폐기(Spoiled) 상태인지. 폐기 물류의 목적지는 처분 장소이며 원래 배송지에서는 빠진다(기획서 11.6).</summary>
        public bool IsSpoiled => Status == ParcelStatus.Spoiled;

        /// <summary>해당 배송지에 하차할 수 있는 물류인지. 폐기 물류는 원래 배송지로 하차하지 않는다.</summary>
        public bool IsDeliverableTo(DeliveryTarget target)
        {
            return IsSpoiled == false && Target == target;
        }

        public Parcel(
            int id,
            DeliveryTarget target,
            DeliveryType type,
            DeliveryGenerationType generationType,
            CargoSpec cargo,
            float creationTime,
            bool hasDeadline,
            bool deadlineStartsOnLoad,
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
            DeadlineStartsOnLoad = deadlineStartsOnLoad;
            Deadline = hasDeadline ? deadline : 0f;
            Reward = reward;

            RemainingDeadline = Deadline;
            Status = ParcelStatus.Pending;
            CurrentDurability = cargo.Durability;
        }

        /// <summary>현재 내구도를 damage만큼 줄이고(0 하한), 이번 호출로 0에 도달했으면 true를 돌려준다.</summary>
        public bool ApplyDurabilityDamage(float damage)
        {
            if (IsDepleted || damage <= 0f)
            {
                return false;
            }

            CurrentDurability = System.Math.Max(CurrentDurability - damage, 0f);
            return IsDepleted;
        }

        /// <summary>하차 완료 시점의 남은 신선도 비율로 신선도 보상 B_fresh × clamp(남은÷총량, 0, 1)을 확정한다(기획서 14.2). Fresh가 아니면 아무것도 하지 않는다.</summary>
        public void SettleFreshBonus()
        {
            if (IsFresh && Deadline > 0f)
            {
                FreshBonusEarned = Reward.FreshBonusMax * System.Math.Clamp(RemainingDeadline / Deadline, 0f, 1f);
            }
        }

        // 처리 순서 묶음(기획서 7.4): 상차된 Fresh → 제한 시간 있는 물류(Express) → 대기 중인 Fresh → 나머지
        private int ProcessingRank => IsFresh ? (Status == ParcelStatus.Pending ? 2 : 0) : (HasDeadline ? 1 : 3);

        /// <summary>
        /// 같은 배송지 물류의 처리 순서(기획서 7.4): 상차된 Fresh, Express, 대기 Fresh, Normal 순이며
        /// 앞의 세 묶음은 남은 시간이 짧은 순, 같으면 생성 순이다. 대기 Fresh는 감소 전이라 남은 값이 총량과 같다.
        /// </summary>
        public static int CompareProcessingOrder(Parcel a, Parcel b)
        {
            int byRank = a.ProcessingRank.CompareTo(b.ProcessingRank);
            if (byRank != 0)
            {
                return byRank;
            }

            if (a.ProcessingRank < 3)
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
