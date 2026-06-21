using System.Collections.Generic;

namespace Cardmong.Network.Dto
{
    [System.Serializable]
    public class BattleStartRequest
    {
        public int   DeckId          { get; set; }
        public long? OpponentUserId  { get; set; }
    }

    [System.Serializable]
    public class BattleResultDto
    {
        public long   BattleId      { get; set; }
        public string Outcome       { get; set; }  // ATTACKER_WIN / DEFENDER_WIN / DRAW
        public int    DurationTicks { get; set; }
        public int    RatingChange  { get; set; }
        public int    ExpGained     { get; set; }
        public int    GoldGained    { get; set; }
        public List<BattleLogDto> BattleLog { get; set; }

        public string Result => Outcome == "ATTACKER_WIN" ? "WIN" : (Outcome == "DRAW" ? "DRAW" : "LOSE");
    }

    [System.Serializable]
    public class BattleLogDto
    {
        public string Type     { get; set; }  // ATTACK / SKILL / HEAL / STUN / DEATH
        public long?  SourceId { get; set; }
        public long?  TargetId { get; set; }
        public int    Value    { get; set; }
        public string Extra    { get; set; }
        public int    Tick     { get; set; }
    }

    [System.Serializable]
    public class RewardDto
    {
        public int Exp  { get; set; }
        public int Gold { get; set; }
    }
}
