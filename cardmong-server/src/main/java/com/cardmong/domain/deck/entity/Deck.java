package com.cardmong.domain.deck.entity;

import com.cardmong.domain.user.entity.User;
import jakarta.persistence.*;
import lombok.*;
import org.hibernate.annotations.CreationTimestamp;
import org.hibernate.annotations.UpdateTimestamp;

import java.time.LocalDateTime;
import java.util.ArrayList;
import java.util.List;

@Entity
@Table(name = "decks")
@Getter
@NoArgsConstructor(access = AccessLevel.PROTECTED)
public class Deck {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Integer id;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "user_id", nullable = false)
    private User user;

    @Column(nullable = false)
    private String name = "기본 덱";

    private boolean isMain = false;
    private int totalEnergy = 0;

    @OneToMany(mappedBy = "deck", cascade = CascadeType.ALL, orphanRemoval = true)
    private List<DeckCard> deckCards = new ArrayList<>();

    @CreationTimestamp private LocalDateTime createdAt;
    @UpdateTimestamp  private LocalDateTime updatedAt;

    public static Deck create(User user, String name) {
        Deck deck = new Deck();
        deck.user = user;
        deck.name = name;
        return deck;
    }

    public void updateName(String name) { this.name = name; }
    public void setMain(boolean main)   { this.isMain = main; }
    public void updateEnergy(int energy) { this.totalEnergy = energy; }
}
