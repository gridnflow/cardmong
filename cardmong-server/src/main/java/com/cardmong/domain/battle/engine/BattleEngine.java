package com.cardmong.domain.battle.engine;

import com.cardmong.domain.battle.engine.ai.AggressiveAI;
import com.cardmong.domain.battle.engine.ai.MonsterAI;
import com.cardmong.domain.battle.engine.ai.SupportAI;
import com.cardmong.domain.battle.engine.ai.TankAI;
import com.cardmong.domain.card.entity.Card;
import org.springframework.stereotype.Component;

import java.util.Comparator;
import java.util.List;

@Component
public class BattleEngine {

    private static final int MAX_TICKS = 600;

    public BattleResult simulate(BattleContext ctx) {
        for (int tick = 0; tick < MAX_TICKS && !ctx.isOver(); tick++) {
            ctx.nextTick();

            // Speed-ordered action sequence
            List<BattleMonster> all = sortBySpeed(ctx);

            for (BattleMonster m : all) {
                if (!m.isAlive()) continue;
                m.tickCooldowns();
                if (m.isStunned()) continue;

                MonsterAI ai = selectAI(m);
                ai.act(m, ctx);

                if (ctx.isOver()) break;
            }
        }

        return buildResult(ctx);
    }

    private List<BattleMonster> sortBySpeed(BattleContext ctx) {
        return java.util.stream.Stream
                .concat(ctx.getAttackers().stream(), ctx.getDefenders().stream())
                .filter(BattleMonster::isAlive)
                .sorted(Comparator.comparingInt(BattleMonster::getSpeed).reversed())
                .toList();
    }

    private MonsterAI selectAI(BattleMonster m) {
        return switch (m.getRole()) {
            case SUPPORT -> new SupportAI();
            case TANK    -> new TankAI();
            default      -> new AggressiveAI();
        };
    }

    private BattleResult buildResult(BattleContext ctx) {
        boolean attackersAlive = !ctx.aliveAttackers().isEmpty();
        boolean defendersAlive = !ctx.aliveDefenders().isEmpty();

        BattleResult.Outcome outcome;
        if (attackersAlive && !defendersAlive)      outcome = BattleResult.Outcome.ATTACKER_WIN;
        else if (!attackersAlive && defendersAlive) outcome = BattleResult.Outcome.DEFENDER_WIN;
        else                                         outcome = BattleResult.Outcome.DRAW;

        return new BattleResult(outcome, ctx.getCurrentTick(), ctx.getEvents());
    }
}
