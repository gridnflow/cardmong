package com.cardmong.domain.battle.engine.ai;

import com.cardmong.domain.battle.engine.BattleContext;
import com.cardmong.domain.battle.engine.BattleMonster;

public interface MonsterAI {
    void act(BattleMonster self, BattleContext ctx);
}
