# Unity 클라이언트 구조 설계

## 기본 설정

```
Unity 버전:      2022.3 LTS
렌더 파이프라인: URP (Universal Render Pipeline)
언어:            C#
해상도 기준:     1080 x 1920 (모바일 세로)
타겟:            Android 우선, iOS 동일 코드베이스
```

## 외부 라이브러리

| 라이브러리 | 용도 |
|---|---|
| UniTask | 비동기 처리 (async/await, 코루틴 대체) |
| DOTween | UI / 몬스터 이동 애니메이션 |
| Newtonsoft.Json | JSON 직렬화 (Unity 기본 JsonUtility 대체) |

---

## 폴더 구조

```
Assets/
│
├── _Project/
│   │
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── GameManager.cs           싱글턴, 앱 상태 관리
│   │   │   ├── SceneLoader.cs           씬 전환
│   │   │   ├── AppStateManager.cs       로그인/로비/전투 상태 전환
│   │   │   └── ServiceLocator.cs        의존성 주입 대체
│   │   │
│   │   ├── Network/
│   │   │   ├── ApiClient.cs             HTTP 요청 공통 처리
│   │   │   ├── AuthApi.cs
│   │   │   ├── CardApi.cs
│   │   │   ├── DeckApi.cs
│   │   │   ├── BattleApi.cs
│   │   │   ├── RankingApi.cs
│   │   │   └── dto/
│   │   │       ├── CardDto.cs
│   │   │       ├── DeckDto.cs
│   │   │       ├── BattleResultDto.cs
│   │   │       └── BattleLogDto.cs
│   │   │
│   │   ├── Battle/
│   │   │   ├── BattleDirector.cs        전투 로그 재생 총괄
│   │   │   ├── BattleField.cs           전투 필드 관리
│   │   │   ├── MonsterEntity.cs         전투 중 몬스터 개체
│   │   │   ├── MonsterSpawner.cs        몬스터 소환
│   │   │   ├── SkillAnimator.cs         스킬 이펙트 재생
│   │   │   ├── DamagePopup.cs           데미지 숫자 팝업
│   │   │   └── BattleEventQueue.cs      이벤트 큐 순서 처리
│   │   │
│   │   ├── UI/
│   │   │   ├── Common/
│   │   │   │   ├── UIManager.cs         팝업 / 화면 스택 관리
│   │   │   │   ├── PopupBase.cs         팝업 공통 베이스
│   │   │   │   ├── ToastMessage.cs      하단 토스트 알림
│   │   │   │   └── LoadingOverlay.cs    로딩 오버레이
│   │   │   │
│   │   │   ├── Lobby/
│   │   │   │   ├── LobbyScreen.cs
│   │   │   │   └── UserProfilePanel.cs
│   │   │   │
│   │   │   ├── Card/
│   │   │   │   ├── CardCollectionScreen.cs
│   │   │   │   ├── CardDetailPopup.cs
│   │   │   │   └── CardUpgradeScreen.cs
│   │   │   │
│   │   │   ├── Deck/
│   │   │   │   ├── DeckBuildScreen.cs
│   │   │   │   └── DeckSlot.cs
│   │   │   │
│   │   │   ├── Battle/
│   │   │   │   ├── BattleScreen.cs
│   │   │   │   ├── BattleResultScreen.cs
│   │   │   │   ├── MonsterHpBar.cs
│   │   │   │   └── BattleSpeedButton.cs  배속 버튼 (1x / 2x / 3x)
│   │   │   │
│   │   │   └── Ranking/
│   │   │       └── RankingScreen.cs
│   │   │
│   │   ├── Data/
│   │   │   ├── LocalStorage.cs          PlayerPrefs 래퍼
│   │   │   ├── SessionData.cs           로그인 세션 (싱글턴)
│   │   │   └── CardDataCache.cs         카드 마스터 데이터 캐시
│   │   │
│   │   └── Util/
│   │       ├── Extensions.cs
│   │       ├── Timer.cs
│   │       └── ObjectPool.cs
│   │
│   ├── Scenes/
│   │   ├── Boot.unity                   앱 진입, 토큰 확인
│   │   ├── Login.unity
│   │   ├── Lobby.unity
│   │   ├── DeckBuild.unity
│   │   ├── Battle.unity
│   │   └── Ranking.unity
│   │
│   ├── Prefabs/
│   │   ├── Monsters/
│   │   ├── Skills/
│   │   ├── UI/
│   │   └── Popup/
│   │
│   ├── Animations/
│   │   ├── Monsters/
│   │   │   ├── Idle.anim
│   │   │   ├── Move.anim
│   │   │   ├── Attack.anim
│   │   │   ├── Skill.anim
│   │   │   ├── Hit.anim
│   │   │   └── Death.anim
│   │   └── UI/
│   │
│   ├── Art/
│   │   ├── Sprites/
│   │   │   ├── Cards/
│   │   │   ├── Monsters/
│   │   │   ├── UI/
│   │   │   └── Icons/
│   │   └── VFX/
│   │
│   └── SO/
│       ├── MonsterDataSO.cs
│       └── SkillDataSO.cs
│
└── Plugins/
    ├── Newtonsoft.Json/
    └── DOTween/
```

