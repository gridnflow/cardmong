package com.cardmong.domain.ranking.dto;

/**
 * 리더보드의 한 줄. 순위(rank)는 Redis Sorted Set에서, 표시용 상세 정보는
 * MySQL(User / UserStats)에서 가져와 조합한다.
 */
public record RankingEntryResponse(
        long rank,
        long userId,
        String nickname,
        int level,
        int ratingPoint,
        int winCount,
        int loseCount
) {
}
