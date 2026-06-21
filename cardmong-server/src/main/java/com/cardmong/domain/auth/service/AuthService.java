package com.cardmong.domain.auth.service;

import com.cardmong.domain.auth.dto.LoginRequest;
import com.cardmong.domain.auth.dto.RegisterRequest;
import com.cardmong.domain.auth.dto.TokenResponse;
import com.cardmong.domain.user.entity.User;
import com.cardmong.domain.user.entity.UserStats;
import com.cardmong.domain.user.repository.UserRepository;
import com.cardmong.domain.user.repository.UserStatsRepository;
import com.cardmong.global.exception.BusinessException;
import com.cardmong.global.jwt.JwtProvider;
import com.cardmong.global.response.ErrorCode;
import lombok.RequiredArgsConstructor;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
@RequiredArgsConstructor
public class AuthService {

    private final UserRepository userRepository;
    private final UserStatsRepository userStatsRepository;
    private final JwtProvider jwtProvider;
    private final PasswordEncoder passwordEncoder;

    @Transactional
    public TokenResponse register(RegisterRequest request) {
        if (userRepository.existsByEmail(request.email()))
            throw new BusinessException(ErrorCode.USER_EMAIL_DUPLICATE);

        if (userRepository.existsByNickname(request.nickname()))
            throw new BusinessException(ErrorCode.USER_NICKNAME_DUPLICATE);

        User user = userRepository.save(
                User.create(request.email(), request.nickname(),
                        passwordEncoder.encode(request.password()))
        );

        userStatsRepository.save(UserStats.create(user));

        return buildTokenResponse(user);
    }

    @Transactional(readOnly = true)
    public TokenResponse login(LoginRequest request) {
        User user = userRepository.findByEmail(request.email())
                .orElseThrow(() -> new BusinessException(ErrorCode.AUTH_INVALID_CREDENTIALS));

        if (!passwordEncoder.matches(request.password(), user.getPasswordHash()))
            throw new BusinessException(ErrorCode.AUTH_INVALID_CREDENTIALS);

        return buildTokenResponse(user);
    }

    private TokenResponse buildTokenResponse(User user) {
        return new TokenResponse(
                user.getId(),
                user.getNickname(),
                jwtProvider.createAccessToken(user.getId()),
                jwtProvider.createRefreshToken(user.getId()),
                3600
        );
    }
}
