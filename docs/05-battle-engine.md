# 전투 시뮬레이션 엔진 설계

## 설계 방향

```
방식: 틱 기반 시뮬레이션 (Tick-based)
틱 단위: 100ms
최대 전투 시간: 60초 (600틱)

전투 시작 → 틱 반복 → 전투 종료 → 로그 반환
```

## AI 방식: Behavior Tree 채택

```
Behavior Tree 채택 이유:
- Utility AI: 수치 계산 기반, 튜닝이 복잡함
- GOAP: 계획 수립 오버헤드, 이 게임 규모에 과함
- Behavior Tree: 직관적 구조, 조건/행동 분리 명확
                 스킬/역할별 트리 교체 용이, 디버깅 쉬움
```

---

## 전투 흐름

```
BattleService.startBattle()
        │
        ▼
BattleEngine.simulate(attackerDeck, defenderDeck)
        │
        ├── 1. 몬스터 초기화 (BattleMonster 생성)
        │
        ├── 2. 틱 루프 시작 (0 ~ 최대 600틱)
        │       │
        │       ├── 모든 몬스터 쿨다운 감소
        │       ├── 상태이상 처리 (번, 빙결, 스턴)
        │       ├── 각 몬스터 AI 실행
        │       │     └── BehaviorTree.evaluate()
        │       │           ├── 죽었으면 패스
        │       │           ├── 스턴이면 패스
        │       │           ├── 궁극기 사용 가능? → SkillExecutor.execute()
        │       │           ├── 일반 스킬 사용 가능? → SkillExecutor.execute()
        │       │           ├── 사거리 내 적 있음? → 기본 공격
        │       │           └── 이동
        │       │
        │       └── 이벤트 로그 기록
        │
        ├── 3. 한 팀 전멸 시 종료
        │
        └── 4. BattleResult 반환 (로그 포함)
```

---

## 클래스 관계

```
BattleEngine
    └── BattleContext          전투 상태 컨테이너
    │       └── BattleMonster  몬스터 상태
    │       └── BattleEvent    이벤트 로그
    │
    ├── MonsterAI (interface)
    │       ├── AggressiveAI   공격형 (Mage, Assassin)
    │       ├── TankAI         탱커형
    │       └── SupportAI      지원형 (Healer, Support)
    │
    ├── SkillExecutor          스킬 실행
    │       └── DamageCalculator  데미지 계산
    │
    └── EffectProcessor        상태이상 처리
```

---

## 핵심 코드

### BattleEngine.java
```java
@Component
@RequiredArgsConstructor
public class BattleEngine {

    private static final int MAX_TICKS = 600;
    private static final int TICK_MS   = 100;

    private final SkillExecutor skillExecutor;
    private final EffectProcessor effectProcessor;

    public BattleResult simulate(List<UserCard> attackerCards,
                                 List<UserCard> defenderCards) {

        BattleContext ctx = BattleContext.init(
            attackerCards, defenderCards, TICK_MS
        );

        for (int tick = 0; tick < MAX_TICKS; tick++) {
            ctx.setCurrentTick(tick);
            processTick(ctx);
            if (ctx.isBattleOver()) break;
        }

        return ctx.buildResult();
    }

    private void processTick(BattleContext ctx) {
        ctx.getAllAlive().forEach(m -> m.decreaseCooldowns(ctx.getTickMs()));
        ctx.getAllAlive().forEach(m -> effectProcessor.process(m, ctx));
        ctx.getAllAlive().forEach(m -> {
            if (!m.isStunned()) {
                m.getAi().evaluate(m, ctx);
            }
        });
    }
}
```

