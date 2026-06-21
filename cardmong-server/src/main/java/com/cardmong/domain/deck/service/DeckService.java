package com.cardmong.domain.deck.service;

import com.cardmong.domain.card.entity.UserCard;
import com.cardmong.domain.card.repository.UserCardRepository;
import com.cardmong.domain.deck.dto.DeckRequest;
import com.cardmong.domain.deck.dto.DeckResponse;
import com.cardmong.domain.deck.entity.Deck;
import com.cardmong.domain.deck.entity.DeckCard;
import com.cardmong.domain.deck.repository.DeckRepository;
import com.cardmong.domain.user.entity.User;
import com.cardmong.domain.user.repository.UserRepository;
import com.cardmong.global.exception.BusinessException;
import com.cardmong.global.response.ErrorCode;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.ArrayList;
import java.util.List;

@Service
@RequiredArgsConstructor
public class DeckService {

    private final DeckRepository deckRepository;
    private final UserCardRepository userCardRepository;
    private final UserRepository userRepository;

    @Transactional(readOnly = true)
    public List<DeckResponse> getMyDecks(Long userId) {
        return deckRepository.findByUserIdWithCards(userId).stream()
                .map(DeckResponse::from).toList();
    }

    @Transactional
    public DeckResponse createDeck(Long userId, DeckRequest request) {
        if (request.userCardIds().size() > 5)
            throw new BusinessException(ErrorCode.DECK_CARD_LIMIT_EXCEEDED);

        User user = userRepository.getReferenceById(userId);
        Deck deck = deckRepository.save(Deck.create(user, request.name()));

        List<DeckCard> deckCards = buildDeckCards(deck, userId, request.userCardIds());
        deck.getDeckCards().addAll(deckCards);

        int totalEnergy = deckCards.stream()
                .mapToInt(dc -> dc.getUserCard().getCard().getEnergyCost()).sum();
        deck.updateEnergy(totalEnergy);

        return DeckResponse.from(deck);
    }

    @Transactional
    public DeckResponse updateDeck(Long userId, Integer deckId, DeckRequest request) {
        Deck deck = deckRepository.findByIdAndUserId(deckId, userId)
                .orElseThrow(() -> new BusinessException(ErrorCode.DECK_NOT_FOUND));

        deck.getDeckCards().clear();
        deck.updateName(request.name());

        List<DeckCard> deckCards = buildDeckCards(deck, userId, request.userCardIds());
        deck.getDeckCards().addAll(deckCards);

        int totalEnergy = deckCards.stream()
                .mapToInt(dc -> dc.getUserCard().getCard().getEnergyCost()).sum();
        deck.updateEnergy(totalEnergy);

        return DeckResponse.from(deck);
    }

    @Transactional
    public void deleteDeck(Long userId, Integer deckId) {
        Deck deck = deckRepository.findByIdAndUserId(deckId, userId)
                .orElseThrow(() -> new BusinessException(ErrorCode.DECK_NOT_FOUND));
        deckRepository.delete(deck);
    }

    private List<DeckCard> buildDeckCards(Deck deck, Long userId, List<Long> userCardIds) {
        List<DeckCard> deckCards = new ArrayList<>();
        for (int i = 0; i < userCardIds.size(); i++) {
            UserCard uc = userCardRepository.findByIdAndUserId(userCardIds.get(i), userId)
                    .orElseThrow(() -> new BusinessException(ErrorCode.CARD_NOT_OWNED));
            deckCards.add(DeckCard.create(deck, uc, i));
        }
        return deckCards;
    }
}
