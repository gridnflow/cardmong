# DB 스키마 설계

## 도메인 구조

```
1. 유저          users, user_stats
2. 카드          cards, user_cards
3. 덱            decks, deck_cards
4. 몬스터        monsters, monster_skills
5. 스킬          skills, skill_effects
6. 전투          battles, battle_logs
7. 아이템/장비   items, user_items, monster_equipment
8. 랭킹          rankings, seasons
9. 보상          reward_logs
```

## 테이블 관계

```
users
├── user_stats          (1:1)
├── user_cards          (1:N)  → cards
│   └── monster_equipment (1:N) → user_items
├── decks               (1:N)
│   └── deck_cards      (1:N)  → user_cards
├── battles             (1:N)  → battle_logs
├── rankings            (1:N)  → seasons
├── user_items          (1:N)  → items
└── reward_logs         (1:N)

cards
├── card_skills         (1:N)  → skills
├── element_relations          → elements
└── evolves_from_id            → cards (자기참조)
```

---

## SQL 스키마

### 1. 유저

```sql
CREATE TABLE users (
    id            BIGINT       UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    email         VARCHAR(255) NOT NULL UNIQUE,
    nickname      VARCHAR(50)  NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    status        ENUM('ACTIVE', 'BANNED', 'DORMANT') NOT NULL DEFAULT 'ACTIVE',
    created_at    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

CREATE TABLE user_stats (
    user_id       BIGINT   UNSIGNED PRIMARY KEY,
    level         INT      NOT NULL DEFAULT 1,
    exp           BIGINT   NOT NULL DEFAULT 0,
    gold          BIGINT   NOT NULL DEFAULT 0,
    gem           INT      NOT NULL DEFAULT 0,
    win_count     INT      NOT NULL DEFAULT 0,
    lose_count    INT      NOT NULL DEFAULT 0,
    rating_point  INT      NOT NULL DEFAULT 1000,
    updated_at    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_user_stats_user FOREIGN KEY (user_id) REFERENCES users(id)
);
```

### 2. 카드 (마스터 데이터)

```sql
CREATE TABLE elements (
    id    TINYINT     UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    name  VARCHAR(20) NOT NULL UNIQUE
);

CREATE TABLE cards (
    id                 INT          UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    name               VARCHAR(100) NOT NULL,
    description        TEXT,
    element_id         TINYINT      UNSIGNED NOT NULL,
    rarity             ENUM('COMMON','RARE','EPIC','LEGENDARY','MYTHIC','ANCIENT') NOT NULL,
    role               ENUM('TANK','HEALER','SUPPORT','ASSASSIN','MAGE','BRUISER') NOT NULL,
    energy_cost        TINYINT      NOT NULL DEFAULT 3,
    base_hp            INT          NOT NULL,
    base_attack        INT          NOT NULL,
    base_defense       INT          NOT NULL,
    base_speed         INT          NOT NULL,
    base_crit_chance   DECIMAL(5,2) NOT NULL DEFAULT 5.00,
    base_crit_damage   DECIMAL(5,2) NOT NULL DEFAULT 150.00,
    evolution_stage    TINYINT      NOT NULL DEFAULT 1,
    evolves_from_id    INT          UNSIGNED DEFAULT NULL,
    is_active          BOOLEAN      NOT NULL DEFAULT TRUE,
    CONSTRAINT fk_card_element  FOREIGN KEY (element_id)      REFERENCES elements(id),
    CONSTRAINT fk_card_evolves  FOREIGN KEY (evolves_from_id) REFERENCES cards(id)
);

CREATE TABLE user_cards (
    id            BIGINT   UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id       BIGINT   UNSIGNED NOT NULL,
    card_id       INT      UNSIGNED NOT NULL,
    level         TINYINT  NOT NULL DEFAULT 1,
    exp           INT      NOT NULL DEFAULT 0,
    upgrade_count TINYINT  NOT NULL DEFAULT 0,
    is_locked     BOOLEAN  NOT NULL DEFAULT FALSE,
    obtained_at   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_uc_user FOREIGN KEY (user_id) REFERENCES users(id),
    CONSTRAINT fk_uc_card FOREIGN KEY (card_id) REFERENCES cards(id)
);
```