---

## 씬 흐름

```
Boot.unity
  └── 토큰 있음 → Lobby.unity
  └── 토큰 없음 → Login.unity

Login.unity
  └── 로그인 성공 → Lobby.unity

Lobby.unity
  ├── [카드] 버튼 → CardCollection.unity
  ├── [덱] 버튼   → DeckBuild.unity
  ├── [전투] 버튼 → 매칭 후 Battle.unity
  └── [랭킹] 버튼 → Ranking.unity

Battle.unity
  └── 전투 종료 → BattleResult 팝업 → Lobby.unity
```

---

## 핵심 코드

### ApiClient.cs
```csharp
public class ApiClient
{
    private readonly string _baseUrl;
    private string _accessToken;

    public ApiClient(string baseUrl) { _baseUrl = baseUrl; }

    public void SetToken(string token) { _accessToken = token; }

    public async UniTask<T> GetAsync<T>(string path)
    {
        using var request = UnityWebRequest.Get(_baseUrl + path);
        SetAuthHeader(request);
        await request.SendWebRequest();
        return HandleResponse<T>(request);
    }

    public async UniTask<T> PostAsync<T>(string path, object body)
    {
        string json = JsonConvert.SerializeObject(body);
        using var request = new UnityWebRequest(_baseUrl + path, "POST");
        request.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        SetAuthHeader(request);
        await request.SendWebRequest();
        return HandleResponse<T>(request);
    }

    private void SetAuthHeader(UnityWebRequest request)
    {
        if (!string.IsNullOrEmpty(_accessToken))
            request.SetRequestHeader("Authorization", $"Bearer {_accessToken}");
    }

    private T HandleResponse<T>(UnityWebRequest request)
    {
        if (request.result != UnityWebRequest.Result.Success)
            throw new ApiException(request.responseCode, request.error);

        var response = JsonConvert.DeserializeObject<ApiResponse<T>>(
            request.downloadHandler.text);

        if (!response.Success)
            throw new ApiException(response.Error.Code, response.Error.Message);

        return response.Data;
    }
}
```

### BattleDirector.cs
```csharp
public class BattleDirector : MonoBehaviour
{
    [SerializeField] private MonsterSpawner spawner;
    [SerializeField] private BattleField field;
    [SerializeField] private BattleScreen battleScreen;

    private List<BattleLogDto> _logs;
    private Dictionary<int, MonsterEntity> _monsters;
    private float _playbackSpeed = 1f;

    public async UniTask PlayBattle(BattleResultDto result)
    {
        _monsters = spawner.SpawnAll(result.AttackerDeck, result.DefenderDeck);

        int prevTimeMs = 0;
        foreach (var log in result.Logs)
        {
            int waitMs = log.TimeMs - prevTimeMs;
            if (waitMs > 0)
                await UniTask.Delay((int)(waitMs / _playbackSpeed));

            await PlayEvent(log);
            prevTimeMs = log.TimeMs;
        }

        battleScreen.ShowResult(result.Result);
    }

    private async UniTask PlayEvent(BattleLogDto log)
    {
        if (!_monsters.TryGetValue(log.ActorCardId, out var actor)) return;

        switch (log.EventType)
        {
            case "ATTACK":
                var attackTarget = _monsters[log.TargetCardId.Value];
                await actor.PlayAttack(attackTarget, log.Value ?? 0);
                break;
            case "SKILL":
                var skillTarget = _monsters[log.TargetCardId.Value];
                await actor.PlaySkill(log.ExtraData["skillName"].ToString(),
                                      skillTarget, log.Value ?? 0);
                break;
            case "MOVE":
                int x = Convert.ToInt32(log.ExtraData["x"]);
                int y = Convert.ToInt32(log.ExtraData["y"]);
                await actor.MoveTo(field.GetPosition(x, y));
                break;
            case "HEAL":
                var healTarget = _monsters[log.TargetCardId.Value];
                await healTarget.PlayHeal(log.Value ?? 0);
                break;
            case "DEATH":
                await actor.PlayDeath();
                break;
            case "DEBUFF":
                actor.ApplyDebuffVfx(log.ExtraData["effect"].ToString());
                break;
        }
    }

    public void SetSpeed(float speed) { _playbackSpeed = speed; }
}
```

