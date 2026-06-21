using System.Collections.Generic;
using System.Threading.Tasks;
using Cardmong.Network.Dto;

namespace Cardmong.Network
{
    public static class CardApi
    {
        public static Task<List<CardDto>> GetAllCards()
            => ApiClient.Instance.GetAsync<List<CardDto>>("/cards");

        public static Task<List<UserCardDto>> GetMyCards()
            => ApiClient.Instance.GetAsync<List<UserCardDto>>("/users/me/cards");

        public static Task<UserCardDto> UpgradeCard(long userCardId, List<long> materialCardIds)
            => ApiClient.Instance.PostAsync<UserCardDto>(
                $"/users/me/cards/{userCardId}/upgrade",
                new { MaterialCardIds = materialCardIds });

        public static Task<UserCardDto> EvolveCard(long userCardId)
            => ApiClient.Instance.PostAsync<UserCardDto>(
                $"/users/me/cards/{userCardId}/evolve", null);
    }
}