### BattleContext.java
```java
@Getter
public class BattleContext {

    private final List<BattleMonster> attackerTeam;
    private final List<BattleMonster> defenderTeam;
    private final List<BattleEvent> eventLog;
    private final int tickMs;
    private int currentTick;

    public static BattleContext init(List<UserCard> attackerCards,
                                     List<UserCard> defenderCards,
                                     int tickMs) {
        return new BattleContext(
            BattleMonster.fromCards(attackerCards, Team.ATTACKER),
            BattleMonster.fromCards(defenderCards, Team.DEFENDER),
            tickMs
        );
    }

    public List<BattleMonster> getAllAlive() {
        return Stream.concat(attackerTeam.stream(), defenderTeam.stream())
            .filter(BattleMonster::isAlive).toList();
    }

    public List<BattleMonster> getEnemies(BattleMonster monster) {
        return (monster.getTeam() == Team.ATTACKER ? defenderTeam : attackerTeam)
            .stream().filter(BattleMonster::isAlive).toList();
    }

    public List<BattleMonster> getAllies(BattleMonster monster) {
        return (monster.getTeam() == Team.ATTACKER ? attackerTeam : defenderTeam)
            .stream().filter(BattleMonster::isAlive).toList();
    }

    public boolean isBattleOver() {
        return attackerTeam.stream().noneMatch(BattleMonster::isAlive)
            || defenderTeam.stream().noneMatch(BattleMonster::isAlive);
    }

    public int getCurrentTimeMs() { return currentTick * tickMs; }

    public BattleResult buildResult() {
        boolean attackerWin = defenderTeam.stream().noneMatch(BattleMonster::isAlive);
        return new BattleResult(
            attackerWin ? Team.ATTACKER : Team.DEFENDER,
            getCurrentTimeMs(),
            Collections.unmodifiableList(eventLog)
        );
    }
}
```

### BattleMonster.java
```java
@Getter
public class BattleMonster {

    private final int userCardId;
    private final Team team;
    private final MonsterAI ai;

    private int currentHp;
    private final int maxHp;
    private int currentMana;
    private final int maxMana;
    private final int attack;
    private final int defense;
    private final int speed;
    private final double critChance;
    private final double critDamage;
    private final ElementType element;
    private final List<BattleSkill> skills;
    private final Map<EffectType, ActiveEffect> activeEffects = new EnumMap<>(EffectType.class);

    private int posX;
    private int posY;

    public static BattleMonster from(UserCard userCard, Team team, int slotIndex) {
        Card card = userCard.getCard();
        int level = userCard.getLevel();
        return BattleMonster.builder()
            .userCardId(userCard.getId())
            .team(team)
            .maxHp(calcStat(card.getBaseHp(), level))
            .currentHp(calcStat(card.getBaseHp(), level))
            .attack(calcStat(card.getBaseAttack(), level))
            .defense(calcStat(card.getBaseDefense(), level))
            .speed(card.getBaseSpeed())
            .element(card.getElement())
            .skills(BattleSkill.fromCardSkills(card.getCardSkills()))
            .ai(AiFactory.create(card.getRole()))
            .posX(team == Team.ATTACKER ? 0 : 4)
            .posY(slotIndex)
            .build();
    }

    private static int calcStat(int base, int level) {
        return (int) (base * (1 + (level - 1) * 0.08));  // 레벨당 8% 증가
    }

    public void takeDamage(int damage) { currentHp = Math.max(0, currentHp - damage); }
    public void heal(int amount) { currentHp = Math.min(maxHp, currentHp + amount); }
    public void gainMana(int amount) { currentMana = Math.min(maxMana, currentMana + amount); }
    public void decreaseCooldowns(int tickMs) { skills.forEach(s -> s.decreaseCooldown(tickMs)); }
    public boolean isAlive() { return currentHp > 0; }
    public boolean isStunned() { return activeEffects.containsKey(EffectType.STUN); }

    public int distanceTo(BattleMonster other) {
        return Math.abs(posX - other.posX) + Math.abs(posY - other.posY);
    }
}
```

