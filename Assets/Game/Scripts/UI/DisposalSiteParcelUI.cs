using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using Game.Logistics;
using Game.Player.Interaction;

namespace Game.UI
{
    /// <summary>처분 장소에 처분 대기 중인(차량에 실린 폐기) 물류를 개별 아이콘으로 표시한다. 배송지의 DeliveryTargetParcelUI와 같은 방식이다(기획서 11.6).</summary>
    [RequireComponent(typeof(DisposalSite))]
    public class DisposalSiteParcelUI : MonoBehaviour
    {
        [SerializeField] private RectTransform _iconRoot;
        [SerializeField] private Image _iconTemplate;
        [SerializeField] private Color _iconColor = new(0.9f, 0.4f, 0.3f, 1f);

        private PlayerInteractionController _controller;
        private readonly List<Image> _icons = new();

        private void Awake()
        {
            if (_iconRoot == null || _iconTemplate == null)
            {
                Debug.LogError($"Parcel icon references are missing in {gameObject.name}");
                enabled = false;
                return;
            }

            _iconTemplate.gameObject.SetActive(false);
        }

        private void Start()
        {
            _controller = FindAnyObjectByType<PlayerInteractionController>();
            if (_controller == null)
            {
                Debug.LogError("PlayerInteractionController is missing in the scene");
                return;
            }

            _controller.LoadedParcelsChanged += OnLoadedParcelsChanged;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.LoadedParcelsChanged -= OnLoadedParcelsChanged;
            }
        }

        private void OnLoadedParcelsChanged(Parcel parcel)
        {
            Refresh();
        }

        private void Refresh()
        {
            int count = 0;
            IReadOnlyList<Parcel> loaded = _controller.LoadedParcels;
            for (int i = 0; i < loaded.Count; i++)
            {
                if (loaded[i].IsSpoiled == false)
                {
                    continue;
                }

                while (_icons.Count <= count)
                {
                    Image icon = Instantiate(_iconTemplate, _iconRoot);
                    icon.color = _iconColor;
                    _icons.Add(icon);
                }

                _icons[count++].gameObject.SetActive(true);
            }

            for (int i = count; i < _icons.Count; i++)
            {
                _icons[i].gameObject.SetActive(false);
            }
        }
    }
}
