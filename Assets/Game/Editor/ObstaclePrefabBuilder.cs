using System.IO;

using UnityEditor;
using UnityEngine;

using Game.WorldObjects;

namespace Game.EditorTools
{
    /// <summary>Story 011: 카탈로그 방해물 프리팹과 효과 설정 SO를 만든다. 여러 번 실행해도 같은 결과가 나온다.</summary>
    public static class ObstaclePrefabBuilder
    {
        private const string PropsDir = "Assets/Game/Prefabs/Props/";
        private const string ConfigDir = "Assets/Game/Configs/Obstacles/";
        private const string MaterialPath = "Assets/Game/PhysicsMaterials/ObstacleNoBounce.asset";
        private const string Roads = "Assets/Resources/Roads/FBX/";
        private const string Vehicles = "Assets/Resources/Vehicles/FBX/";

        // 킷마다 원본 크기가 달라 배율도 다르다
        private const float RoadsScale = 1.875f;           // Roads 킷: 기존 Props 프리팹과 같은 배율
        private const float VehiclesScale = 0.25f;         // Vehicles 킷: 플레이어 차량(delivery)과 같은 배율
        private const float InteractionZonePadding = 0.4f; // 상호작용 구역이 모델 둘레로 넓어지는 거리(차량 길이 약 절반)
        private const float RoadworkInteractionSeconds = 3f;

