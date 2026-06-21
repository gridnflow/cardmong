using UnityEngine;

namespace Cardmong.UI.Common
{
    public abstract class PopupBase : MonoBehaviour
    {
        public virtual void Open()  => gameObject.SetActive(true);
        public virtual void Close() => UIManager.Instance.CloseTopPopup();
    }
}
