using UnityEngine;

namespace WCore.Factory
{
    public abstract class Factory<TView, TArgs> : MonoBehaviour where TView : MonoBehaviour
    {
        public abstract TView Create(TArgs args);
    }
}