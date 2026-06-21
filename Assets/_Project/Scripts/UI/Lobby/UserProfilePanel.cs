using UnityEngine;
using TMPro;
using Cardmong.Data;

namespace Cardmong.UI.Lobby
{
    public class UserProfilePanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nicknameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI gemText;

        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            nicknameText.text = SessionData.Instance.Nickname ?? "-";
        }
    }
}
