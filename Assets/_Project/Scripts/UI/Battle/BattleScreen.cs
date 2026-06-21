using UnityEngine;
using TMPro;
using Cardmong.Battle;
using Cardmong.Network;
using Cardmong.Data;
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
            try
            {
                int deckId = SessionData.Instance.SelectedDeckId > 0 ? SessionData.Instance.SelectedDeckId : 1;
                var result = await BattleApi.StartBattle(deckId);

                director.OnBattleFinished += ShowResult;
                director.StartPlayback(result);
            }
            catch (System.Exception e)
            {
                ToastMessage.Show($"배틀 시작 실패: {e.Message}");
            }
            finally
            {
                LoadingOverlay.Hide();
            }
        }

        private void ShowResult(string result)
        {
            resultPanel.SetActive(true);
            resultText.text = result == "WIN" ? "승리!" : (result == "DRAW" ? "무승부" : "패배...");
            if (rewardText != null)
                rewardText.text = "";
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
