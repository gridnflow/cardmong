package com.cardmong.domain.card.repository;

import com.cardmong.domain.card.entity.UserCard;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;

import java.util.List;
import java.util.Optional;

public interface UserCardRepository extends JpaRepository<UserCard, Long> {

    @Query("SELECT uc FROM UserCard uc JOIN FETCH uc.card WHERE uc.user.id = :userId")
    List<UserCard> findByUserIdWithCard(Long userId);

    Optional<UserCard> findByIdAndUserId(Long id, Long userId);
}
