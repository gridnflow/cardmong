package com.cardmong.domain.battle.engine.ai;

import com.cardmong.domain.battle.engine.BattleContext;
import com.cardmong.domain.battle.engine.BattleContext.TickEvent;
import com.cardmong.domain.battle.engine.BattleMonster;
import com.cardmong.domain.battle.engine.DamageCalculator;

import java.util.Comparator;
import java.util.List;

public class AggressiveAI implements MonsterAI {

    @Override
    public void act(BattleMonster self, BattleContext ctx) {
        List<BattleMonster> enemies = ctx.getEnemiesOf(self).stream()
                .filter(BattleMonster::isAlive).toList();
        if (enemies.isEmpty()) return;

        // Target: lowest HP%
        BattleMonster target = enemies.stream()
                .min(Comparator.comparingDouble(BattleMonster::getHpPercent))
                .orElseThrow();

        if (self.canUseSkill()) {
            int dmg = DamageCalculator.calculateSkill(self, target, 1.8);
            target.takeDamage(dmg);
            self.resetSkillCooldown(5);
            ctx.addEvent(new TickEvent("SKILL", self.getUserCardId(),
                    target.getUserCardId(), dmg, "power_strike", ctx.getCurrentTick()));
            if (!target.isAlive())
                ctx.addEvent(new TickEvent("DEATH", null,
                        target.getUserCardId(), 0, null, ctx.getCurrentTick()));
        } else {
            int dmg = DamageCalculator.calculate(self, target);
            target.takeDamage(dmg);
            ctx.addEvent(new TickEvent("ATTACK", self.getUserCardId(),
                    target.getUserCardId(), dmg, null, ctx.getCurrentTick()));
            if (!target.isAlive())
                ctx.addEvent(new TickEvent("DEATH", null,
                        target.getUserCardId(), 0, null, ctx.getCurrentTick()));
        }
    }
}