### 3. 덱

```sql
CREATE TABLE decks (
    id           INT      UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id      BIGINT   UNSIGNED NOT NULL,
    name         VARCHAR(50) NOT NULL DEFAULT '기본 덱',
    is_main      BOOLEAN  NOT NULL DEFAULT FALSE,
    total_energy INT      NOT NULL DEFAULT 0,
    created_at   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_deck_user FOREIGN KEY (user_id) REFERENCES users(id)
);

CREATE TABLE deck_cards (
    deck_id      INT    UNSIGNED NOT NULL,
    user_card_id BIGINT UNSIGNED NOT NULL,
    slot_index   TINYINT NOT NULL,
    PRIMARY KEY (deck_id, slot_index),
    CONSTRAINT fk_dc_deck      FOREIGN KEY (deck_id)      REFERENCES decks(id) ON DELETE CASCADE,
    CONSTRAINT fk_dc_user_card FOREIGN KEY (user_card_id) REFERENCES user_cards(id)
);
```

### 4. 스킬

```sql
CREATE TABLE skills (
    id           INT          UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    name         VARCHAR(100) NOT NULL,
    description  TEXT,
    skill_type   ENUM('DAMAGE','HEAL','SHIELD','SUMMON','BUFF','DEBUFF','CROWD_CONTROL','TRANSFORM') NOT NULL,
    target_type  ENUM('SINGLE','AREA','SELF','ALL_ENEMIES','ALL_ALLIES') NOT NULL,
    mana_cost    INT          NOT NULL DEFAULT 0,
    cooldown     DECIMAL(4,1) NOT NULL DEFAULT 0.0,
    range        DECIMAL(4,1) NOT NULL DEFAULT 1.0,
    base_value   INT          NOT NULL DEFAULT 0,
    effect_type  VARCHAR(50)  DEFAULT NULL
);

CREATE TABLE card_skills (
    card_id      INT     UNSIGNED NOT NULL,
    skill_id     INT     UNSIGNED NOT NULL,
    skill_slot   ENUM('PASSIVE','NORMAL','ULTIMATE') NOT NULL,
    PRIMARY KEY (card_id, skill_slot),
    CONSTRAINT fk_cs_card  FOREIGN KEY (card_id)  REFERENCES cards(id),
    CONSTRAINT fk_cs_skill FOREIGN KEY (skill_id) REFERENCES skills(id)
);
```

### 5. 원소 상성

```sql
CREATE TABLE element_relations (
    attacker_element_id TINYINT UNSIGNED NOT NULL,
    defender_element_id TINYINT UNSIGNED NOT NULL,
    multiplier          DECIMAL(4,2) NOT NULL,
    PRIMARY KEY (attacker_element_id, defender_element_id),
    CONSTRAINT fk_er_attacker FOREIGN KEY (attacker_element_id) REFERENCES elements(id),
    CONSTRAINT fk_er_defender FOREIGN KEY (defender_element_id) REFERENCES elements(id)
);

-- 초기 데이터
INSERT INTO element_relations VALUES
(1, 2, 1.5),  -- FIRE > NATURE
(2, 3, 1.5),  -- NATURE > WATER
(3, 1, 1.5),  -- WATER > FIRE
(4, 5, 1.5),  -- LIGHT > DARK
(5, 4, 1.5),  -- DARK > LIGHT
(6, 3, 1.5);  -- ELECTRIC > WATER
```

### 6. 전투

