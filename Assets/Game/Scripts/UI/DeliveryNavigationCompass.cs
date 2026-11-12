using System.Collections.Generic;

using UnityEngine;

using TMPro;

using Game.Logistics;
using Game.Player.Interaction;

namespace Game.UI
{
    /// <summary>상단 나침반 패널에 적재 물류의 배송지별 방향 화살표와 물류센터 방향 화살표를 나란히 표시한다. 물류센터 화살표는 항상 표시한다.</summary>
    public class DeliveryNavigationCompass : MonoBehaviour
    {
        // TMP '>' 글리프는 기본이 오른쪽을 향하므로, 위쪽 기준 회전값에 90도를 더해 맞춘다
        private const float RightFacingGlyphOffset = 90f;

        [Header("References")]
        [SerializeField] private RectTransform _arrowTemplate;
        [SerializeField] private PlayerInteractionController _interactionController;
        [SerializeField] private Camera _camera;

        [Header("Layout")]
        [Tooltip("화살표 사이 간격(픽셀)")]
        [SerializeField, Min(0f)] private float _arrowSpacing = 48f;
        [SerializeField] private Color _logisticsCenterColor = new Color(0.549f, 0.778f, 0.961f);

        // 표시 중인 화살표. 화살표 하나가 목적지 하나를 담당한다.
        private readonly List<TMP_Text> _arrows = new();

        // 배송지별 화살표 대상. 남은 제한 시간이 짧은 순서다(기획서 9.3).
        private readonly List<DeliveryTarget> _targets = new();

        private LogisticsCenter _logisticsCenter;

        private void Awake()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_arrowTemplate == null)
            {
                Debug.LogError($"Navigation arrow template is missing in {gameObject.name}");
                enabled = false;
                return;
            }

            // 템플릿은 복제 원본으로만 쓰고 화면에는 표시하지 않는다
            _arrowTemplate.gameObject.SetActive(false);
        }

        private void Start()
        {
            // HUD는 씬 Canvas에 있으므로 플레이어와 물류센터는 씬에서 찾는다
            if (_interactionController == null)
            {
                _interactionController = FindAnyObjectByType<PlayerInteractionController>();
            }

            if (_interactionController == null)
            {
                Debug.LogError("PlayerInteractionController is missing in the scene");
            }

            _logisticsCenter = FindAnyObjectByType<LogisticsCenter>();
            if (_logisticsCenter == null)
            {
                Debug.LogError("LogisticsCenter is missing in the scene");
            }
        }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                return;
            }

            // 플레이어를 못 찾은 상태에서도 물류센터 화살표는 유지한다
            if (_interactionController != null)
            {
                CollectTargets();
            }

            int arrowCount = _targets.Count + (_logisticsCenter != null ? 1 : 0);
            EnsureArrowCount(arrowCount);

            if (arrowCount == 0)
            {
                return;
            }

            // 물류센터를 왼쪽 첫 칸에 고정하고, 배송지 화살표를 오른쪽으로 이어 붙인다
            int index = 0;
            if (_logisticsCenter != null)
            {
                PlaceArrow(index, _logisticsCenter.transform.position, arrowCount, _logisticsCenterColor);
                index++;
            }

            for (int i = 0; i < _targets.Count; i++)
            {
                PlaceArrow(index, _targets[i].transform.position, arrowCount, Color.white);
                index++;
            }
        }

        /// <summary>적재 물류의 배송지를 남은 제한 시간이 짧은 순서로 중복 없이 모은다.</summary>
        private void CollectTargets()
        {
            _targets.Clear();

            var loadedParcels = _interactionController.LoadedParcels;
            while (true)
            {
                Parcel next = null;
                for (int i = 0; i < loadedParcels.Count; i++)
                {
                    Parcel parcel = loadedParcels[i];
                    if (parcel == null || parcel.Target == null || _targets.Contains(parcel.Target))
                    {
                        continue;
                    }

                    if (next == null || parcel.RemainingDeadline < next.RemainingDeadline)
                    {
                        next = parcel;
                    }
                }

                if (next == null)
                {
                    return;
                }

                _targets.Add(next.Target);
            }
        }

        private void EnsureArrowCount(int count)
        {
            while (_arrows.Count < count)
            {
                _arrows.Add(Instantiate(_arrowTemplate.GetComponent<TMP_Text>(), _arrowTemplate.parent));
            }

            for (int i = 0; i < _arrows.Count; i++)
            {
                bool isUsed = i < count;
                if (_arrows[i].gameObject.activeSelf != isUsed)
                {
                    _arrows[i].gameObject.SetActive(isUsed);
                }
            }
        }

        private void PlaceArrow(int index, Vector3 destination, int arrowCount, Color color)
        {
            TMP_Text arrow = _arrows[index];
            arrow.color = color;

            // 화살표들을 패널 가운데 기준으로 균등 배치한다
            float x = (index - (arrowCount - 1) * 0.5f) * _arrowSpacing;
            arrow.rectTransform.anchoredPosition = new Vector2(x, 0f);
            arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, RightFacingGlyphOffset - GetBearing(destination));
        }

        /// <summary>카메라 정면을 0도, 오른쪽(시계 방향)을 양수로 하는 목적지 방위(도)를 구한다.</summary>
        private float GetBearing(Vector3 destination)
        {
            Vector3 toDestination = destination - _camera.transform.position;
            toDestination.y = 0f;

            Vector3 forward = _camera.transform.forward;
            forward.y = 0f;

            // 목적지가 카메라와 수직으로 겹치면 방위가 정의되지 않으므로 뒤쪽으로 본다
            if (toDestination.sqrMagnitude < 0.0001f || forward.sqrMagnitude < 0.0001f)
            {
                return 180f;
            }

            return Vector3.SignedAngle(forward, toDestination, Vector3.up);
        }
    }
}
