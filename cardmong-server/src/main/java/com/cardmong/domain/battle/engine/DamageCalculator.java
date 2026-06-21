package com.cardmong.domain.battle.engine;

import java.util.concurrent.ThreadLocalRandom;

public final class DamageCalculator {

    private DamageCalculator() {}

    public static int calculate(BattleMonster attacker, BattleMonster target) {
        double raw  = attacker.getAttack() * 1.0 / (1 + target.getDefense() * 0.01);
        double jitter = 0.9 + ThreadLocalRandom.current().nextDouble() * 0.2; // 90~110%
        return Math.max(1, (int) (raw * jitter));
    }

    public static int calculateSkill(BattleMonster attacker, BattleMonster target, double multiplier) {
        return Math.max(1, (int) (calculate(attacker, target) * multiplier));
    }
}
