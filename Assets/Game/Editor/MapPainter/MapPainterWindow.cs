using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

using UnityEditor;
using UnityEditor.SceneManagement;

namespace Game.EditorTools
{
    /// <summary>아트 담당자가 씬 뷰에서 클릭으로 맵 조각을 배치하는 도구. 팔레트는 폴더를 스캔해 자동으로 채운다.</summary>
    public class MapPainterWindow : EditorWindow
    {
        private enum PaintMode
        {
            Place,
            Erase,
        }

        private const float PaletteButtonSize = 92f;
        private const int PaletteButtonsPerRow = 3;

        [SerializeField] private string _folderPath = "Assets/Game/Prefabs/Map";
        [SerializeField] private string _rootName = "Map";
        [SerializeField] private float _cellSize = 3f;
        [SerializeField] private float _paintHeight;
        [SerializeField] private bool _avoidOverlap = true;
        [SerializeField] private bool _isPaintEnabled;
        [SerializeField] private PaintMode _mode = PaintMode.Place;
        [SerializeField] private int _rotation;

        private readonly Dictionary<string, bool> _categoryFoldouts = new();
        private List<MapPaletteEntry> _entries = new();
        private int _selectedIndex = -1;
        private bool _strokeStarted;
        private Vector3? _lastPaintedCell;
        private Vector2 _paletteScroll;
        private GUIStyle _titleStyle;
        private GUIStyle _paletteButtonStyle;

        [MenuItem("Tools/Over?Load!/맵 페인터")]
        private static void OpenWindow()
        {
            MapPainterWindow window = GetWindow<MapPainterWindow>("맵 페인터");
            window.minSize = new Vector2(340f, 520f);
            window.Show();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            RefreshPalette();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnGUI()
        {
            InitStyles();

            EditorGUILayout.Space(4f);
            GUILayout.Label("맵 페인터", _titleStyle);
            EditorGUILayout.HelpBox(
                "① 아래 팔레트에서 놓을 조각을 고릅니다.\n"
                + "② 아래의 [맵에 그리기]를 켜고 씬 뷰에서 칸을 클릭합니다.\n"
                + "③ 잘못 놓았으면 [지우기] 모드로 클릭하거나 Ctrl+Z를 누릅니다.\n"
                + "씬 뷰 카메라 이동은 그대로 Alt+드래그로 하시면 됩니다.",
                MessageType.Info);

            DrawMainToolbar();
            DrawStatusBar();
            EditorGUILayout.Space(4f);
            DrawPlacementSettings();
            EditorGUILayout.Space(4f);
            DrawPaletteSource();
            EditorGUILayout.Space(4f);
            DrawPalette();
        }

        private void DrawMainToolbar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                Color previous = GUI.backgroundColor;

                GUI.backgroundColor = _isPaintEnabled ? new Color(0.55f, 0.90f, 0.55f) : Color.white;
                if (GUILayout.Button(new GUIContent(
                        _isPaintEnabled ? "맵에 그리기: 켜짐" : "맵에 그리기: 꺼짐",
                        "켜면 씬 뷰 클릭이 배치로 동작합니다. 끄면 평소처럼 오브젝트를 선택할 수 있습니다."),
                    GUILayout.Height(30f)))
                {
                    _isPaintEnabled = _isPaintEnabled == false;
                }

                GUI.backgroundColor = previous;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.backgroundColor = _mode == PaintMode.Place ? new Color(0.70f, 0.85f, 1f) : Color.white;
                if (GUILayout.Button(new GUIContent("놓기", "고른 조각을 클릭한 칸에 놓습니다."), GUILayout.Height(26f)))
                {
                    _mode = PaintMode.Place;
                }

                GUI.backgroundColor = _mode == PaintMode.Erase ? new Color(1f, 0.75f, 0.65f) : Color.white;
                if (GUILayout.Button(new GUIContent("지우기", "클릭한 칸에 놓인 조각을 지웁니다."), GUILayout.Height(26f)))
                {
                    _mode = PaintMode.Erase;
                }

                GUI.backgroundColor = Color.white;

                if (GUILayout.Button(new GUIContent("회전", "조각을 90도 회전합니다. 씬 뷰에서 R 키를 눌러도 됩니다."),
                        GUILayout.Height(26f), GUILayout.Width(64f)))
                {
                    RotateClockwise();
                }

                GUILayout.Label($"{_rotation}°", GUILayout.Width(40f));
            }

            if (GUILayout.Button(new GUIContent(
                    "탑뷰로 보기",
                    "씬 뷰를 위에서 내려다보는 시점으로 바꿉니다. 배치하기 가장 편한 시점입니다.")))
            {
                FocusTopView();
            }
        }

