using UnityEngine;

using Game.Logistics;
using Game.Player.Interaction;
using Game.UI.Hud;

namespace Game.UI
{
    /// <summary>처분 장소에 처분 대기 중인(차량에 실린 폐기) 물류를 개별 아이콘으로 표시한다. 배송지의 DeliveryTargetParcelUI와 같은 방식이다(기획서 11.6).</summary>
    [RequireComponent(typeof(DisposalSite))]
    public class DisposalSiteParcelUI : MonoBehaviour
    {
        [Tooltip("처분 장소 기준 아이콘 묶음 하단 위치(월드 m)")]
        [SerializeField] private Vector3 _iconOffset = new(0f, 1f, 0f); // TODO(tuning)

        private PlayerInteractionController _controller;
        private WorldParcelIconsHud.IconGroup _group;

        private void Start()
        {
            _controller = FindAnyObjectByType<PlayerInteractionController>();
            WorldParcelIconsHud hud = WorldParcelIconsHud.Instance;
            if (_controller == null || hud == null)
            {
                Debug.LogError("PlayerInteractionController or WorldParcelIconsHud is missing in the scene");
                enabled = false;
                return;
            }

            _group = hud.AddGroup(transform, _iconOffset);
            _controller.LoadedParcelsChanged += OnLoadedParcelsChanged;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.LoadedParcelsChanged -= OnLoadedParcelsChanged;
            }

            if (_group != null && WorldParcelIconsHud.Instance != null)
            {
                WorldParcelIconsHud.Instance.RemoveGroup(_group);
            }
        }

        private void OnLoadedParcelsChanged(Parcel parcel)
        {
            Refresh();
        }

        private void Refresh()
        {
            int count = 0;
            var loaded = _controller.LoadedParcels;
            for (int i = 0; i < loaded.Count; i++)
            {
                if (loaded[i].IsSpoiled)
                {
                    _group.SetIcon(count++, loaded[i].Type, true, false, -1f);
                }
            }

            _group.HideFrom(count);
        }
    }
}
