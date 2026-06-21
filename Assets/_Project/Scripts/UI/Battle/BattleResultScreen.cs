using UnityEngine;
using TMPro;
using Cardmong.Core;
using Cardmong.Network.Dto;

namespace Cardmong.UI.Battle
{
    public class BattleResultScreen : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private TextMeshProUGUI expRewardText;
        [SerializeField] private TextMeshProUGUI goldRewardText;
        [SerializeField] private TextMeshProUGUI ratingChangeText;

        public void Show(BattleResultDto result)
        {
            gameObject.SetActive(true);

            resultText.text      = result.Result == "WIN" ? "Victory!" : "Defeat";
            expRewardText.text   = $"+{result.Rewards.Exp} EXP";
            goldRewardText.text  = $"+{result.Rewards.Gold} Gold";
            ratingChangeText.text = result.RatingChange >= 0
                ? $"+{result.RatingChange}"
                : $"{result.RatingChange}";
        }

        public void OnClickLobby() => SceneLoader.Load(SceneLoader.Lobby);

        public void OnClickRetry() => SceneLoader.Load(SceneLoader.Battle);
    }
}
