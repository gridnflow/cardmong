package com.cardmong.domain.deck.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.Size;

import java.util.List;

public record DeckRequest(
        @NotBlank String name,
        @NotEmpty @Size(max = 5) List<Long> userCardIds
) {}
