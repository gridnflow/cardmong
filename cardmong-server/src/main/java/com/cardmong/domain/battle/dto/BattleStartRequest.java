package com.cardmong.domain.battle.dto;

import jakarta.validation.constraints.NotNull;

public record BattleStartRequest(
        @NotNull Integer deckId,
        Long opponentUserId  // null = AI opponent
) {}
