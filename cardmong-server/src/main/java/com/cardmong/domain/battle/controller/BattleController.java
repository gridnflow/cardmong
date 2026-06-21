package com.cardmong.domain.battle.controller;

import com.cardmong.domain.battle.dto.BattleResultResponse;
import com.cardmong.domain.battle.dto.BattleStartRequest;
import com.cardmong.domain.battle.service.BattleService;
import com.cardmong.global.response.ApiResponse;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/v1/battles")
@RequiredArgsConstructor
public class BattleController {

    private final BattleService battleService;

    @PostMapping
    public ApiResponse<BattleResultResponse> startBattle(
            @AuthenticationPrincipal Long userId,
            @Valid @RequestBody BattleStartRequest request) {
        return ApiResponse.ok(battleService.startBattle(userId, request));
    }

    @GetMapping("/{battleId}")
    public ApiResponse<BattleResultResponse> getBattleResult(
            @AuthenticationPrincipal Long userId,
            @PathVariable Long battleId) {
        return ApiResponse.ok(battleService.getBattleResult(userId, battleId));
    }
}
