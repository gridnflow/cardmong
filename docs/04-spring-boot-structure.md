# Spring Boot 프로젝트 구조

## 아키텍처 방향

```
패키지 구조 방식: 도메인 중심 (Domain-driven)
이유:
- 기능별로 묶여있어 팀 분업 용이
- 나중에 마이크로서비스 분리 시 패키지 단위로 분리 가능
- 전투 엔진, 카드, 유저 등 도메인이 명확하게 구분됨
```

## 레이어 역할

| 레이어 | 역할 |
|---|---|
| Controller | HTTP 요청/응답만 담당, 비즈니스 로직 없음 |
| Service | 비즈니스 로직, 트랜잭션 처리 |
| Repository | DB 접근 (JPA) |
| Entity | DB 테이블 매핑 |
| DTO | 요청/응답 데이터 전달 객체 |
| Engine | 전투 시뮬레이션 (DB 접근 없음, 순수 로직) |

---

## 전체 폴더 구조

```
cardmong-server/
│
├── build.gradle
├── settings.gradle
├── Dockerfile
├── docker-compose.yml
│
└── src/main/java/com/cardmong/
    │
    ├── CardmongApplication.java
    │
    ├── global/
    │   ├── config/
    │   │   ├── SecurityConfig.java
    │   │   ├── RedisConfig.java
    │   │   ├── JpaConfig.java
    │   │   └── SwaggerConfig.java
    │   │
    │   ├── jwt/
    │   │   ├── JwtProvider.java
    │   │   ├── JwtFilter.java
    │   │   └── JwtProperties.java
    │   │
    │   ├── exception/
    │   │   ├── GlobalExceptionHandler.java
    │   │   ├── BusinessException.java
    │   │   └── ErrorCode.java
    │   │
    │   └── response/
    │       └── ApiResponse.java
    │
    └── domain/
        ├── auth/
        │   ├── controller/AuthController.java
        │   ├── service/AuthService.java
        │   └── dto/
        │       ├── RegisterRequest.java
        │       ├── LoginRequest.java
        │       └── TokenResponse.java
        │
        ├── user/
        │   ├── controller/UserController.java
        │   ├── service/UserService.java
        │   ├── repository/
        │   │   ├── UserRepository.java
        │   │   └── UserStatsRepository.java
        │   ├── entity/
        │   │   ├── User.java
        │   │   └── UserStats.java
        │   └── dto/
        │       ├── UserProfileResponse.java
        │       └── UserStatsResponse.java
        │
        ├── card/
        │   ├── controller/
        │   │   ├── CardController.java
        │   │   └── UserCardController.java
        │   ├── service/
        │   │   ├── CardService.java
        │   │   └── UserCardService.java
        │   ├── repository/
        │   │   ├── CardRepository.java
        │   │   ├── UserCardRepository.java
        │   │   └── CardSkillRepository.java
        │   ├── entity/
        │   │   ├── Card.java
        │   │   ├── UserCard.java
        │   │   ├── Element.java
        │   │   └── ElementRelation.java
        │   └── dto/
        │       ├── CardResponse.java
        │       ├── UserCardResponse.java
        │       ├── CardUpgradeRequest.java
        │       └── CardUpgradeResponse.java
        │
        ├── skill/
        │   ├── repository/SkillRepository.java
        │   ├── entity/
        │   │   ├── Skill.java
        │   │   └── CardSkill.java
        │   └── dto/SkillResponse.java
        │
        ├── deck/
        │   ├── controller/DeckController.java
        │   ├── service/DeckService.java
        │   ├── repository/
        │   │   ├── DeckRepository.java
        │   │   └── DeckCardRepository.java
        │   ├── entity/
        │   │   ├── Deck.java
        │   │   └── DeckCard.java
        │   └── dto/
        │       ├── DeckCreateRequest.java
        │       ├── DeckUpdateRequest.java
        │       └── DeckResponse.java
        │
        ├── battle/
        │   ├── controller/BattleController.java
        │   ├── service/BattleService.java
        │   ├── engine/
        │   │   ├── BattleEngine.java
        │   │   ├── BattleContext.java
        │   │   ├── BattleMonster.java
        │   │   ├── BattleTick.java
        │   │   ├── ai/
        │   │   │   ├── MonsterAI.java
        │   │   │   ├── AggressiveAI.java
        │   │   │   └── SupportAI.java
        │   │   └── skill/
        │   │       ├── SkillExecutor.java
        │   │       └── EffectProcessor.java
        │   ├── repository/
        │   │   ├── BattleRepository.java
        │   │   └── BattleLogRepository.java
        │   ├── entity/
        │   │   ├── Battle.java
        │   │   └── BattleLog.java
        │   └── dto/
        │       ├── BattleStartRequest.java
        │       ├── BattleResultResponse.java
        │       └── BattleLogResponse.java
        │
        ├── item/
        │   ├── controller/ItemController.java
        │   ├── service/ItemService.java
        │   ├── repository/
        │   │   ├── ItemRepository.java
        │   │   ├── UserItemRepository.java
        │   │   └── MonsterEquipmentRepository.java
        │   ├── entity/
        │   │   ├── Item.java
        │   │   ├── UserItem.java
        │   │   └── MonsterEquipment.java
        │   └── dto/
        │       ├── EquipRequest.java
        │       └── EquipResponse.java
        │
        ├── ranking/
        │   ├── controller/RankingController.java
        │   ├── service/RankingService.java
        │   ├── repository/
        │   │   ├── RankingRepository.java
        │   │   └── SeasonRepository.java
        │   ├── entity/
        │   │   ├── Ranking.java
        │   │   └── Season.java
        │   └── dto/RankingResponse.java
        │
        └── reward/
            ├── service/RewardService.java
            ├── repository/RewardLogRepository.java
            ├── entity/RewardLog.java
            └── dto/RewardResponse.java
```

