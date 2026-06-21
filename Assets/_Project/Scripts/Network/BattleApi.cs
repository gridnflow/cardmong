using System.Threading.Tasks;
using Cardmong.Network.Dto;

namespace Cardmong.Network
{
    public static class BattleApi
    {
        public static Task<BattleResultDto> StartBattle(int deckId, long? opponentUserId = null)
            => ApiClient.Instance.PostAsync<BattleResultDto>("/battles",
                new BattleStartRequest { DeckId = deckId, OpponentUserId = opponentUserId });

        public static Task<BattleResultDto> GetBattleResult(long battleId)
            => ApiClient.Instance.GetAsync<BattleResultDto>($"/battles/{battleId}");
    }
}
