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
        [Tooltip("적재한 Fresh 아이콘 아래에 그리는 신선도 바 색")]
        [SerializeField] private Color _freshnessColor = new(0.45f, 0.9f, 0.55f, 1f);
        [Tooltip("신선도 바 높이(아이콘 높이 대비 비율)")]
        [SerializeField, Range(0.05f, 0.5f)] private float _freshnessBarHeight = 0.2f;

        private DeliveryTarget _target;
        private readonly List<Image> _icons = new();

        // 아이콘 i가 표시 중인 물류와 신선도 바. 아이콘은 풀링되므로 같은 인덱스로 관리한다
        private readonly List<Parcel> _iconParcels = new();
        private readonly List<RectTransform> _freshnessBars = new();

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

        // 신선도는 게임 시간에 따라 줄어들므로 바를 매 프레임 갱신한다
        private void Update()
        {
            for (int i = 0; i < _icons.Count; i++)
            {
                Parcel parcel = _iconParcels[i];
                bool showBar = parcel != null && parcel.IsFresh && parcel.Status == ParcelStatus.Loaded && parcel.Deadline > 0f;
                if (showBar == false)
                {
                    if (_freshnessBars[i] != null)
                    {
                        _freshnessBars[i].gameObject.SetActive(false);
                    }

                    continue;
                }

                RectTransform bar = GetFreshnessBar(i);
                bar.gameObject.SetActive(true);
                bar.anchorMax = new Vector2(Mathf.Clamp01(parcel.RemainingDeadline / parcel.Deadline), _freshnessBarHeight);
            }
        }

        private RectTransform GetFreshnessBar(int index)
        {
            if (_freshnessBars[index] == null)
            {
                var barObject = new GameObject("FreshnessBar", typeof(RectTransform), typeof(Image));
                var bar = (RectTransform)barObject.transform;
                bar.SetParent(_icons[index].rectTransform, false);
                bar.anchorMin = Vector2.zero;
                bar.offsetMin = Vector2.zero;
                bar.offsetMax = Vector2.zero;
                Image image = barObject.GetComponent<Image>();
                image.color = _freshnessColor;
                image.raycastTarget = false;
                _freshnessBars[index] = bar;
            }

            return _freshnessBars[index];
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

                Image icon = GetIcon(iconIndex);
                _iconParcels[iconIndex++] = parcels[i];
                icon.color = parcels[i].Status == ParcelStatus.Loaded ? _loadedColor : Color.white;
                icon.gameObject.SetActive(true);
            }

            for (int i = iconIndex; i < _icons.Count; i++)
            {
                _icons[i].gameObject.SetActive(false);
                _iconParcels[i] = null;
            }
        }

        private Image GetIcon(int index)
        {
            while (_icons.Count <= index)
            {
                Image icon = Instantiate(_iconTemplate, _iconRoot);
                icon.gameObject.SetActive(false);
                _icons.Add(icon);
                _iconParcels.Add(null);
                _freshnessBars.Add(null);
            }

            return _icons[index];
        }

        private static bool IsRemaining(Parcel parcel)
        {
            // 폐기 물류의 아이콘은 처분 장소로 옮겨 간다(기획서 11.6)
            return parcel.Status != ParcelStatus.Delivered && parcel.Status != ParcelStatus.Failed && parcel.IsSpoiled == false;
        }
    }
}