### AggressiveAI.java
```java
@Component
public class AggressiveAI implements MonsterAI {

    @Override
    public void evaluate(BattleMonster self, BattleContext ctx) {
        List<BattleMonster> enemies = ctx.getEnemies(self);
        if (enemies.isEmpty()) return;

        BattleMonster target = enemies.stream()
            .min(Comparator.comparingInt(BattleMonster::getCurrentHp))
            .orElseThrow();

        self.getReadyUltimate().ifPresentOrElse(
            skill -> useSkill(self, target, skill, ctx),
            () -> self.getReadyNormalSkill().ifPresentOrElse(
                skill -> useSkill(self, target, skill, ctx),
                () -> {
                    if (self.distanceTo(target) <= 1) basicAttack(self, target, ctx);
                    else moveToward(self, target, ctx);
                }
            )
        );
    }

    private void useSkill(BattleMonster self, BattleMonster target,
                          BattleSkill skill, BattleContext ctx) {
        if (self.distanceTo(target) > skill.getRange()) {
            moveToward(self, target, ctx);
            return;
        }
        SkillExecutor.execute(self, target, skill, ctx);
    }

    private void basicAttack(BattleMonster self, BattleMonster target, BattleContext ctx) {
        int damage = DamageCalculator.calc(self, target, null, ctx);
        target.takeDamage(damage);
        self.gainMana(10);
        ctx.addEvent(BattleEvent.attack(ctx.getCurrentTimeMs(), self, target, damage));
        if (!target.isAlive()) {
            ctx.addEvent(BattleEvent.death(ctx.getCurrentTimeMs(), target));
        }
    }

    private void moveToward(BattleMonster self, BattleMonster target, BattleContext ctx) {
        int dx = Integer.signum(target.getPosX() - self.getPosX());
        int dy = Integer.signum(target.getPosY() - self.getPosY());
        self.setPosX(self.getPosX() + dx);
        self.setPosY(self.getPosY() + dy);
        ctx.addEvent(BattleEvent.move(ctx.getCurrentTimeMs(), self));
    }
}
```

### DamageCalculator.java
```java
public class DamageCalculator {

    public static int calc(BattleMonster attacker, BattleMonster defender,
                           BattleSkill skill, BattleContext ctx) {
        int base = (skill != null)
            ? attacker.getAttack() + skill.getBaseValue()
            : attacker.getAttack();

        double reduced = base * (100.0 / (100.0 + defender.getDefense()));
        double elementMultiplier = ctx.getElementMultiplier(
            attacker.getElement(), defender.getElement()
        );
        double critMultiplier = isCrit(attacker) ? attacker.getCritDamage() / 100.0 : 1.0;
        double passiveMultiplier = attacker.getPassiveMultiplier();

        return (int) (reduced * elementMultiplier * critMultiplier * passiveMultiplier);
    }

    private static boolean isCrit(BattleMonster attacker) {
        return Math.random() * 100 < attacker.getCritChance();
    }
}
```

---

## 전투 로그 예시 출력

```json
{
  "battleId": 1024,
  "result": "WIN",
  "durationMs": 15300,
  "logs": [
    { "timeMs": 0,    "eventType": "MOVE",   "actorCardId": 1 },
    { "timeMs": 100,  "eventType": "MOVE",   "actorCardId": 4 },
    { "timeMs": 1200, "eventType": "ATTACK", "actorCardId": 1, "targetCardId": 4, "value": 145 },
    { "timeMs": 2400, "eventType": "SKILL",  "actorCardId": 1, "targetCardId": 4, "value": 320,
      "extraData": { "skillName": "Fireball", "effect": "BURN" } },
    { "timeMs": 2400, "eventType": "DEBUFF", "actorCardId": 4,
      "extraData": { "effect": "BURN" } },
    { "timeMs": 2500, "eventType": "EFFECT_DAMAGE", "actorCardId": 4, "value": 30 },
    { "timeMs": 5100, "eventType": "DEATH",  "actorCardId": 4 }
  ]
}
```

---

## 상태이상 종류

| 효과 | 설명 |
|---|---|
| BURN | 매 틱 화상 데미지 |
| POISON | 매 틱 독 데미지 |
| FREEZE | 이동 불가 |
| STUN | 행동 전체 불가 |

## AI 역할별 매핑

| 역할 | AI 타입 |
|---|---|
| MAGE | AggressiveAI |
| ASSASSIN | AggressiveAI |
| BRUISER | AggressiveAI |
| TANK | TankAI |
| HEALER | SupportAI |
| SUPPORT | SupportAI |
