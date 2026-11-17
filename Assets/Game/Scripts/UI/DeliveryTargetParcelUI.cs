using System.Collections.Generic;

using UnityEngine;

using Game.Logistics;
using Game.UI.Hud;

namespace Game.UI
{
    /// <summary>배송지에 할당된 물류를 개별 아이콘으로 표시한다(WorldParcelIconsHud에 묶음 등록). 플레이어가 실은 물류는 테두리로 강조하고, 적재한 Fresh는 신선도 바를 붙인다.</summary>
    [RequireComponent(typeof(DeliveryTarget))]
    public class DeliveryTargetParcelUI : MonoBehaviour
    {
        [Tooltip("배송지 기준 아이콘 묶음 하단 위치(월드 m)")]
        [SerializeField] private Vector3 _iconOffset = new(0f, 1f, 0f); // TODO(tuning)

        private DeliveryTarget _target;
        private WorldParcelIconsHud.IconGroup _group;
        private readonly List<Parcel> _iconParcels = new();

        private void Awake()
        {
            _target = GetComponent<DeliveryTarget>();
        }

        // HUD의 Awake(Instance 설정)가 끝난 뒤 등록하려고 Start에서 한다
        private void Start()
        {
            WorldParcelIconsHud hud = WorldParcelIconsHud.Instance;
            if (hud == null)
            {
                Debug.LogError("WorldParcelIconsHud is missing in the scene");
                enabled = false;
                return;
            }

            _group = hud.AddGroup(transform, _iconOffset);
            _target.ParcelsChanged += OnParcelsChanged;
            RefreshIcons(_target.AllocatedParcels);
        }

        private void OnDestroy()
        {
            if (_target != null)
            {
                _target.ParcelsChanged -= OnParcelsChanged;
            }

            if (_group != null && WorldParcelIconsHud.Instance != null)
            {
                WorldParcelIconsHud.Instance.RemoveGroup(_group);
            }

            _group = null;
        }

        private void OnParcelsChanged(Parcel parcel)
        {
            RefreshIcons(_target.AllocatedParcels);
        }

        // 신선도는 게임 시간에 따라 줄어들므로 Fresh 아이콘은 매 프레임 갱신한다
        private void Update()
        {
            for (int i = 0; i < _iconParcels.Count; i++)
            {
                if (_iconParcels[i].IsFresh)
                {
                    ShowIcon(i, _iconParcels[i]);
                }
            }
        }

        private void RefreshIcons(IReadOnlyList<Parcel> parcels)
        {
            _iconParcels.Clear();
            for (int i = 0; i < parcels.Count; i++)
            {
                if (IsRemaining(parcels[i]))
                {
                    _iconParcels.Add(parcels[i]);
                    ShowIcon(_iconParcels.Count - 1, parcels[i]);
                }
            }

            _group.HideFrom(_iconParcels.Count);
        }

        private void ShowIcon(int index, Parcel parcel)
        {
            bool loaded = parcel.Status == ParcelStatus.Loaded;
            bool showBar = parcel.IsFresh && loaded && parcel.Deadline > 0f;
            _group.SetIcon(index, parcel.Type, false, loaded, showBar ? parcel.RemainingDeadline / parcel.Deadline : -1f);
        }

        private static bool IsRemaining(Parcel parcel)
        {
            // 폐기 물류의 아이콘은 처분 장소로 옮겨 간다(기획서 11.6)
            return parcel.Status != ParcelStatus.Delivered && parcel.Status != ParcelStatus.Failed && parcel.IsSpoiled == false;
        }
    }
}