        private void DrawPlacementSettings()
        {
            EditorGUILayout.LabelField("배치 설정", EditorStyles.boldLabel);

            _cellSize = EditorGUILayout.FloatField(new GUIContent(
                "칸 크기(m)", "격자 한 칸의 크기입니다. 기본 3m는 현재 맵 도로 폭과 같습니다."), _cellSize);
            _cellSize = Mathf.Max(0.1f, _cellSize);

            _paintHeight = EditorGUILayout.FloatField(new GUIContent(
                "놓는 높이(Y)", "조각을 놓을 높이입니다. 보통 0으로 두시면 됩니다."), _paintHeight);

            _rootName = EditorGUILayout.TextField(new GUIContent(
                "묶을 오브젝트 이름", "배치한 조각을 모아 둘 씬 최상위 오브젝트 이름입니다. 없으면 자동으로 만듭니다."), _rootName);

            _avoidOverlap = EditorGUILayout.Toggle(new GUIContent(
                "이미 놓인 칸은 건너뛰기", "켜면 같은 칸에 조각을 겹쳐 놓지 않습니다."), _avoidOverlap);
        }

        private void DrawPaletteSource()
        {
            EditorGUILayout.LabelField("팔레트 폴더", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                _folderPath = EditorGUILayout.TextField(new GUIContent(
                        string.Empty,
                        "프리팹이 들어 있는 폴더입니다. 하위 폴더 이름이 팔레트의 분류가 됩니다."),
                    _folderPath);

                if (GUILayout.Button(new GUIContent("새로고침", "폴더에 프리팹을 추가한 뒤 눌러 주세요."), GUILayout.Width(80f)))
                {
                    RefreshPalette();
                }
            }

            if (AssetDatabase.IsValidFolder(_folderPath) == false)
            {
                EditorGUILayout.HelpBox(
                    $"폴더를 찾을 수 없습니다: {_folderPath}\n"
                    + "프리팹을 넣을 폴더를 직접 만들어 경로를 입력해 주세요.",
                    MessageType.Warning);
            }
        }

        private void DrawPalette()
        {
            EditorGUILayout.LabelField("팔레트", EditorStyles.boldLabel);

            if (_entries.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "팔레트가 비어 있습니다.\n"
                    + "· 위 폴더에 맵 프리팹을 넣고 [새로고침]을 눌러 주세요.\n"
                    + "· 폴더 안에 [도로], [건물], [소품]처럼 하위 폴더를 만들면 분류되어 보입니다.",
                    MessageType.Info);
                return;
            }

            _paletteScroll = EditorGUILayout.BeginScrollView(
                _paletteScroll,
                GUILayout.MinHeight(180f),
                GUILayout.ExpandHeight(true));

            string currentCategory = null;
            int column = 0;
            bool isRowOpen = false;

            for (int i = 0; i < _entries.Count; i++)
            {
                MapPaletteEntry entry = _entries[i];

                if (entry.Category != currentCategory)
                {
                    CloseRow(ref isRowOpen);
                    currentCategory = entry.Category;
                    column = 0;

                    if (_categoryFoldouts.ContainsKey(currentCategory) == false)
                    {
                        _categoryFoldouts[currentCategory] = true;
                    }

                    _categoryFoldouts[currentCategory] = EditorGUILayout.Foldout(
                        _categoryFoldouts[currentCategory],
                        MapPainterPalette.GetCategoryLabel(currentCategory),
                        true);

                    if (_categoryFoldouts[currentCategory] == false)
                    {
                        continue;
                    }
                }

                if (_categoryFoldouts.TryGetValue(entry.Category, out bool isOpen) && isOpen == false)
                {
                    continue;
                }

                if (column == 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    isRowOpen = true;
                }

                DrawPaletteButton(i, entry);

                column++;
                if (column >= PaletteButtonsPerRow)
                {
                    CloseRow(ref isRowOpen);
                    column = 0;
                }
            }

