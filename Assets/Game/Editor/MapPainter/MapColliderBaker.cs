using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Game.EditorTools
{
    /// <summary>
    /// 맵 타일의 충돌 메시를 하나로 합쳐 단일 MeshCollider로 만든다.
    /// 타일마다 별도 콜라이더를 두면 이음매에서 물리 충격이 생기므로, 접지면을 한 덩어리로 베이크한다.
    /// </summary>
    public static class MapColliderBaker
    {
        private const string MapRootName = "Map";
        private const string BakedObjectName = "MapCollision";
        private const string GroundObjectName = "MapGround";
        private const string MeshFolderPath = "Assets/Game/Map";
        private const string MeshAssetPath = "Assets/Game/Map/MapCollision.asset";
        private const int DrivableLayer = 7;

        // 0.1 mm 이내로 붙어 있는 정점은 같은 것으로 보고 용접한다
        private const float WeldPrecision = 0.0001f;

        // 도로 표면 높이. 평면은 BoxCollider가 담당하므로 메시에서는 이 높이의 삼각형을 제외한다.
        // TODO: 경사로·다리 타일이 추가되면 타일별 평면 파라미터로 확장해야 한다.
        private const float DrivableSurfaceHeight = 0.03f;
        private const float SurfaceTolerance = 0.002f;

        [MenuItem("Tools/Over?Load!/맵 콜라이더 베이크")]
        public static void Bake()
        {
            GameObject map = GameObject.Find(MapRootName);
            if (map == null)
            {
                Debug.LogError($"[맵 콜라이더 베이크] {MapRootName} 오브젝트를 찾을 수 없습니다.");
                return;
            }

            List<MeshCollider> sources = CollectSources(map);
            if (sources.Count == 0)
            {
                Debug.LogWarning("[맵 콜라이더 베이크] 합칠 드라이버블 콜라이더가 없습니다.");
                return;
            }

            List<Vector3> vertices;
            List<int> triangles;
            BuildGeometry(map.transform, sources, out vertices, out triangles);

            Mesh mesh = SaveMeshAsset(vertices, triangles);
            PhysicsMaterial material = sources[0].sharedMaterial;
            GetOrCreateBakedCollider(map, mesh, material);
            GetOrCreateGroundCollider(map, mesh.bounds, material);
            int disabledCount = DisableSourceColliders(sources);

            EditorSceneManager.MarkSceneDirty(map.scene);
            Debug.Log($"[맵 콜라이더 베이크] 타일 {sources.Count}개 → 메시 1개 " +
                      $"(정점 {vertices.Count}, 삼각형 {triangles.Count / 3}), 콜라이더 {disabledCount}개 비활성화");
        }

        private static List<MeshCollider> CollectSources(GameObject map)
        {
            List<MeshCollider> sources = new();

            foreach (MeshCollider collider in map.GetComponentsInChildren<MeshCollider>(true))
            {
                // 이전 베이크 결과와 드라이버블 외 레이어는 제외한다
                if (collider.gameObject.layer != DrivableLayer)
                {
                    continue;
                }

                if (collider.gameObject.name == BakedObjectName || collider.sharedMesh == null)
                {
                    continue;
                }

                sources.Add(collider);
            }

            return sources;
        }

        /// <summary>월드가 아닌 Map 기준 좌표로 합친다. Map이 이동해도 충돌이 함께 따라가게 하기 위함이다.</summary>
        private static void BuildGeometry(Transform mapTransform, List<MeshCollider> sources,
                                          out List<Vector3> vertices, out List<int> triangles)
        {
            vertices = new List<Vector3>();
            triangles = new List<int>();

            // 합치는 과정에서 겹치는 정점을 같은 인덱스로 모은다
            Dictionary<Vector3Int, int> weldMap = new();

            Matrix4x4 toMapSpace = mapTransform.worldToLocalMatrix;

            foreach (MeshCollider source in sources)
            {
                Mesh sourceMesh = source.sharedMesh;
                Matrix4x4 localToMap = toMapSpace * source.transform.localToWorldMatrix;

                Mesh.MeshDataArray dataArray = Mesh.AcquireReadOnlyMeshData(sourceMesh);
                try
                {
                    Mesh.MeshData data = dataArray[0];
                    int stride = data.GetVertexBufferStride(0);
                    if (stride % 12 != 0)
                    {
                        Debug.LogError($"[맵 콜라이더 베이크] 지원하지 않는 정점 레이아웃입니다: {sourceMesh.name} (stride {stride})");
                        continue;
                    }

                    // 스트림은 정점당 여러 Vector3(위치·노멀·탄젠트·UV)로 구성되며 위치가 첫 번째다
                    int vectorsPerVertex = stride / 12;
                    NativeArray<Vector3> stream = data.GetVertexData<Vector3>(0);

                    int[] remap = new int[data.vertexCount];
                    for (int i = 0; i < data.vertexCount; i++)
                    {
                        Vector3 position = localToMap.MultiplyPoint3x4(stream[i * vectorsPerVertex]);
                        Vector3Int key = new Vector3Int(
                            Mathf.RoundToInt(position.x / WeldPrecision),
                            Mathf.RoundToInt(position.y / WeldPrecision),
                            Mathf.RoundToInt(position.z / WeldPrecision));

                        if (weldMap.TryGetValue(key, out int existingIndex) == false)
                        {
                            existingIndex = vertices.Count;
                            weldMap.Add(key, existingIndex);
                            vertices.Add(position);
                        }

                        remap[i] = existingIndex;
                    }

                    if (sourceMesh.indexFormat == IndexFormat.UInt16)
                    {
                        NativeArray<ushort> indices = data.GetIndexData<ushort>();
                        for (int i = 0; i < indices.Length; i += 3)
                        {
                            AddTriangle(vertices, triangles,
                                        remap[indices[i]], remap[indices[i + 1]], remap[indices[i + 2]]);
                        }
                    }
                    else
                    {
                        NativeArray<uint> indices = data.GetIndexData<uint>();
                        for (int i = 0; i < indices.Length; i += 3)
                        {
                            AddTriangle(vertices, triangles,
                                        remap[indices[i]], remap[indices[i + 1]], remap[indices[i + 2]]);
                        }
                    }
                }
                finally
                {
                    dataArray.Dispose();
                }
            }
        }

        /// <summary>도로 표면 높이의 삼각형은 접지용 BoxCollider가 대신하므로 메시에서 뺀다. 메시 평면이 남아 있으면 물리 충격이 생긴다.</summary>
        private static void AddTriangle(List<Vector3> vertices, List<int> triangles, int a, int b, int c)
        {
            float limit = DrivableSurfaceHeight + SurfaceTolerance;
            if (vertices[a].y <= limit && vertices[b].y <= limit && vertices[c].y <= limit)
            {
                return;
            }

            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
        }

        private static Mesh SaveMeshAsset(List<Vector3> vertices, List<int> triangles)
        {
            EnsureFolder();

            Mesh asset = AssetDatabase.LoadAssetAtPath<Mesh>(MeshAssetPath);
            if (asset == null)
            {
                asset = new Mesh { name = BakedObjectName, indexFormat = IndexFormat.UInt32 };
                ApplyGeometry(asset, vertices, triangles);
                AssetDatabase.CreateAsset(asset, MeshAssetPath);
                return asset;
            }

            // 기존 에셋의 GUID를 유지해야 씬의 참조가 끊기지 않으므로 내용만 교체한다
            asset.Clear();
            asset.indexFormat = IndexFormat.UInt32;
            ApplyGeometry(asset, vertices, triangles);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static void ApplyGeometry(Mesh mesh, List<Vector3> vertices, List<int> triangles)
        {
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }

        private static void EnsureFolder()
        {
            if (AssetDatabase.IsValidFolder(MeshFolderPath))
            {
                return;
            }

            AssetDatabase.CreateFolder("Assets/Game", "Map");
        }

        private static void GetOrCreateBakedCollider(GameObject map, Mesh mesh, PhysicsMaterial material)
        {
            Transform existing = map.transform.Find(BakedObjectName);
            GameObject target = existing != null ? existing.gameObject : new GameObject(BakedObjectName);

            if (existing == null)
            {
                Undo.RegisterCreatedObjectUndo(target, "맵 콜라이더 베이크");
                target.transform.SetParent(map.transform, false);
            }

            target.layer = DrivableLayer;

            MeshCollider collider = target.GetComponent<MeshCollider>();
            if (collider == null)
            {
                collider = Undo.AddComponent<MeshCollider>(target);
            }

            collider.sharedMesh = mesh;
            collider.convex = false;
            collider.sharedMaterial = material;
            collider.enabled = true;
        }

        /// <summary>접지면은 삼각형 메시 대신 해석적 BoxCollider로 둔다. 메시 접촉은 타일 경계마다 수직 충격을 만든다.</summary>
        private static void GetOrCreateGroundCollider(GameObject map, Bounds meshBounds, PhysicsMaterial material)
        {
            Transform existing = map.transform.Find(GroundObjectName);
            GameObject target = existing != null ? existing.gameObject : new GameObject(GroundObjectName);

            if (existing == null)
            {
                Undo.RegisterCreatedObjectUndo(target, "맵 콜라이더 베이크");
                target.transform.SetParent(map.transform, false);
            }

            target.layer = DrivableLayer;
            target.transform.localPosition = Vector3.zero;
            target.transform.localRotation = Quaternion.identity;
            target.transform.localScale = Vector3.one;

            BoxCollider collider = target.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = Undo.AddComponent<BoxCollider>(target);
            }

            // 두께 1 m 박스의 윗면이 도로 표면 높이가 되도록 아래로 내린다
            collider.size = new Vector3(meshBounds.size.x, 1f, meshBounds.size.z);
            collider.center = new Vector3(meshBounds.center.x, DrivableSurfaceHeight - 0.5f, meshBounds.center.z);
            collider.sharedMaterial = material;
            collider.enabled = true;
        }

        private static int DisableSourceColliders(List<MeshCollider> sources)
        {
            int disabledCount = 0;
            foreach (MeshCollider source in sources)
            {
                if (source.enabled == false)
                {
                    continue;
                }

                Undo.RecordObject(source, "맵 콜라이더 베이크");
                source.enabled = false;
                disabledCount++;
            }

            return disabledCount;
        }
    }
}
