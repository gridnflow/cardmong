package com.cardmong.domain.card.entity;

import jakarta.persistence.*;
import lombok.*;

@Entity
@Table(name = "cards")
@Getter
@NoArgsConstructor(access = AccessLevel.PROTECTED)
public class Card {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Integer id;

    @Column(nullable = false)
    private String name;

    private String description;

    @Column(nullable = false)
    private String element;

    @Enumerated(EnumType.STRING)
    @Column(nullable = false)
    private Rarity rarity;

    @Enumerated(EnumType.STRING)
    @Column(nullable = false)
    private Role role;

    private int energyCost = 3;
    private int baseHp;
    private int baseAttack;
    private int baseDefense;
    private int baseSpeed;
    private double baseCritChance = 5.0;
    private double baseCritDamage = 150.0;
    private int evolutionStage = 1;
    private boolean isActive = true;

    public enum Rarity  { COMMON, RARE, EPIC, LEGENDARY, MYTHIC, ANCIENT }
    public enum Role    { TANK, HEALER, SUPPORT, ASSASSIN, MAGE, BRUISER }
}
