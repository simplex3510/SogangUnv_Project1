using UnityEngine;

using Game.Systems;

namespace Game.WorldObjects
{
    /// <summary>
    /// 정적 방해물의 실행 허브(방해물 프레임워크 R3, R6). 같은 오브젝트의 효과·수명 컴포넌트를 모아
    /// 트리거가 TryExecute를 부르면 한 번만 실행한다. 물류·내구도 계열 심볼은 참조하지 않는다(R1).
    /// </summary>
    [DisallowMultipleComponent]
    public class ObstacleModule : MonoBehaviour
    {
        private readonly OneShotGate _gate = new();
        private ObstacleEffect[] _effects;
        private ObstacleLifetime _lifetime;

        private void Awake()
        {
            _effects = GetComponents<ObstacleEffect>();
            _lifetime = GetComponent<ObstacleLifetime>();
        }

        /// <summary>지금 실행할 수 있는지. 이미 실행했거나, 대상이 제거됐거나, 조작 잠금(하루 종료) 중이면 false.</summary>
        public bool CanExecute(IVehicleEffectTarget target)
        {
            return _gate.IsUsed == false && IsAlive(target) && IsOperating();
        }

        /// <summary>효과를 실행한다. 실행했으면 true. 잠금·대상 제거로 건너뛰면 소모하지 않고 false.</summary>
        public bool TryExecute(IVehicleEffectTarget target)
        {
            if (CanExecute(target) == false || _gate.TryUse() == false)
            {
                return false;
            }

            for (int i = 0; i < _effects.Length; i++)
            {
                _effects[i].Apply(target);
            }

            if (_lifetime != null)
            {
                _lifetime.OnExecuted();
            }

            return true;
        }

        private static bool IsAlive(IVehicleEffectTarget target)
        {
            return target != null && (target is Object unityObject == false || unityObject != null);
        }

        // 운영 시스템이 없는 장면(개발용 맵)에서는 잠그지 않는다
        private static bool IsOperating()
        {
            return DailyOperationSystem.Instance == null || DailyOperationSystem.Instance.IsOperating;
        }
    }
}
