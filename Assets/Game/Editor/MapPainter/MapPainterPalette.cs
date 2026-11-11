using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace Game.EditorTools
{
    /// <summary>팔레트에 표시할 프리팹 하나. 아이콘과 크기는 처음 필요할 때 계산해 캐시한다.</summary>
    public sealed class MapPaletteEntry
    {
        public string DisplayName { get; }
        public string Category { get; }
        public GameObject Prefab { get; }

        private Texture2D _icon;
        private Vector3 _size;
        private bool _hasSize;

        public MapPaletteEntry(GameObject prefab, string category)
        {
            Prefab = prefab;
            DisplayName = prefab.name;
            Category = category;
        }

        public Texture2D Icon
        {
            get
            {
                if (_icon == null)
                {
                    _icon = AssetPreview.GetAssetPreview(Prefab);
                    if (_icon == null)
                    {
                        _icon = AssetPreview.GetMiniThumbnail(Prefab);
                    }
                }

                return _icon;
            }
        }

        /// <summary>프리팹의 실제 크기(m). 배치 미리보기 상자를 그릴 때 사용한다.</summary>
        public Vector3 Size
        {
            get
            {
                if (_hasSize == false)
                {
                    _size = MapPainterPalette.CalculateSize(Prefab);
                    _hasSize = true;
                }

                return _size;
            }
        }
    }

    /// <summary>지정한 폴더의 프리팹을 스캔해 하위 폴더를 카테고리로 묶는다. 아트 담당자는 프리팹을 폴더에 넣기만 하면 된다.</summary>
    public static class MapPainterPalette
    {
        // 폴더 이름을 그대로 보여 주면 영문이라 읽기 어려우므로 알려진 폴더만 한글로 바꾼다
        private static readonly Dictionary<string, string> CategoryLabels = new()
        {
            { "Roads", "도로" },
            { "Buildings", "건물" },
            { "Props", "소품" },
            { "Environment", "환경" },
            { "Other", "기타" },
        };

        public static List<MapPaletteEntry> Scan(string folder)
        {
            List<MapPaletteEntry> entries = new();

            if (AssetDatabase.IsValidFolder(folder) == false)
            {
                return entries;
            }

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    continue;
                }

                entries.Add(new MapPaletteEntry(prefab, GetCategory(folder, path)));
            }

            entries.Sort((a, b) =>
            {
                int order = CategoryOrder(a.Category).CompareTo(CategoryOrder(b.Category));
                if (order != 0)
                {
                    return order;
                }

                int category = string.CompareOrdinal(a.Category, b.Category);
                return category != 0 ? category : string.CompareOrdinal(a.DisplayName, b.DisplayName);
            });

            return entries;
        }

        /// <summary>그리는 순서대로 도로 → 건물 → 소품 → 환경 순으로 보여 준다.</summary>
        private static int CategoryOrder(string category)
        {
            return category switch
            {
                "Roads" => 0,
                "Buildings" => 1,
                "Props" => 2,
                "Environment" => 3,
                _ => 4,
            };
        }

        public static string GetCategoryLabel(string category)
        {
            return CategoryLabels.TryGetValue(category, out string label) ? label : category;
        }

        /// <summary>프리팹을 잠깐 만들어 렌더러 전체 크기를 잰다. 복잡한 프리팹은 한 번만 계산된다.</summary>
        public static Vector3 CalculateSize(GameObject prefab)
        {
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0)
                {
                    return Vector3.one;
                }

                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }

                return bounds.size;
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static string GetCategory(string rootFolder, string assetPath)
        {
            string relative = assetPath.Substring(rootFolder.Length).TrimStart('/');
            int slash = relative.IndexOf('/');
            return slash > 0 ? relative.Substring(0, slash) : "Other";
        }
    }
}
