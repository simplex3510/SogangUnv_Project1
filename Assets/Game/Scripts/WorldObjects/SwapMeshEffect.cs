using UnityEngine;

namespace Game.WorldObjects
{
    /// <summary>실행 시 자식 렌더 메시를 지정한 메시로 바꾼다(콘이 쓰러진 모습 등 연출용, 카탈로그 2.1 규칙 1). 콜라이더는 바꾸지 않는다.</summary>
    public class SwapMeshEffect : ObstacleEffect
    {
        [SerializeField] private Mesh _swappedMesh;

        public override void Apply(IVehicleEffectTarget target)
        {
            MeshFilter filter = GetComponentInChildren<MeshFilter>();
            if (filter == null || _swappedMesh == null)
            {
                Debug.LogError($"MeshFilter or swapped mesh is missing in {gameObject.name}");
                return;
            }

            filter.sharedMesh = _swappedMesh;
        }
    }
}
