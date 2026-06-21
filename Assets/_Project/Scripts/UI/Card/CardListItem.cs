using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cardmong.Network.Dto;

namespace Cardmong.UI.Card
{
    public class CardListItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI rarityText;
        [SerializeField] private TextMeshProUGUI energyText;
        [SerializeField] private Image selectedOverlay;
        [SerializeField] private Button button;

        private UserCardDto _card;
        private Action<UserCardDto> _onClick;

        public void Init(UserCardDto card, Action<UserCardDto> onClick)
        {
            _card    = card;
            _onClick = onClick;

            nameText.text   = card.Name;
            levelText.text  = $"Lv.{card.Level}";
            rarityText.text = card.Rarity;
            energyText.text = $"{card.EnergyCost}";

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onClick?.Invoke(_card));
        }

        public void SetSelected(bool selected)
        {
            selectedOverlay.gameObject.SetActive(selected);
        }
    }
}
