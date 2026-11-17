using System;

using UnityEngine;

using Game.Logistics;
using Game.Player.Movement;

namespace Game.Player.Interaction
{
    /// <summary>
    /// 차량 충돌 시작 후 판정 창 동안의 충격량으로 적재된 물류의 내구도를 줄인다.
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

        // 한 충돌의 충격이 여러 물리 스텝에 나뉘어 들어오므로, 충돌 직전 속도와 판정 창이 끝난 뒤 속도를 비교한다
        private Vector3 _lastStepVelocity;   // 직전 FixedUpdate 시점(물리 스텝 전) 속도
        private Vector3 _windowStartVelocity;
        private int _windowStepsLeft; // 0이면 판정 창이 닫혀 있다
        private float _cooldownEndTime = float.NegativeInfinity;

        /// <summary>물류의 내구도가 이번 충돌로 0에 도달했을 때 발생한다. 폐기 전환 처리는 구독자(Story 005)가 맡는다.</summary>
        public event Action<Parcel> ParcelDepleted;

        /// <summary>적재 물류 중 하나 이상의 내구도가 실제로 줄어든 충돌마다 한 번 발생한다. 값은 특징 배율 적용 전 일반 물류 기준 감소량이다(기획서 13장 −N 표시).</summary>
        public event Action<float> ImpactDamageApplied;

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

        /// <summary>충돌로 차량이 받은 수평 속도 변화량(m/s). 수직 성분(착지·튀어오름)은 제외한다.</summary>
        public static float CalculateDeltaVelocity(Vector3 velocityBefore, Vector3 velocityAfter)
        {
            Vector3 delta = velocityAfter - velocityBefore;
            delta.y = 0f;
            return delta.magnitude;
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

        // 충돌 시작이 판정 창을 연다
        private void OnCollisionEnter(Collision collision)
        {
            if (IsGroundContact(collision))
            {
                return;
            }

            // 같은 충돌의 다른 콜라이더 콜백은 이미 열린 창에 포함된다
            if (_windowStepsLeft == 0)
            {
                _windowStepsLeft = Mathf.Max(1, Mathf.RoundToInt(_impactConfig.ImpactWindow / Time.fixedDeltaTime));
                _windowStartVelocity = _lastStepVelocity;
            }
        }

        // 접촉 유지(벽을 미는 동안)는 창을 열지 않는다. 밀기 시작 직전 속도가 0에 가까워 감소도 없다
        private void FixedUpdate()
        {
            if (_windowStepsLeft > 0 && --_windowStepsLeft == 0)
            {
                ResolveImpact(CalculateDeltaVelocity(_windowStartVelocity, _rigidbody.linearVelocity));
            }

            _lastStepVelocity = _rigidbody.linearVelocity;
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

        private void ResolveImpact(float deltaVelocity)
        {
            Debug.Log($"[CargoImpact] v {_windowStartVelocity.magnitude:0.##} → {_rigidbody.linearVelocity.magnitude:0.##} → deltaVelocity={deltaVelocity:0.##}");

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
            bool anyDamaged = false;
            for (int i = 0; i < parcels.Count; i++)
            {
                anyDamaged |= DamageParcel(parcels[i], damage);
            }

            // 적재 물류가 없거나 모두 이미 0이면 감소가 없으므로 알리지 않는다
            if (anyDamaged)
            {
                ImpactDamageApplied?.Invoke(damage);
            }
        }

        /// <summary>물류 하나의 내구도를 줄이고, 실제로 줄었으면 true를 돌려준다.</summary>
        private bool DamageParcel(Parcel parcel, float damage)
        {
            if (parcel.IsDepleted)
            {
                return false;
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

            return parcel.CurrentDurability < before;
        }
    }
}
