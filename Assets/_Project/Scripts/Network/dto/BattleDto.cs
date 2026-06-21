using System.Collections.Generic;

namespace Cardmong.Network.Dto
{
    [System.Serializable]
    public class BattleStartRequest
    {
        public string BattleType { get; set; }
        public int    DeckId     { get; set; }
    }

    [System.Serializable]
    public class BattleResultDto
    {
        public long   BattleId     { get; set; }
        public string Result       { get; set; }  // WIN / LOSE
        public int    DurationMs   { get; set; }
        public int    RatingChange { get; set; }
        public RewardDto Rewards   { get; set; }
        public List<BattleLogDto> Logs { get; set; }
    }

    [System.Serializable]
    public class BattleLogDto
    {
        public int    TimeMs       { get; set; }
        public string EventType    { get; set; }
        public int    ActorCardId  { get; set; }
        public int?   TargetCardId { get; set; }
        public int?   Value        { get; set; }
        public Dictionary<string, object> ExtraData { get; set; }
    }

    [System.Serializable]
    public class RewardDto
    {
        public int Exp  { get; set; }
        public int Gold { get; set; }
    }
}
