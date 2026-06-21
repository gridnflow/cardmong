package com.cardmong.domain.deck.repository;

import com.cardmong.domain.deck.entity.Deck;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;

import java.util.List;
import java.util.Optional;

public interface DeckRepository extends JpaRepository<Deck, Integer> {

    @Query("SELECT d FROM Deck d JOIN FETCH d.deckCards dc JOIN FETCH dc.userCard uc JOIN FETCH uc.card WHERE d.user.id = :userId")
    List<Deck> findByUserIdWithCards(Long userId);

    Optional<Deck> findByIdAndUserId(Integer id, Long userId);
}
