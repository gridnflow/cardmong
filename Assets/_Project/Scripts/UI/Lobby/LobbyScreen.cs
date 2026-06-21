using UnityEngine;
using TMPro;
using Cardmong.Core;
using Cardmong.Data;

namespace Cardmong.UI.Lobby
{
    public class LobbyScreen : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nicknameText;
        [SerializeField] private TextMeshProUGUI levelText;

        private void Start()
        {
            nicknameText.text = SessionData.Instance.Nickname;
        }

        public void OnClickCard()    => SceneLoader.Load(SceneLoader.DeckBuild);
        public void OnClickBattle()  => SceneLoader.Load(SceneLoader.Battle);
        public void OnClickRanking() => SceneLoader.Load(SceneLoader.Ranking);
    }
}
