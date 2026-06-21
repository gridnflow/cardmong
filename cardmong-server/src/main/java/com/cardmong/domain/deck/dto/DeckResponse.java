package com.cardmong.domain.deck.dto;

import com.cardmong.domain.deck.entity.Deck;
import com.cardmong.domain.deck.entity.DeckCard;

import java.util.List;

public record DeckResponse(
        Integer deckId,
        String name,
        boolean isMain,
        int totalEnergy,
        List<DeckCardInfo> cards
) {
    public record DeckCardInfo(int slot, Long userCardId, String cardName, int level, String rarity) {}

    public static DeckResponse from(Deck deck) {
        List<DeckCardInfo> cards = deck.getDeckCards().stream()
                .map(dc -> new DeckCardInfo(
                        dc.getSlotIndex(),
                        dc.getUserCard().getId(),
                        dc.getUserCard().getCard().getName(),
                        dc.getUserCard().getLevel(),
                        dc.getUserCard().getCard().getRarity().name()
                )).toList();

        return new DeckResponse(deck.getId(), deck.getName(),
                deck.isMain(), deck.getTotalEnergy(), cards);
    }
}
