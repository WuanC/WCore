using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace WGUI
{
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(Canvas))]
    [DisallowMultipleComponent]
    public class UIBase : MonoBehaviour
    {
        [Title("Panel Root")]
        [SerializeField] protected RectTransform _panelRoot;
        [SerializeField] protected Button _closeButton;
        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private Vector2 _originalAnchoredPos;
        private bool _hasCachedOriginalAnchoredPos = false;
        private UIAnimationType _lastShowAnimationType = UIAnimationType.FadeScale;

        private CancellationTokenSource _cts;
        private UIState _state = UIState.Hidden;
        private float _animationDuration;

        public event Action<UIBase> OnShowComplete;
        public event Action<UIBase> OnHideComplete;
        public Canvas Canvas => _canvas;

        #region Unity LifeCycle

        protected virtual void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _canvasGroup = GetComponent<CanvasGroup>();

            CacheShownPositionIfNeeded();
            _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }
        protected virtual void Start()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(() =>
                {
                    UIManager.Instance.Hide(this);
                });
            }

        }
        protected virtual void OnDestroy()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
            }
            _cts?.Cancel();
            _cts?.Dispose();
        }
        #endregion


        #region Public Methods
        public async UniTask ShowAsync(UIAnimationType type = UIAnimationType.FadeScale)
        {
            if (_state == UIState.Visible || _state == UIState.Showing)
                return;

            CancelAnimation();
            var token = _cts.Token;

            SetState(UIState.Showing);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            _lastShowAnimationType = type;
            try
            {
                await PlayShowAsync(type, token);

                SetState(UIState.Visible);
                OnShowComplete?.Invoke(this);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("Show animation was canceled.");
            }

        }
        public async UniTask HideAsync(UIAnimationType type = UIAnimationType.FadeScale, bool useLastAnimationType = false)
        {
            if (_state == UIState.Hidden || _state == UIState.Hiding)
                return;

            CancelAnimation();
            var token = _cts.Token;

            SetState(UIState.Hiding);
            if (useLastAnimationType)
                type = _lastShowAnimationType;
            try
            {
                await PlayHideAsync(type, token);

                gameObject.SetActive(false);
                SetState(UIState.Hidden);
                OnHideComplete?.Invoke(this);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("Hide animation was canceled.");
            }

        }
        public bool IsTransitioning()
        {
            return _state == UIState.Showing || _state == UIState.Hiding || _state == UIState.Revealing;
        }
        public void BlockUI()
        {
            if (_state == UIState.Blocked)
                return;

            SetState(UIState.Blocked);
        }
        public void SetAnimationDuration(float duration)
        {
            _animationDuration = duration;
        }
        public async UniTask RestartShowAsync()
        {
            CancelAnimation();

            SetState(UIState.Showing);

            gameObject.SetActive(true);

            await PlayShowAsync(_lastShowAnimationType, _cts.Token);

            SetState(UIState.Visible);

            OnShowComplete?.Invoke(this);
        }
        #endregion

        #region Private Methods
        private void CacheShownPositionIfNeeded()
        {
            RectTransform rect = GetPanelRect();
            if (rect == null)
                return;

            if (_hasCachedOriginalAnchoredPos)
                return;

            _originalAnchoredPos = rect.anchoredPosition;
            _hasCachedOriginalAnchoredPos = true;
        }
        private RectTransform GetPanelRect()
        {
            return _panelRoot != null ? _panelRoot : transform as RectTransform;
        }

        private Vector2 GetHiddenPosition(Vector2 direction)
        {

            float offset = direction.x != 0
                ? Screen.width
                : Screen.height;

            return _originalAnchoredPos + direction * offset;
        }



        private UniTask ShowInstantAsync()
        {

            RectTransform rect = GetPanelRect();
            if (rect != null)
            {
                CacheShownPositionIfNeeded();
                rect.anchoredPosition = _originalAnchoredPos;
                rect.localScale = Vector3.one;
            }

            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            _canvasGroup.alpha = 1f;
            return UniTask.CompletedTask;
        }


        private UniTask HideInstantAsync()
        {

            RectTransform rect = GetPanelRect();
            if (rect != null)
            {
                CacheShownPositionIfNeeded();
                rect.anchoredPosition = _originalAnchoredPos;
            }

            _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
            return UniTask.CompletedTask;

        }
        private void CancelAnimation()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
        }
        private UniTask PlayShowAsync(UIAnimationType type, CancellationToken token)
        {
            return type switch
            {
                UIAnimationType.None => ShowInstantAsync(),
                UIAnimationType.FadeScale => PlayFadeInAsync(token),
                UIAnimationType.SlideFromLeft => PlaySlideInAsync(Vector2.left, token),
                UIAnimationType.SlideFromRight => PlaySlideInAsync(Vector2.right, token),
                UIAnimationType.SlideFromTop => PlaySlideInAsync(Vector2.up, token),
                UIAnimationType.SlideFromBottom => PlaySlideInAsync(Vector2.down, token),
                _ => UniTask.CompletedTask
            };
        }
        private UniTask PlayHideAsync(UIAnimationType type, CancellationToken token)
        {
            return type switch
            {
                UIAnimationType.None => HideInstantAsync(),
                UIAnimationType.FadeScale => PlayFadeOutAsync(token),
                UIAnimationType.SlideFromLeft => PlaySlideOutAsync(Vector2.left, token),
                UIAnimationType.SlideFromRight => PlaySlideOutAsync(Vector2.right, token),
                UIAnimationType.SlideFromTop => PlaySlideOutAsync(Vector2.up, token),
                UIAnimationType.SlideFromBottom => PlaySlideOutAsync(Vector2.down, token),
                _ => UniTask.CompletedTask
            };
        }
        private async UniTask PlayFadeInAsync(CancellationToken token)
        {

            _canvasGroup.alpha = 0f;
            if (_panelRoot != null)
                _panelRoot.localScale = Vector3.one * 0.85f;
            RectTransform rect = GetPanelRect();
            rect.anchoredPosition = _originalAnchoredPos;
            Sequence seq = DOTween.Sequence();

            seq.Join(_canvasGroup.DOFade(1f, _animationDuration));

            if (_panelRoot != null)
                seq.Join(_panelRoot.DOScale(1f, _animationDuration));

            await seq.ToUniTask(cancellationToken: token, tweenCancelBehaviour: TweenCancelBehaviour.Kill);
        }
        private async UniTask PlayFadeOutAsync(CancellationToken token)
        {

            Sequence seq = DOTween.Sequence();
            RectTransform rect = GetPanelRect();
            rect.anchoredPosition = _originalAnchoredPos;
            seq.Join(_canvasGroup.DOFade(0f, _animationDuration));

            if (_panelRoot != null)
                seq.Join(_panelRoot.DOScale(0.9f, _animationDuration));

            await seq.ToUniTask(cancellationToken: token, tweenCancelBehaviour: TweenCancelBehaviour.Kill);
        }
        private async UniTask PlaySlideInAsync(Vector2 dir, CancellationToken token)
        {
            RectTransform rect = GetPanelRect();
            CacheShownPositionIfNeeded();

            Vector2 start = GetHiddenPosition(dir);

            rect.anchoredPosition = start;
            _canvasGroup.alpha = 1f;

            Tween t = rect.DOAnchorPos(_originalAnchoredPos, _animationDuration);

            await t.ToUniTask(cancellationToken: token, tweenCancelBehaviour: TweenCancelBehaviour.Kill);
        }
        private async UniTask PlaySlideOutAsync(Vector2 dir, CancellationToken token)
        {
            RectTransform rect = GetPanelRect();
            CacheShownPositionIfNeeded();

            Vector2 end = GetHiddenPosition(dir);
            _canvasGroup.alpha = 1f;

            Tween t = rect.DOAnchorPos(end, _animationDuration);

            await t.ToUniTask(cancellationToken: token, tweenCancelBehaviour: TweenCancelBehaviour.Kill);
        }
        private void SetState(UIState state)
        {
            _state = state;

            bool canInteract = state == UIState.Visible;

            //_canvasGroup.interactable = canInteract;
            _canvasGroup.blocksRaycasts = canInteract;
        }
        #endregion
    }
}
