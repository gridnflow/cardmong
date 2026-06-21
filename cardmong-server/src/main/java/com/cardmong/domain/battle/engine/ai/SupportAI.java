package com.cardmong.domain.battle.engine.ai;

import com.cardmong.domain.battle.engine.BattleContext;
import com.cardmong.domain.battle.engine.BattleContext.TickEvent;
import com.cardmong.domain.battle.engine.BattleMonster;
import com.cardmong.domain.battle.engine.DamageCalculator;

import java.util.Comparator;
import java.util.List;

public class SupportAI implements MonsterAI {

    @Override
    public void act(BattleMonster self, BattleContext ctx) {
        List<BattleMonster> allies = ctx.getAlliesOf(self).stream()
                .filter(a -> a.isAlive() && a != self).toList();

        // Heal lowest-HP ally if skill ready
        if (self.canUseSkill() && !allies.isEmpty()) {
            BattleMonster target = allies.stream()
                    .min(Comparator.comparingDouble(BattleMonster::getHpPercent))
                    .orElseThrow();
            int healAmt = (int) (self.getAttack() * 0.8);
            target.heal(healAmt);
            self.resetSkillCooldown(6);
            ctx.addEvent(new TickEvent("HEAL", self.getUserCardId(),
                    target.getUserCardId(), healAmt, "group_heal", ctx.getCurrentTick()));
            return;
        }

        // Otherwise attack weakest enemy
        List<BattleMonster> enemies = ctx.getEnemiesOf(self).stream()
                .filter(BattleMonster::isAlive).toList();
        if (enemies.isEmpty()) return;

        BattleMonster target = enemies.stream()
                .min(Comparator.comparingDouble(BattleMonster::getHpPercent))
                .orElseThrow();
        int dmg = DamageCalculator.calculate(self, target);
        target.takeDamage(dmg);
        ctx.addEvent(new TickEvent("ATTACK", self.getUserCardId(),
                target.getUserCardId(), dmg, null, ctx.getCurrentTick()));
        if (!target.isAlive())
            ctx.addEvent(new TickEvent("DEATH", null,
                    target.getUserCardId(), 0, null, ctx.getCurrentTick()));
    }
}
