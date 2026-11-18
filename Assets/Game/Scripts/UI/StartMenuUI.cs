using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UIElements;

using Game.Manager;
using Game.Player.Movement;
using Game.Systems;

namespace Game.UI
{
    /// <summary>
    /// 시작 화면. 메뉴 카메라 구도와 사선 메뉴를 보여 주고, 시작을 누르면 인게임 카메라로 돌아간 뒤 하루를 시작한다.
    /// 게임 설명 패널과 Esc 퇴근 확인창도 함께 맡는다(design/quick-specs/guide-panel-addition-2026-11-18.md).
    /// </summary>
    [RequireComponent(typeof(PanelRenderer))]
    public class StartMenuUI : MonoBehaviour
    {
        private enum State { Menu, Starting, Playing }

        private const string HiddenClass = "start-menu--hidden";
        private const string AreaHiddenClass = "start-menu__area--hidden";
        private const string GuideHiddenClass = "guide-area--hidden";
        private const string ConfirmHiddenClass = "quit-confirm--hidden";
        private const string RowClass = "start-menu__button";
        private const string KeyboardClass = "start-menu--keyboard";

        // Esc 처리 순서: 확인창 → 게임 설명 → (물류센터 창은 100) → 확인창 열기
        private const int EscapeConfirmPriority = 300;
        private const int EscapeGuidePriority = 200;
        private const int EscapeOpenConfirmPriority = 0;

        private const float GuideArrowScrollPixels = 90f;
        private const float GuidePageScrollRatio = 0.8f;

        [SerializeField] private StartScreenConfig _config;
        [SerializeField] private CinemachineCamera _menuCamera;
        [SerializeField] private CinemachineBrain _brain;
        [Tooltip("메뉴 동안 숨길 HUD 패널")]
        [SerializeField] private PanelRenderer[] _hudPanels;

        private readonly Dictionary<PanelRenderer, VisualElement> _hudRoots = new();
        private State _state = State.Menu;
        private bool _isGuideOpen;
        private bool _isConfirmOpen;
        private VisualElement _menu;
        private VisualElement _menuArea;
        private VisualElement _guideArea;
        private ScrollView _guideScroll;
        private VisualElement _confirm;
        private Button _guideButton;
        private Button _guideCloseButton;
        private Button _quitNoButton;

        private void Awake()
        {
            // UI 씬이 맵 씬과 분리되면 인스펙터 참조가 끊기므로 차량에 붙은 메뉴 카메라를 찾는다
            if (_menuCamera == null)
            {
                _menuCamera = FindAnyObjectByType<PlayerMovementController>()?.GetComponentInChildren<CinemachineCamera>(true);
            }

            if (_brain == null)
            {
                _brain = FindAnyObjectByType<CinemachineBrain>();
            }

            if (_config == null || _menuCamera == null || _brain == null || _hudPanels == null)
            {
                Debug.LogError($"StartMenuUI references are missing in {gameObject.name}");
                enabled = false;
                return;
            }

            foreach (PanelRenderer hud in _hudPanels)
            {
                hud.RegisterUIReloadCallback(OnHudReload);
            }

            _menuCamera.enabled = true;
        }

        private void OnDestroy()
        {
            if (_hudPanels == null)
            {
                return;
            }

            foreach (PanelRenderer hud in _hudPanels)
            {
                if (hud != null)
                {
                    hud.UnregisterUIReloadCallback(OnHudReload);
                }
            }
        }

        private void OnEnable()
        {
            GetComponent<PanelRenderer>().RegisterUIReloadCallback(OnUIReload);
            GameManager.AddEscapeHandler(EscapeConfirmPriority, TryCloseConfirmOnEscape);
            GameManager.AddEscapeHandler(EscapeGuidePriority, TryCloseGuideOnEscape);
            GameManager.AddEscapeHandler(EscapeOpenConfirmPriority, OpenConfirmOnEscape);
        }

        private void OnDisable()
        {
            GetComponent<PanelRenderer>().UnregisterUIReloadCallback(OnUIReload);
            GameManager.RemoveEscapeHandler(TryCloseConfirmOnEscape);
            GameManager.RemoveEscapeHandler(TryCloseGuideOnEscape);
            GameManager.RemoveEscapeHandler(OpenConfirmOnEscape);
            _menu = null;
        }

        // HUD 트리가 다시 만들어져도 메뉴 동안은 숨김을 유지한다
        private void OnHudReload(PanelRenderer panel, VisualElement root, int version)
        {
            _hudRoots[panel] = root;
            root.style.display = _state == State.Playing ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnUIReload(PanelRenderer _, VisualElement root, int version)
        {
            _menu = root.Q("startMenu");
            _menuArea = root.Q("menuArea");
            _guideArea = root.Q("guideArea");
            _guideScroll = root.Q<ScrollView>("guideScroll");
            _confirm = root.Q("quitConfirm");
            Button startButton = root.Q<Button>("startButton");
            _guideButton = root.Q<Button>("guideButton");
            _guideCloseButton = root.Q<Button>("guideCloseButton");
            Button quitButton = root.Q<Button>("quitButton");
            Button quitYesButton = root.Q<Button>("quitYesButton");
            _quitNoButton = root.Q<Button>("quitNoButton");

            if (_menu == null || _menuArea == null || _guideArea == null || _guideScroll == null || _confirm == null
                || startButton == null || _guideButton == null || _guideCloseButton == null || quitButton == null
                || quitYesButton == null || _quitNoButton == null)
            {
                Debug.LogError($"StartMenuUI UXML elements are missing in {gameObject.name}");
                enabled = false;
                return;
            }

            _menu.EnableInClassList(HiddenClass, _state != State.Menu);
            _menuArea.style.width = Length.Percent(_config.MenuWidthPercent);
            _guideArea.style.width = Length.Percent(_config.MenuWidthPercent);
            ApplyGuide();
            _confirm.EnableInClassList(ConfirmHiddenClass, _isConfirmOpen == false);

            // 아래 행일수록 오른쪽으로 밀어 '\' 모양 사선을 만든다
            List<Button> rows = root.Query<Button>(className: RowClass).ToList();
            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].style.translate = new Translate(i * _config.RowOffset, 0f);
            }

