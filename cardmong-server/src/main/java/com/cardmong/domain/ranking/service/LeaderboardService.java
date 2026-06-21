package com.cardmong.domain.ranking.service;

import com.cardmong.domain.ranking.dto.MyRankingResponse;
import com.cardmong.domain.ranking.dto.RankingEntryResponse;
import com.cardmong.domain.user.entity.User;
import com.cardmong.domain.user.entity.UserStats;
import com.cardmong.domain.user.repository.UserRepository;
import com.cardmong.domain.user.repository.UserStatsRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.data.redis.core.DefaultTypedTuple;
import org.springframework.data.redis.core.StringRedisTemplate;
import org.springframework.data.redis.core.ZSetOperations;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.function.Function;
import java.util.stream.Collectors;

/**
 * Redis Sorted Set 기반 실시간 글로벌 리더보드.
 *
 * <p>설계: MySQL(user_stats.ratingPoint)이 영속 source of truth, Redis ZSET은 빠른 순위
 * 조회를 위한 파생 인덱스다. ZSET은 회원당 (member=userId, score=ratingPoint)을 보관해
 * 순위/구간 조회를 O(log N)으로 처리하고, 닉네임·레벨 등 표시용 상세는 조회된 페이지의
 * userId 들에 대해서만 MySQL에서 일괄 로딩한다.
 */
@Service
@RequiredArgsConstructor
public class LeaderboardService {

    public static final String KEY = "ranking:global";

    private final StringRedisTemplate redis;
    private final UserRepository userRepository;
    private final UserStatsRepository userStatsRepository;

    private ZSetOperations<String, String> zset() {
        return redis.opsForZSet();
    }

    /** 전투 결과 등으로 레이팅이 바뀌면 호출. ZADD는 절대값으로 갱신되어 멱등하다. */
    public void updateScore(Long userId, int ratingPoint) {
        zset().add(KEY, userId.toString(), ratingPoint);
    }

    public long totalPlayers() {
        Long count = zset().zCard(KEY);
        return count == null ? 0L : count;
    }

    /** 상위 limit명 (1위부터). */
    @Transactional(readOnly = true)
    public List<RankingEntryResponse> getTop(int limit) {
        int end = Math.max(0, limit - 1);
        Set<String> ids = zset().reverseRange(KEY, 0, end);
        return buildEntries(toLongs(ids), 1L);
    }

    /** 내 순위와 위/아래 span명. 랭킹 미등록이면 me=null. */
    @Transactional(readOnly = true)
    public MyRankingResponse getMyRanking(Long userId, int span) {
        Long rank = zset().reverseRank(KEY, userId.toString());
        long total = totalPlayers();
        if (rank == null) {
            return new MyRankingResponse(null, total, List.of());
        }
        long start = Math.max(0L, rank - span);
        long end = rank + span;
        Set<String> ids = zset().reverseRange(KEY, start, end);
        List<RankingEntryResponse> neighbors = buildEntries(toLongs(ids), start + 1);
        RankingEntryResponse me = neighbors.stream()
                .filter(e -> e.userId() == userId.longValue())
                .findFirst()
                .orElse(null);
        return new MyRankingResponse(me, total, neighbors);
    }

    /** MySQL의 모든 전적을 ZSET으로 재구성한다(서버 기동 시 동기화). */
    @Transactional(readOnly = true)
    public long rebuildFromDatabase() {
        List<UserStats> all = userStatsRepository.findAll();
        redis.delete(KEY);
        if (all.isEmpty()) {
            return 0L;
        }
        Set<ZSetOperations.TypedTuple<String>> tuples = all.stream()
                .map(s -> (ZSetOperations.TypedTuple<String>) new DefaultTypedTuple<>(
                        String.valueOf(s.getUserId()), (double) s.getRatingPoint()))
                .collect(Collectors.toSet());
        zset().add(KEY, tuples);
        return all.size();
    }

    // reverseRange는 순서를 보존하는 LinkedHashSet을 반환한다.
    private List<Long> toLongs(Set<String> ids) {
        if (ids == null || ids.isEmpty()) {
            return List.of();
        }
        return ids.stream().map(Long::valueOf).toList();
    }

    private List<RankingEntryResponse> buildEntries(List<Long> orderedIds, long startRank) {
        if (orderedIds.isEmpty()) {
            return List.of();
        }
        Map<Long, User> users = userRepository.findAllById(orderedIds).stream()
                .collect(Collectors.toMap(User::getId, Function.identity()));
        Map<Long, UserStats> stats = userStatsRepository.findAllById(orderedIds).stream()
                .collect(Collectors.toMap(UserStats::getUserId, Function.identity()));

        List<RankingEntryResponse> result = new ArrayList<>(orderedIds.size());
        long rank = startRank;
        for (Long id : orderedIds) {
            User user = users.get(id);
            UserStats stat = stats.get(id);
            if (user == null || stat == null) {
                rank++; // MySQL에서 사라진 회원은 건너뛰되 순위 번호는 유지
                continue;
            }
            result.add(new RankingEntryResponse(
                    rank++, id, user.getNickname(), stat.getLevel(),
                    stat.getRatingPoint(), stat.getWinCount(), stat.getLoseCount()));
        }
        return result;
    }
}
