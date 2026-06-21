using UnityEngine;

namespace Cardmong.UI.Common
{
    public class LoadingOverlay : MonoBehaviour
    {
        public static LoadingOverlay Instance { get; private set; }

        [SerializeField] private GameObject overlayPanel;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        public static void Show() => Instance?.overlayPanel.SetActive(true);
        public static void Hide() => Instance?.overlayPanel.SetActive(false);
    }
}
