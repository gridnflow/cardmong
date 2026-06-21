package com.cardmong.domain.user.repository;

import com.cardmong.domain.user.entity.UserStats;
import org.springframework.data.jpa.repository.JpaRepository;

public interface UserStatsRepository extends JpaRepository<UserStats, Long> {
}
