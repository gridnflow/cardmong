package com.cardmong.domain.battle.engine.ai;

import com.cardmong.domain.battle.engine.BattleContext;
import com.cardmong.domain.battle.engine.BattleContext.TickEvent;
import com.cardmong.domain.battle.engine.BattleMonster;
import com.cardmong.domain.battle.engine.CombatResolver;
import com.cardmong.domain.battle.engine.StatusEffect;

import java.util.Comparator;
import java.util.List;

public class SupportAI implements MonsterAI {

    @Override
    public void act(BattleMonster self, BattleContext ctx) {
        List<BattleMonster> allies = ctx.getAlliesOf(self).stream()
                .filter(a -> a.isAlive() && a != self).toList();

        // Heal lowest-HP ally if skill ready, and grant a regen over time
        if (self.canUseSkill() && !allies.isEmpty()) {
            BattleMonster target = allies.stream()
                    .min(Comparator.comparingDouble(BattleMonster::getHpPercent))
                    .orElseThrow();

            int healAmt = (int) (self.getAttack() * 0.8);
            target.heal(healAmt);
            ctx.addEvent(new TickEvent("HEAL", self.getUserCardId(),
                    target.getUserCardId(), healAmt, "group_heal", ctx.getCurrentTick()));

            int regen = Math.max(1, (int) (self.getAttack() * 0.2));
            target.applyStatus(StatusEffect.Type.REGEN, 3, regen);
            ctx.addEvent(new TickEvent("STATUS", self.getUserCardId(),
                    target.getUserCardId(), regen, "regen", ctx.getCurrentTick()));

            self.resetSkillCooldown(6);
            return;
        }

        // Otherwise attack weakest enemy
        List<BattleMonster> enemies = ctx.getEnemiesOf(self).stream()
                .filter(BattleMonster::isAlive).toList();
        if (enemies.isEmpty()) return;

        BattleMonster target = enemies.stream()
                .min(Comparator.comparingDouble(BattleMonster::getHpPercent))
                .orElseThrow();
        CombatResolver.basicAttack(self, target, ctx);
    }
}
