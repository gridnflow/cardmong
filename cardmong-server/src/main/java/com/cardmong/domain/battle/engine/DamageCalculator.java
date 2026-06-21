package com.cardmong.domain.battle.engine;

import java.util.concurrent.ThreadLocalRandom;

public final class DamageCalculator {

    private DamageCalculator() {}

    public static int calculate(BattleMonster attacker, BattleMonster target) {
        double raw    = attacker.getAttack() * 1.0 / (1 + target.getDefense() * 0.01);
        double jitter = 0.9 + ThreadLocalRandom.current().nextDouble() * 0.2; // 90~110%
        double element = attacker.getElement().multiplierAgainst(target.getElement());
        double out    = attacker.outgoingMultiplier();   // CHILL 등으로 감소
        double in     = target.incomingMultiplier();     // VULNERABLE 등으로 증가
        return Math.max(1, (int) (raw * jitter * element * out * in));
    }

    public static int calculateSkill(BattleMonster attacker, BattleMonster target, double multiplier) {
        return Math.max(1, (int) (calculate(attacker, target) * multiplier));
    }
}
