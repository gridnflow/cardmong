package com.cardmong.domain.card.dto;

import com.cardmong.domain.card.entity.Card;

public record CardResponse(
        Integer cardId,
        String name,
        String element,
        String rarity,
        String role,
        int energyCost,
        int baseHp,
        int baseAttack,
        int baseDefense,
        int baseSpeed,
        int evolutionStage
) {
    public static CardResponse from(Card card) {
        return new CardResponse(
                card.getId(), card.getName(), card.getElement(),
                card.getRarity().name(), card.getRole().name(),
                card.getEnergyCost(), card.getBaseHp(), card.getBaseAttack(),
                card.getBaseDefense(), card.getBaseSpeed(), card.getEvolutionStage()
        );
    }
}
