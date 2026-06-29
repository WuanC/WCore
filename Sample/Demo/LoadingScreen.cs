using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WGUI.Demo
{
    public class LoadingScreen : UIBase
    {
        public Slider progressBar;
        public float loadDuration = 2f;
        public float delayDuration = 1f;
        [SerializeField] TextMeshProUGUI progressText;


        private void Start()
        {
            StartCoroutine(LoadRoutine());
        }
        IEnumerator LoadRoutine()
        {
            float elapsed = 0f;
            while (elapsed < loadDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / loadDuration);
                progressText.text = Mathf.RoundToInt(progress * 100f) + "%";
                progressBar.value = progress;
                yield return null;
            }

            progressBar.value = 1f;
            yield return new WaitForSeconds(delayDuration);
            UIManager.Instance.Hide<LoadingScreen>();
        }
    }
}