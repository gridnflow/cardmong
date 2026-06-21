using System.Collections.Generic;

namespace Cardmong.Network.Dto
{
    [System.Serializable]
    public class CardDto
    {
        public int    CardId         { get; set; }
        public string Name           { get; set; }
        public string Element        { get; set; }
        public string Rarity         { get; set; }
        public string Role           { get; set; }
        public int    EnergyCost     { get; set; }
        public int    BaseHp         { get; set; }
        public int    BaseAttack     { get; set; }
        public int    BaseDefense    { get; set; }
        public int    BaseSpeed      { get; set; }
        public int    EvolutionStage { get; set; }
        public List<SkillSummaryDto> Skills { get; set; }
    }

    [System.Serializable]
    public class UserCardDto
    {
        public long    UserCardId    { get; set; }
        public int     CardId        { get; set; }
        public string  Name          { get; set; }
        public string  Element       { get; set; }
        public string  Rarity        { get; set; }
        public int     EnergyCost    { get; set; }
        public int     Level         { get; set; }
        public int     Exp           { get; set; }
        public int     UpgradeCount  { get; set; }
        public bool    IsLocked      { get; set; }
    }

    [System.Serializable]
    public class SkillSummaryDto
    {
        public string Slot { get; set; }
        public string Name { get; set; }
    }
}
