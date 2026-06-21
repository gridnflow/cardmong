package com.cardmong.domain.auth.controller;

import com.cardmong.domain.auth.dto.LoginRequest;
import com.cardmong.domain.auth.dto.RegisterRequest;
import com.cardmong.domain.auth.dto.TokenResponse;
import com.cardmong.domain.auth.service.AuthService;
import com.cardmong.global.response.ApiResponse;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/v1/auth")
@RequiredArgsConstructor
public class AuthController {

    private final AuthService authService;

    @PostMapping("/register")
    public ApiResponse<TokenResponse> register(@Valid @RequestBody RegisterRequest request) {
        return ApiResponse.ok(authService.register(request));
    }

    @PostMapping("/login")
    public ApiResponse<TokenResponse> login(@Valid @RequestBody LoginRequest request) {
        return ApiResponse.ok(authService.login(request));
    }
}