            // 처음 포커스가 출근에 있어도 키보드로 움직이기 전에는 강조하지 않는다
            VisualElement menu = _menu;
            root.RegisterCallback<NavigationMoveEvent>(_ => menu.AddToClassList(KeyboardClass));
            root.RegisterCallback<PointerMoveEvent>(_ => menu.RemoveFromClassList(KeyboardClass));

            startButton.clicked += OnStartClicked;
            quitButton.clicked += GameManager.Instance.Quit;
            _guideButton.clicked += OpenGuide;
            _guideCloseButton.clicked += CloseGuide;
            quitYesButton.clicked += GameManager.Instance.Quit;
            _quitNoButton.clicked += CloseConfirm;

            // 게임 설명 안에서는 ↑/↓와 PageUp/PageDown으로 스크롤한다(포커스 이동 대신)
            _guideArea.RegisterCallback<NavigationMoveEvent>(OnGuideNavigation);
            _guideArea.RegisterCallback<KeyDownEvent>(OnGuideKeyDown);

            if (_isConfirmOpen)
            {
                _quitNoButton.Focus();
            }
            else if (_isGuideOpen)
            {
                _guideCloseButton.Focus();
            }
            else if (_state == State.Menu)
            {
                startButton.Focus();
            }
        }

        private void OnStartClicked()
        {
            if (_state != State.Menu)
            {
                return;
            }

            _state = State.Starting;
            _menu?.AddToClassList(HiddenClass);
            StartCoroutine(BlendToGameCamera());
        }

        private void OpenGuide()
        {
            if (_state != State.Menu || _isGuideOpen)
            {
                return;
            }

            _isGuideOpen = true;
            ApplyGuide();
            _guideScroll.scrollOffset = Vector2.zero;
            _guideCloseButton.Focus();
        }

        private void CloseGuide()
        {
            if (_isGuideOpen == false)
            {
                return;
            }

            _isGuideOpen = false;
            ApplyGuide();
            _guideButton.Focus();
        }

        // 게임 설명이 열리면 로고·버튼 영역은 통째로 숨겨 패널과 겹치지 않게 한다
        private void ApplyGuide()
        {
            _menuArea.EnableInClassList(AreaHiddenClass, _isGuideOpen);
            _guideArea.EnableInClassList(GuideHiddenClass, _isGuideOpen == false);
        }

        private void OnGuideNavigation(NavigationMoveEvent evt)
        {
            if (evt.direction != NavigationMoveEvent.Direction.Up && evt.direction != NavigationMoveEvent.Direction.Down)
            {
                return;
            }

            float sign = evt.direction == NavigationMoveEvent.Direction.Down ? 1f : -1f;
            _guideScroll.scrollOffset += new Vector2(0f, sign * GuideArrowScrollPixels);
            evt.StopPropagation();
        }

        private void OnGuideKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.PageUp && evt.keyCode != KeyCode.PageDown)
            {
                return;
            }

            float sign = evt.keyCode == KeyCode.PageDown ? 1f : -1f;
            float page = _guideScroll.contentViewport.resolvedStyle.height * GuidePageScrollRatio;
            _guideScroll.scrollOffset += new Vector2(0f, sign * page);
            evt.StopPropagation();
        }

        private bool TryCloseConfirmOnEscape()
        {
            if (_isConfirmOpen == false)
            {
                return false;
            }

            CloseConfirm();
            return true;
        }

        private bool TryCloseGuideOnEscape()
        {
            if (_isGuideOpen == false)
            {
                return false;
            }

            CloseGuide();
            return true;
        }

        // 열린 창이 없을 때의 Esc. 게임을 멈추고 퇴근 여부를 묻는다
        private bool OpenConfirmOnEscape()
        {
            if (_confirm == null)
            {
                return false;
            }

            _isConfirmOpen = true;
            _confirm.RemoveFromClassList(ConfirmHiddenClass);
            GameManager.Instance.SetPaused(true);
            _quitNoButton.Focus();
            return true;
        }

        private void CloseConfirm()
        {
            if (_isConfirmOpen == false)
            {
                return;
            }

            _isConfirmOpen = false;
            _confirm.AddToClassList(ConfirmHiddenClass);
            GameManager.Instance.SetPaused(false);
        }

        private IEnumerator BlendToGameCamera()
        {
            CinemachineBlendDefinition previousBlend = _brain.DefaultBlend;
            _brain.DefaultBlend = new CinemachineBlendDefinition(_config.BlendStyle, _config.BlendTime);
            _menuCamera.enabled = false;

            yield return new WaitForSeconds(_config.BlendTime);

            _brain.DefaultBlend = previousBlend;
            _state = State.Playing;

            foreach (VisualElement hudRoot in _hudRoots.Values)
            {
                hudRoot.style.display = DisplayStyle.Flex;
            }

            DailyOperationSystem.Instance.StartDay();
        }
    }
}
