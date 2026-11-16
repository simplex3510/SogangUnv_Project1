using UnityEngine;

namespace Game.WorldObjects
{
    /// <summary>방해물 효과 컴포넌트의 기반. 같은 오브젝트에 여러 개 붙이면 ObstacleModule이 모두 실행한다.</summary>
    public abstract class ObstacleEffect : MonoBehaviour
    {
        [SerializeField] protected ObstacleEffectConfig _config;

        /// <summary>효과를 실행한다. 대상이 살아 있음은 호출 전에 확인된다.</summary>
        public abstract void Apply(IVehicleEffectTarget target);

        protected bool HasConfig()
        {
            if (_config != null)
            {
                return true;
            }

            Debug.LogError($"ObstacleEffectConfig is not assigned in {gameObject.name}");
            return false;
        }
    }
}
