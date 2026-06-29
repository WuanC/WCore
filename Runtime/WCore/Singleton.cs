using UnityEngine;

namespace WCore
{



    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        [SerializeField] protected bool canDontDestroyOnLoad = true;
        private static T instance;
        public static T Instance
        {
            get
            {
                return instance;
            }
        }

        protected virtual void Awake()
        {
            CreateInstance();
        }

        private void CreateInstance()
        {
            if (instance == null)
            {
                instance = this as T;

                if (canDontDestroyOnLoad)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }

        }
    }

}