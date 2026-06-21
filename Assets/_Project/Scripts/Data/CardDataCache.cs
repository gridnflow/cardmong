using System.Collections.Generic;
using Cardmong.Network.Dto;

namespace Cardmong.Data
{
    public class CardDataCache
    {
        public static CardDataCache Instance { get; } = new CardDataCache();

        private readonly Dictionary<int, CardDto> _cards = new();

        private CardDataCache() { }

        public void Store(List<CardDto> cards)
        {
            _cards.Clear();
            foreach (var card in cards)
                _cards[card.CardId] = card;
        }

        public CardDto Get(int cardId)
        {
            _cards.TryGetValue(cardId, out var card);
            return card;
        }

        public bool Has(int cardId) => _cards.ContainsKey(cardId);
    }
}
