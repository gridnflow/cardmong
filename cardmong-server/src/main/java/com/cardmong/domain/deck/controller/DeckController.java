package com.cardmong.domain.deck.controller;

import com.cardmong.domain.deck.dto.DeckRequest;
import com.cardmong.domain.deck.dto.DeckResponse;
import com.cardmong.domain.deck.service.DeckService;
import com.cardmong.global.response.ApiResponse;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/v1/users/me/decks")
@RequiredArgsConstructor
public class DeckController {

    private final DeckService deckService;

    @GetMapping
    public ApiResponse<List<DeckResponse>> getMyDecks(@AuthenticationPrincipal Long userId) {
        return ApiResponse.ok(deckService.getMyDecks(userId));
    }

    @PostMapping
    public ApiResponse<DeckResponse> createDeck(
            @AuthenticationPrincipal Long userId,
            @Valid @RequestBody DeckRequest request) {
        return ApiResponse.ok(deckService.createDeck(userId, request));
    }

    @PutMapping("/{deckId}")
    public ApiResponse<DeckResponse> updateDeck(
            @AuthenticationPrincipal Long userId,
            @PathVariable Integer deckId,
            @Valid @RequestBody DeckRequest request) {
        return ApiResponse.ok(deckService.updateDeck(userId, deckId, request));
    }

    @DeleteMapping("/{deckId}")
    public ApiResponse<Void> deleteDeck(
            @AuthenticationPrincipal Long userId,
            @PathVariable Integer deckId) {
        deckService.deleteDeck(userId, deckId);
        return ApiResponse.ok(null);
    }
}
