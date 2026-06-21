package com.cardmong.domain.battle.engine;

import lombok.Getter;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;

@Getter
public class BattleContext {

    public record TickEvent(
            String type,           // ATTACK, SKILL, MOVE, HEAL, DEATH, STUN
            Long sourceId,
            Long targetId,
            int value,
            String extra,          // skill name or debuff name
            int tick
    ) {}

    private final List<BattleMonster> attackers;
    private final List<BattleMonster> defenders;
    private final List<TickEvent> events = new ArrayList<>();
    private int currentTick = 0;

    public BattleContext(List<BattleMonster> attackers, List<BattleMonster> defenders) {
        this.attackers = attackers;
        this.defenders = defenders;
    }

    public void addEvent(TickEvent event) { events.add(event); }

    public void nextTick() { currentTick++; }

    public List<BattleMonster> getEnemiesOf(BattleMonster m) {
        return m.getTeam() == BattleMonster.Team.ATTACKER ? defenders : attackers;
    }

    public List<BattleMonster> getAlliesOf(BattleMonster m) {
        return m.getTeam() == BattleMonster.Team.ATTACKER ? attackers : defenders;
    }

    public List<BattleMonster> aliveAttackers() {
        return attackers.stream().filter(BattleMonster::isAlive).toList();
    }

    public List<BattleMonster> aliveDefenders() {
        return defenders.stream().filter(BattleMonster::isAlive).toList();
    }

    public boolean isOver() {
        return aliveAttackers().isEmpty() || aliveDefenders().isEmpty();
    }
}
