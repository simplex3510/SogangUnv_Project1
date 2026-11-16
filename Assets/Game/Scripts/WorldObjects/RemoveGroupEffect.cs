using UnityEngine;

namespace Game.WorldObjects
{
    /// <summary>같은 그룹 루트(부모) 아래의 다른 오브젝트를 모두 제거한다(막힌 길 개방 등). 자기 자신은 수명 컴포넌트가 처리한다.</summary>
    public class RemoveGroupEffect : ObstacleEffect
    {
        public override void Apply(IVehicleEffectTarget target)
        {
            Transform root = transform.parent;
            if (root == null)
            {
                return;
            }

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform sibling = root.GetChild(i);
                if (sibling != transform)
                {
                    Destroy(sibling.gameObject);
                }
            }
        }
    }
}
