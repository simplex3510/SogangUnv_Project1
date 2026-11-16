namespace Game.WorldObjects
{
    /// <summary>차량 최고 속도에 배율을 지속 시간 동안 적용한다.</summary>
    public class SpeedMultiplierEffect : ObstacleEffect
    {
        public override void Apply(IVehicleEffectTarget target)
        {
            if (HasConfig())
            {
                target.ApplySpeedMultiplier(_config.SpeedMultiplier, _config.SpeedDurationSeconds);
            }
        }
    }
}
