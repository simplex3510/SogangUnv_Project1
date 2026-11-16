using System;

using UnityEngine;

namespace Game.WorldObjects
{
    /// <summary>차량 진행 방향을 요(yaw)로 돌려 달라고 요청한다. 무작위 방향은 주입된 난수원으로 정한다.</summary>
    public class HeadingRotationEffect : ObstacleEffect
    {
        /// <summary>0 이상 1 미만을 돌려주는 난수원. 비어 있으면 UnityEngine.Random을 쓴다. 검증 시 고정값을 주입한다.</summary>
        public Func<float> RandomSource { get; set; }

        public override void Apply(IVehicleEffectTarget target)
        {
            if (HasConfig() == false)
            {
                return;
            }

            float yaw = _config.HeadingYawDegrees;
            if (_config.HeadingRandomSign)
            {
                float roll = RandomSource != null ? RandomSource() : UnityEngine.Random.value;
                yaw *= roll < 0.5f ? -1f : 1f;
            }

            target.RequestHeadingRotation(yaw);
        }
    }
}
