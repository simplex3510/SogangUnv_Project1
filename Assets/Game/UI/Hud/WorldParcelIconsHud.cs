using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using Game.Logistics;

namespace Game.UI.Hud
{
    /// <summary>
    /// 배송지·처분장 위에 뜨는 물류 아이콘 패널(Story 026). 대상이 <see cref="AddGroup"/>으로 자기 아이콘 묶음을 등록하면
    /// 묶음이 대상의 월드 위치를 따라간다. 화면 밖이거나 카메라 뒤면 숨긴다. 표시만 하고 상태는 바꾸지 않는다.
    /// </summary>
    [RequireComponent(typeof(PanelRenderer))]
    public class WorldParcelIconsHud : MonoBehaviour
    {
        private const string LoadedClass = "world-icon--loaded";

        /// <summary>씬의 월드 아이콘 HUD(1개 배치 가정).</summary>
        public static WorldParcelIconsHud Instance { get; private set; }

        [Tooltip("월드→화면 변환 카메라. 비우면 Camera.main")]
        [SerializeField] private Camera _camera;
        [Tooltip("묶음 템플릿(WorldIconGroup.uxml)")]
        [SerializeField] private VisualTreeAsset _groupTemplate;
        [Tooltip("아이콘 템플릿(WorldParcelIcon.uxml)")]
        [SerializeField] private VisualTreeAsset _iconTemplate;

        private readonly List<IconGroup> _groups = new();
        private VisualElement _root;

        /// <summary>대상 하나의 아이콘 묶음. 아이콘은 풀링한다.</summary>
        public class IconGroup
        {
            internal Transform Anchor;
            internal Vector3 Offset;
            internal VisualElement Element;
            internal WorldParcelIconsHud Owner;
            internal readonly List<VisualElement> Icons = new();

            /// <summary>index번째 아이콘을 보이고 유형 색·적재 강조·신선도 바를 정한다. 바가 없으면 freshness는 음수.</summary>
            public void SetIcon(int index, DeliveryType type, bool spoiled, bool loaded, float freshness)
            {
                VisualElement icon = GetIcon(index);
                icon.style.display = DisplayStyle.Flex;
                icon.EnableInClassList(LoadedClass, loaded);

                VisualElement parcelIcon = icon.Q("parcelIcon");
                parcelIcon.EnableInClassList("parcel-icon--express", type == DeliveryType.Express);
                parcelIcon.EnableInClassList("parcel-icon--fresh", type == DeliveryType.Fresh);
                parcelIcon.EnableInClassList("parcel-icon--spoiled", spoiled);

                VisualElement bar = icon.Q("bar");
                bar.style.display = freshness >= 0f ? DisplayStyle.Flex : DisplayStyle.None;
                if (freshness >= 0f)
                {
                    bar.style.width = Length.Percent(Mathf.Clamp01(freshness) * 100f);
                }
            }

            /// <summary>count번째부터 끝까지 아이콘을 숨긴다.</summary>
            public void HideFrom(int count)
            {
                for (int i = count; i < Icons.Count; i++)
                {
                    Icons[i].style.display = DisplayStyle.None;
                }
            }

            private VisualElement GetIcon(int index)
            {
                while (Icons.Count <= index)
                {
                    VisualElement icon = Owner._iconTemplate.Instantiate().Q("icon");
                    Element.Add(icon);
                    Icons.Add(icon);
                }

                return Icons[index];
            }
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            GetComponent<PanelRenderer>().RegisterUIReloadCallback(OnUIReload);
        }

        private void OnDisable()
        {
            GetComponent<PanelRenderer>().UnregisterUIReloadCallback(OnUIReload);
            _root = null;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // 트리를 다시 만들면 등록된 묶음을 새 루트로 옮긴다
        private void OnUIReload(PanelRenderer _, VisualElement root, int version)
        {
            _root = root.Q("worldIconsHud");
            if (_root == null)
            {
                Debug.LogError($"WorldParcelIconsHud UXML elements are missing in {gameObject.name}");
                return;
            }

            foreach (IconGroup group in _groups)
            {
                _root.Add(group.Element);
            }
        }

        /// <summary>anchor 위 offset(월드 m)에 아이콘 묶음을 만든다. 대상이 사라질 때 <see cref="RemoveGroup"/>으로 돌려준다.</summary>
        public IconGroup AddGroup(Transform anchor, Vector3 offset)
        {
            var group = new IconGroup
            {
                Anchor = anchor,
                Offset = offset,
                Owner = this,
                Element = _groupTemplate.Instantiate().Q("group"),
            };
            group.Element.style.display = DisplayStyle.None;
            _groups.Add(group);
            _root?.Add(group.Element);
            return group;
        }

        public void RemoveGroup(IconGroup group)
        {
            _groups.Remove(group);
            group.Element.RemoveFromHierarchy();
        }

        private void LateUpdate()
        {
            if (_root == null)
            {
                return;
            }

            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null)
                {
                    return;
                }
            }

            foreach (IconGroup group in _groups)
            {
                Vector3 world = group.Anchor.position + group.Offset;
                Vector3 viewport = _camera.WorldToViewportPoint(world);
                bool visible = viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
                group.Element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                if (visible == false)
                {
                    continue;
                }

                Vector2 position = RuntimePanelUtils.CameraTransformWorldToPanel(_root.panel, world, _camera);
                group.Element.style.left = position.x;
                group.Element.style.top = position.y;
            }
        }
    }
}
