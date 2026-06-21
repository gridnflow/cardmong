using UnityEngine;
using TMPro;
using Cardmong.Battle;
using Cardmong.Network;
using Cardmong.UI.Common;

namespace Cardmong.UI.Battle
{
    public class BattleScreen : MonoBehaviour
    {
        [SerializeField] private BattleDirector director;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private TextMeshProUGUI rewardText;

        private async void Start()
        {
            LoadingOverlay.Show();

            // 대표 덱 ID는 SessionData에서 가져옴 (0이면 첫 번째 덱으로 가정)
            int deckId = SessionData.Instance.SelectedDeckId > 0 ? SessionData.Instance.SelectedDeckId : 1;
            var result = await BattleApi.StartBattle(deckId);

            LoadingOverlay.Hide();

            director.OnBattleFinished += ShowResult;
            director.StartPlayback(result);
        }

        private void ShowResult(string result)
        {
            resultPanel.SetActive(true);
            resultText.text = result == "WIN" ? "승리!" : "패배...";
        }

        public void OnClickSpeed1x() => director.SetSpeed(1f);
        public void OnClickSpeed2x() => director.SetSpeed(2f);
        public void OnClickSpeed3x() => director.SetSpeed(3f);

        public void OnClickLobby()
        {
            Core.SceneLoader.Load(Core.SceneLoader.Lobby);
        }
    }
}
