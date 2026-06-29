using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using WCore;

namespace WGUI
{
    public class UIManager : Singleton<UIManager>
    {
        [InlineEditor]
        [SerializeField] private UIConfigSO uiConfig;

        private readonly Dictionary<Type, AsyncOperationHandle<GameObject>> _loaded = new();
        [SerializeField] private readonly Dictionary<Type, UIBase> _spawned = new();
        private readonly Dictionary<Type, UniTask<UIBase>> _spawning = new();
        private readonly List<UIBase> _popupStack = new();
        [SerializeField] private UIBase _currentFullScreen;
        [SerializeField] private UIBase _currentPopup;
        private UINotification _currentNotification;


        public bool HasPopupOnTop => _currentPopup != null || _popupStack.Count > 0;
        private UniTask _preloadTask;

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }
        private void OnDestroy()
        {
            Dispose();
        }

        #endregion

        #region Initialization / Dispose

        private void Initialize()
        {
            if (uiConfig == null)
            {
                Debug.LogError("[UIManager] UIConfigExtend is null.");
                return;
            }
        }

        private void Dispose()
        {
            foreach (AsyncOperationHandle<GameObject> handle in _loaded.Values)
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
            }
            _loaded.Clear();

            _spawned.Clear();
            _popupStack.Clear();