            CloseRow(ref isRowOpen);
            EditorGUILayout.EndScrollView();
        }

        private void DrawPaletteButton(int index, MapPaletteEntry entry)
        {
            Vector3 size = entry.Size;
            GUIContent content = new($"{entry.DisplayName}", entry.Icon,
                $"{entry.DisplayName}\n크기: {size.x:0.0}m × {size.z:0.0}m\n클릭해서 이 조각을 고릅니다.");

            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = index == _selectedIndex ? new Color(0.60f, 0.90f, 0.60f) : Color.white;

            if (GUILayout.Button(content, _paletteButtonStyle,
                    GUILayout.Width(PaletteButtonSize), GUILayout.Height(PaletteButtonSize)))
            {
                _selectedIndex = index;
                _mode = PaintMode.Place;
                Repaint();
            }

            GUI.backgroundColor = previous;
        }

        private void DrawStatusBar()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            string selected = _selectedIndex >= 0 && _selectedIndex < _entries.Count
                ? _entries[_selectedIndex].DisplayName
                : "없음";
            string mode = _mode == PaintMode.Place ? "놓기" : "지우기";
            string paint = _isPaintEnabled ? "켜짐" : "꺼짐";

            EditorGUILayout.LabelField($"고른 조각: {selected}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"모드: {mode} · 회전: {_rotation}° · 그리기: {paint}", EditorStyles.miniLabel);

            if (_isPaintEnabled)
            {
                EditorGUILayout.LabelField("씬 뷰에서 Esc를 누르면 그리기가 꺼집니다.", EditorStyles.miniBoldLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (_isPaintEnabled == false)
            {
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                DrawSceneHint("플레이 중에는 맵 페인터를 사용할 수 없습니다.");
                return;
            }

            Event current = Event.current;
            if (current == null)
            {
                return;
            }

            // Alt 조합은 씬 뷰 카메라 조작이므로 건드리지 않는다
            if (current.alt)
            {
                return;
            }

            if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape)
            {
                _isPaintEnabled = false;
                Repaint();
                current.Use();
                return;
            }

            if (current.type == EventType.KeyDown && current.keyCode == KeyCode.R)
            {
                RotateClockwise();
                current.Use();
                return;
            }

            if (TryGetCell(current, out Vector3 cell) == false)
            {
                return;
            }

            DrawSceneHint(_mode == PaintMode.Place
                ? "맵 페인터: 놓기 모드 (Esc로 끄기, R로 회전)"
                : "맵 페인터: 지우기 모드 (Esc로 끄기)");
            DrawOverlay(cell);

            if (current.type == EventType.MouseDown && current.button == 0)
            {
                BeginStroke();
                PaintCell(cell);
                current.Use();
            }
            else if (current.type == EventType.MouseDrag && current.button == 0)
            {
                if (_lastPaintedCell.HasValue == false || _lastPaintedCell.Value != cell)
                {
                    PaintCell(cell);
                }

                current.Use();
            }
            else if (current.type == EventType.MouseUp && current.button == 0)
            {
                EndStroke();
                current.Use();
            }
        }

        private bool TryGetCell(Event current, out Vector3 cell)
        {
            cell = Vector3.zero;

            Ray ray = HandleUtility.GUIPointToWorldRay(current.mousePosition);
            Plane plane = new(Vector3.up, new Vector3(0f, _paintHeight, 0f));
            if (plane.Raycast(ray, out float distance) == false)
            {
                return false;
            }

            Vector3 world = ray.GetPoint(distance);
            cell = new Vector3(
                Mathf.Round(world.x / _cellSize) * _cellSize,
                _paintHeight,
                Mathf.Round(world.z / _cellSize) * _cellSize);
            return true;
        }

        private void DrawOverlay(Vector3 cell)
        {
            bool isErase = _mode == PaintMode.Erase;
            Handles.color = isErase ? new Color(1f, 0.55f, 0.45f, 0.85f) : new Color(0.35f, 0.85f, 0.40f, 0.85f);

            // 커서 주변 격자
            Handles.color = new Color(1f, 1f, 1f, 0.12f);
            for (int i = -3; i <= 3; i++)
            {
                float offset = i * _cellSize;
                Handles.DrawLine(
                    new Vector3(cell.x + offset, cell.y + 0.01f, cell.z - 3f * _cellSize),
                    new Vector3(cell.x + offset, cell.y + 0.01f, cell.z + 3f * _cellSize));
                Handles.DrawLine(
                    new Vector3(cell.x - 3f * _cellSize, cell.y + 0.01f, cell.z + offset),
                    new Vector3(cell.x + 3f * _cellSize, cell.y + 0.01f, cell.z + offset));
            }

            Handles.color = isErase ? new Color(1f, 0.55f, 0.45f, 0.9f) : new Color(0.35f, 0.85f, 0.40f, 0.9f);
            Vector3 cellCenter = cell + Vector3.up * 0.02f;
            Handles.DrawWireCube(cellCenter, new Vector3(_cellSize, 0.02f, _cellSize));

            if (isErase)
            {
                GameObject existing = FindObjectAt(cell);
                if (existing != null)
                {
                    Handles.Label(cell + Vector3.up * 1.5f, $"지울 조각: {existing.name}");
                }
                else
                {
                    Handles.Label(cell + Vector3.up * 1.5f, "이 칸에는 지울 조각이 없습니다.");
                }

                return;
            }

            if (_selectedIndex < 0 || _selectedIndex >= _entries.Count)
            {
                Handles.Label(cell + Vector3.up * 1.5f, "팔레트에서 조각을 먼저 고르세요.");
                return;
            }

            MapPaletteEntry entry = _entries[_selectedIndex];
            Vector3 size = entry.Size;
            bool isQuarterTurn = _rotation % 180 != 0;
            float footprintX = isQuarterTurn ? size.z : size.x;
            float footprintZ = isQuarterTurn ? size.x : size.z;

            Handles.DrawWireCube(
                cell + Vector3.up * (size.y * 0.5f),
                new Vector3(footprintX, size.y, footprintZ));
            Handles.Label(
                cell + Vector3.up * (size.y + 0.6f),
                $"{entry.DisplayName}  ({footprintX:0.0}m × {footprintZ:0.0}m)");
        }

        private void DrawSceneHint(string message)
        {
            Handles.BeginGUI();
            GUIStyle style = new(EditorStyles.helpBox)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
            };
            GUILayout.BeginArea(new Rect(12f, 12f, 420f, 30f), style);
            GUILayout.Label(message);
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        private void PaintCell(Vector3 cell)
        {
            if (_mode == PaintMode.Erase)
            {
                EraseAt(cell);
                _lastPaintedCell = cell;
                return;
            }

            if (_selectedIndex < 0 || _selectedIndex >= _entries.Count)
            {
                return;
            }

            GameObject root = GetOrCreateRoot();
            if (root == null)
            {
                return;
            }

            if (_avoidOverlap && FindObjectAt(cell) != null)
            {
                _lastPaintedCell = cell;
                return;
            }

            MapPaletteEntry entry = _entries[_selectedIndex];
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(entry.Prefab);
            instance.transform.SetParent(root.transform, true);
            instance.transform.position = cell;
            instance.transform.rotation = Quaternion.Euler(0f, _rotation, 0f);

            Undo.RegisterCreatedObjectUndo(instance, "맵 조각 배치");
            EditorSceneManager.MarkSceneDirty(instance.scene);

            _lastPaintedCell = cell;
            Repaint();
        }

        private void EraseAt(Vector3 cell)
        {
            GameObject existing = FindObjectAt(cell);
            if (existing == null)
            {
                _lastPaintedCell = cell;
                return;
            }

            Undo.DestroyObjectImmediate(existing);
            _lastPaintedCell = cell;
            Repaint();
        }

        private GameObject FindObjectAt(Vector3 cell)
        {
            GameObject root = FindRoot();
            if (root == null)
            {
                return null;
            }

            float half = _cellSize * 0.45f;
            foreach (Transform child in root.transform)
            {
                Vector3 position = child.position;
                if (Mathf.Abs(position.x - cell.x) <= half && Mathf.Abs(position.z - cell.z) <= half)
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        private GameObject FindRoot()
        {
            Scene scene = SceneManager.GetActiveScene();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == _rootName)
                {
                    return root;
                }
            }

            return null;
        }

        private GameObject GetOrCreateRoot()
        {
            GameObject root = FindRoot();
            if (root != null)
            {
                return root;
            }

            if (string.IsNullOrWhiteSpace(_rootName))
            {
                return null;
            }

            root = new GameObject(_rootName);
            Undo.RegisterCreatedObjectUndo(root, "맵 루트 생성");
            return root;
        }

        private void BeginStroke()
        {
            Undo.IncrementCurrentGroup();
            _strokeStarted = true;
            _lastPaintedCell = null;
        }

        private void EndStroke()
        {
            if (_strokeStarted)
            {
                Undo.SetCurrentGroupName("맵 조각 배치");
            }

            _strokeStarted = false;
            _lastPaintedCell = null;
        }

        private void RotateClockwise()
        {
            _rotation = (_rotation + 90) % 360;
            Repaint();
        }

        private void FocusTopView()
        {
            SceneView view = SceneView.lastActiveSceneView;
            if (view == null)
            {
                return;
            }

            view.orthographic = true;
            view.LookAt(Vector3.zero, Quaternion.Euler(90f, 0f, 0f), 60f);
            view.Repaint();
        }

        private void RefreshPalette()
        {
            _entries = MapPainterPalette.Scan(_folderPath);
            _selectedIndex = _entries.Count > 0 ? Mathf.Clamp(_selectedIndex, 0, _entries.Count - 1) : -1;
            Repaint();
        }

        private void InitStyles()
        {
            _titleStyle ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 18 };

            _paletteButtonStyle ??= new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleCenter,
                imagePosition = ImagePosition.ImageAbove,
                wordWrap = true,
                fontSize = 10,
                fixedWidth = PaletteButtonSize,
                fixedHeight = PaletteButtonSize,
            };
        }

        private void CloseRow(ref bool isRowOpen)
        {
            if (isRowOpen)
            {
                EditorGUILayout.EndHorizontal();
                isRowOpen = false;
            }
        }
    }
}
