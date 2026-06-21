using System.Collections.Generic;
using System.Threading.Tasks;
using Cardmong.Network.Dto;

namespace Cardmong.Network
{
    public static class DeckApi
    {
        public static Task<List<DeckDto>> GetMyDecks()
            => ApiClient.Instance.GetAsync<List<DeckDto>>("/users/me/decks");

        public static Task<DeckDto> CreateDeck(string name, List<long> userCardIds)
            => ApiClient.Instance.PostAsync<DeckDto>("/users/me/decks",
                new DeckCreateRequest { Name = name, UserCardIds = userCardIds });

        public static Task<DeckDto> UpdateDeck(int deckId, string name, List<long> userCardIds)
            => ApiClient.Instance.PutAsync<DeckDto>($"/users/me/decks/{deckId}",
                new DeckCreateRequest { Name = name, UserCardIds = userCardIds });

        public static Task DeleteDeck(int deckId)
            => ApiClient.Instance.DeleteAsync($"/users/me/decks/{deckId}");
    }
}
