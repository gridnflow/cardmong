package com.cardmong.domain.ranking.controller;

import com.cardmong.domain.ranking.dto.MyRankingResponse;
import com.cardmong.domain.ranking.dto.RankingEntryResponse;
import com.cardmong.domain.ranking.service.LeaderboardService;
import com.cardmong.global.response.ApiResponse;
import lombok.RequiredArgsConstructor;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/v1/ranking")
@RequiredArgsConstructor
public class RankingController {

    private final LeaderboardService leaderboardService;

    /** 글로벌 상위 랭킹 (공개). */
    @GetMapping("/top")
    public ApiResponse<List<RankingEntryResponse>> getTop(
            @RequestParam(defaultValue = "20") int limit) {
        return ApiResponse.ok(leaderboardService.getTop(Math.min(Math.max(limit, 1), 100)));
    }

    /** 내 순위와 주변 순위. */
    @GetMapping("/me")
    public ApiResponse<MyRankingResponse> getMyRanking(
            @AuthenticationPrincipal Long userId,
            @RequestParam(defaultValue = "3") int span) {
        return ApiResponse.ok(leaderboardService.getMyRanking(userId, Math.min(Math.max(span, 0), 10)));
    }
}
