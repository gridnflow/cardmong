# REST API 설계

## 기본 규칙

```
Base URL     https://api.cardmong.com/v1
인증         Authorization: Bearer {JWT}
응답 형식    JSON
인증 불필요  /auth/** 만 예외
```

## 공통 응답 형식

```json
// 성공
{
  "success": true,
  "data": { ... }
}

// 실패
{
  "success": false,
  "error": {
    "code": "CARD_NOT_FOUND",
    "message": "카드를 찾을 수 없습니다."
  }
}
```

---

## 1. 인증 (Auth)

| Method | Path | 설명 |
|---|---|---|
| POST | /auth/register | 회원가입 |
| POST | /auth/login | 로그인 → JWT 발급 |
| POST | /auth/logout | 로그아웃 |
| POST | /auth/refresh | 토큰 갱신 |
| POST | /auth/guest | 게스트 로그인 |

### POST /auth/register
```json
Request
{
  "email": "test@test.com",
  "nickname": "DragonMaster",
  "password": "password123!"
}

Response
{
  "success": true,
  "data": {
    "userId": 1,
    "nickname": "DragonMaster",
    "accessToken": "eyJ...",
    "refreshToken": "eyJ..."
  }
}
```

### POST /auth/login
```json
Request
{
  "email": "test@test.com",
  "password": "password123!"
}

Response
{
  "success": true,
  "data": {
    "accessToken": "eyJ...",
    "refreshToken": "eyJ...",
    "expiresIn": 3600
  }
}
```

---

## 2. 유저 (User)

| Method | Path | 설명 |
|---|---|---|
| GET | /users/me | 내 프로필 조회 |
| PATCH | /users/me | 닉네임 변경 |
| GET | /users/me/stats | 내 스탯 조회 |
| GET | /users/{userId} | 다른 유저 프로필 조회 |

### GET /users/me
```json
Response
{
  "success": true,
  "data": {
    "userId": 1,
    "nickname": "DragonMaster",
    "level": 15,
    "exp": 3200,
    "gold": 5000,
    "gem": 100,
    "winCount": 42,
    "loseCount": 18,
    "ratingPoint": 1340
  }
}
```

---

## 3. 카드 (Card)

| Method | Path | 설명 |
|---|---|---|
| GET | /cards | 전체 카드 도감 조회 |
| GET | /cards/{cardId} | 카드 상세 조회 |
| GET | /users/me/cards | 내 보유 카드 목록 |
| GET | /users/me/cards/{userCardId} | 내 카드 상세 조회 |
| POST | /users/me/cards/{userCardId}/upgrade | 카드 강화 |
| POST | /users/me/cards/{userCardId}/evolve | 카드 진화 |

### GET /cards
```json
Query Params: ?element=FIRE&rarity=LEGENDARY&role=MAGE&page=0&size=20

Response
{
  "success": true,
  "data": {
    "cards": [
      {
        "cardId": 1,
        "name": "Flame Dragon",
        "element": "FIRE",
        "rarity": "LEGENDARY",
        "role": "MAGE",
        "energyCost": 5,
        "baseHp": 1200,
        "baseAttack": 180,
        "baseDefense": 70,
        "baseSpeed": 90,
        "skills": [
          { "slot": "NORMAL",   "name": "Fireball" },
          { "slot": "ULTIMATE", "name": "Meteor Storm" },
          { "slot": "PASSIVE",  "name": "Fire Boost" }
        ],
        "evolutionStage": 2,
        "evolvesFromId": 5
      }
    ],
    "totalCount": 45,
    "page": 0,
    "size": 20
  }
}
```

### POST /users/me/cards/{userCardId}/upgrade
```json
Request
{
  "materialCardIds": [101, 102, 103]
}

Response
{
  "success": true,
  "data": {
    "userCardId": 55,
    "level": 12,
    "upgradeCount": 3,
    "statChanges": {
      "hp": "+60",
      "attack": "+18"
    }
  }
}
```

---

## 4. 덱 (Deck)

| Method | Path | 설명 |
|---|---|---|
| GET | /users/me/decks | 덱 목록 조회 |
| POST | /users/me/decks | 덱 생성 |
| GET | /users/me/decks/{deckId} | 덱 상세 조회 |
| PUT | /users/me/decks/{deckId} | 덱 수정 |
| DELETE | /users/me/decks/{deckId} | 덱 삭제 |
| PATCH | /users/me/decks/{deckId}/main | 대표 덱 설정 |

### POST /users/me/decks
```json
Request
{
  "name": "불속성 공격덱",
  "userCardIds": [55, 72, 88, 91, 103]
}

Response
{
  "success": true,
  "data": {
    "deckId": 7,
    "name": "불속성 공격덱",
    "totalEnergy": 18,
    "cards": [
      {
        "slot": 0,
        "userCardId": 55,
        "cardName": "Flame Dragon",
        "level": 12,
        "rarity": "LEGENDARY"
      }
    ]
  }
}
```

---

## 5. 전투 (Battle)

