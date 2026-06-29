using UnityEngine;

namespace WCore.ObjectPool
{
    public class ReturnToPool : MonoBehaviour
    {
        [HideInInspector] public Pool pool;
        public void OnDisable()
        {
            pool.AddToPool(gameObject);
        }
    }
}