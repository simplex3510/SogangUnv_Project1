using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Systems
{
    /// <summary>Boot 씬에서 게임 씬들을 같은 프레임에 Additive로 불러온다.</summary>
    // 같은 프레임에 불러야 모든 씬의 Awake가 끝난 뒤 Start가 돈다. 순서: Systems → 맵 → UI
    public class GameSceneLoader : MonoBehaviour
    {
        [SerializeField] private string[] _scenePaths;
        [SerializeField] private string _activeScenePath; // 조명·스카이박스 기준이 되는 맵 씬

        /// <summary>재시작 때 다시 불러올 Boot 씬 경로. Boot 없이 시작했으면 null.</summary>
        public static string BootScenePath { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            BootScenePath = null;
        }

        private void Awake()
        {
            BootScenePath = gameObject.scene.path;
            SceneManager.sceneLoaded += OnSceneLoaded;

            foreach (string path in _scenePaths)
            {
                SceneManager.LoadScene(path, LoadSceneMode.Additive);
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.path == _activeScenePath)
            {
                SceneManager.SetActiveScene(scene);
            }
        }
    }
}