| Method | Path | 설명 |
|---|---|---|
| POST | /battles | 전투 시작 |
| GET | /battles/{battleId} | 전투 결과 조회 |
| GET | /battles/{battleId}/logs | 전투 로그 조회 |
| GET | /users/me/battles | 내 전투 기록 |

### POST /battles
```json
Request
{
  "battleType": "PVE",
  "deckId": 7
}

Response
{
  "success": true,
  "data": {
    "battleId": 1024,
    "result": "WIN",
    "durationMs": 32400,
    "rewards": {
      "exp": 120,
      "gold": 300,
      "cards": []
    },
    "ratingChange": 18,
    "logUrl": "/battles/1024/logs"
  }
}
```

### GET /battles/{battleId}/logs
```json
Response
{
  "success": true,
  "data": {
    "battleId": 1024,
    "durationMs": 32400,
    "logs": [
      {
        "tickMs": 0,
        "eventType": "MOVE",
        "actorCardId": 1,
        "extraData": { "toX": 3, "toY": 2 }
      },
      {
        "tickMs": 1200,
        "eventType": "SKILL",
        "actorCardId": 1,
        "targetCardId": 4,
        "value": 180,
        "extraData": { "skillName": "Fireball", "effect": "BURN" }
      },
      {
        "tickMs": 8500,
        "eventType": "DEATH",
        "actorCardId": 4
      }
    ]
  }
}
```

---

## 6. 아이템 / 장비 (Item)

| Method | Path | 설명 |
|---|---|---|
| GET | /items | 전체 아이템 목록 |
| GET | /users/me/items | 내 보유 아이템 목록 |
| POST | /users/me/cards/{userCardId}/equipment | 장비 장착 |
| DELETE | /users/me/cards/{userCardId}/equipment/{slot} | 장비 해제 |

### POST /users/me/cards/{userCardId}/equipment
```json
Request
{
  "userItemId": 301,
  "slot": "WEAPON"
}

Response
{
  "success": true,
  "data": {
    "userCardId": 55,
    "slot": "WEAPON",
    "itemName": "Dragon Sword",
    "statChanges": {
      "attack": "+45"
    }
  }
}
```

---

## 7. 랭킹 (Ranking)

| Method | Path | 설명 |
|---|---|---|
| GET | /rankings | 시즌 랭킹 상위 100 |
| GET | /rankings/me | 내 랭킹 조회 |
| GET | /seasons | 시즌 목록 |
| GET | /seasons/current | 현재 시즌 정보 |

### GET /rankings
```json
Query Params: ?seasonId=3&page=0&size=100

Response
{
  "success": true,
  "data": {
    "season": {
      "id": 3,
      "name": "Season 3 - Dragon War",
      "endedAt": "2026-07-31T23:59:59"
    },
    "rankings": [
      {
        "rank": 1,
        "userId": 42,
        "nickname": "DragonMaster",
        "rating": 2840,
        "tier": "MASTER",
        "win": 145,
        "lose": 23
      }
    ]
  }
}
```

---

## 8. 보상 (Reward)

| Method | Path | 설명 |
|---|---|---|
| GET | /users/me/rewards | 보상 수령 내역 |
| POST | /users/me/rewards/daily | 일일 보상 수령 |

---

## 에러 코드 정의

| 코드 | 설명 |
|---|---|
| AUTH_001 | 토큰 없음 |
| AUTH_002 | 토큰 만료 |
| AUTH_003 | 권한 없음 |
| USER_001 | 유저 없음 |
| USER_002 | 닉네임 중복 |
| CARD_001 | 카드 없음 |
| CARD_002 | 보유하지 않은 카드 |
| CARD_003 | 진화 조건 미충족 |
| CARD_004 | 강화 재료 부족 |
| DECK_001 | 덱 없음 |
| DECK_002 | 카드 수 초과 (5장 초과) |
| DECK_003 | 에너지 초과 |
| DECK_004 | 최대 덱 수 초과 |
| BATTLE_001 | 덱 미설정 |
| BATTLE_002 | 진행 중인 전투 있음 |
| ITEM_001 | 아이템 없음 |
| ITEM_002 | 장착 슬롯 불일치 |

---

## 전체 엔드포인트 요약

```
인증
  POST   /auth/register
  POST   /auth/login
  POST   /auth/refresh

유저
  GET    /users/me
  GET    /users/me/stats

카드
  GET    /cards
  GET    /users/me/cards
  POST   /users/me/cards/{id}/upgrade
  POST   /users/me/cards/{id}/evolve

덱
  GET    /users/me/decks
  POST   /users/me/decks
  PUT    /users/me/decks/{id}
  DELETE /users/me/decks/{id}
  PATCH  /users/me/decks/{id}/main

전투
  POST   /battles
  GET    /battles/{id}
  GET    /battles/{id}/logs

장비
  POST   /users/me/cards/{id}/equipment
  DELETE /users/me/cards/{id}/equipment/{slot}

랭킹
  GET    /rankings
  GET    /rankings/me

보상
  POST   /users/me/rewards/daily
```
