using System.Collections.Generic;
using UnityEngine;

namespace WCore.ObjectPool
{
    public class PoolManager : Singleton<PoolManager>
    {
        private Dictionary<GameObject, Pool> pools = new Dictionary<GameObject, Pool>();
        public GameObject GetFromPool(GameObject baseObject, Transform parent = null)
        {
            if (!pools.ContainsKey(baseObject))
            {
                pools.Add(baseObject, new Pool(baseObject, parent));
            }
            return pools[baseObject].Get(parent);
        }

        public void DeleteKey(GameObject key)
        {
            if (!pools.ContainsKey(key)) return;
            pools[key].ClearPool();
            pools.Remove(key);
        }
        public void ClearPool()
        {
            foreach (var pool in pools.Values)
            {
                pool.ClearPool();
            }
            pools.Clear();
        }
    }
}