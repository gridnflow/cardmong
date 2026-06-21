package com.cardmong.domain.card.controller;

import com.cardmong.domain.card.dto.CardResponse;
import com.cardmong.domain.card.dto.UserCardResponse;
import com.cardmong.domain.card.service.CardService;
import com.cardmong.global.response.ApiResponse;
import lombok.RequiredArgsConstructor;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/v1")
@RequiredArgsConstructor
public class CardController {

    private final CardService cardService;

    @GetMapping("/cards")
    public ApiResponse<List<CardResponse>> getAllCards() {
        return ApiResponse.ok(cardService.getAllCards());
    }

    @GetMapping("/users/me/cards")
    public ApiResponse<List<UserCardResponse>> getMyCards(
            @AuthenticationPrincipal Long userId) {
        return ApiResponse.ok(cardService.getMyCards(userId));
    }
}
