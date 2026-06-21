package com.cardmong.domain.battle.dto;

import com.cardmong.domain.battle.engine.BattleContext.TickEvent;
import com.cardmong.domain.battle.engine.BattleResult.Outcome;

import java.util.List;

public record BattleResultResponse(
        Long battleId,
        Outcome outcome,
        int durationTicks,
        int ratingChange,
        int expGained,
        int goldGained,
        List<TickEvent> battleLog
) {}
