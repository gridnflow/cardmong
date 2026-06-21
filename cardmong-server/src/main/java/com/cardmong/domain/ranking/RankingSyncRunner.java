package com.cardmong.domain.ranking;

import com.cardmong.domain.ranking.service.LeaderboardService;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.boot.ApplicationArguments;
import org.springframework.boot.ApplicationRunner;
import org.springframework.stereotype.Component;

/**
 * 서버 기동 시 MySQL의 전적을 Redis 리더보드(ZSET)로 한 번 동기화한다.
 * Redis가 비어 있거나(첫 부팅) 재시작 후에도 랭킹이 즉시 일관성을 갖도록 한다.
 * Redis 연결 실패가 서버 기동 자체를 막지 않도록 예외는 경고 로깅 후 무시한다.
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class RankingSyncRunner implements ApplicationRunner {

    private final LeaderboardService leaderboardService;

    @Override
    public void run(ApplicationArguments args) {
        try {
            long synced = leaderboardService.rebuildFromDatabase();
            log.info("[Ranking] Leaderboard synced from DB: {} players", synced);
        } catch (Exception e) {
            log.warn("[Ranking] Leaderboard sync skipped (is Redis up?): {}", e.getMessage());
        }
    }
}
