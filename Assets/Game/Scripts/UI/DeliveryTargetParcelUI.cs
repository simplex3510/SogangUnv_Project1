using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using Game.Logistics;

namespace Game.UI
{
    /// <summary>배송지에 할당된 물류를 개별 아이콘으로 표시한다. 플레이어가 실은 물류는 하차 상호작용 UI와 같은 색으로 강조한다.</summary>
    [RequireComponent(typeof(DeliveryTarget))]
    public class DeliveryTargetParcelUI : MonoBehaviour
    {
        [SerializeField] private RectTransform _iconRoot;
        [SerializeField] private Image _iconTemplate;
        [Tooltip("플레이어가 적재한 물류 아이콘 색. 하차 상호작용 UI 색과 맞춘다")]
        [SerializeField] private Color _loadedColor = new(0.549f, 0.778f, 0.961f, 1f);

        private DeliveryTarget _target;
        private readonly List<Image> _icons = new();

        private void Awake()
        {
            _target = GetComponent<DeliveryTarget>();

            if (_iconRoot == null || _iconTemplate == null)
            {
                Debug.LogError($"Parcel icon references are missing in {gameObject.name}");
                enabled = false;
                return;
            }

            _iconTemplate.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (_target == null)
            {
                return;
            }

            _target.ParcelsChanged += OnParcelsChanged;
            RefreshIcons(_target.AllocatedParcels);
        }

        private void OnDisable()
        {
            if (_target != null)
            {
                _target.ParcelsChanged -= OnParcelsChanged;
            }
        }

        private void OnParcelsChanged(Parcel parcel)
        {
            RefreshIcons(_target.AllocatedParcels);
        }

        private void RefreshIcons(IReadOnlyList<Parcel> parcels)
        {
            int iconIndex = 0;
            for (int i = 0; i < parcels.Count; i++)
            {
                if (IsRemaining(parcels[i]) == false)
                {
                    continue;
                }

                Image icon = GetIcon(iconIndex++);
                icon.color = parcels[i].Status == ParcelStatus.Loaded ? _loadedColor : Color.white;
                icon.gameObject.SetActive(true);
            }

            for (int i = iconIndex; i < _icons.Count; i++)
            {
                _icons[i].gameObject.SetActive(false);
            }
        }

        private Image GetIcon(int index)
        {
            while (_icons.Count <= index)
            {
                Image icon = Instantiate(_iconTemplate, _iconRoot);
                icon.gameObject.SetActive(false);
                _icons.Add(icon);
            }

            return _icons[index];
        }

        private static bool IsRemaining(Parcel parcel)
        {
            return parcel.Status != ParcelStatus.Delivered && parcel.Status != ParcelStatus.Failed;
        }
    }
}
