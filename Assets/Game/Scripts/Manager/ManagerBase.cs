using UnityEngine;

namespace Game.Manager
{
    /// <summary>
    /// Singleton 기반 ManagerBase 클래스
    /// </summary>
    public abstract class ManagerBase<T> : MonoBehaviour
        where T : ManagerBase<T>
    {
        private static T instance;
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject singletonObject = GameObject.Find(typeof(T).Name);
                    if (singletonObject)
                    {
                        instance = singletonObject.GetComponent<T>();
                    }
                    else
                    {
                        singletonObject = new GameObject(typeof(T).Name);
                        instance = singletonObject.AddComponent<T>();
                    }

                    DontDestroyOnLoad(instance.gameObject);
                }

                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogError("Multiple instances of singleton detected!");
                Destroy(this.gameObject);
            }
            else
            {
                instance = (T)this;
            }
        }
    }
}