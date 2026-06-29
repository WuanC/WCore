using System.Collections;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace WGUI
{
    public class UINotification : UIBase
    {
        [SerializeField] private TextMeshProUGUI _messageText;
        Coroutine _autoHideCoroutine;
        protected override void Start()
        {
            base.Start();
            OnShowComplete += OnShowCompleteHandler;
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            OnShowComplete -= OnShowCompleteHandler;
        }
        private void OnShowCompleteHandler(UIBase uiBase)
        {
            _autoHideCoroutine = StartCoroutine(AutoHideCoroutine());
        }
        private IEnumerator AutoHideCoroutine()
        {
            yield return new WaitForSeconds(2f);
            UIManager.Instance.Hide(this);
        }
        public void SetMessage(string message)
        {
            if (_autoHideCoroutine != null)
            {
                StopCoroutine(_autoHideCoroutine);
                _autoHideCoroutine = null;
            }
            _messageText.text = message;
        }


    }
}