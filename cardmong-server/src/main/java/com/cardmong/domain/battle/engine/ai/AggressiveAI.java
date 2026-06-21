package com.cardmong.domain.battle.engine.ai;

import com.cardmong.domain.battle.engine.BattleContext;
import com.cardmong.domain.battle.engine.BattleMonster;
import com.cardmong.domain.battle.engine.CombatResolver;

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
            CombatResolver.skillAttack(self, target, 1.8, "power_strike", ctx);
            self.resetSkillCooldown(5);
        } else {
            CombatResolver.basicAttack(self, target, ctx);
        }
    }
}
