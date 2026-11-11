using UnityEngine;

namespace Game.Logistics
{
    /// <summary>배송지 할당 규칙. 후보 중 균등 확률 선택과 하루 시작 리셋을 담당하며, 배송지별 누적 할당량 데이터는 DeliveryTarget이 보관한다.</summary>
    public static class DeliveryAssignmentSystem
    {
        /// <summary>할당 가능한 배송지 중 하나를 균등 확률로 선택해 할당한다. 후보가 없으면 false를 반환하고 아무것도 할당하지 않는다.</summary>
        public static bool TryAssignRandom(out DeliveryTarget target)
        {
            target = null;

            var targets = LogisticsCenter.DeliveryTargets;
            if (targets == null || targets.Count == 0)
            {
                return false;
            }

            // 후보 수를 먼저 세고, 같은 순회로 N번째 후보를 고른다(후보 목록 할당 회피)
            int candidateCount = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] != null && targets[i].CanAllocate())
                {
                    candidateCount++;
                }
            }

            if (candidateCount == 0)
            {
                return false;
            }

            int pick = Random.Range(0, candidateCount);
            for (int i = 0; i < targets.Count; i++)
            {
                DeliveryTarget candidate = targets[i];
                if (candidate == null || candidate.CanAllocate() == false)
                {
                    continue;
                }

                if (pick == 0)
                {
                    candidate.AssignParcel();
                    target = candidate;
                    return true;
                }

                pick--;
            }

            return false;
        }

        /// <summary>하루 시작 시 모든 배송지의 일일 누적 할당량을 초기화한다.</summary>
        public static void ResetDailyAssignment()
        {
            var targets = LogisticsCenter.DeliveryTargets;
            if (targets == null)
            {
                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] != null)
                {
                    targets[i].ResetDailyAssignment();
                }
            }
        }
    }
}
