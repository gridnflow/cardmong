package com.cardmong.domain.battle.engine;

import java.util.List;

public record BattleResult(
        Outcome outcome,
        int durationTicks,
        List<BattleContext.TickEvent> events
) {
    public enum Outcome { ATTACKER_WIN, DEFENDER_WIN, DRAW }
}
