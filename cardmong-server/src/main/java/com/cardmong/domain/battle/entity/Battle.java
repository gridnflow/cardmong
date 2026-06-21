package com.cardmong.domain.battle.entity;

import com.cardmong.domain.user.entity.User;
import jakarta.persistence.*;
import lombok.*;
import org.hibernate.annotations.CreationTimestamp;

import java.time.LocalDateTime;

@Entity
@Table(name = "battles")
@Getter
@NoArgsConstructor(access = AccessLevel.PROTECTED)
public class Battle {

    public enum BattleStatus { IN_PROGRESS, PLAYER_WIN, PLAYER_LOSE, DRAW }

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "attacker_id", nullable = false)
    private User attacker;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "defender_id")
    private User defender;

    @Enumerated(EnumType.STRING)
    private BattleStatus status = BattleStatus.IN_PROGRESS;

    private Long winnerUserId;
    private int attackerRatingChange = 0;
    private int defenderRatingChange = 0;
    private int durationTicks = 0;

    @Column(columnDefinition = "LONGTEXT")
    private String battleLogJson;

    @CreationTimestamp
    private LocalDateTime createdAt;

    public static Battle create(User attacker, User defender) {
        Battle b = new Battle();
        b.attacker = attacker;
        b.defender = defender;
        return b;
    }

    public void complete(BattleStatus status, Long winnerUserId,
                         int attackerRatingChange, int defenderRatingChange,
                         int durationTicks, String battleLogJson) {
        this.status = status;
        this.winnerUserId = winnerUserId;
        this.attackerRatingChange = attackerRatingChange;
        this.defenderRatingChange = defenderRatingChange;
        this.durationTicks = durationTicks;
        this.battleLogJson = battleLogJson;
    }
}
