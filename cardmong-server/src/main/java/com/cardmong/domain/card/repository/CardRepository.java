package com.cardmong.domain.card.repository;

import com.cardmong.domain.card.entity.Card;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;

public interface CardRepository extends JpaRepository<Card, Integer> {
    List<Card> findByIsActiveTrue();
}
