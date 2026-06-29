using System.Collections.Generic;
using UnityEngine;

namespace WCore.ObjectPool
{
    public class Pool
    {
        private Stack<GameObject> stack = new Stack<GameObject>();
        private GameObject prefab;

        private Transform parent;
        private GameObject tmpObject;
        private ReturnToPool returnToPool;
        public Pool(GameObject baseObject, Transform parent = null)
        {
            this.prefab = baseObject;
            this.parent = parent;
        }
        public GameObject Get(Transform newParent = null)
        {
            if (stack.Count > 0)
            {
                tmpObject = stack.Pop();
                tmpObject.SetActive(true);
                if (newParent != null && tmpObject.transform.parent != newParent)
                {
                    tmpObject.transform.SetParent(newParent);
                }

                return tmpObject;
            }
            tmpObject = GameObject.Instantiate(prefab, newParent);
            returnToPool = tmpObject.AddComponent<ReturnToPool>();
            returnToPool.pool = this;
            return tmpObject;

        }
        public void AddToPool(GameObject obj)
        {
            stack.Push(obj);
        }
        public void ClearPool()
        {
            for (int i = stack.Count - 1; i >= 0; i--)
            {
                GameObject.Destroy(stack.Pop());
            }
            stack.Clear();
        }
    }
}