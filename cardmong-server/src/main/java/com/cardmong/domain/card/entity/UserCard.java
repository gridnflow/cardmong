package com.cardmong.domain.card.entity;

import com.cardmong.domain.user.entity.User;
import jakarta.persistence.*;
import lombok.*;
import org.hibernate.annotations.CreationTimestamp;

import java.time.LocalDateTime;

@Entity
@Table(name = "user_cards")
@Getter
@NoArgsConstructor(access = AccessLevel.PROTECTED)
public class UserCard {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "user_id", nullable = false)
    private User user;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "card_id", nullable = false)
    private Card card;

    private int level = 1;
    private int exp = 0;
    private int upgradeCount = 0;
    private boolean isLocked = false;

    @CreationTimestamp
    private LocalDateTime obtainedAt;

    public static UserCard create(User user, Card card) {
        UserCard uc = new UserCard();
        uc.user = user;
        uc.card = card;
        return uc;
    }

    public void addExp(int amount) {
        this.exp += amount;
        int newLevel = (this.exp / 500) + 1;
        if (newLevel > this.level) this.level = newLevel;
    }
}
