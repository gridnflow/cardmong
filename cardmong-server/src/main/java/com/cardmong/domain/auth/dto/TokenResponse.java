package com.cardmong.domain.auth.dto;

public record TokenResponse(
        Long userId,
        String nickname,
        String accessToken,
        String refreshToken,
        int expiresIn
) {}
