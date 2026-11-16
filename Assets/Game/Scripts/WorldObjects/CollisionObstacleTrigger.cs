using UnityEngine;

namespace Game.WorldObjects
{
    /// <summary>트리거: 차량이 물리적으로 접촉하면 실행한다. 접촉 판정은 플레이어 충돌 로직이 따로 하며 여기서는 간섭하지 않는다.</summary>
    [RequireComponent(typeof(ObstacleModule))]
    public class CollisionObstacleTrigger : MonoBehaviour
    {
        private ObstacleModule _module;

        private void Awake()
        {
            _module = GetComponent<ObstacleModule>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            IVehicleEffectTarget target = collision.collider.GetComponentInParent<IVehicleEffectTarget>();
            if (target != null)
            {
                _module.TryExecute(target);
            }
        }
    }
}