        [MenuItem("Game/Build Obstacle Prefabs")]
        public static void Build()
        {
            PhysicsMaterial noBounce = GetPhysicsMaterial();
            Directory.CreateDirectory(ConfigDir);

            ObstacleEffectConfig cone = Config("ObstacleCone", 0.8f, 1f, 0f, false, 10f);
            ObstacleEffectConfig box = Config("ObstacleBox", 1f, 0f, 0f, false, 15f);
            ObstacleEffectConfig debrisS = Config("ObstacleDebrisS", 1f, 0f, 15f, true, 0f);
            ObstacleEffectConfig debrisL = Config("ObstacleDebrisL", 0.6f, 2f, 0f, false, 0f);

            // 기존 프리팹 수정
            EditExisting("Cone", 5f, noBounce, root =>
            {
                Mesh coneMesh = MeshOf(Vehicles + "cone.fbx", out Material mat);
                MeshFilter filter = root.GetComponentInChildren<MeshFilter>();
                filter.sharedMesh = coneMesh;
                filter.transform.localScale = Vector3.one * VehiclesScale;
                filter.GetComponent<MeshRenderer>().sharedMaterial = mat;
                filter.GetComponent<MeshCollider>().sharedMesh = coneMesh;
                AddTrigger<CollisionObstacleTrigger>(root);
                AddLifetime(root, ObstacleLifetime.LifetimeMode.RemoveAfterLinger, cone);
                Add<SpeedMultiplierEffect>(root, cone);
                Add<SwapMeshEffect>(root, null, "_swappedMesh", MeshOf(Vehicles + "cone-flat.fbx", out _));
            });
            EditExisting("Box", 15f, noBounce, root =>
            {
                root.transform.localScale = Vector3.one * VehiclesScale; // 메시·콜라이더가 루트에 있다
                AddTrigger<CollisionObstacleTrigger>(root);
                AddLifetime(root, ObstacleLifetime.LifetimeMode.RemoveAfterLinger, box);
            });
            EditExisting("Dumpster", 80f, noBounce, null);

            // 신규: 잔해(트리거 콜라이더, Rigidbody 없음)
            BuildDebris("DebrisS", Vehicles + "debris-bolt.fbx", debrisS, true);
            BuildDebris("DebrisL", Vehicles + "debris-tire.fbx", debrisL, false);

            BuildRoadwork(noBounce);

            // 신규: 고정 시설(효과 없음)과 머리 위 변형(콜라이더 없음)
            GameObject sign = NewModel("RoadSign", Roads + "road-sign-stop.fbx", RoadsScale, out _);
            AddSolid(sign.GetComponentInChildren<MeshFilter>().gameObject, noBounce);
            Save(sign, "RoadSign");
            GameObject overhead = NewModel("OverheadWires", Roads + "electricity-wires.fbx", RoadsScale, out _);
            Save(overhead, "OverheadWires");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void BuildDebris(string name, string fbx, ObstacleEffectConfig config, bool heading)
        {
            GameObject root = NewModel(name, fbx, VehiclesScale, out Mesh mesh);
            BoxCollider zone = root.AddComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.center = mesh.bounds.center * VehiclesScale;
            zone.size = mesh.bounds.size * VehiclesScale;
            AddTrigger<TriggerEnterObstacleTrigger>(root);
            if (heading)
            {
                Add<HeadingRotationEffect>(root, config);
            }
            else
            {
                Add<SpeedMultiplierEffect>(root, config);
            }

            AddLifetime(root, ObstacleLifetime.LifetimeMode.RemoveImmediately, null);
            Save(root, name);
        }

        private static void BuildRoadwork(PhysicsMaterial noBounce)
        {
            GameObject group = new GameObject("Roadwork");
            GameObject barrier = NewModel("Barrier", Roads + "construction-barrier.fbx", RoadsScale, out Mesh barrierMesh, group.transform);
            GameObject fence = NewModel("Fence", Roads + "construction-fence.fbx", RoadsScale, out Mesh fenceMesh, group.transform);
            GameObject light = NewModel("Light", Roads + "construction-light.fbx", RoadsScale, out Mesh lightMesh, group.transform);

            // 부품의 긴 축(Z)으로 이어 붙여 차선을 막는 벽을 만든다. 배치는 레벨 디자인에서 조정한다
            float barrierHalf = barrierMesh.bounds.extents.z * RoadsScale;
            fence.transform.localPosition = new Vector3(0f, 0f, -(barrierHalf + fenceMesh.bounds.extents.z * RoadsScale));
            light.transform.localPosition = new Vector3(0f, 0f, barrierHalf + lightMesh.bounds.extents.z * RoadsScale);

            foreach (GameObject part in new[] { barrier, fence, light })
            {
                AddSolid(part.GetComponentInChildren<MeshFilter>().gameObject, noBounce);
            }

            // Barrier 오브젝트가 허브이고, 상호작용 구역은 모델 둘레를 넓힌 트리거다
            BoxCollider zone = barrier.AddComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.center = barrierMesh.bounds.center * RoadsScale;
            zone.size = barrierMesh.bounds.size * RoadsScale +new Vector3(InteractionZonePadding * 2f, 0f, InteractionZonePadding * 2f);

            InteractionObstacleTrigger trigger = barrier.AddComponent<InteractionObstacleTrigger>();
            SerializedObject so = new SerializedObject(trigger);
            so.FindProperty("_interactionDuration").floatValue = RoadworkInteractionSeconds;
            so.ApplyModifiedPropertiesWithoutUndo();
            Add<RemoveGroupEffect>(barrier, null);
            AddLifetime(barrier, ObstacleLifetime.LifetimeMode.RemoveImmediately, null);
            Save(group, "Roadwork");
        }

        // ---- 헬퍼 ----

        private static void EditExisting(string name, float mass, PhysicsMaterial material, System.Action<GameObject> extra)
        {
            string path = PropsDir + name + ".prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            Rigidbody body = root.GetComponent<Rigidbody>();
            if (body == null)
            {
                body = root.AddComponent<Rigidbody>();
            }

            body.isKinematic = false;
            body.mass = mass;
            foreach (Collider c in root.GetComponentsInChildren<Collider>())
            {
                c.sharedMaterial = material;
                if (c is MeshCollider mc)
                {
                    mc.convex = true; // 비고정 Rigidbody는 볼록 메시만 허용한다
                }
            }

            extra?.Invoke(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static GameObject NewModel(string name, string fbx, float scale, out Mesh mesh, Transform parent = null)
        {
            mesh = MeshOf(fbx, out Material mat);
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            GameObject model = new GameObject("Model");
            model.transform.SetParent(root.transform, false);
            model.transform.localScale = Vector3.one * scale;
            model.AddComponent<MeshFilter>().sharedMesh = mesh;
            model.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return root;
        }

        private static void AddSolid(GameObject model, PhysicsMaterial material)
        {
            MeshCollider c = model.AddComponent<MeshCollider>();
            c.sharedMesh = model.GetComponent<MeshFilter>().sharedMesh;
            c.sharedMaterial = material;
        }

        private static Mesh MeshOf(string fbx, out Material material)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            MeshFilter filter = asset.GetComponentInChildren<MeshFilter>();
            material = filter.GetComponent<MeshRenderer>().sharedMaterial;
            return filter.sharedMesh;
        }

        private static void Add<T>(GameObject go, ObstacleEffectConfig config, string field = null, Object value = null) where T : Component
        {
            T comp = go.GetComponent<T>();
            if (comp == null)
            {
                comp = go.AddComponent<T>();
            }

            SerializedObject so = new SerializedObject(comp);
            if (config != null)
            {
                so.FindProperty("_config").objectReferenceValue = config;
            }

            if (field != null)
            {
                so.FindProperty(field).objectReferenceValue = value;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddTrigger<T>(GameObject go) where T : Component
        {
            if (go.GetComponent<T>() == null)
            {
                go.AddComponent<T>(); // RequireComponent가 ObstacleModule을 함께 붙인다
            }
        }

        private static void AddLifetime(GameObject go, ObstacleLifetime.LifetimeMode mode, ObstacleEffectConfig config)
        {
            ObstacleLifetime life = go.GetComponent<ObstacleLifetime>();
            if (life == null)
            {
                life = go.AddComponent<ObstacleLifetime>();
            }

            SerializedObject so = new SerializedObject(life);
            so.FindProperty("_mode").enumValueIndex = (int)mode;
            so.FindProperty("_config").objectReferenceValue = config;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Save(GameObject root, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(root, PropsDir + name + ".prefab");
            Object.DestroyImmediate(root);
        }

        private static ObstacleEffectConfig Config(string name, float mult, float duration, float yaw, bool randomSign, float linger)
        {
            string path = ConfigDir + name + ".asset";
            ObstacleEffectConfig config = AssetDatabase.LoadAssetAtPath<ObstacleEffectConfig>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ObstacleEffectConfig>();
                AssetDatabase.CreateAsset(config, path);
            }

            SerializedObject so = new SerializedObject(config);
            so.FindProperty("_speedMultiplier").floatValue = mult;
            so.FindProperty("_speedDurationSeconds").floatValue = duration;
            so.FindProperty("_headingYawDegrees").floatValue = yaw;
            so.FindProperty("_headingRandomSign").boolValue = randomSign;
            so.FindProperty("_lingerSeconds").floatValue = linger;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            return config;
        }

        private static PhysicsMaterial GetPhysicsMaterial()
        {
            PhysicsMaterial m = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(MaterialPath);
            if (m == null)
            {
                m = new PhysicsMaterial("ObstacleNoBounce");
                AssetDatabase.CreateAsset(m, MaterialPath);
            }

            m.bounciness = 0f; // collision-durability-rules.md 3장
            m.bounceCombine = PhysicsMaterialCombine.Minimum;
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
