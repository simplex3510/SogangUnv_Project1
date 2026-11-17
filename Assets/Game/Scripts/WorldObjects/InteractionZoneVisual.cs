using UnityEngine;
using UnityEngine.Rendering;

namespace Game.WorldObjects
{
    /// <summary>
    /// 상호작용 트리거 박스 위에 위로 떠오르는 빛기둥을 그린다. 판정에는 관여하지 않는다.
    /// 스펙: design/quick-specs/interaction-zone-glow-2026-11-17.md
    /// </summary>
    public class InteractionZoneVisual : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("빛기둥을 맞출 상호작용 트리거 박스")]
        [SerializeField] private BoxCollider _zone;

        [Tooltip("Game/InteractionZoneGlow 셰이더 머티리얼. 밝기·흐름 등은 머티리얼에서 조절한다")]
        [SerializeField] private Material _material;

        [SerializeField] private Color _color = new(0.3f, 0.8f, 1f, 1f);

        [Tooltip("빛기둥 높이(월드 단위)")]
        [SerializeField, Range(0.2f, 3f)] private float _height = 0.8f;

        private void Start()
        {
            if (_zone == null || _material == null)
            {
                Debug.LogWarning($"{name}: InteractionZoneVisual에 구역 또는 머티리얼이 없어 빛기둥을 그리지 않는다.", this);
                return;
            }

            // 트리거의 부모 스케일과 무관하게 높이를 월드 단위로 맞춘다.
            float localHeight = _height / _zone.transform.lossyScale.y;
            Vector3 size = _zone.size;
            Vector3 center = _zone.center;

            var glow = new GameObject("InteractionZoneGlow");
            glow.transform.SetParent(_zone.transform, false);
            glow.transform.localPosition = new Vector3(center.x, center.y - size.y * 0.5f + localHeight * 0.5f, center.z);
            glow.transform.localScale = new Vector3(size.x, localHeight, size.z);

            // 콜라이더 없는 메시만 붙여 물리·상호작용 판정에 영향을 주지 않는다.
            glow.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            var meshRenderer = glow.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = _material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, _color);
            meshRenderer.SetPropertyBlock(block);
        }
    }
}
