using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UIElements;

using Game.Manager;
using Game.Systems;

namespace Game.UI
{
    /// <summary>시작 화면. 메뉴 카메라 구도와 사선 메뉴를 보여 주고, 시작을 누르면 인게임 카메라로 돌아간 뒤 하루를 시작한다.</summary>
    [RequireComponent(typeof(PanelRenderer))]
    public class StartMenuUI : MonoBehaviour
    {
        private enum State { Menu, Starting, Playing }

        private const string HiddenClass = "start-menu--hidden";
        private const string RowClass = "start-menu__button";
        private const string KeyboardClass = "start-menu--keyboard";

        [SerializeField] private StartScreenConfig _config;
        [SerializeField] private CinemachineCamera _menuCamera;
        [SerializeField] private CinemachineBrain _brain;
        [Tooltip("메뉴 동안 숨길 HUD 패널")]
        [SerializeField] private PanelRenderer[] _hudPanels;

        /// <summary>조작법 버튼을 눌렀을 때. Story 029에서 조작법 화면이 구독한다.</summary>
#pragma warning disable CS0067 // Story 029 전까지 발생시키지 않는 연결 지점
        public event Action ControlsRequested;
#pragma warning restore CS0067

        private readonly Dictionary<PanelRenderer, VisualElement> _hudRoots = new();
        private State _state = State.Menu;
        private VisualElement _menu;

        private void Awake()
        {
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
        }

        private void OnDisable()
        {
            GetComponent<PanelRenderer>().UnregisterUIReloadCallback(OnUIReload);
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
            Button startButton = root.Q<Button>("startButton");
            Button controlsButton = root.Q<Button>("controlsButton");
            Button quitButton = root.Q<Button>("quitButton");

            if (_menu == null || startButton == null || controlsButton == null || quitButton == null)
            {
                Debug.LogError($"StartMenuUI UXML elements are missing in {gameObject.name}");
                enabled = false;
                return;
            }

            _menu.EnableInClassList(HiddenClass, _state != State.Menu);
            root.Q("menuArea").style.width = Length.Percent(_config.MenuWidthPercent);

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
            // shortcut: 조작법 화면은 Story 029. 그때 활성화하고 ControlsRequested를 발생시킨다
            controlsButton.SetEnabled(false);

            if (_state == State.Menu)
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
