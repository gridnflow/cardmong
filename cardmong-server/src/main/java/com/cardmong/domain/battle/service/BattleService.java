package com.cardmong.domain.battle.service;

import com.cardmong.domain.battle.dto.BattleResultResponse;
import com.cardmong.domain.battle.dto.BattleStartRequest;
import com.cardmong.domain.battle.engine.*;
import com.cardmong.domain.battle.entity.Battle;
import com.cardmong.domain.battle.repository.BattleRepository;
import com.cardmong.domain.card.entity.UserCard;
import com.cardmong.domain.deck.entity.Deck;
import com.cardmong.domain.deck.entity.DeckCard;
import com.cardmong.domain.deck.repository.DeckRepository;
import com.cardmong.domain.user.entity.User;
import com.cardmong.domain.user.entity.UserStats;
import com.cardmong.domain.user.repository.UserRepository;
import com.cardmong.domain.user.repository.UserStatsRepository;
import com.cardmong.global.exception.BusinessException;
import com.cardmong.global.response.ErrorCode;
import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.ArrayList;
import java.util.List;

@Service
@RequiredArgsConstructor
public class BattleService {

    private static final int WIN_EXP    = 100;
    private static final int LOSE_EXP   =  30;
    private static final int WIN_GOLD   = 200;
    private static final int LOSE_GOLD  =  50;
    private static final int WIN_RATING = +25;
    private static final int LOSE_RATING = -15;

    private final BattleEngine       battleEngine;
    private final BattleRepository   battleRepository;
    private final DeckRepository     deckRepository;
    private final UserRepository     userRepository;
    private final UserStatsRepository userStatsRepository;
    private final ObjectMapper       objectMapper;

    @Transactional
    public BattleResultResponse startBattle(Long attackerUserId, BattleStartRequest request) {
        Deck attackerDeck = deckRepository.findByIdAndUserId(request.deckId(), attackerUserId)
                .orElseThrow(() -> new BusinessException(ErrorCode.DECK_NOT_FOUND));

        User attacker = userRepository.getReferenceById(attackerUserId);

        // Defender: another player or AI stub using same deck
        User defender = request.opponentUserId() != null
                ? userRepository.findById(request.opponentUserId())
                      .orElseThrow(() -> new BusinessException(ErrorCode.USER_NOT_FOUND))
                : attacker;

        Deck defenderDeck = deckRepository
                .findByUserIdWithCards(defender.getId())
                .stream().findFirst()
                .orElse(attackerDeck); // AI fallback: mirror deck

        // Build monsters
        List<BattleMonster> attackerMonsters = toMonsters(attackerDeck, BattleMonster.Team.ATTACKER);
        List<BattleMonster> defenderMonsters = toMonsters(defenderDeck, BattleMonster.Team.DEFENDER);

        BattleContext ctx = new BattleContext(attackerMonsters, defenderMonsters);
        BattleResult  result = battleEngine.simulate(ctx);

        // Persist
        Battle battle = Battle.create(attacker, defender);
        String logJson = serializeLog(result.events());
        boolean attackerWon = result.outcome() == BattleResult.Outcome.ATTACKER_WIN;
        boolean draw        = result.outcome() == BattleResult.Outcome.DRAW;

        int ratingChange = draw ? 0 : (attackerWon ? WIN_RATING : LOSE_RATING);
        int expGained    = attackerWon ? WIN_EXP  : LOSE_EXP;
        int goldGained   = attackerWon ? WIN_GOLD : LOSE_GOLD;

        battle.complete(
                mapStatus(result.outcome()),
                attackerWon ? attackerUserId : defender.getId(),
                ratingChange,
                draw ? 0 : (attackerWon ? LOSE_RATING : WIN_RATING),
                result.durationTicks(),
                logJson
        );
        battleRepository.save(battle);

        // Update stats
        UserStats stats = userStatsRepository.findById(attackerUserId)
                .orElseThrow(() -> new BusinessException(ErrorCode.USER_NOT_FOUND));
        stats.addExp(expGained);
        stats.addGold(goldGained);
        if (attackerWon) stats.recordWin(WIN_RATING); else stats.recordLose(-LOSE_RATING);

        return new BattleResultResponse(battle.getId(), result.outcome(),
                result.durationTicks(), ratingChange, expGained, goldGained, result.events());
    }

    @Transactional(readOnly = true)
    public BattleResultResponse getBattleResult(Long userId, Long battleId) {
        Battle battle = battleRepository.findByIdAndAttackerId(battleId, userId)
                .orElseThrow(() -> new BusinessException(ErrorCode.BATTLE_NOT_FOUND));

        List<BattleContext.TickEvent> events = deserializeLog(battle.getBattleLogJson());
        boolean attackerWon = battle.getStatus() == Battle.BattleStatus.PLAYER_WIN;
        boolean draw        = battle.getStatus() == Battle.BattleStatus.DRAW;

        BattleResult.Outcome outcome = draw ? BattleResult.Outcome.DRAW
                : (attackerWon ? BattleResult.Outcome.ATTACKER_WIN : BattleResult.Outcome.DEFENDER_WIN);

        return new BattleResultResponse(battle.getId(), outcome,
                battle.getDurationTicks(), battle.getAttackerRatingChange(),
                attackerWon ? WIN_EXP : LOSE_EXP,
                attackerWon ? WIN_GOLD : LOSE_GOLD,
                events);
    }

    private List<BattleMonster> toMonsters(Deck deck, BattleMonster.Team team) {
        List<BattleMonster> monsters = new ArrayList<>();
        int col = team == BattleMonster.Team.ATTACKER ? 0 : 4;
        int row = 0;
        for (DeckCard dc : deck.getDeckCards()) {
            monsters.add(new BattleMonster(dc.getUserCard(), team, new int[]{row++, col}));
        }
        return monsters;
    }

    private Battle.BattleStatus mapStatus(BattleResult.Outcome outcome) {
        return switch (outcome) {
            case ATTACKER_WIN -> Battle.BattleStatus.PLAYER_WIN;
            case DEFENDER_WIN -> Battle.BattleStatus.PLAYER_LOSE;
            case DRAW         -> Battle.BattleStatus.DRAW;
        };
    }

    private String serializeLog(List<BattleContext.TickEvent> events) {
        try { return objectMapper.writeValueAsString(events); }
        catch (JsonProcessingException e) { return "[]"; }
    }

    private List<BattleContext.TickEvent> deserializeLog(String json) {
        try {
            return objectMapper.readValue(json,
                    objectMapper.getTypeFactory().constructCollectionType(
                            List.class, BattleContext.TickEvent.class));
        } catch (Exception e) { return List.of(); }
    }
}
