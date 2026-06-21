package com.cardmong.domain.card.dto;

import com.cardmong.domain.card.entity.UserCard;

public record UserCardResponse(
        Long userCardId,
        Integer cardId,
        String name,
        String element,
        String rarity,
        int energyCost,
        int level,
        int exp,
        int upgradeCount
) {
    public static UserCardResponse from(UserCard uc) {
        return new UserCardResponse(
                uc.getId(), uc.getCard().getId(), uc.getCard().getName(),
                uc.getCard().getElement(), uc.getCard().getRarity().name(),
                uc.getCard().getEnergyCost(), uc.getLevel(), uc.getExp(),
                uc.getUpgradeCount()
        );
    }
}