```sql
CREATE TABLE battles (
    id               BIGINT   UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    battle_type      ENUM('PVE','PVP','RAID','GUILD') NOT NULL,
    attacker_id      BIGINT   UNSIGNED NOT NULL,
    defender_id      BIGINT   UNSIGNED NOT NULL,
    attacker_deck_id INT      UNSIGNED NOT NULL,
    defender_deck_id INT      UNSIGNED NOT NULL,
    winner_id        BIGINT   UNSIGNED DEFAULT NULL,
    status           ENUM('IN_PROGRESS','FINISHED','CANCELLED') NOT NULL DEFAULT 'IN_PROGRESS',
    duration_ms      INT      DEFAULT NULL,
    started_at       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    finished_at      DATETIME DEFAULT NULL,
    CONSTRAINT fk_battle_attacker FOREIGN KEY (attacker_id) REFERENCES users(id),
    CONSTRAINT fk_battle_defender FOREIGN KEY (defender_id) REFERENCES users(id)
);

CREATE TABLE battle_logs (
    id             BIGINT   UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    battle_id      BIGINT   UNSIGNED NOT NULL,
    tick_ms        INT      NOT NULL,
    event_type     ENUM('ATTACK','SKILL','MOVE','DEATH','HEAL','BUFF','DEBUFF') NOT NULL,
    actor_card_id  INT      UNSIGNED NOT NULL,
    target_card_id INT      UNSIGNED DEFAULT NULL,
    value          INT      DEFAULT NULL,
    extra_data     JSON     DEFAULT NULL,
    CONSTRAINT fk_bl_battle FOREIGN KEY (battle_id) REFERENCES battles(id) ON DELETE CASCADE
);
```

### 7. 아이템 / 장비

```sql
CREATE TABLE items (
    id         INT          UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    name       VARCHAR(100) NOT NULL,
    item_type  ENUM('WEAPON','ARMOR','ARTIFACT','RUNE','GEM') NOT NULL,
    rarity     ENUM('COMMON','RARE','EPIC','LEGENDARY','MYTHIC','ANCIENT') NOT NULL,
    stat_type  VARCHAR(30)  NOT NULL,
    stat_value INT          NOT NULL
);

CREATE TABLE user_items (
    id          BIGINT   UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id     BIGINT   UNSIGNED NOT NULL,
    item_id     INT      UNSIGNED NOT NULL,
    quantity    INT      NOT NULL DEFAULT 1,
    obtained_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_ui_user FOREIGN KEY (user_id) REFERENCES users(id),
    CONSTRAINT fk_ui_item FOREIGN KEY (item_id) REFERENCES items(id)
);

CREATE TABLE monster_equipment (
    user_card_id BIGINT   UNSIGNED NOT NULL,
    user_item_id BIGINT   UNSIGNED NOT NULL,
    slot         ENUM('WEAPON','ARMOR','ARTIFACT','RUNE','GEM') NOT NULL,
    PRIMARY KEY (user_card_id, slot),
    CONSTRAINT fk_me_card FOREIGN KEY (user_card_id) REFERENCES user_cards(id),
    CONSTRAINT fk_me_item FOREIGN KEY (user_item_id) REFERENCES user_items(id)
);
```

### 8. 랭킹 / 시즌

```sql
CREATE TABLE seasons (
    id         INT      UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    name       VARCHAR(50) NOT NULL,
    started_at DATETIME NOT NULL,
    ended_at   DATETIME NOT NULL
);

CREATE TABLE rankings (
    id         BIGINT   UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    season_id  INT      UNSIGNED NOT NULL,
    user_id    BIGINT   UNSIGNED NOT NULL,
    rating     INT      NOT NULL DEFAULT 1000,
    rank_tier  ENUM('BRONZE','SILVER','GOLD','PLATINUM','DIAMOND','MASTER') NOT NULL DEFAULT 'BRONZE',
    win        INT      NOT NULL DEFAULT 0,
    lose       INT      NOT NULL DEFAULT 0,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uq_season_user (season_id, user_id),
    CONSTRAINT fk_rank_season FOREIGN KEY (season_id) REFERENCES seasons(id),
    CONSTRAINT fk_rank_user   FOREIGN KEY (user_id)   REFERENCES users(id)
);
```

### 9. 보상 로그

```sql
CREATE TABLE reward_logs (
    id          BIGINT   UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id     BIGINT   UNSIGNED NOT NULL,
    source_type ENUM('BATTLE','QUEST','DAILY','SEASON','ADMIN') NOT NULL,
    source_id   BIGINT   DEFAULT NULL,
    reward_type ENUM('GOLD','GEM','EXP','CARD','ITEM') NOT NULL,
    reward_id   INT      UNSIGNED DEFAULT NULL,
    amount      INT      NOT NULL,
    created_at  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_rl_user FOREIGN KEY (user_id) REFERENCES users(id)
);
```
