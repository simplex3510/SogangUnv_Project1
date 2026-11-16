using UnityEngine;

namespace Game.WorldObjects
{
    /// <summary>실행 후 오브젝트의 수명을 정한다. 이 컴포넌트가 없으면 유지(Persist)와 같다.</summary>
    public class ObstacleLifetime : MonoBehaviour
    {
        public enum LifetimeMode
        {
            RemoveImmediately,
            RemoveAfterLinger,
            Persist,
        }

        [SerializeField] private LifetimeMode _mode = LifetimeMode.RemoveImmediately;

        [Tooltip("RemoveAfterLinger일 때만 필요하다. 잔류 시간을 가진 설정")]
        [SerializeField] private ObstacleEffectConfig _config;

        /// <summary>효과 실행 직후 ObstacleModule이 호출한다.</summary>
        public void OnExecuted()
        {
            switch (_mode)
            {
                case LifetimeMode.RemoveImmediately:
                    Destroy(gameObject);
                    break;

                case LifetimeMode.RemoveAfterLinger:
                    if (_config == null)
                    {
                        Debug.LogError($"ObstacleEffectConfig is not assigned in {gameObject.name}");
                        return;
                    }

                    Destroy(gameObject, _config.LingerSeconds);
                    break;
            }
        }
    }
}
