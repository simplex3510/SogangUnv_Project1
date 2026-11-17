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
            internal VisualElement WaitingPanel;
            internal VisualElement LoadedPanel;
            internal WorldParcelIconsHud Owner;
            internal readonly List<VisualElement> Icons = new();

            /// <summary>index번째 아이콘을 보이고 유형 색·남은 시간 바(Express 빨강, Fresh 초록)를 정한다. loaded면 아래(배송중) 패널, 아니면 위(미배송) 패널에 둔다. 바가 없으면 freshness(남은 비율)는 음수.</summary>
            public void SetIcon(int index, DeliveryType type, bool spoiled, bool loaded, float freshness)
            {
                VisualElement icon = GetIcon(index);
                icon.style.display = DisplayStyle.Flex;
                VisualElement panel = loaded ? LoadedPanel : WaitingPanel;
                if (icon.parent != panel)
                {
                    // 패널 안에서도 index 순서를 지키도록 앞선 아이콘 수만큼 뒤에 끼운다
                    int position = 0;
                    for (int i = 0; i < index; i++)
                    {
                        if (Icons[i].parent == panel)
                        {
                            position++;
                        }
                    }

                    panel.Insert(position, icon);
                }

                panel.style.display = DisplayStyle.Flex;

                VisualElement parcelIcon = icon.Q("parcelIcon");
                parcelIcon.EnableInClassList("parcel-icon--express", type == DeliveryType.Express);
                parcelIcon.EnableInClassList("parcel-icon--fresh", type == DeliveryType.Fresh);
                parcelIcon.EnableInClassList("parcel-icon--spoiled", spoiled);

                VisualElement bar = icon.Q("bar");
                bar.EnableInClassList("world-icon-bar--express", type == DeliveryType.Express);
                bar.style.display = freshness >= 0f ? DisplayStyle.Flex : DisplayStyle.None;
                if (freshness >= 0f)
                {
                    bar.style.width = Length.Percent(Mathf.Clamp01(freshness) * 100f);
                }
            }

            /// <summary>count번째부터 끝까지 아이콘을 숨기고, 보이는 아이콘이 없는 패널도 숨긴다.</summary>
            public void HideFrom(int count)
            {
                bool hasWaiting = false;
                bool hasLoaded = false;
                for (int i = 0; i < Icons.Count; i++)
                {
                    bool visible = i < count;
                    Icons[i].style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                    hasWaiting |= visible && Icons[i].parent == WaitingPanel;
                    hasLoaded |= visible && Icons[i].parent == LoadedPanel;
                }

                WaitingPanel.style.display = hasWaiting ? DisplayStyle.Flex : DisplayStyle.None;
                LoadedPanel.style.display = hasLoaded ? DisplayStyle.Flex : DisplayStyle.None;
            }

            private VisualElement GetIcon(int index)
            {
                while (Icons.Count <= index)
                {
                    // 패널 배치는 SetIcon이 정한다
                    Icons.Add(Owner._iconTemplate.Instantiate().Q("icon"));
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
            VisualElement element = _groupTemplate.Instantiate().Q("group");
            var group = new IconGroup
            {
                Anchor = anchor,
                Offset = offset,
                Owner = this,
                Element = element,
                WaitingPanel = element.Q("waitingPanel"),
                LoadedPanel = element.Q("loadedPanel"),
            };
            group.Element.style.display = DisplayStyle.None;
            group.HideFrom(0);
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
