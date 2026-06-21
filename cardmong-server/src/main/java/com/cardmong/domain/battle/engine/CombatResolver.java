package com.cardmong.domain.battle.engine;

import com.cardmong.domain.battle.engine.BattleContext.TickEvent;

import java.util.concurrent.ThreadLocalRandom;

/**
 * 공격 1회의 모든 처리(피해 적용 → 이벤트 기록 → 속성 온-히트 효과 → 사망 처리)를
 * 한곳에 모은다. 각 AI가 공격 코드를 중복 작성하지 않게 하고, 속성 효과가 모든
 * 공격 경로에 일관되게 적용되도록 한다.
 */
public final class CombatResolver {

    private CombatResolver() {}

    public static void basicAttack(BattleMonster self, BattleMonster target, BattleContext ctx) {
        resolve(self, target, DamageCalculator.calculate(self, target), "ATTACK", null, ctx);
    }

    public static void skillAttack(BattleMonster self, BattleMonster target,
                                   double multiplier, String skillName, BattleContext ctx) {
        resolve(self, target, DamageCalculator.calculateSkill(self, target, multiplier),
                "SKILL", skillName, ctx);
    }

    private static void resolve(BattleMonster self, BattleMonster target, int dmg,
                                String type, String skillName, BattleContext ctx) {
        target.takeDamage(dmg);
        ctx.addEvent(new TickEvent(type, self.getUserCardId(), target.getUserCardId(),
                dmg, skillName, ctx.getCurrentTick()));
        applyElementOnHit(self, target, ctx);
        if (!target.isAlive()) {
            ctx.addEvent(new TickEvent("DEATH", null, target.getUserCardId(),
                    0, null, ctx.getCurrentTick()));
        }
    }

    /** 공격자의 속성에 따라 대상에게 상태이상을 부여한다. */
    private static void applyElementOnHit(BattleMonster self, BattleMonster target, BattleContext ctx) {
        if (!target.isAlive()) return;

        String applied = null;
        int value = 0;
        switch (self.getElement()) {
            case FIRE -> {
                int m = Math.max(1, (int) (self.getAttack() * 0.15));
                target.applyStatus(StatusEffect.Type.BURN, 3, m);
                applied = "burn"; value = 3;
            }
            case EARTH -> {
                int m = Math.max(1, (int) (self.getAttack() * 0.10));
                target.applyStatus(StatusEffect.Type.POISON, 4, m);
                applied = "poison"; value = 4;
            }
            case WATER -> {
                target.applyStatus(StatusEffect.Type.CHILL, 3, 0);
                applied = "chill"; value = 3;
            }
            case WIND -> {
                target.applyStatus(StatusEffect.Type.VULNERABLE, 2, 0);
                applied = "vulnerable"; value = 2;
            }
            case LIGHTNING -> {
                if (ThreadLocalRandom.current().nextDouble() < 0.30) {
                    target.applyStun(1);
                    applied = "shock"; value = 1;
                }
            }
            default -> { /* NEUTRAL: no on-hit effect */ }
        }

        if (applied != null) {
            ctx.addEvent(new TickEvent("STATUS", self.getUserCardId(), target.getUserCardId(),
                    value, applied, ctx.getCurrentTick()));
        }
    }
}
