using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace WGUI
{
    [Serializable]
    public class UIConfig
    {
        [Title("UI Config")]
        [Required]
        [SerializeField] private AssetReference _assetReference;
        [SerializeField] private CanvasType _canvasType = CanvasType.FullScreen;
        [SerializeField] private UIAnimationType _showAnimationType = UIAnimationType.None;
        [SerializeField] private UIAnimationType _hideAnimationType = UIAnimationType.None;
        [SerializeField] private UILifeCyclePolicy _lifeCyclePolicy = UILifeCyclePolicy.SaveInstance;
        [SerializeField] private int _sortingOrder = -1;

        [SerializeField] private float _animationDuration = 0.2f;
        private string _typeName;

        public AssetReference AssetReference => _assetReference;
        public CanvasType CanvasType => _canvasType;
        public UIAnimationType ShowAnimationType => _showAnimationType;
        public UIAnimationType HideAnimationType => _hideAnimationType;
        public float AnimationDuration => _animationDuration;
        public UILifeCyclePolicy LifeCyclePolicy => _lifeCyclePolicy;
        public string TypeName
        {
            get => _typeName;
            set => _typeName = value;
        }
        public int SortingOrder => _sortingOrder;
    }
}