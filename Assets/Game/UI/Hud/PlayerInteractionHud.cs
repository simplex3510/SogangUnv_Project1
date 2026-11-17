using UnityEngine;
using UnityEngine.UIElements;

using Game.Interaction;
using Game.Player.Interaction;
using Game.Player.Recovery;

namespace Game.UI.Hud
{
    /// <summary>
    /// 플레이어 좌우에 붙는 스크린 공간 키 안내(Story 022). 오른쪽은 상호작용(Space + 종류 아이콘), 왼쪽은 복구(R).
    /// 진행도는 패널 안이 아래에서 위로 차는 것으로 보인다. 표시만 하고 상태는 바꾸지 않는다.
    /// </summary>
    [RequireComponent(typeof(PanelRenderer))]
    public class PlayerInteractionHud : MonoBehaviour
    {
        private const string HiddenClass = "key-panel--hidden";
        private const string LoadIconClass = "interaction-icon--load";
        private const string UnloadIconClass = "interaction-icon--unload";
        private const string DisposeIconClass = "interaction-icon--dispose";
        private const string ObstacleIconClass = "interaction-icon--obstacle";

        [Tooltip("월드→화면 변환 카메라. 비우면 Camera.main")]
        [SerializeField] private Camera _camera;
        [Tooltip("플레이어 화면 위치에서 상호작용 패널 왼쪽 가장자리까지의 거리(패널 px)")]
        [SerializeField] private float _rightOffset = 70f; // TODO(tuning)
        [Tooltip("플레이어 화면 위치에서 복구 패널 오른쪽 가장자리까지의 거리(패널 px)")]
        [SerializeField] private float _leftOffset = 70f; // TODO(tuning)

        private PlayerInteractionController _interaction;
        private VehicleRecoveryController _recovery;
        private VisualElement _recoveryPanel;
        private VisualElement _recoveryFill;
        private VisualElement _interactionPanel;
        private VisualElement _interactionFill;
        private VisualElement _interactionIcon;

        private InteractionTypes _type;
        private bool _interactionVisible;
        private bool _recoveryVisible;
        private string _shownIconClass;

        private void OnEnable()
        {
            GetComponent<PanelRenderer>().RegisterUIReloadCallback(OnUIReload);
        }

        private void OnDisable()
        {
            GetComponent<PanelRenderer>().UnregisterUIReloadCallback(OnUIReload);
            _recoveryPanel = null;
        }

        // PanelRenderer는 트리를 만들거나 다시 불러올 때마다 루트를 넘겨준다
        private void OnUIReload(PanelRenderer _, VisualElement root, int version)
        {
            _recoveryPanel = root.Q("recoveryPanel");
            _recoveryFill = root.Q("recoveryFill");
            _interactionPanel = root.Q("interactionPanel");
            _interactionFill = root.Q("interactionFill");
            _interactionIcon = root.Q("interactionIcon");

            if (_recoveryPanel == null || _recoveryFill == null || _interactionPanel == null
                || _interactionFill == null || _interactionIcon == null)
            {
                Debug.LogError($"PlayerInteractionHud UXML elements are missing in {gameObject.name}");
                enabled = false;
                return;
            }

            // UI를 다시 불러오면 표시 상태를 현재 값으로 되돌린다
            _shownIconClass = null;
            _interactionPanel.EnableInClassList(HiddenClass, _interactionVisible == false);
            _recoveryPanel.EnableInClassList(HiddenClass, _recoveryVisible == false);
        }

        private void Start()
        {
            // HUD는 별도 Hud 프리팹이므로 플레이어는 씬에서 찾는다
            _interaction = FindAnyObjectByType<PlayerInteractionController>();
            _recovery = FindAnyObjectByType<VehicleRecoveryController>();
            if (_interaction == null || _recovery == null)
            {
                Debug.LogError("PlayerInteractionHud: PlayerInteractionController or VehicleRecoveryController is missing in the scene");
                enabled = false;
                return;
            }

            if (_camera == null)
            {
                _camera = Camera.main;
            }

            _interaction.InteractionContextChanged += OnInteractionContextChanged;
            _interaction.InteractionProgressChanged += OnInteractionProgressChanged;
            _recovery.RecoveryProgressChanged += OnRecoveryProgressChanged;
            _recovery.KeyIconStateChanged += OnRecoveryStateChanged;
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Unsubscribe()
        {
            if (_interaction != null)
            {
                _interaction.InteractionContextChanged -= OnInteractionContextChanged;
                _interaction.InteractionProgressChanged -= OnInteractionProgressChanged;
            }

            if (_recovery != null)
            {
                _recovery.RecoveryProgressChanged -= OnRecoveryProgressChanged;
                _recovery.KeyIconStateChanged -= OnRecoveryStateChanged;
            }
        }

        private void OnInteractionContextChanged(bool canInteract, InteractionTypes type)
        {
            _interactionVisible = canInteract && type != InteractionTypes.None;
            _type = type;
            _interactionPanel?.EnableInClassList(HiddenClass, _interactionVisible == false);
            if (_interactionVisible == false)
            {
                SetFill(_interactionFill, 0f);
            }
        }

        private void OnInteractionProgressChanged(float progress, bool isActive)
        {
            SetFill(_interactionFill, isActive ? progress : 0f);
        }

        private void OnRecoveryProgressChanged(float progress, bool isActive)
        {
            SetFill(_recoveryFill, isActive ? progress : 0f);
        }

        // 잠금 중에는 visible이 false로 온다
        private void OnRecoveryStateChanged(bool visible, bool locked)
        {
            _recoveryVisible = visible;
            _recoveryPanel?.EnableInClassList(HiddenClass, visible == false);
        }

        private static void SetFill(VisualElement fill, float progress)
        {
            if (fill != null)
            {
                fill.style.height = Length.Percent(Mathf.Clamp01(progress) * 100f);
            }
        }

        private void LateUpdate()
        {
            if (_recoveryPanel == null || _interaction == null || _camera == null)
            {
                return;
            }

            // 같은 Unload 종류라도 대상이 처분 장소로 바뀌면 이벤트가 없으므로 아이콘은 매 프레임 확인한다
            if (_interactionVisible)
            {
                SetIcon(GetIconClass());
            }

            Vector2 position = RuntimePanelUtils.CameraTransformWorldToPanel(
                _recoveryPanel.panel, _interaction.transform.position, _camera);
            _interactionPanel.style.left = position.x + _rightOffset;
            _interactionPanel.style.top = position.y;
            _recoveryPanel.style.left = position.x - _leftOffset;
            _recoveryPanel.style.top = position.y;
        }

        private string GetIconClass()
        {
            return _type switch
            {
                InteractionTypes.Load => LoadIconClass,
                InteractionTypes.Obstacle => ObstacleIconClass,
                _ => _interaction.IsTargetDisposalSite ? DisposeIconClass : UnloadIconClass,
            };
        }

        private void SetIcon(string iconClass)
        {
            if (iconClass == _shownIconClass)
            {
                return;
            }

            if (_shownIconClass != null)
            {
                _interactionIcon.RemoveFromClassList(_shownIconClass);
            }

            _interactionIcon.AddToClassList(iconClass);
            _shownIconClass = iconClass;
        }
    }
}
