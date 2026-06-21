package com.cardmong.domain.battle.engine;

import com.cardmong.domain.battle.engine.BattleContext.TickEvent;
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
    private final Element element;
    private final Team team;
    private final int[] position; // [row, col] in 5x5 grid

    private int hp;
    private final int maxHp;
    private final int attack;
    private final int defense;
    private final int speed;
    private final int attackRange;

    private final List<StatusEffect> statuses = new ArrayList<>();
    private int stunTicks = 0;
    private boolean dead = false;
    private int skillCooldown = 0;

    public BattleMonster(UserCard uc, Team team, int[] position) {
        Card c = uc.getCard();
        int lv = uc.getLevel();

        this.userCardId = uc.getId();
        this.cardName   = c.getName();
        this.role       = c.getRole();
        this.element    = Element.from(c.getElement());
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

    /** 보호막(SHIELD)으로 먼저 흡수한 뒤 남은 피해를 HP에 적용한다. */
    public void takeDamage(int dmg) {
        int remaining = dmg;
        for (StatusEffect s : statuses) {
            if (remaining <= 0) break;
            if (s.getType() == StatusEffect.Type.SHIELD && s.getMagnitude() > 0) {
                int absorb = Math.min(s.getMagnitude(), remaining);
                s.reduceShield(absorb);
                remaining -= absorb;
            }
        }
        this.hp = Math.max(0, this.hp - remaining);
        if (this.hp == 0) this.dead = true;
    }

    public void heal(int amount) {
        if (dead) return;
        this.hp = Math.min(this.maxHp, this.hp + amount);
    }

    public void applyStun(int ticks) {
        this.stunTicks = Math.max(this.stunTicks, ticks);
    }

    /** 같은 종류는 리프레시, 없으면 새로 부여. */
    public void applyStatus(StatusEffect.Type type, int ticks, int magnitude) {
        for (StatusEffect s : statuses) {
            if (s.getType() == type) {
                s.refresh(ticks, magnitude);
                return;
            }
        }
        statuses.add(new StatusEffect(type, ticks, magnitude));
    }

    /** 매 틱: 도트/회복 적용 후 지속시간 감소 및 만료 제거. */
    public void tickStatuses(BattleContext ctx) {
        if (dead || statuses.isEmpty()) return;
        for (StatusEffect s : new ArrayList<>(statuses)) {
            switch (s.getType()) {
                case BURN, POISON -> {
                    int d = s.getMagnitude();
                    takeDamage(d);
                    ctx.addEvent(new TickEvent("DOT", null, userCardId, d,
                            s.getType().name().toLowerCase(), ctx.getCurrentTick()));
                    if (dead) {
                        ctx.addEvent(new TickEvent("DEATH", null, userCardId, 0, null,
                                ctx.getCurrentTick()));
                    }
                }
                case REGEN -> {
                    int h = s.getMagnitude();
                    heal(h);
                    ctx.addEvent(new TickEvent("REGEN", null, userCardId, h, "regen",
                            ctx.getCurrentTick()));
                }
                default -> { /* SHIELD/CHILL/VULNERABLE: passive, no per-tick action */ }
            }
            s.decrement();
            if (dead) break;
        }
        statuses.removeIf(s -> s.isExpired()
                || (s.getType() == StatusEffect.Type.SHIELD && s.getMagnitude() <= 0));
    }

    /** CHILL 동안 가하는 피해 감소 배수. */
    public double outgoingMultiplier() {
        double m = 1.0;
        for (StatusEffect s : statuses) {
            if (s.getType() == StatusEffect.Type.CHILL) m *= 0.8;
        }
        return m;
    }

    /** VULNERABLE 동안 받는 피해 증가 배수. */
    public double incomingMultiplier() {
        double m = 1.0;
        for (StatusEffect s : statuses) {
            if (s.getType() == StatusEffect.Type.VULNERABLE) m *= 1.2;
        }
        return m;
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
