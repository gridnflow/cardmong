using System.Collections;
using TMPro;
using UnityEngine;

namespace Cardmong.UI.Common
{
    public class ToastMessage : MonoBehaviour
    {
        public static ToastMessage Instance { get; private set; }

        [SerializeField] private GameObject toastPanel;
        [SerializeField] private TextMeshProUGUI messageText;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        public static void Show(string message, float duration = 2f)
        {
            Instance?.ShowInternal(message, duration);
        }

        private void ShowInternal(string message, float duration)
        {
            StopAllCoroutines();
            messageText.text = message;
            toastPanel.SetActive(true);
            StartCoroutine(HideAfter(duration));
        }

        private IEnumerator HideAfter(float duration)
        {
            yield return new WaitForSeconds(duration);
            toastPanel.SetActive(false);
        }
    }
}
