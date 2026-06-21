using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using Cardmong.Network;
using Cardmong.Network.Dto;
using Cardmong.UI.Common;

namespace Cardmong.UI.Deck
{
    public class DeckBuildScreen : MonoBehaviour
    {
        [SerializeField] private Transform cardListParent;
        [SerializeField] private DeckSlot[] deckSlots;
        [SerializeField] private TextMeshProUGUI energyText;

        private List<UserCardDto> _myCards     = new();
        private List<UserCardDto> _selectedCards = new();

        private const int MaxEnergy = 25;
        private const int MaxSlots  = 5;

        private async void Start()
        {
            LoadingOverlay.Show();
            try
            {
                _myCards = await CardApi.GetMyCards();
                RefreshDeckSlots();
            }
            catch (System.Exception e)
            {
                ToastMessage.Show($"카드 로드 실패: {e.Message}");
            }
            finally
            {
                LoadingOverlay.Hide();
            }
        }

        public void OnCardClicked(UserCardDto card)
        {
            if (_selectedCards.Contains(card))
            {
                _selectedCards.Remove(card);
            }
            else
            {
                if (_selectedCards.Count >= MaxSlots)
                {
                    ToastMessage.Show("덱은 최대 5장까지 구성할 수 있습니다.");
                    return;
                }
                if (GetTotalEnergy() + card.EnergyCost > MaxEnergy)
                {
                    ToastMessage.Show("에너지 한도를 초과했습니다.");
                    return;
                }
                _selectedCards.Add(card);
            }

            RefreshDeckSlots();
            UpdateEnergyDisplay();
        }

        private void RefreshDeckSlots()
        {
            for (int i = 0; i < deckSlots.Length; i++)
            {
                if (i < _selectedCards.Count)
                    deckSlots[i].SetCard(_selectedCards[i]);
                else
                    deckSlots[i].Clear();
            }
        }

        private int GetTotalEnergy() => _selectedCards.Sum(c => c.EnergyCost);

        private void UpdateEnergyDisplay()
            => energyText.text = $"{GetTotalEnergy()} / {MaxEnergy}";

        public async void OnSaveDeck()
        {
            if (_selectedCards.Count == 0)
            {
                ToastMessage.Show("카드를 선택해주세요.");
                return;
            }

            LoadingOverlay.Show();
            try
            {
                await DeckApi.CreateDeck("내 덱", _selectedCards.Select(c => c.UserCardId).ToList());
                ToastMessage.Show("덱이 저장되었습니다.");
            }
            catch (System.Exception e)
            {
                ToastMessage.Show($"덱 저장 실패: {e.Message}");
            }
            finally
            {
                LoadingOverlay.Hide();
            }
        }
    }
}