### MonsterEntity.cs
```csharp
public class MonsterEntity : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private MonsterHpBar hpBar;
    [SerializeField] private SkillAnimator skillAnimator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private int _maxHp;
    private int _currentHp;

    public void Init(CardDto card, Team team)
    {
        _maxHp     = card.BaseHp;
        _currentHp = card.BaseHp;
        hpBar.Init(_maxHp);
        if (team == Team.Defender) spriteRenderer.flipX = true;
    }

    public async UniTask PlayAttack(MonsterEntity target, int damage)
    {
        animator.SetTrigger("Attack");
        await UniTask.Delay(300);
        target.TakeDamage(damage);
        DamagePopup.Show(target.transform.position, damage, DamageType.Normal);
    }

    public async UniTask PlaySkill(string skillName, MonsterEntity target, int damage)
    {
        animator.SetTrigger("Skill");
        await skillAnimator.Play(skillName, target.transform.position);
        if (damage > 0)
        {
            target.TakeDamage(damage);
            DamagePopup.Show(target.transform.position, damage, DamageType.Skill);
        }
    }

    public async UniTask MoveTo(Vector3 targetPos)
    {
        animator.SetBool("IsMoving", true);
        await transform.DOMove(targetPos, 0.3f).AsyncWaitForCompletion();
        animator.SetBool("IsMoving", false);
    }

    public async UniTask PlayHeal(int amount)
    {
        _currentHp = Math.Min(_maxHp, _currentHp + amount);
        hpBar.UpdateHp(_currentHp);
        DamagePopup.Show(transform.position, amount, DamageType.Heal);
        await UniTask.Delay(200);
    }

    public async UniTask PlayDeath()
    {
        animator.SetTrigger("Death");
        await UniTask.Delay(800);
        gameObject.SetActive(false);
    }

    public void TakeDamage(int damage)
    {
        _currentHp = Math.Max(0, _currentHp - damage);
        hpBar.UpdateHp(_currentHp);
        animator.SetTrigger("Hit");
    }

    public void ApplyDebuffVfx(string effectType) { skillAnimator.PlayDebuffVfx(effectType); }
}
```

### SessionData.cs
```csharp
public class SessionData
{
    public static SessionData Instance { get; } = new();

    public string AccessToken  { get; private set; }
    public string RefreshToken { get; private set; }
    public long   UserId       { get; private set; }
    public string Nickname     { get; private set; }

    public void SetSession(string accessToken, string refreshToken,
                           long userId, string nickname)
    {
        AccessToken  = accessToken;
        RefreshToken = refreshToken;
        UserId       = userId;
        Nickname     = nickname;
        LocalStorage.Save("access_token",  accessToken);
        LocalStorage.Save("refresh_token", refreshToken);
    }

    public bool IsLoggedIn() => !string.IsNullOrEmpty(AccessToken);

    public void Clear()
    {
        AccessToken  = null;
        RefreshToken = null;
        LocalStorage.Delete("access_token");
        LocalStorage.Delete("refresh_token");
    }
}
```

---

## Android / iOS 빌드 설정

```
공통
  Package Name   com.cardmong.game
  Version        1.0.0

Android
  Min SDK        API 26 (Android 8.0)
  Target SDK     API 34
  Scripting      IL2CPP
  Architecture   ARM64

iOS (나중에 추가)
  Min iOS        14.0
  Scripting      IL2CPP
  → 코드 변경 없이 Build Settings에서 타겟만 변경
```
