using UnityEngine;

namespace Game.Player.Interaction
{
    /// <summary>차량 충돌로 인한 물류 내구도 감소 설정(collision-durability-rules.md 3장, 기획서 10.2).</summary>
    [CreateAssetMenu(menuName = "Game/Player/CargoImpactConfig", fileName = "CargoImpactConfig")]
    public class CargoImpactConfig : ScriptableObject
    {
        [Tooltip("이 속도 변화량(m/s) 미만의 충돌은 내구도를 줄이지 않는다")]
        [SerializeField, Min(0f)] private float _impactThreshold = 1.5f; // TODO(tuning)

        [Tooltip("하한선을 넘은 속도 변화량 1 m/s당 감소량")]
        [SerializeField, Min(0f)] private float _damagePerVelocity = 12f; // TODO(tuning)

        [Tooltip("한 번의 충돌로 줄어드는 내구도의 상한")]
        [SerializeField, Min(0f)] private float _maxDamagePerImpact = 60f; // TODO(tuning)

        [Tooltip("감소 후 추가 감소를 막는 시간(초)")]
        [SerializeField, Min(0f)] private float _impactCooldown = 0.5f; // TODO(tuning)

        [Tooltip("충돌 시작 후 접촉 유지 충격량까지 합산하는 판정 창(초). 고정물 충돌의 충격이 여러 물리 스텝에 나뉘어 들어오는 것을 모은다")]
        [SerializeField, Min(0f)] private float _impactWindow = 0.06f; // TODO(tuning)

        public float ImpactThreshold => _impactThreshold;
        public float ImpactWindow => _impactWindow;
        public float DamagePerVelocity => _damagePerVelocity;
        public float MaxDamagePerImpact => _maxDamagePerImpact;
        public float ImpactCooldown => _impactCooldown;
    }
}
