package com.cardmong.domain.ranking.dto;

import java.util.List;

/**
 * 내 순위 정보. me 가 null 이면 아직 랭킹에 등록되지 않은 상태(전적 없음).
 * neighbors 는 내 위/아래 인접 순위들(나 포함).
 */
public record MyRankingResponse(
        RankingEntryResponse me,
        long totalPlayers,
        List<RankingEntryResponse> neighbors
) {
}
