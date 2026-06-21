package com.cardmong.domain.battle.repository;

import com.cardmong.domain.battle.entity.Battle;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;
import java.util.Optional;

public interface BattleRepository extends JpaRepository<Battle, Long> {

    Optional<Battle> findByIdAndAttackerId(Long id, Long attackerId);

    List<Battle> findTop10ByAttackerIdOrderByCreatedAtDesc(Long attackerId);
}
