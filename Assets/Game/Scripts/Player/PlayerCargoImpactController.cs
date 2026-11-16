using System;

using UnityEngine;

using Game.Logistics;
using Game.Player.Movement;

namespace Game.Player.Interaction
{
    /// <summary>
    /// 차량 충돌 시작 시점에 적재된 물류의 내구도를 줄인다.
    /// 규칙: design/gdd/collision-durability-rules.md 2~4장, 기획서 game-concept.md 6.3·6.4·10.2.
    /// 내구도는 이 컴포넌트만 줄인다(world-obstacle-objects-framework R1).
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(PlayerInteractionController))]
    public class PlayerCargoImpactController : MonoBehaviour
    {
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private CargoImpactConfig _impactConfig;
        [SerializeField] private CargoTraitConfig _traitConfig;
        [Tooltip("지면 판정 기준(GroundNormalMinDot)을 주행과 같은 값으로 쓰기 위한 참조")]
        [SerializeField] private VehicleConfig _vehicleConfig;

        private PlayerInteractionController _interaction;

        // 차체 콜라이더가 여럿이라 한 충돌의 충격량이 콜라이더별 콜백으로 나뉘어 들어온다. 같은 물리 스텝분을 합산해 한 번에 판정한다
        private Vector3 _pendingImpulse;
        private bool _hasPendingImpact;
        private float _cooldownEndTime = float.NegativeInfinity;

        /// <summary>물류의 내구도가 이번 충돌로 0에 도달했을 때 발생한다. 폐기 전환 처리는 구독자(Story 005)가 맡는다.</summary>
        public event Action<Parcel> ParcelDepleted;

        private void Reset()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void Awake()
        {
            if (_impactConfig == null || _traitConfig == null || _vehicleConfig == null)
            {
                Debug.LogError($"CargoImpactConfig, CargoTraitConfig or VehicleConfig is not assigned in {gameObject.name}");
                enabled = false;
                return;
            }

            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
            }

            _interaction = GetComponent<PlayerInteractionController>();
        }

        /// <summary>충돌로 차량이 받은 속도 변화량(m/s) = 충격량 크기 ÷ 차량 질량.</summary>
        public static float CalculateDeltaVelocity(float impulseMagnitude, float mass)
        {
            return impulseMagnitude / mass;
        }

        /// <summary>속도 변화량에서 기준 감소량을 구한다. 하한선 미만이면 0, 상한을 넘지 않는다.</summary>
        public static float CalculateDamage(float deltaVelocity, float threshold, float damagePerVelocity, float maxDamage)
        {
            if (deltaVelocity < threshold)
            {
                return 0f;
            }

            return Mathf.Min((deltaVelocity - threshold) * damagePerVelocity, maxDamage);
        }

        // 충돌 시작 시점만 받는다. OnCollisionStay는 구현하지 않는다(벽을 미는 동안의 엔진 힘 배제)
        private void OnCollisionEnter(Collision collision)
        {
            if (IsGroundContact(collision))
            {
                return;
            }

            _pendingImpulse += collision.impulse;
            _hasPendingImpact = true;
        }

        private void FixedUpdate()
        {
            if (_hasPendingImpact == false)
            {
                return;
            }

            float impulseMagnitude = _pendingImpulse.magnitude;
            _pendingImpulse = Vector3.zero;
            _hasPendingImpact = false;

            ResolveImpact(impulseMagnitude);
        }

        /// <summary>모든 접촉 법선이 지면 기준 이상이면 지면 접촉·착지로 본다.</summary>
        private bool IsGroundContact(Collision collision)
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y < _vehicleConfig.GroundNormalMinDot)
                {
                    return false;
                }
            }

            return true;
        }

        private void ResolveImpact(float impulseMagnitude)
        {
            float deltaVelocity = CalculateDeltaVelocity(impulseMagnitude, _rigidbody.mass);
            Debug.Log($"[CargoImpact] impulse={impulseMagnitude:0.##} mass={_rigidbody.mass:0.##} → deltaVelocity={deltaVelocity:0.##}");

            if (Time.fixedTime < _cooldownEndTime)
            {
                Debug.Log($"[CargoImpact] 쿨다운 중 재접촉 deltaVelocity={deltaVelocity:0.##} → 무시");
                return;
            }

            float damage = CalculateDamage(deltaVelocity, _impactConfig.ImpactThreshold,
                                           _impactConfig.DamagePerVelocity, _impactConfig.MaxDamagePerImpact);
            if (damage <= 0f)
            {
                Debug.Log($"[CargoImpact] deltaVelocity={deltaVelocity:0.##} < 하한선 {_impactConfig.ImpactThreshold:0.##} → 무시");
                return;
            }

            _cooldownEndTime = Time.fixedTime + _impactConfig.ImpactCooldown;
            Debug.Log($"[CargoImpact] damage=min(({deltaVelocity:0.##}-{_impactConfig.ImpactThreshold:0.##})*{_impactConfig.DamagePerVelocity:0.##}, {_impactConfig.MaxDamagePerImpact:0.##})={damage:0.##}");

            var parcels = _interaction.LoadedParcels;
            for (int i = 0; i < parcels.Count; i++)
            {
                DamageParcel(parcels[i], damage);
            }
        }

        private void DamageParcel(Parcel parcel, float damage)
        {
            if (parcel.IsDepleted)
            {
                return;
            }

            float multiplier = _traitConfig.GetDamageMultiplier(parcel.Cargo.Traits);
            float before = parcel.CurrentDurability;
            bool depleted = parcel.ApplyDurabilityDamage(damage * multiplier);

            string label = multiplier > 1f ? $"Fragile ×{multiplier:0.##}" : "Normal";
            Debug.Log($"[CargoImpact] Parcel#{parcel.Id} ({label})  {before:0.##} → {parcel.CurrentDurability:0.##}");

            if (depleted)
            {
                ParcelDepleted?.Invoke(parcel);
            }
        }
    }
}
