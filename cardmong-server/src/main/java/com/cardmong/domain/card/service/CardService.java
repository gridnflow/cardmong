package com.cardmong.domain.card.service;

import com.cardmong.domain.card.dto.CardResponse;
import com.cardmong.domain.card.dto.UserCardResponse;
import com.cardmong.domain.card.repository.CardRepository;
import com.cardmong.domain.card.repository.UserCardRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;

@Service
@RequiredArgsConstructor
public class CardService {

    private final CardRepository cardRepository;
    private final UserCardRepository userCardRepository;

    @Transactional(readOnly = true)
    public List<CardResponse> getAllCards() {
        return cardRepository.findByIsActiveTrue().stream()
                .map(CardResponse::from).toList();
    }

    @Transactional(readOnly = true)
    public List<UserCardResponse> getMyCards(Long userId) {
        return userCardRepository.findByUserIdWithCard(userId).stream()
                .map(UserCardResponse::from).toList();
    }
}
