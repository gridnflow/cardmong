using System.Threading.Tasks;
using Cardmong.Network.Dto;

namespace Cardmong.Network
{
    public static class BattleApi
    {
        public static Task<BattleResultDto> StartBattle(string battleType, int deckId)
            => ApiClient.Instance.PostAsync<BattleResultDto>("/battles",
                new BattleStartRequest { BattleType = battleType, DeckId = deckId });

        public static Task<BattleResultDto> GetBattleResult(long battleId)
            => ApiClient.Instance.GetAsync<BattleResultDto>($"/battles/{battleId}");
    }
}
