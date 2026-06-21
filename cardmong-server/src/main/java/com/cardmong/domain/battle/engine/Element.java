package com.cardmong.domain.battle.engine;

/**
 * 속성과 상성. 5원소 순환: FIRE > WIND > EARTH > LIGHTNING > WATER > FIRE.
 * 각 속성은 정확히 하나를 강하게(×1.5), 하나에 약하게(×0.75) 작용하고
 * 나머지에는 보통(×1.0)이다. NEUTRAL은 항상 ×1.0.
 */
public enum Element {
    FIRE, WATER, EARTH, LIGHTNING, WIND, NEUTRAL;

    public static Element from(String raw) {
        if (raw == null) return NEUTRAL;
        try {
            return Element.valueOf(raw.trim().toUpperCase());
        } catch (IllegalArgumentException e) {
            return NEUTRAL;
        }
    }

    public double multiplierAgainst(Element target) {
        if (this == NEUTRAL || target == NEUTRAL) return 1.0;
        if (beats(this, target)) return 1.5;
        if (beats(target, this)) return 0.75;
        return 1.0;
    }

    private static boolean beats(Element a, Element b) {
        return switch (a) {
            case FIRE      -> b == WIND;
            case WIND      -> b == EARTH;
            case EARTH     -> b == LIGHTNING;
            case LIGHTNING -> b == WATER;
            case WATER     -> b == FIRE;
            default        -> false;
        };
    }
}
