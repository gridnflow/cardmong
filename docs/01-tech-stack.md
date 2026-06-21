# 기술 스택 선택

## 게임 컨셉

| 항목 | 내용 |
|---|---|
| 장르 | 카드 수집 + Auto Battle + 몬스터 육성 RPG |
| 플랫폼 | Android (우선), iOS (추후) |
| 전투 방식 | 서버 시뮬레이션 (클라이언트는 로그 재생만) |

## 최종 기술 스택

### 클라이언트
| 항목 | 선택 | 이유 |
|---|---|---|
| 엔진 | Unity 2022.3 LTS | 모바일 게임 점유율 70%, Android/iOS 동일 코드베이스 |
| 언어 | C# | Unity 표준 |
| 렌더링 | URP | 모바일 최적화 |

### 백엔드
| 항목 | 선택 | 이유 |
|---|---|---|
| 언어 | Java 21 | 한국 게임사 표준, Virtual Thread 지원 |
| 프레임워크 | Spring Boot 3.3 | 한국 게임사 (넷마블, 컴투스, 라인게임즈 등) 사용 |
| DB | MySQL 8.0 | 게임 서버 표준 DB, AWS Aurora MySQL 지원 |
| 캐시 | Redis 7.2 | 세션, 랭킹 캐시 |
| 인프라 | AWS ECS | 초기 단계 적합, 트래픽 증가 시 EKS 전환 |
| 푸시 알림 | Firebase FCM | Android/iOS 통합 |

## 전투 방식 선택 이유

```
서버 시뮬레이션 방식 채택

클라이언트 → "전투 시작" 요청
서버 → 전투 전체 계산
서버 → 결과 + 전투 로그 응답
클라이언트 → 로그대로 애니메이션 재생

이유:
- Auto Battle 컨셉에 정확히 맞음
- 모바일 네트워크 불안정 무관
- 핵/치팅 원천 차단
- WebSocket 불필요 → 구현 복잡도 대폭 감소
```

## 단계별 확장 계획

```
Phase 1 — MVP
단일 Spring Boot + PostgreSQL + Redis
Android 전용

Phase 2 — 베타
PvP 매치메이킹 추가
iOS 빌드 추가 (Unity 타겟 변경만)
AWS ECS 배포

Phase 3 — 성장
Redis → 랭킹 고도화
Firebase FCM 푸시 알림

Phase 4 — 스케일
Kafka 도입 (MAU 100만 이상)
Kubernetes 전환
글로벌 서버 분산
```

## 제외한 기술과 이유

| 기술 | 제외 이유 |
|---|---|
| Go Battle Engine | MVP 단계에서 불필요, Spring Boot로 충분 |
| Kafka | 초기 규모에 오버엔지니어링, MAU 100만 이상일 때 도입 |
| Kubernetes | 초기 팀에서 운영 복잡도 과다 |
| WebSocket | Auto Battle은 실시간 동기화 불필요 |
