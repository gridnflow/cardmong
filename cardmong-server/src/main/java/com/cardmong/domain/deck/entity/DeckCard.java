package com.cardmong.domain.deck.entity;

import com.cardmong.domain.card.entity.UserCard;
import jakarta.persistence.*;
import lombok.*;

@Entity
@Table(name = "deck_cards")
@Getter
@NoArgsConstructor(access = AccessLevel.PROTECTED)
public class DeckCard {

    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "deck_id", nullable = false)
    private Deck deck;

    @ManyToOne(fetch = FetchType.LAZY)
    @JoinColumn(name = "user_card_id", nullable = false)
    private UserCard userCard;

    private int slotIndex;

    public static DeckCard create(Deck deck, UserCard userCard, int slotIndex) {
        DeckCard dc = new DeckCard();
        dc.deck      = deck;
        dc.userCard  = userCard;
        dc.slotIndex = slotIndex;
        return dc;
    }
}
