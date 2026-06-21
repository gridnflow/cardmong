using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cardmong.Network.Dto;

namespace Cardmong.UI.Deck
{
    public class DeckSlot : MonoBehaviour
    {
        [SerializeField] private GameObject emptyState;
        [SerializeField] private GameObject filledState;
        [SerializeField] private TextMeshProUGUI cardNameText;
        [SerializeField] private TextMeshProUGUI levelText;

        public void SetCard(UserCardDto card)
        {
            emptyState.SetActive(false);
            filledState.SetActive(true);
            cardNameText.text = card.Name;
            levelText.text    = $"Lv.{card.Level}";
        }

        public void Clear()
        {
            emptyState.SetActive(true);
            filledState.SetActive(false);
        }
    }
}
