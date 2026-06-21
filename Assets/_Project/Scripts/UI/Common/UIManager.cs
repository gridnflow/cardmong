using System.Collections.Generic;
using UnityEngine;

namespace Cardmong.UI.Common
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        private readonly Stack<PopupBase> _popupStack = new();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void OpenPopup(PopupBase popup)
        {
            popup.gameObject.SetActive(true);
            _popupStack.Push(popup);
        }

        public void CloseTopPopup()
        {
            if (_popupStack.Count == 0) return;
            var popup = _popupStack.Pop();
            popup.gameObject.SetActive(false);
        }

        public void CloseAllPopups()
        {
            while (_popupStack.Count > 0)
                _popupStack.Pop().gameObject.SetActive(false);
        }
    }
}
