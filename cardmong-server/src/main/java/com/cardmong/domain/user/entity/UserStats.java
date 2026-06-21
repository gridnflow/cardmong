package com.cardmong.domain.user.entity;

import jakarta.persistence.*;
import lombok.*;
import org.hibernate.annotations.UpdateTimestamp;

import java.time.LocalDateTime;

@Entity
@Table(name = "user_stats")
@Getter
@NoArgsConstructor(access = AccessLevel.PROTECTED)
public class UserStats {

    @Id
    private Long userId;

    @MapsId
    @OneToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "user_id")
    private User user;

    private int level = 1;
    private long exp = 0;
    private long gold = 0;
    private int gem = 0;
    private int winCount = 0;
    private int loseCount = 0;
    private int ratingPoint = 1000;

    @UpdateTimestamp
    private LocalDateTime updatedAt;

    public static UserStats create(User user) {
        UserStats stats = new UserStats();
        stats.user = user;
        return stats;
    }

    public void addExp(int amount) {
        this.exp += amount;
        int newLevel = (int) (this.exp / 1000) + 1;
        if (newLevel > this.level) this.level = newLevel;
    }

    public void addGold(long amount) { this.gold += amount; }

    public void recordWin(int ratingChange) {
        this.winCount++;
        this.ratingPoint += ratingChange;
    }

    public void recordLose(int ratingChange) {
        this.loseCount++;
        this.ratingPoint = Math.max(0, this.ratingPoint - ratingChange);
    }
}
