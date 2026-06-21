using System.Collections.Generic;

namespace Cardmong.Network.Dto
{
    [System.Serializable]
    public class DeckDto
    {
        public int    DeckId      { get; set; }
        public string Name        { get; set; }
        public bool   IsMain      { get; set; }
        public int    TotalEnergy { get; set; }
        public List<DeckCardDto> Cards { get; set; }
    }

    [System.Serializable]
    public class DeckCardDto
    {
        public int    Slot       { get; set; }
        public long   UserCardId { get; set; }
        public string CardName   { get; set; }
        public int    Level      { get; set; }
        public string Rarity     { get; set; }
        public int    EnergyCost { get; set; }
    }

    [System.Serializable]
    public class DeckCreateRequest
    {
        public string     Name        { get; set; }
        public List<long> UserCardIds { get; set; }
    }
}
