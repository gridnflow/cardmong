package com.cardmong.domain.battle.engine;

/**
 * 지속 상태이상/버프. 매 틱 처리된다.
 * <ul>
 *   <li>BURN / POISON: 매 틱 magnitude 피해(도트)</li>
 *   <li>REGEN: 매 틱 magnitude 회복</li>
 *   <li>SHIELD: magnitude만큼 피해 흡수(소진되면 제거)</li>
 *   <li>CHILL: 지속 동안 가하는 피해 ×0.8</li>
 *   <li>VULNERABLE: 지속 동안 받는 피해 ×1.2</li>
 * </ul>
 */
public class StatusEffect {

    public enum Type { BURN, POISON, REGEN, SHIELD, CHILL, VULNERABLE }

    private final Type type;
    private int remainingTicks;
    private int magnitude;

    public StatusEffect(Type type, int remainingTicks, int magnitude) {
        this.type = type;
        this.remainingTicks = remainingTicks;
        this.magnitude = magnitude;
    }

    public Type getType()        { return type; }
    public int getRemainingTicks() { return remainingTicks; }
    public int getMagnitude()    { return magnitude; }

    public void decrement() {
        if (remainingTicks > 0) remainingTicks--;
    }

    public boolean isExpired() {
        return remainingTicks <= 0;
    }

    public void reduceShield(int amount) {
        this.magnitude = Math.max(0, this.magnitude - amount);
    }

    /** 같은 종류가 다시 걸리면 더 강하고 긴 쪽으로 갱신(스택 대신 리프레시). */
    public void refresh(int ticks, int magnitude) {
        this.remainingTicks = Math.max(this.remainingTicks, ticks);
        this.magnitude      = Math.max(this.magnitude, magnitude);
    }
}
