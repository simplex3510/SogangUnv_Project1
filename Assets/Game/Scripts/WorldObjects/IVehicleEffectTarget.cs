namespace Game.WorldObjects
{
    /// <summary>
    /// 방해물 효과가 차량에 영향을 주는 유일한 통로(방해물 프레임워크 R3).
    /// 효과는 Rigidbody 속도를 직접 쓰지 않고, 차량 컨트롤러가 요청을 받아 적용한다.
    /// </summary>
    public interface IVehicleEffectTarget
    {
        /// <summary>최고 속도 배율을 지속 시간 동안 적용한다. 겹치면 가장 강한 값만 적용하고 지속 시간을 갱신한다.</summary>
        public void ApplySpeedMultiplier(float multiplier, float durationSeconds);

        /// <summary>진행 방향을 yaw(도, 양수=오른쪽)만큼 돌려 달라고 요청한다.</summary>
        public void RequestHeadingRotation(float yawDegrees);
    }
}
