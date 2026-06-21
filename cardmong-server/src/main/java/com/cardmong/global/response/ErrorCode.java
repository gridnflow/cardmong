package com.cardmong.global.response;

import lombok.Getter;
import lombok.RequiredArgsConstructor;

@Getter
@RequiredArgsConstructor
public enum ErrorCode {

    // Auth
    AUTH_TOKEN_MISSING("Token is missing."),
    AUTH_TOKEN_EXPIRED("Token has expired."),
    AUTH_UNAUTHORIZED("Unauthorized."),
    AUTH_INVALID_CREDENTIALS("Invalid email or password."),

    // User
    USER_NOT_FOUND("User not found."),
    USER_NICKNAME_DUPLICATE("Nickname already in use."),
    USER_EMAIL_DUPLICATE("Email already in use."),

    // Card
    CARD_NOT_FOUND("Card not found."),
    CARD_NOT_OWNED("You do not own this card."),
    CARD_EVOLVE_CONDITION_NOT_MET("Evolution conditions not met."),
    CARD_UPGRADE_MATERIAL_INSUFFICIENT("Insufficient upgrade materials."),

    // Deck
    DECK_NOT_FOUND("Deck not found."),
    DECK_CARD_LIMIT_EXCEEDED("Deck can contain up to 5 cards."),
    DECK_ENERGY_LIMIT_EXCEEDED("Energy limit exceeded."),
    DECK_ACCESS_DENIED("You do not have access to this deck."),

    // Battle
    BATTLE_NOT_FOUND("Battle not found."),
    BATTLE_DECK_NOT_SET("No deck selected for battle."),
    BATTLE_ALREADY_IN_PROGRESS("A battle is already in progress."),

    // Item
    ITEM_NOT_FOUND("Item not found."),
    ITEM_SLOT_MISMATCH("Item does not match the equipment slot."),

    // Server
    INTERNAL_SERVER_ERROR("Internal server error.");

    private final String message;
}