            _currentFullScreen = null;
            _currentPopup = null;
            _currentNotification = null;
        }



        #endregion

        #region Public API

        public void Show<T>(Action<T> onInit = null, Action onComplete = null) where T : UIBase
        {
            ShowInternalAsync<T>(onInit, onComplete).Forget();
        }

        public UniTask<T> ShowAsync<T>(Action<T> onInit = null, Action onComplete = null) where T : UIBase
        {
            return ShowInternalAsync<T>(onInit, onComplete);
        }
        private async UniTask<T> ShowInternalAsync<T>(Action<T> onInit = null, Action onComplete = null) where T : UIBase
        {
            Type type = typeof(T);

            UIConfig config = uiConfig.GetConfig(type);
            if (config == null)
            {
                Debug.LogError($"[UIManager] No UIConfig for {type.Name}");
                return null;
            }
            UIBase newUI = await GetOrSpawn<T>();
            if (newUI == null)
                return null;
            onInit?.Invoke(newUI as T);
            newUI.SetAnimationDuration(config.AnimationDuration);
            if (newUI is UINotification notification && notification.gameObject.activeSelf)
            {
                await notification.RestartShowAsync();
                onComplete?.Invoke();
                return newUI as T;
            }
            HandleCurrentUIBeforeShow(newUI, config);
            try
            {
                await newUI.ShowAsync(config.ShowAnimationType);

                onComplete?.Invoke();
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[UIManager] Show animation for {type.Name} was canceled.");

            }
            return newUI as T;

        }

        private async UniTask<T> GetOrSpawn<T>() where T : UIBase
        {
            Type type = typeof(T);

            if (_spawned.TryGetValue(type, out UIBase cached) && cached != null)
                return cached as T;

            if (_spawning.TryGetValue(type, out UniTask<UIBase> existingTask))
            {
                Debug.LogWarning($"[UIManager] {type.Name} đang được tải/tạo, đợi tiến trình cũ...");
                UIBase awaitedUI = await existingTask;
                return awaitedUI as T;
            }

            UniTask<UIBase> spawnTask = InternallySpawnAndRegister<T>(type);
            _spawning[type] = spawnTask;

            try
            {
                UIBase result = await spawnTask;
                return result as T;
            }
            finally
            {

                _spawning.Remove(type);
            }
        }

        private async UniTask<UIBase> InternallySpawnAndRegister<T>(Type type) where T : UIBase
        {
            UIConfig config = uiConfig.GetConfig(type);
            if (config == null || config.AssetReference == null)
            {
                Debug.LogError($"[UIManager] Missing prefab config for {type.Name}");
                return null;
            }

            T ui = await Spawn<T>(config);
            if (ui == null) return null;

            ui.gameObject.SetActive(false);

            if (!_spawned.TryAdd(type, ui))
            {
                Debug.LogError($"[UIManager] Failed to add spawned UI for {type.Name}");
                Destroy(ui.gameObject);
                return null;
            }

            SetCanvasProperties(ui, config);

            return ui;
        }

        private async UniTask<T> Spawn<T>(UIConfig config) where T : UIBase
        {
            UIBase cachedPrefab = null;
            if (_loaded.TryGetValue(typeof(T), out AsyncOperationHandle<GameObject> cachedHandle))
            {
                cachedPrefab = cachedHandle.Result.GetComponent<UIBase>();
            }
            else
            {
                var handle = config.AssetReference.LoadAssetAsync<GameObject>();
                await handle;
                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError(handle.OperationException);
                    return null;
                }
                cachedPrefab = handle.Result.GetComponent<UIBase>();
                _loaded.TryAdd(typeof(T), handle);
            }

            UIBase instance = Instantiate(cachedPrefab, transform);
            return instance as T;
        }

        public void Hide<T>(Action onComplete = null) where T : UIBase
        {
            HideInternal<T>(onComplete).Forget();
        }
        public void Hide(UIBase ui)
        {
            HideUI(ui).Forget();
        }
        private async UniTaskVoid HideInternal<T>(Action onComplete = null) where T : UIBase
        {
            if (!_spawned.TryGetValue(typeof(T), out UIBase ui))
                return;

            try
            {
                await HideUI(ui);
                onComplete?.Invoke();
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[UIManager] Hide {typeof(T).Name} was canceled.");
            }
        }

        public void HideAll(Action onComplete = null)
        {
            HideAllInternal(onComplete).Forget();
        }

        private async UniTaskVoid HideAllInternal(Action onComplete = null)
        {
            List<UniTask> tasks = new();
            foreach (UIBase ui in new List<UIBase>(_spawned.Values))
            {
                tasks.Add(HideUI(ui));
            }

            try
            {
                await UniTask.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[UIManager] HideAll was canceled.");
            }

            onComplete?.Invoke();
        }

        private async UniTask HideUI(UIBase ui)
        {
            if (ui == null)
                return;

            UIConfig config = uiConfig.GetConfig(ui.GetType());
            if (config == null)
                return;

            await ui.HideAsync(config.HideAnimationType);

            HandleUIClose(ui);

            if (config.LifeCyclePolicy == UILifeCyclePolicy.ReleaseInstance)
            {
                Type type = ui.GetType();

                Destroy(ui.gameObject);
                _spawned.Remove(type);

                if (_loaded.TryGetValue(type, out var handle))
                {
                    if (handle.IsValid())
                        Addressables.Release(handle);

                    _loaded.Remove(type);
                }
            }
        }

        #endregion

        #region Show Flow



        private void HandleCurrentUIBeforeShow(UIBase newUI, UIConfig config)
        {
            switch (config.CanvasType)
            {
                case CanvasType.FullScreen:
                    HandleFullScreenBeforeShow(newUI);
                    break;


                case CanvasType.Popup:
                    HandlePopupBeforeShow(newUI);
                    break;

                case CanvasType.Notification:
                    _currentNotification = newUI as UINotification;
                    break;

            }
        }

        private void HandleFullScreenBeforeShow(UIBase newUI)
        {
            if (_currentFullScreen != null && _currentFullScreen != newUI)
            {
                Type type = _currentFullScreen.GetType();
                HideUI(_currentFullScreen).Forget();
            }
            _currentFullScreen = newUI;
        }

        private void HandlePopupBeforeShow(UIBase newUI)
        {
            if (newUI == null)
                return;
            _popupStack.Remove(newUI);
            _popupStack.Add(newUI);
            _currentPopup = newUI;

            RefreshPopupCanvasOrders();
        }


        #endregion


        #region Hide / Close


        private void HandleUIClose(UIBase ui)
        {
            if (ui == null)
                return;

            _popupStack.Remove(ui);

            if (_currentFullScreen == ui)
                _currentFullScreen = null;

            if (_currentPopup == ui)
                _currentPopup = _popupStack.Count > 0 ? _popupStack[^1] : null;

            if (_currentNotification == ui)
                _currentNotification = null;
            RefreshPopupCanvasOrders();
        }

        #endregion

        #region Canvas Sorting

        private void SetCanvasProperties(UIBase ui, UIConfig config)
        {
            if (ui == null || ui.Canvas == null)
                return;

            if (config == null)
            {
                Debug.LogError($"[UIManager] Missing UIConfig for {ui.GetType().Name}");
                return;
            }

            CanvasType canvasType = config.CanvasType;
            int baseOrder = GetBaseOrder(canvasType);

            ui.Canvas.overrideSorting = true;
            ui.Canvas.sortingOrder = uiConfig.ResolveSortingOrder(config, baseOrder);


            if (canvasType == CanvasType.Popup)
            {
                int popupIndex = _popupStack.IndexOf(ui);
                if (popupIndex < 0)
                    popupIndex = _popupStack.Count;

                ui.Canvas.sortingOrder += popupIndex;
            }
        }


        private void RefreshPopupCanvasOrders()
        {
            for (int i = _popupStack.Count - 1; i >= 0; i--)
            {
                UIBase popup = _popupStack[i];

                if (popup == null)
                {
                    _popupStack.RemoveAt(i);
                    continue;
                }

                UIConfig config = uiConfig.GetConfig(popup.GetType());
                SetCanvasProperties(popup, config);
            }

            _currentPopup = _popupStack.Count > 0 ? _popupStack[^1] : null;
        }

        private int GetBaseOrder(CanvasType type)
        {
            return type switch
            {
                CanvasType.FullScreen => 0,
                CanvasType.Popup => 200,
                CanvasType.Notification => 1000,
                _ => 0
            };
        }

        #endregion

    }
}