---

## 핵심 공통 코드

### ApiResponse.java
```java
@Getter
@AllArgsConstructor
public class ApiResponse<T> {

    private final boolean success;
    private final T data;
    private final ErrorInfo error;

    public static <T> ApiResponse<T> ok(T data) {
        return new ApiResponse<>(true, data, null);
    }

    public static ApiResponse<?> fail(ErrorCode code) {
        return new ApiResponse<>(false, null,
            new ErrorInfo(code.name(), code.getMessage()));
    }

    public record ErrorInfo(String code, String message) {}
}
```

### ErrorCode.java
```java
@Getter
@RequiredArgsConstructor
public enum ErrorCode {

    AUTH_TOKEN_MISSING("토큰이 없습니다."),
    AUTH_TOKEN_EXPIRED("토큰이 만료되었습니다."),
    AUTH_UNAUTHORIZED("권한이 없습니다."),

    USER_NOT_FOUND("유저를 찾을 수 없습니다."),
    USER_NICKNAME_DUPLICATE("이미 사용 중인 닉네임입니다."),

    CARD_NOT_FOUND("카드를 찾을 수 없습니다."),
    CARD_NOT_OWNED("보유하지 않은 카드입니다."),
    CARD_EVOLVE_CONDITION_NOT_MET("진화 조건을 충족하지 못했습니다."),

    DECK_NOT_FOUND("덱을 찾을 수 없습니다."),
    DECK_CARD_LIMIT_EXCEEDED("덱은 최대 5장까지 구성 가능합니다."),
    DECK_ENERGY_LIMIT_EXCEEDED("에너지 한도를 초과했습니다."),

    BATTLE_DECK_NOT_SET("덱이 설정되지 않았습니다."),
    BATTLE_ALREADY_IN_PROGRESS("이미 진행 중인 전투가 있습니다."),

    ITEM_NOT_FOUND("아이템을 찾을 수 없습니다."),
    ITEM_SLOT_MISMATCH("슬롯 타입이 맞지 않습니다.");

    private final String message;
}
```

---

## application.yml

```yaml
spring:
  datasource:
    url: jdbc:mysql://localhost:3306/cardmong?characterEncoding=UTF-8
    username: ${DB_USERNAME}
    password: ${DB_PASSWORD}
    driver-class-name: com.mysql.cj.jdbc.Driver

  jpa:
    hibernate:
      ddl-auto: validate
    properties:
      hibernate:
        dialect: org.hibernate.dialect.MySQLDialect
        format_sql: true

  data:
    redis:
      host: ${REDIS_HOST:localhost}
      port: 6379

jwt:
  secret: ${JWT_SECRET}
  access-expiration: 3600
  refresh-expiration: 2592000

server:
  port: 8080
```

---

## build.gradle

```groovy
plugins {
    id 'java'
    id 'org.springframework.boot' version '3.3.0'
    id 'io.spring.dependency-management' version '1.1.4'
}

java {
    sourceCompatibility = JavaVersion.VERSION_21
}

dependencies {
    implementation 'org.springframework.boot:spring-boot-starter-web'
    implementation 'org.springframework.boot:spring-boot-starter-data-jpa'
    implementation 'org.springframework.boot:spring-boot-starter-data-redis'
    implementation 'org.springframework.boot:spring-boot-starter-security'
    implementation 'org.springframework.boot:spring-boot-starter-validation'

    runtimeOnly 'com.mysql:mysql-connector-j'

    implementation 'io.jsonwebtoken:jjwt-api:0.12.5'
    runtimeOnly 'io.jsonwebtoken:jjwt-impl:0.12.5'
    runtimeOnly 'io.jsonwebtoken:jjwt-jackson:0.12.5'

    compileOnly 'org.projectlombok:lombok'
    annotationProcessor 'org.projectlombok:lombok'

    implementation 'org.springdoc:springdoc-openapi-starter-webmvc-ui:2.3.0'

    testImplementation 'org.springframework.boot:spring-boot-starter-test'
    testImplementation 'org.springframework.security:spring-security-test'
}
```

---

## docker-compose.yml (로컬 개발용)

```yaml
services:
  mysql:
    image: mysql:8.0
    environment:
      MYSQL_ROOT_PASSWORD: root
      MYSQL_DATABASE: cardmong
      MYSQL_USER: cardmong
      MYSQL_PASSWORD: cardmong
    ports:
      - "3306:3306"
    volumes:
      - mysql-data:/var/lib/mysql

  redis:
    image: redis:7.2-alpine
    ports:
      - "6379:6379"

volumes:
  mysql-data:
```
