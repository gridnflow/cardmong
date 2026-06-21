package com.cardmong.domain.battle.engine.ai;

import com.cardmong.domain.battle.engine.BattleContext;
import com.cardmong.domain.battle.engine.BattleContext.TickEvent;
import com.cardmong.domain.battle.engine.BattleMonster;
import com.cardmong.domain.battle.engine.DamageCalculator;

import java.util.Comparator;
import java.util.List;

public class TankAI implements MonsterAI {

    @Override
    public void act(BattleMonster self, BattleContext ctx) {
        List<BattleMonster> enemies = ctx.getEnemiesOf(self).stream()
                .filter(BattleMonster::isAlive).toList();
        if (enemies.isEmpty()) return;

        // Target: highest attack (taunt the biggest threat)
        BattleMonster target = enemies.stream()
                .max(Comparator.comparingInt(BattleMonster::getAttack))
                .orElseThrow();

        if (self.canUseSkill()) {
            // Shield bash - stun
            int dmg = DamageCalculator.calculateSkill(self, target, 1.2);
            target.takeDamage(dmg);
            target.applyStun(3);
            self.resetSkillCooldown(8);
            ctx.addEvent(new TickEvent("STUN", self.getUserCardId(),
                    target.getUserCardId(), dmg, "shield_bash", ctx.getCurrentTick()));
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
