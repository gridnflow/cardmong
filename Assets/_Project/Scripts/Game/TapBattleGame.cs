using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace Cardmong.Game
{
    /// <summary>
    /// 오프라인 카드 배틀 슬라이스(세로 모바일).
    /// 하단 카드를 누르면 그 안의 몬스터가 튀어나와 적을 공격하고, 적도 주기적으로
    /// 플레이어를 공격한다. 플레이어 HP가 0이 되면 게임오버 → 탭으로 재시작.
    /// 모든 UI는 코드로 생성하고 입력은 New Input System으로 처리한다.
    /// </summary>
    public class TapBattleGame : MonoBehaviour
    {
        private struct Archetype
        {
            public string Name;
            public Color Color;
            public int Atk;
            public float Cooldown;
            public Archetype(string n, Color c, int atk, float cd) { Name = n; Color = c; Atk = atk; Cooldown = cd; }
        }

        private class Card
        {
            public RectTransform Rt;
            public Image CdOverlay;
            public string Name;
            public Color Color;
            public int Atk;
            public float CdTotal;
            public float Cd;
        }

        private static readonly Archetype[] Types =
        {
            new Archetype("Slime",  new Color(0.45f, 0.78f, 0.40f), 12, 0.9f),
            new Archetype("Wolf",   new Color(0.40f, 0.62f, 0.92f), 16, 1.0f),
            new Archetype("Imp",    new Color(0.88f, 0.45f, 0.40f), 20, 1.2f),
            new Archetype("Golem",  new Color(0.62f, 0.55f, 0.72f), 30, 1.9f),
            new Archetype("Drake",  new Color(0.93f, 0.62f, 0.28f), 36, 2.3f),
        };

        private static readonly Color[] EnemyPalette =
        {
            new Color(0.86f, 0.40f, 0.42f),
            new Color(0.55f, 0.50f, 0.85f),
            new Color(0.40f, 0.70f, 0.66f),
            new Color(0.84f, 0.66f, 0.34f),
            new Color(0.70f, 0.44f, 0.62f),
        };

        private const int HandSize = 4;
        private const float MonsterBase = 360f;
        private const int PlayerMaxHp = 100;

        private Sprite _box;
        private RectTransform _canvasRt;

        private Image _enemy;
        private RectTransform _enemyRt;
        private TMP_Text _enemyLabel;
        private Image _hpFill;
        private TMP_Text _hpText;
        private TMP_Text _killText;
        private Vector2 _enemyBasePos;

        private Image _playerHpFill;
        private TMP_Text _playerHpText;
        private RectTransform _playerHpRt;
        private Vector2 _playerHpBasePos;

        private GameObject _gameOverPanel;
        private TMP_Text _gameOverInfo;

        private readonly List<Card> _cards = new();

        private int _kills;
        private int _maxHp;
        private int _hp;
        private int _playerHp;
        private bool _busy;          // 적 등장/소멸 중
        private bool _gameOver;
        private bool _enemyAttacking;
        private float _enemyHit;
        private float _playerHurt;
        private float _enemyAtkTimer;
        private float _enemyAtkInterval;
        private Color _enemyColor;

        private void Start()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;

            var tex = Texture2D.whiteTexture;
            _box = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                 new Vector2(0.5f, 0.5f), 100f);

            BuildUI();
            _playerHp = PlayerMaxHp;
            UpdatePlayerHpText();
            NewEnemy(instant: true);
        }

        private void Update()
        {
            var p = Pointer.current;
            bool pressed = p != null && p.press.wasPressedThisFrame;

            if (_gameOver)
            {
                if (pressed) Restart();
                return;
            }

            // 카드 쿨다운
            foreach (var c in _cards)
            {
                if (c.Cd > 0f)
                {
                    c.Cd = Mathf.Max(0f, c.Cd - Time.deltaTime);
                    c.CdOverlay.fillAmount = c.Cd / c.CdTotal;
                }
            }

            // 적 타격 반동(스케일)
            if (_enemyHit > 0f)
            {
                _enemyHit -= Time.deltaTime;
                float k = Mathf.Clamp01(_enemyHit / 0.14f);
                _enemyRt.localScale = Vector3.one * (1f + 0.16f * k);
            }

            // 플레이어 피격 흔들림
            if (_playerHurt > 0f)
            {
                _playerHurt -= Time.deltaTime;
                float k = Mathf.Clamp01(_playerHurt / 0.3f);
                float off = Mathf.Sin(_playerHurt * 60f) * 14f * k;
                _playerHpRt.anchoredPosition = _playerHpBasePos + new Vector2(off, 0);
            }
            else
            {
                _playerHpRt.anchoredPosition = _playerHpBasePos;
            }

            // HP 바 부드럽게
            _hpFill.fillAmount = Mathf.MoveTowards(_hpFill.fillAmount,
                _maxHp > 0 ? (float)_hp / _maxHp : 0f, 2.5f * Time.deltaTime);
            _playerHpFill.fillAmount = Mathf.MoveTowards(_playerHpFill.fillAmount,
                (float)_playerHp / PlayerMaxHp, 2.5f * Time.deltaTime);

            // 적의 공격 타이머
            if (!_busy && !_enemyAttacking)
            {
                _enemyAtkTimer -= Time.deltaTime;
                if (_enemyAtkTimer <= 0f)
                {
                    _enemyAtkTimer = _enemyAtkInterval;
                    StartCoroutine(EnemyAttack());
                }
            }

            // 입력: 카드 탭
            if (!_busy && pressed)
            {
                Vector2 sp = p.position.ReadValue();
                foreach (var c in _cards)
                {
                    if (c.Cd <= 0f && RectTransformUtility.RectangleContainsScreenPoint(c.Rt, sp, null))
                    {
                        StartCoroutine(PlayCard(c));
                        break;
                    }
                }
            }
        }

        // ------------------------------------------------------- player attacks

        private IEnumerator PlayCard(Card c)
        {
            c.Cd = c.CdTotal;
            c.CdOverlay.fillAmount = 1f;

            var token = NewImage("Token", _canvasRt, c.Color);
            AddFace(token.rectTransform, 120f / MonsterBase);
            var trt = token.rectTransform;
            Anchor(trt, new Vector2(0.5f, 0.5f), CanvasPoint(c.Rt), new Vector2(120, 120));

            Vector2 from = trt.anchoredPosition;
            Vector2 to = CanvasPoint(_enemyRt) + new Vector2(0, -30);

            float t = 0f;
            const float durOut = 0.22f;
            while (t < durOut)
            {
                t += Time.deltaTime;
                float k = EaseOutCubic(t / durOut);
                trt.anchoredPosition = Vector2.LerpUnclamped(from, to, k);
                trt.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.7f, k);
                yield return null;
            }

            DamageEnemy(c.Atk);

            t = 0f;
            const float durBack = 0.18f;
            while (t < durBack)
            {
                t += Time.deltaTime;
                float k = t / durBack;
                trt.anchoredPosition = Vector2.Lerp(to, from, k);
                trt.localScale = Vector3.one * Mathf.Lerp(1.7f, 0.3f, k);
                yield return null;
            }
            Destroy(token.gameObject);
        }

        private void DamageEnemy(int atk)
        {
            if (_busy) return;

            bool crit = Random.value < 0.12f;
            int dmg = Mathf.Max(1, atk + Random.Range(-2, 3)) * (crit ? 2 : 1);

            _hp = Mathf.Max(0, _hp - dmg);
            UpdateHpText();
            _enemyHit = 0.14f;
            FloatNumber((crit ? "CRIT " : "") + dmg, crit ? 80 : 56,
                        crit ? new Color(1f, 0.55f, 0.15f) : Color.white,
                        CanvasPoint(_enemyRt) + new Vector2(0, 130));

            if (_hp <= 0)
                StartCoroutine(KillAndRespawn());
        }

        // ------------------------------------------------------- enemy attacks

        private IEnumerator EnemyAttack()
        {
            if (_busy || _gameOver) yield break;
            _enemyAttacking = true;

            // 적이 사용할 카드를 화면 중앙에 보여준다
            Archetype a = Types[Random.Range(0, Types.Length)];
            RectTransform card = SpawnEnemyCard(a);
            yield return ScalePop(card, Vector3.zero, Vector3.one, 0.18f, true);
            yield return new WaitForSeconds(0.3f); // 어떤 카드인지 볼 시간

            // 그 카드에서 몬스터가 튀어나와 플레이어에게 날아간다
            var token = NewImage("EToken", _canvasRt, a.Color);
            AddFace(token.rectTransform, 120f / MonsterBase);
            var trt = token.rectTransform;
            Vector2 from = CanvasPoint(card);
            Anchor(trt, new Vector2(0.5f, 0.5f), from, new Vector2(120, 120));
            Vector2 to = CanvasPoint(_playerHpRt) + new Vector2(0, 60);

            float t = 0f;
            const float durOut = 0.22f;
            while (t < durOut)
            {
                t += Time.deltaTime;
                float k = EaseOutCubic(t / durOut);
                trt.anchoredPosition = Vector2.LerpUnclamped(from, to, k);
                trt.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.6f, k);
                yield return null;
            }

            if (!_gameOver && !_busy)
                DamagePlayer(a);

            t = 0f;
            const float durBack = 0.16f;
            while (t < durBack)
            {
                t += Time.deltaTime;
                float k = t / durBack;
                trt.anchoredPosition = Vector2.Lerp(to, from, k);
                trt.localScale = Vector3.one * Mathf.Lerp(1.6f, 0.3f, k);
                yield return null;
            }
            Destroy(token.gameObject);

            yield return ScalePop(card, Vector3.one, Vector3.zero, 0.14f, false);
            if (card) Destroy(card.gameObject);
            _enemyAttacking = false;
        }

        private void DamagePlayer(Archetype a)
        {
            int dmg = Mathf.Max(1, a.Atk + _kills * 2 + Random.Range(-2, 3));
            _playerHp = Mathf.Max(0, _playerHp - dmg);
            UpdatePlayerHpText();
            _playerHurt = 0.3f;
            FloatNumber("-" + dmg, 60, new Color(1f, 0.45f, 0.45f),
                        CanvasPoint(_playerHpRt) + new Vector2(0, 70));

            if (_playerHp <= 0)
                GameOver();
        }

        private RectTransform SpawnEnemyCard(Archetype a)
        {
            var panel = NewImage("EnemyAtkCard", _canvasRt, new Color(0.30f, 0.16f, 0.18f));
            var rt = panel.rectTransform;
            Anchor(rt, new Vector2(0.5f, 0.5f), new Vector2(0, -120), new Vector2(230, 320));

            var tag = NewText("Tag", rt, "ENEMY", 24, new Color(1f, 0.55f, 0.55f));
            tag.fontStyle = FontStyles.Bold;
            Anchor(tag.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, 30), new Vector2(210, 34));

            FillCard(rt, a);
            rt.localScale = Vector3.zero;
            return rt;
        }

        private IEnumerator ScalePop(RectTransform rt, Vector3 a, Vector3 b, float dur, bool overshoot)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / dur);
                rt.localScale = Vector3.LerpUnclamped(a, b, overshoot ? EaseOutBack(p) : p);
                yield return null;
            }
            rt.localScale = b;
        }

        // ------------------------------------------------------- enemy lifecycle

        private void NewEnemy(bool instant)
        {
            _maxHp = 120 + _kills * 60;
            _hp = _maxHp;
            _enemyColor = EnemyPalette[_kills % EnemyPalette.Length];
            _enemy.color = _enemyColor;
            _enemyLabel.text = $"ENEMY  Lv.{_kills + 1}";
            _hpFill.fillAmount = 1f;
            UpdateHpText();

            _enemyAtkInterval = Mathf.Max(1.2f, 3.0f - _kills * 0.15f);
            _enemyAtkTimer = _enemyAtkInterval + 0.6f; // 등장 직후 약간의 유예
            _enemyRt.anchoredPosition = _enemyBasePos;

            if (instant)
            {
                _enemyRt.localScale = Vector3.one;
                _busy = false;
            }
            else
            {
                StartCoroutine(SpawnAnim());
            }
        }

        private IEnumerator SpawnAnim()
        {
            _busy = true;
            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                _enemyRt.localScale = Vector3.one * EaseOutBack(t / 0.35f);
                yield return null;
            }
            _enemyRt.localScale = Vector3.one;
            _busy = false;
        }

        private IEnumerator KillAndRespawn()
        {
            _busy = true;
            _kills++;
            _killText.text = $"KILLS  {_kills}";

            float t = 0f;
            Vector3 s0 = _enemyRt.localScale;
            while (t < 0.22f)
            {
                t += Time.deltaTime;
                float p = t / 0.22f;
                _enemyRt.localScale = Vector3.LerpUnclamped(s0, Vector3.zero, p);
                _enemy.color = Color.Lerp(_enemyColor, new Color(1f, 1f, 1f, 0f), p);
                yield return null;
            }
            _enemyRt.localScale = Vector3.zero;
            yield return new WaitForSeconds(0.12f);
            NewEnemy(instant: false);
        }

        // -------------------------------------------------------------- restart

        private void GameOver()
        {
            _gameOver = true;
            _gameOverInfo.text = $"GAME OVER\nKILLS  {_kills}\n\ntap to retry";
            _gameOverPanel.SetActive(true);
        }

        private void Restart()
        {
            _gameOver = false;
            _gameOverPanel.SetActive(false);
            _kills = 0;
            _killText.text = "KILLS  0";
            _playerHp = PlayerMaxHp;
            UpdatePlayerHpText();
            _playerHpFill.fillAmount = 1f;
            foreach (var c in _cards)
            {
                c.Cd = 0f;
                c.CdOverlay.fillAmount = 0f;
            }
            _enemyAttacking = false;
            NewEnemy(instant: true);
        }

        private void UpdateHpText() => _hpText.text = $"{_hp} / {_maxHp}";
        private void UpdatePlayerHpText() => _playerHpText.text = $"YOU   {_playerHp} / {PlayerMaxHp}";

        private void FloatNumber(string text, float size, Color color, Vector2 pos)
        {
            var t = NewText("Float", _canvasRt, text, size, color);
            t.fontStyle = FontStyles.Bold;
            var jitter = new Vector2(Random.Range(-90f, 90f), Random.Range(-20f, 30f));
            Anchor(t.rectTransform, new Vector2(0.5f, 0.5f), pos + jitter, new Vector2(500, 110));
            StartCoroutine(FloatUp(t));
        }

        private IEnumerator FloatUp(TMP_Text t)
        {
            RectTransform rt = t.rectTransform;
            Vector2 start = rt.anchoredPosition;
            Color c = t.color;
            float el = 0f;
            const float dur = 0.7f;
            while (el < dur)
            {
                el += Time.deltaTime;
                float p = el / dur;
                rt.anchoredPosition = start + new Vector2(0, 150f * p);
                t.color = new Color(c.r, c.g, c.b, 1f - p);
                yield return null;
            }
            Destroy(t.gameObject);
        }

        // -------------------------------------------------------------- UI build

        private void BuildUI()
        {
            var canvasGo = new GameObject("GameCanvas", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0f;
            _canvasRt = canvasGo.GetComponent<RectTransform>();

            var bg = NewImage("BG", _canvasRt, new Color(0.10f, 0.11f, 0.18f));
            Stretch(bg.rectTransform);

            var title = NewText("Title", _canvasRt, "CARDMONG  BATTLE", 46, new Color(1f, 0.85f, 0.2f));
            title.fontStyle = FontStyles.Bold;
            Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -100), new Vector2(960, 70));

            _killText = NewText("Kills", _canvasRt, "KILLS  0", 38, Color.white);
            Anchor(_killText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -170), new Vector2(960, 56));

            var hpBg = NewImage("HpBg", _canvasRt, new Color(0f, 0f, 0f, 0.55f));
            Anchor(hpBg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 540), new Vector2(700, 54));
            _hpFill = NewImage("HpFill", hpBg.rectTransform, new Color(0.86f, 0.30f, 0.32f));
            Stretch(_hpFill.rectTransform, 6f);
            _hpFill.type = Image.Type.Filled;
            _hpFill.fillMethod = Image.FillMethod.Horizontal;
            _hpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _hpFill.fillAmount = 1f;
            _hpText = NewText("HpText", hpBg.rectTransform, "", 30, Color.white);
            Stretch(_hpText.rectTransform);

            _enemy = NewImage("Enemy", _canvasRt, EnemyPalette[0]);
            _enemyRt = _enemy.rectTransform;
            _enemyBasePos = new Vector2(0, 250);
            Anchor(_enemyRt, new Vector2(0.5f, 0.5f), _enemyBasePos, new Vector2(MonsterBase, MonsterBase));
            AddFace(_enemyRt, 1f);

            _enemyLabel = NewText("EnemyLabel", _canvasRt, "", 34, Color.white);
            Anchor(_enemyLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(700, 56));

            // 플레이어 HP (하단)
            _playerHpBasePos = new Vector2(0, 700);
            var pBg = NewImage("PlayerHpBg", _canvasRt, new Color(0f, 0f, 0f, 0.55f));
            _playerHpRt = pBg.rectTransform;
            Anchor(_playerHpRt, new Vector2(0.5f, 0f), _playerHpBasePos, new Vector2(700, 50));
            _playerHpFill = NewImage("PlayerHpFill", _playerHpRt, new Color(0.30f, 0.80f, 0.40f));
            Stretch(_playerHpFill.rectTransform, 6f);
            _playerHpFill.type = Image.Type.Filled;
            _playerHpFill.fillMethod = Image.FillMethod.Horizontal;
            _playerHpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _playerHpFill.fillAmount = 1f;
            _playerHpText = NewText("PlayerHpText", _playerHpRt, "", 28, Color.white);
            Stretch(_playerHpText.rectTransform);

            var hint = NewText("Hint", _canvasRt, "tap a card to attack — the enemy strikes back!", 26, new Color(1f, 1f, 1f, 0.55f));
            Anchor(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 632), new Vector2(960, 44));

            BuildHand();
            BuildGameOver();
        }

        private void BuildHand()
        {
            const float cardW = 230f;
            const float gap = 20f;
            float total = HandSize * cardW + (HandSize - 1) * gap;
            float startX = -total / 2f + cardW / 2f;

            for (int i = 0; i < HandSize; i++)
            {
                Archetype a = Types[Random.Range(0, Types.Length)];
                float x = startX + i * (cardW + gap);
                _cards.Add(MakeCard(i, x, a));
            }
        }

        private Card MakeCard(int slot, float x, Archetype a)
        {
            var card = new Card { Name = a.Name, Color = a.Color, Atk = a.Atk, CdTotal = a.Cooldown, Cd = 0f };

            var panel = NewImage($"Card{slot}", _canvasRt, new Color(0.16f, 0.17f, 0.24f));
            card.Rt = panel.rectTransform;
            Anchor(card.Rt, new Vector2(0.5f, 0f), new Vector2(x, 300), new Vector2(230, 320));

            FillCard(card.Rt, a);

            var cd = NewImage("Cd", card.Rt, new Color(0f, 0f, 0f, 0.62f));
            Stretch(cd.rectTransform);
            cd.type = Image.Type.Filled;
            cd.fillMethod = Image.FillMethod.Vertical;
            cd.fillOrigin = (int)Image.OriginVertical.Top;
            cd.fillAmount = 0f;
            card.CdOverlay = cd;

            return card;
        }

        private void FillCard(RectTransform root, Archetype a)
        {
            var nameText = NewText("Name", root, a.Name, 30, Color.white);
            nameText.fontStyle = FontStyles.Bold;
            Anchor(nameText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -34), new Vector2(210, 40));

            var swatch = NewImage("Swatch", root, a.Color);
            Anchor(swatch.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 16), new Vector2(150, 150));
            AddFace(swatch.rectTransform, 150f / MonsterBase);

            var atkText = NewText("Atk", root, $"ATK {a.Atk}", 30, new Color(1f, 0.85f, 0.3f));
            atkText.fontStyle = FontStyles.Bold;
            Anchor(atkText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(210, 40));
        }

        private void BuildGameOver()
        {
            var panel = NewImage("GameOver", _canvasRt, new Color(0.05f, 0.05f, 0.09f, 0.85f));
            Stretch(panel.rectTransform);
            _gameOverPanel = panel.gameObject;
            _gameOverInfo = NewText("GameOverText", panel.rectTransform, "", 64, Color.white);
            _gameOverInfo.fontStyle = FontStyles.Bold;
            Anchor(_gameOverInfo.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 600));
            _gameOverPanel.SetActive(false);
        }

        private void AddFace(RectTransform parent, float s)
        {
            var eye = new Color(0.12f, 0.12f, 0.16f);
            Anchor(NewImage("eyeL", parent, eye).rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-70 * s, 40 * s), new Vector2(60 * s, 84 * s));
            Anchor(NewImage("eyeR", parent, eye).rectTransform, new Vector2(0.5f, 0.5f), new Vector2(70 * s, 40 * s), new Vector2(60 * s, 84 * s));
            Anchor(NewImage("mouth", parent, eye).rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -64 * s), new Vector2(170 * s, 38 * s));
        }

        // ----------------------------------------------------------- UI helpers

        private Image NewImage(string name, RectTransform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = _box;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private TMP_Text NewText(string name, RectTransform parent, string text, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            return t;
        }

        private Vector2 CanvasPoint(RectTransform rt)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, rt.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRt, screen, null, out var local);
            return local;
        }

        private static void Stretch(RectTransform rt, float pad = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, pad);
            rt.offsetMax = new Vector2(-pad, -pad);
        }

        private static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float xm = x - 1f;
            return 1f + c3 * xm * xm * xm + c1 * xm * xm;
        }

        private static float EaseOutCubic(float x)
        {
            float xm = 1f - x;
            return 1f - xm * xm * xm;
        }
    }
}
