package com.cardmong.domain.battle.engine.ai;

import com.cardmong.domain.battle.engine.BattleContext;
import com.cardmong.domain.battle.engine.BattleContext.TickEvent;
import com.cardmong.domain.battle.engine.BattleMonster;
import com.cardmong.domain.battle.engine.CombatResolver;
import com.cardmong.domain.battle.engine.StatusEffect;

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
            // Shield bash: damage + stun, and raise a self shield
            CombatResolver.skillAttack(self, target, 1.2, "shield_bash", ctx);
            target.applyStun(3);
            ctx.addEvent(new TickEvent("STUN", self.getUserCardId(),
                    target.getUserCardId(), 3, "shield_bash", ctx.getCurrentTick()));

            int shield = Math.max(1, (int) (self.getMaxHp() * 0.15));
            self.applyStatus(StatusEffect.Type.SHIELD, 4, shield);
            ctx.addEvent(new TickEvent("STATUS", self.getUserCardId(),
                    self.getUserCardId(), shield, "shield", ctx.getCurrentTick()));

            self.resetSkillCooldown(8);
        } else {
            CombatResolver.basicAttack(self, target, ctx);
        }
    }
}
