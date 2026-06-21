package com.cardmong.domain.battle.engine;

import com.cardmong.domain.card.entity.Card;
import com.cardmong.domain.card.entity.UserCard;
import lombok.Getter;

import java.util.ArrayList;
import java.util.List;

@Getter
public class BattleMonster {

    public enum Team { ATTACKER, DEFENDER }

    private final Long userCardId;
    private final String cardName;
    private final Card.Role role;
    private final Team team;
    private final int[] position; // [row, col] in 5x5 grid

    private int hp;
    private final int maxHp;
    private final int attack;
    private final int defense;
    private final int speed;
    private final int attackRange;

    private final List<String> debuffs = new ArrayList<>();
    private int stunTicks = 0;
    private boolean dead = false;
    private int skillCooldown = 0;

    public BattleMonster(UserCard uc, Team team, int[] position) {
        Card c = uc.getCard();
        int lv = uc.getLevel();

        this.userCardId = uc.getId();
        this.cardName   = c.getName();
        this.role       = c.getRole();
        this.team       = team;
        this.position   = position;

        int hpScale     = (int) (c.getBaseHp()     * (1 + 0.05 * lv));
        int atkScale    = (int) (c.getBaseAttack()  * (1 + 0.05 * lv));
        int defScale    = (int) (c.getBaseDefense() * (1 + 0.05 * lv));

        this.maxHp      = hpScale;
        this.hp         = hpScale;
        this.attack     = atkScale;
        this.defense    = defScale;
        this.speed      = c.getBaseSpeed();
        this.attackRange = c.getRole() == Card.Role.MAGE ? 3 : 1;
    }

    public void takeDamage(int dmg) {
        this.hp = Math.max(0, this.hp - dmg);
        if (this.hp == 0) this.dead = true;
    }

    public void heal(int amount) {
        this.hp = Math.min(this.maxHp, this.hp + amount);
    }

    public void applyStun(int ticks) {
        this.stunTicks = Math.max(this.stunTicks, ticks);
    }

    public void addDebuff(String debuff) {
        if (!debuffs.contains(debuff)) debuffs.add(debuff);
    }

    public void tickCooldowns() {
        if (stunTicks > 0) stunTicks--;
        if (skillCooldown > 0) skillCooldown--;
    }

    public boolean isStunned() { return stunTicks > 0; }

    public void resetSkillCooldown(int ticks) { this.skillCooldown = ticks; }

    public boolean canUseSkill() { return skillCooldown == 0; }

    public boolean isAlive() { return !dead; }

    public double getHpPercent() { return (double) hp / maxHp; }
}
