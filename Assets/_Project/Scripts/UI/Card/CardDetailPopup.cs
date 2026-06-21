using UnityEngine;
using TMPro;
using Cardmong.Network.Dto;
using Cardmong.UI.Common;

namespace Cardmong.UI.Card
{
    public class CardDetailPopup : PopupBase
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI elementText;
        [SerializeField] private TextMeshProUGUI rarityText;
        [SerializeField] private TextMeshProUGUI roleText;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private TextMeshProUGUI attackText;
        [SerializeField] private TextMeshProUGUI defenseText;
        [SerializeField] private TextMeshProUGUI speedText;

        public void Show(CardDto card)
        {
            nameText.text    = card.Name;
            elementText.text = card.Element;
            rarityText.text  = card.Rarity;
            roleText.text    = card.Role;
            hpText.text      = card.BaseHp.ToString();
            attackText.text  = card.BaseAttack.ToString();
            defenseText.text = card.BaseDefense.ToString();
            speedText.text   = card.BaseSpeed.ToString();

            Open();
        }
    }
}
