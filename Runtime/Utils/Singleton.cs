using UnityEngine;

/// <summary>
/// 通用 MonoBehaviour 单例基类
/// </summary>
namespace MGKit
{
    public class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        private static T m_instance;
        private static readonly object m_lock = new object();

        public static T Instance
        {
            get
            {
                lock (m_lock)
                {
                    if (m_instance == null)
                    {
#if UNITY_2022_1_OR_NEWER
                        m_instance = FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
                    m_instance = FindObjectOfType<T>(true);
#endif
                        if (m_instance == null)
                        {
                            GameObject obj = new GameObject();
                            m_instance = obj.AddComponent<T>();
                            obj.name = typeof(T).ToString();
                        }
                    }
                    return m_instance;
                }
            }
        }

        protected virtual bool DontDestroy => true;

        public void Awake()
        {
            lock (m_lock)
            {
                if (m_instance != null && m_instance != this)
                {
                    Destroy(gameObject);
                    return;
                }

                m_instance = this as T;

                if (DontDestroy)
                {
                    if (transform.parent != null)
                    {
                        transform.SetParent(null);
                    }
                    DontDestroyOnLoad(gameObject);
                }
            }

            AwakeOf();
        }

        public virtual void AwakeOf()
        {
        }
    }
}