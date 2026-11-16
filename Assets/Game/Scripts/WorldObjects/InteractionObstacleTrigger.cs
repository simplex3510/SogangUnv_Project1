using UnityEngine;

using Game.Interaction;

namespace Game.WorldObjects
{
    /// <summary>
    /// 트리거: 상호작용 키를 InteractionDuration 동안 유지하면 실행한다(기존 IInteractable 재사용).
    /// 구역(isTrigger 콜라이더) 안에 있는 차량을 대상으로 삼는다.
    /// </summary>
    [RequireComponent(typeof(ObstacleModule))]
    public class InteractionObstacleTrigger : InteractableObject
    {
        private ObstacleModule _module;
        private IVehicleEffectTarget _target;

        // 차체 콜라이더가 여러 개라 Enter/Exit가 콜라이더 수만큼 들어온다
        private int _overlapCount;

        private void Awake()
        {
            _module = GetComponent<ObstacleModule>();

            // 플레이어 컨트롤러가 유형으로 분기하므로 프리팹 배선 실수를 막도록 고정한다
            _interactionType = InteractionTypes.Obstacle;
        }

        private void OnTriggerEnter(Collider other)
        {
            IVehicleEffectTarget target = other.GetComponentInParent<IVehicleEffectTarget>();
            if (target == null)
            {
                return;
            }

            _overlapCount = target == _target ? _overlapCount + 1 : 1;
            _target = target;
        }

        private void OnTriggerExit(Collider other)
        {
            IVehicleEffectTarget target = other.GetComponentInParent<IVehicleEffectTarget>();
            if (target != null && target == _target && --_overlapCount <= 0)
            {
                _target = null;
                _overlapCount = 0;
            }
        }

        public override bool CanInteract()
        {
            return _module.CanExecute(_target);
        }

        public override void Interact()
        {
            _module.TryExecute(_target);
        }
    }
}
