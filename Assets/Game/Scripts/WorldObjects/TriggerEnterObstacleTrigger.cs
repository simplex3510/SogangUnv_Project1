using UnityEngine;

namespace Game.WorldObjects
{
    /// <summary>트리거: 차량이 isTrigger 콜라이더에 들어오면 실행한다.</summary>
    [RequireComponent(typeof(ObstacleModule))]
    public class TriggerEnterObstacleTrigger : MonoBehaviour
    {
        private ObstacleModule _module;

        private void Awake()
        {
            _module = GetComponent<ObstacleModule>();
        }

        private void OnTriggerEnter(Collider other)
        {
            IVehicleEffectTarget target = other.GetComponentInParent<IVehicleEffectTarget>();
            if (target != null)
            {
                _module.TryExecute(target);
            }
        }
    }
}
