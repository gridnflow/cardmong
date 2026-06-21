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

            // 대표 덱 ID는 실제로는 SessionData 또는 이전 씬에서 전달받아야 함
            var result = await BattleApi.StartBattle("PVE", 1);

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
