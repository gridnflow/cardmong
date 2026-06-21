using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace Cardmong.Game
{
    /// <summary>
    /// 오프라인 로그라이크 카드 배틀(세로 모바일).
    /// 컬렉션에서 카드 3장을 골라 로드아웃을 만들고(같은 카드 2장 보유 시 2번 넣어 2회 공격),
    /// 한 전투당 3번만 공격할 수 있다. 3번 안에 적을 처치하면 랜덤 카드 1장을 획득(복불복)하고
    /// 더 강한 적과 싸운다. 공격을 다 쓰고도 못 잡거나 HP가 0이면 게임오버.
    /// 모든 UI는 코드로 생성, 입력은 New Input System으로 처리한다.
    /// </summary>
    public class TapBattleGame : MonoBehaviour
    {
        private enum Element { Fire, Water, Earth, Lightning, Wind }
        private enum Phase { Loadout, Battle, Draft, GameOver }

        private struct Archetype
        {
            public string Name;
            public Element Element;
            public Color Color;
            public int Atk;
            public Archetype(string n, Element el, Color c, int atk)
            { Name = n; Element = el; Color = c; Atk = atk; }
        }

        private static readonly Archetype[] Types =
        {
            new Archetype("Ember",   Element.Fire,      new Color(0.94f, 0.45f, 0.24f), 20),
            new Archetype("Splash",  Element.Water,     new Color(0.34f, 0.66f, 0.95f), 16),
            new Archetype("Boulder", Element.Earth,     new Color(0.66f, 0.52f, 0.36f), 30),
            new Archetype("Spark",   Element.Lightning, new Color(0.96f, 0.85f, 0.30f), 14),
            new Archetype("Gale",    Element.Wind,      new Color(0.45f, 0.82f, 0.62f), 18),
        };

        private static readonly Color[] EnemyPalette =
        {
            new Color(0.86f, 0.40f, 0.42f),
            new Color(0.55f, 0.50f, 0.85f),
            new Color(0.40f, 0.70f, 0.66f),
            new Color(0.84f, 0.66f, 0.34f),
            new Color(0.70f, 0.44f, 0.62f),
        };

        private const int LoadoutSize = 3;
        private const int PlayerMaxHp = 100;
        private const float MonsterBase = 360f;

        private Sprite _box;
        private RectTransform _canvasRt;   // 흔들리는 콘텐츠 루트
        private RectTransform _phaseRoot;  // 페이즈마다 갈아끼우는 UI 루트
        private Phase _phase;

        // 런 상태(전투 사이 유지)
        private int[] _collection;
        private readonly List<int> _loadout = new();
        private int _playerHp;
        private int _stage;

        // 전투 상태
        private Image _enemy;
        private RectTransform _enemyRt;
        private TMP_Text _enemyLabel;
        private Image _hpFill;
        private TMP_Text _hpText;
        private int _enemyHp;
        private int _enemyMaxHp;
        private Color _enemyColor;
        private Element _enemyElement;
        private Vector2 _enemyBasePos;

        private Image _playerHpFill;
        private TMP_Text _playerHpText;
        private RectTransform _playerHpRt;
        private Vector2 _playerHpBasePos;

        private bool[] _used;
        private RectTransform[] _slotRt;
        private Image[] _slotGrey;
        private int _attacksLeft;
        private TMP_Text _attackText;

        private bool _busy;
        private bool _enemyAttacking;
        private float _enemyHit;
        private float _playerHurt;
        private float _enemyAtkTimer;
        private float _enemyAtkInterval;

        private AudioSource _audio;
        private AudioClip[] _elemSfx;
        private AudioClip _sfxHit;
        private AudioClip _sfxDeath;
        private AudioClip _sfxGameOver;
        private float _shake;
        private float _shakeMag;
        private const float ShakeDur = 0.28f;

        private readonly List<(RectTransform rt, System.Action onTap)> _taps = new();

        // ----------------------------------------------------------------- core

        private void Start()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;

            var tex = Texture2D.whiteTexture;
            _box = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                 new Vector2(0.5f, 0.5f), 100f);

            BuildRoot();
            BuildAudio();
            StartRun();
        }

        private void Update()
        {
            var p = Pointer.current;
            bool pressed = p != null && p.press.wasPressedThisFrame;

            if (_shake > 0f)
            {
                _shake -= Time.deltaTime;
                float sk = Mathf.Clamp01(_shake / ShakeDur);
                Vector2 o = Random.insideUnitCircle * _shakeMag * sk;
                _canvasRt.localPosition = new Vector3(o.x, o.y, 0f);
                if (_shake <= 0f) { _shakeMag = 0f; _canvasRt.localPosition = Vector3.zero; }
            }

            if (_phase == Phase.Battle) BattleUpdate();

            if (pressed && !_busy)
            {
                Vector2 sp = p.position.ReadValue();
                for (int i = _taps.Count - 1; i >= 0; i--)
                {
                    var tt = _taps[i];
                    if (tt.rt != null &&
                        RectTransformUtility.RectangleContainsScreenPoint(tt.rt, sp, null))
                    {
                        tt.onTap?.Invoke();
                        break;
                    }
                }
            }
        }

        private void BattleUpdate()
        {
            if (_hpFill != null)
                _hpFill.fillAmount = Mathf.MoveTowards(_hpFill.fillAmount,
                    _enemyMaxHp > 0 ? (float)_enemyHp / _enemyMaxHp : 0f, 2.5f * Time.deltaTime);
            if (_playerHpFill != null)
                _playerHpFill.fillAmount = Mathf.MoveTowards(_playerHpFill.fillAmount,
                    (float)_playerHp / PlayerMaxHp, 2.5f * Time.deltaTime);

            if (_enemyHit > 0f && !_busy)
            {
                _enemyHit -= Time.deltaTime;
                float k = Mathf.Clamp01(_enemyHit / 0.14f);
                if (_enemyRt != null) _enemyRt.localScale = Vector3.one * (1f + 0.16f * k);
            }

            if (_playerHurt > 0f)
            {
                _playerHurt -= Time.deltaTime;
                float k = Mathf.Clamp01(_playerHurt / 0.3f);
                float off = Mathf.Sin(_playerHurt * 60f) * 14f * k;
                if (_playerHpRt != null) _playerHpRt.anchoredPosition = _playerHpBasePos + new Vector2(off, 0);
            }
            else if (_playerHpRt != null)
            {
                _playerHpRt.anchoredPosition = _playerHpBasePos;
            }

            if (!_busy && !_enemyAttacking)
            {
                _enemyAtkTimer -= Time.deltaTime;
                if (_enemyAtkTimer <= 0f)
                {
                    _enemyAtkTimer = _enemyAtkInterval;
                    StartCoroutine(EnemyAttack());
                }
            }
        }

        private void StartRun()
        {
            _playerHp = PlayerMaxHp;
            _stage = 0;
            _collection = new int[Types.Length];
            for (int i = 0; i < 5; i++) _collection[Random.Range(0, Types.Length)]++;
            EnterLoadout();
        }

        private void ClearPhase()
        {
            StopAllCoroutines();
            if (_phaseRoot != null)
                for (int i = _phaseRoot.childCount - 1; i >= 0; i--)
                    Destroy(_phaseRoot.GetChild(i).gameObject);
            _taps.Clear();
            _busy = false;
            _enemyAttacking = false;
            _enemyHit = 0f;
            _playerHurt = 0f;
            _shake = 0f;
            _shakeMag = 0f;
            if (_canvasRt != null) _canvasRt.localPosition = Vector3.zero;

            _hpFill = null; _hpText = null; _enemyRt = null; _enemy = null; _enemyLabel = null;
            _playerHpFill = null; _playerHpText = null; _playerHpRt = null; _attackText = null;
        }

        // -------------------------------------------------------------- LOADOUT

        private void EnterLoadout()
        {
            _loadout.Clear();
            RebuildLoadout();
        }

        private void RebuildLoadout()
        {
            ClearPhase();
            _phase = Phase.Loadout;

            var title = NewText("Title", _phaseRoot, $"STAGE {_stage + 1}", 52, new Color(1f, 0.85f, 0.2f));
            title.fontStyle = FontStyles.Bold;
            Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -90), new Vector2(960, 64));

            var sub = NewText("Sub", _phaseRoot, $"PICK {LoadoutSize} CARDS — duplicates = extra attacks", 26, new Color(1, 1, 1, 0.7f));
            Anchor(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -158), new Vector2(1000, 38));

            var hp = NewText("HP", _phaseRoot, $"YOUR HP  {_playerHp}/{PlayerMaxHp}", 30, new Color(0.5f, 0.9f, 0.55f));
            Anchor(hp.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -214), new Vector2(900, 40));

            // 컬렉션
            var owned = new List<int>();
            for (int i = 0; i < Types.Length; i++) if (_collection[i] > 0) owned.Add(i);

            var clabel = NewText("CL", _phaseRoot, "COLLECTION", 26, new Color(1, 1, 1, 0.6f));
            Anchor(clabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 470), new Vector2(900, 36));

            const float cw = 190f, gap = 12f;
            float total = owned.Count * cw + Mathf.Max(0, owned.Count - 1) * gap;
            float startX = -total / 2f + cw / 2f;
            for (int idx = 0; idx < owned.Count; idx++)
            {
                int ti = owned[idx];
                int inHand = CountIn(_loadout, ti);
                int remain = _collection[ti] - inHand;
                float x = startX + idx * (cw + gap);
                string footer = $"x{_collection[ti]}" + (inHand > 0 ? $"  (in {inHand})" : "");
                Color tint = remain > 0 ? new Color(0.16f, 0.17f, 0.24f) : new Color(0.11f, 0.11f, 0.13f);
                var card = BuildCard(_phaseRoot, Types[ti], new Vector2(x, 300), new Vector2(cw, 250), tint, footer);
                if (remain > 0 && _loadout.Count < LoadoutSize)
                {
                    int captured = ti;
                    AddTap(card.rectTransform, () => { _loadout.Add(captured); RebuildLoadout(); });
                }
            }

            // 로드아웃 슬롯
            var llabel = NewText("LL", _phaseRoot, $"YOUR HAND  ({_loadout.Count}/{LoadoutSize})", 28, new Color(1, 1, 1, 0.8f));
            Anchor(llabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 90), new Vector2(900, 38));

            const float sw = 200f, sgap = 18f;
            float stot = LoadoutSize * sw + (LoadoutSize - 1) * sgap;
            float sx0 = -stot / 2f + sw / 2f;
            for (int s = 0; s < LoadoutSize; s++)
            {
                float x = sx0 + s * (sw + sgap);
                if (s < _loadout.Count)
                {
                    int ti = _loadout[s];
                    var card = BuildCard(_phaseRoot, Types[ti], new Vector2(x, -110), new Vector2(sw, 260), new Color(0.20f, 0.22f, 0.30f), null);
                    int slot = s;
                    AddTap(card.rectTransform, () => { _loadout.RemoveAt(slot); RebuildLoadout(); });
                }
                else
                {
                    var empty = NewImage("Empty", _phaseRoot, new Color(1, 1, 1, 0.07f));
                    Anchor(empty.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, -110), new Vector2(sw, 260));
                    var q = NewText("Q", empty.rectTransform, "+", 70, new Color(1, 1, 1, 0.3f));
                    Stretch(q.rectTransform);
                }
            }

            bool ready = _loadout.Count > 0;
            var fight = NewImage("Fight", _phaseRoot, ready ? new Color(0.90f, 0.35f, 0.30f) : new Color(0.3f, 0.3f, 0.34f));
            Anchor(fight.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 230), new Vector2(720, 150));
            var fl = NewText("FL", fight.rectTransform, ready ? $"FIGHT  ({_loadout.Count}/{LoadoutSize})" : "PICK CARDS", 46, Color.white);
            fl.fontStyle = FontStyles.Bold;
            Stretch(fl.rectTransform);
            if (ready) AddTap(fight.rectTransform, EnterBattle);
        }

        // --------------------------------------------------------------- BATTLE

        private void EnterBattle()
        {
            ClearPhase();
            _phase = Phase.Battle;

            _enemyElement = (Element)Random.Range(0, 5);
            _enemyColor = EnemyPalette[_stage % EnemyPalette.Length];
            _enemyMaxHp = 70 + _stage * 45;
            _enemyHp = _enemyMaxHp;

            var title = NewText("Title", _phaseRoot, $"STAGE {_stage + 1}", 40, new Color(1f, 0.85f, 0.2f));
            title.fontStyle = FontStyles.Bold;
            Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -80), new Vector2(900, 56));

            var hpBg = NewImage("HpBg", _phaseRoot, new Color(0f, 0f, 0f, 0.55f));
            Anchor(hpBg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 540), new Vector2(700, 54));
            _hpFill = NewImage("HpFill", hpBg.rectTransform, new Color(0.86f, 0.30f, 0.32f));
            Stretch(_hpFill.rectTransform, 6f);
            _hpFill.type = Image.Type.Filled;
            _hpFill.fillMethod = Image.FillMethod.Horizontal;
            _hpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _hpFill.fillAmount = 1f;
            _hpText = NewText("HpText", hpBg.rectTransform, "", 30, Color.white);
            Stretch(_hpText.rectTransform);

            _enemy = NewImage("Enemy", _phaseRoot, _enemyColor);
            _enemyRt = _enemy.rectTransform;
            _enemyBasePos = new Vector2(0, 250);
            Anchor(_enemyRt, new Vector2(0.5f, 0.5f), _enemyBasePos, new Vector2(MonsterBase, MonsterBase));
            AddFace(_enemyRt, 1f);

            _enemyLabel = NewText("EL", _phaseRoot, $"ENEMY Lv.{_stage + 1}   [{ElementName(_enemyElement)}]", 32, Color.white);
            Anchor(_enemyLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(820, 50));
            UpdateHpText();

            _playerHpBasePos = new Vector2(0, 700);
            var pBg = NewImage("PHpBg", _phaseRoot, new Color(0f, 0f, 0f, 0.55f));
            _playerHpRt = pBg.rectTransform;
            Anchor(_playerHpRt, new Vector2(0.5f, 0f), _playerHpBasePos, new Vector2(700, 50));
            _playerHpFill = NewImage("PHpFill", _playerHpRt, new Color(0.30f, 0.80f, 0.40f));
            Stretch(_playerHpFill.rectTransform, 6f);
            _playerHpFill.type = Image.Type.Filled;
            _playerHpFill.fillMethod = Image.FillMethod.Horizontal;
            _playerHpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _playerHpFill.fillAmount = Mathf.Clamp01((float)_playerHp / PlayerMaxHp);
            _playerHpText = NewText("PHpText", _playerHpRt, "", 28, Color.white);
            Stretch(_playerHpText.rectTransform);
            UpdatePlayerHpText();

            int n = _loadout.Count;
            _used = new bool[n];
            _slotRt = new RectTransform[n];
            _slotGrey = new Image[n];
            _attacksLeft = n;

            const float cw = 210f, gap = 16f;
            float total = n * cw + Mathf.Max(0, n - 1) * gap;
            float sx0 = -total / 2f + cw / 2f;
            for (int s = 0; s < n; s++)
            {
                float x = sx0 + s * (cw + gap);
                var card = BuildCard(_phaseRoot, Types[_loadout[s]], new Vector2(x, 300), new Vector2(cw, 290), new Color(0.16f, 0.17f, 0.24f), null);
                _slotRt[s] = card.rectTransform;
                var grey = NewImage("Used", card.rectTransform, new Color(0f, 0f, 0f, 0.62f));
                Stretch(grey.rectTransform);
                grey.gameObject.SetActive(false);
                _slotGrey[s] = grey;
                int slot = s;
                AddTap(card.rectTransform, () => PlayerAttack(slot));
            }

            _attackText = NewText("AT", _phaseRoot, $"ATTACKS LEFT  {_attacksLeft}", 30, new Color(1f, 0.8f, 0.3f));
            Anchor(_attackText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 640), new Vector2(900, 42));

            _enemyAtkInterval = Mathf.Max(1.4f, 3.2f - _stage * 0.15f);
            _enemyAtkTimer = _enemyAtkInterval + 0.8f;

            StartCoroutine(EnemySpawn());
        }

        private IEnumerator EnemySpawn()
        {
            _busy = true;
            _enemyRt.localScale = Vector3.zero;
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

        private void PlayerAttack(int slot)
        {
            if (_busy || _used[slot]) return;
            _used[slot] = true;
            _slotGrey[slot].gameObject.SetActive(true);
            _attacksLeft--;
            _attackText.text = $"ATTACKS LEFT  {_attacksLeft}";
            StartCoroutine(PlayerAttackRoutine(slot));
        }

        private IEnumerator PlayerAttackRoutine(int slot)
        {
            Archetype a = Types[_loadout[slot]];
            var token = NewImage("Token", _phaseRoot, a.Color);
            AddFace(token.rectTransform, 120f / MonsterBase);
            var trt = token.rectTransform;
            Vector2 from = CanvasPoint(_slotRt[slot]);
            Anchor(trt, new Vector2(0.5f, 0.5f), from, new Vector2(120, 120));
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

            PlayEffect(a.Element, CanvasPoint(_enemyRt));
            Sfx(a.Element);
            DamageEnemy(a);

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
            if (token) Destroy(token.gameObject);

            if (!_busy && _attacksLeft <= 0 && _enemyHp > 0)
                StartCoroutine(OutOfAttacksRoutine());
        }

        private void DamageEnemy(Archetype a)
        {
            bool crit = Random.value < 0.12f;
            float mult = ElementMultiplier(a.Element, _enemyElement);
            int baseDmg = Mathf.Max(1, a.Atk + Random.Range(-2, 3));
            int dmg = Mathf.Max(1, (int)(baseDmg * mult)) * (crit ? 2 : 1);

            _enemyHp = Mathf.Max(0, _enemyHp - dmg);
            UpdateHpText();
            _enemyHit = 0.14f;
            Shake(crit ? 26f : (mult > 1f ? 16f : 10f));

            string tag = crit ? "CRIT " : (mult > 1.1f ? "WEAK " : (mult < 0.9f ? "RESIST " : ""));
            Color col = crit ? new Color(1f, 0.55f, 0.15f)
                      : (mult > 1.1f ? new Color(1f, 0.8f, 0.2f)
                      : (mult < 0.9f ? new Color(0.7f, 0.8f, 1f) : Color.white));
            FloatNumber(tag + dmg, crit ? 80 : 56, col, CanvasPoint(_enemyRt) + new Vector2(0, 130));

            if (_enemyHp <= 0) StartCoroutine(WinRoutine());
        }

        private IEnumerator WinRoutine()
        {
            _busy = true;
            Shake(22f);
            if (_audio != null) _audio.PlayOneShot(_sfxDeath);

            float t = 0f;
            Vector3 s0 = _enemyRt.localScale;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                float p = t / 0.25f;
                _enemyRt.localScale = Vector3.LerpUnclamped(s0, Vector3.zero, p);
                _enemy.color = Color.Lerp(_enemyColor, new Color(1f, 1f, 1f, 0f), p);
                yield return null;
            }
            _enemyRt.localScale = Vector3.zero;
            _stage++;
            _playerHp = Mathf.Min(PlayerMaxHp, _playerHp + 15); // 처치 보상 회복
            yield return new WaitForSeconds(0.2f);
            EnterDraft();
        }

        private IEnumerator OutOfAttacksRoutine()
        {
            _busy = true;
            var msg = NewText("OOA", _phaseRoot, "OUT OF ATTACKS!", 56, new Color(1f, 0.5f, 0.5f));
            msg.fontStyle = FontStyles.Bold;
            Anchor(msg.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 80));
            yield return new WaitForSeconds(1.0f);
            EnterGameOver("OUT OF ATTACKS");
        }

        // -------------------------------------------------------- enemy attacks

        private IEnumerator EnemyAttack()
        {
            if (_busy) yield break;
            _enemyAttacking = true;

            Archetype a = Types[Random.Range(0, Types.Length)];
            RectTransform card = SpawnEnemyCard(a);
            yield return ScalePop(card, Vector3.zero, Vector3.one, 0.18f, true);
            yield return new WaitForSeconds(0.3f);

            var token = NewImage("EToken", _phaseRoot, a.Color);
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

            if (!_busy)
            {
                PlayEffect(a.Element, CanvasPoint(_playerHpRt) + new Vector2(0, 60));
                Sfx(a.Element);
                DamagePlayer(a);
            }

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
            if (token) Destroy(token.gameObject);

            yield return ScalePop(card, Vector3.one, Vector3.zero, 0.14f, false);
            if (card) Destroy(card.gameObject);
            _enemyAttacking = false;
        }

        private void DamagePlayer(Archetype a)
        {
            int dmg = Mathf.Max(1, a.Atk + _stage * 2 + Random.Range(-2, 3));
            _playerHp = Mathf.Max(0, _playerHp - dmg);
            UpdatePlayerHpText();
            _playerHurt = 0.3f;
            Shake(16f);
            if (_audio != null) _audio.PlayOneShot(_sfxHit);
            FloatNumber("-" + dmg, 60, new Color(1f, 0.45f, 0.45f),
                        CanvasPoint(_playerHpRt) + new Vector2(0, 70));

            if (_playerHp <= 0)
            {
                _enemyAttacking = false;
                EnterGameOver("HP DEPLETED");
            }
        }

        private RectTransform SpawnEnemyCard(Archetype a)
        {
            var panel = BuildCard(_phaseRoot, a, new Vector2(0, -120), new Vector2(220, 300),
                                  new Color(0.30f, 0.16f, 0.18f), "ENEMY");
            panel.rectTransform.localScale = Vector3.zero;
            return panel.rectTransform;
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

        // ---------------------------------------------------------- DRAFT / OVER

        private void EnterDraft()
        {
            ClearPhase();
            _phase = Phase.Draft;

            int drafted = Random.Range(0, Types.Length);
            _collection[drafted]++;

            var v = NewText("V", _phaseRoot, "VICTORY!", 64, new Color(1f, 0.85f, 0.2f));
            v.fontStyle = FontStyles.Bold;
            Anchor(v.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -220), new Vector2(900, 90));
            var nc = NewText("NC", _phaseRoot, "NEW CARD!  (lucky draw)", 34, Color.white);
            Anchor(nc.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -320), new Vector2(900, 50));

            var card = BuildCard(_phaseRoot, Types[drafted], new Vector2(0, -40), new Vector2(300, 400),
                                 new Color(0.18f, 0.20f, 0.30f), $"NOW OWN x{_collection[drafted]}");
            card.rectTransform.localScale = Vector3.zero;
            StartCoroutine(ScalePop(card.rectTransform, Vector3.zero, Vector3.one, 0.4f, true));

            var cont = NewText("C", _phaseRoot, "tap to continue", 30, new Color(1, 1, 1, 0.6f));
            Anchor(cont.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 260), new Vector2(900, 44));

            var full = NewImage("Full", _phaseRoot, new Color(0, 0, 0, 0f));
            Stretch(full.rectTransform);
            AddTap(full.rectTransform, EnterLoadout);
        }

        private void EnterGameOver(string reason)
        {
            ClearPhase();
            _phase = Phase.GameOver;
            if (_audio != null) _audio.PlayOneShot(_sfxGameOver);

            var panel = NewImage("GO", _phaseRoot, new Color(0.05f, 0.05f, 0.09f, 0.92f));
            Stretch(panel.rectTransform);
            var txt = NewText("GOT", panel.rectTransform,
                $"GAME OVER\n{reason}\n\nSTAGE  {_stage}\n\ntap to restart", 54, Color.white);
            txt.fontStyle = FontStyles.Bold;
            Anchor(txt.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(950, 720));
            AddTap(panel.rectTransform, StartRun);
        }

        // ------------------------------------------------------------- elements

        private static bool Beats(Element a, Element b) =>
            (a == Element.Fire && b == Element.Wind) ||
            (a == Element.Wind && b == Element.Earth) ||
            (a == Element.Earth && b == Element.Lightning) ||
            (a == Element.Lightning && b == Element.Water) ||
            (a == Element.Water && b == Element.Fire);

        private static float ElementMultiplier(Element atk, Element def)
        {
            if (Beats(atk, def)) return 1.5f;
            if (Beats(def, atk)) return 0.75f;
            return 1f;
        }

        private static string ElementName(Element e) => e.ToString().ToUpper();

        private static Color ElementColor(Element e) => e switch
        {
            Element.Fire => new Color(0.95f, 0.55f, 0.3f),
            Element.Water => new Color(0.45f, 0.7f, 1f),
            Element.Earth => new Color(0.78f, 0.64f, 0.46f),
            Element.Lightning => new Color(0.97f, 0.88f, 0.4f),
            Element.Wind => new Color(0.55f, 0.9f, 0.7f),
            _ => Color.white,
        };

        // ----------------------------------------------------------- 카드 비주얼

        private Image BuildCard(RectTransform parent, Archetype a, Vector2 pos, Vector2 size, Color tint, string footer)
        {
            var panel = NewImage("Card", parent, tint);
            var rt = panel.rectTransform;
            Anchor(rt, new Vector2(0.5f, 0.5f), pos, size);
            float s = size.x / 220f;

            var nameText = NewText("Name", rt, a.Name, 28 * s, Color.white);
            nameText.fontStyle = FontStyles.Bold;
            Anchor(nameText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -24 * s), new Vector2(size.x * 0.92f, 38 * s));

            var swatch = NewImage("Swatch", rt, a.Color);
            Anchor(swatch.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 22 * s), new Vector2(118 * s, 118 * s));
            AddFace(swatch.rectTransform, 118f * s / MonsterBase);

            var atk = NewText("Atk", rt, $"ATK {a.Atk}", 26 * s, new Color(1f, 0.85f, 0.3f));
            atk.fontStyle = FontStyles.Bold;
            Anchor(atk.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 54 * s), new Vector2(size.x * 0.9f, 32 * s));

            var elem = NewText("Elem", rt, ElementName(a.Element), 22 * s, ElementColor(a.Element));
            Anchor(elem.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 22 * s), new Vector2(size.x * 0.9f, 28 * s));

            if (footer != null)
            {
                var f = NewText("Foot", rt, footer, 24 * s, new Color(1, 1, 1, 0.9f));
                f.fontStyle = FontStyles.Bold;
                Anchor(f.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, 26 * s), new Vector2(size.x * 1.1f, 32 * s));
            }
            return panel;
        }

        private static int CountIn(List<int> list, int value)
        {
            int c = 0;
            foreach (var x in list) if (x == value) c++;
            return c;
        }

        // ------------------------------------------------------------- 떠오르는 수

        private void FloatNumber(string text, float size, Color color, Vector2 pos)
        {
            var t = NewText("Float", _phaseRoot, text, size, color);
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
            if (t) Destroy(t.gameObject);
        }

        // ---------------------------------------------------------- 속성 이펙트

        private void PlayEffect(Element e, Vector2 center)
        {
            switch (e)
            {
                case Element.Fire:
                    Burst(center, 16, 90f, 38f, 200f, 400f, -160f, 0.55f, 44f,
                          new Color(1f, 0.80f, 0.28f), new Color(0.85f, 0.12f, 0.04f), 0f);
                    break;
                case Element.Water:
                    Burst(center, 18, 90f, 120f, 240f, 470f, 760f, 0.6f, 30f,
                          new Color(0.58f, 0.84f, 1f), new Color(0.18f, 0.44f, 0.92f), 0f);
                    break;
                case Element.Earth:
                    Burst(center, 11, 90f, 80f, 240f, 440f, 980f, 0.6f, 48f,
                          new Color(0.70f, 0.56f, 0.40f), new Color(0.34f, 0.26f, 0.17f), 240f);
                    break;
                case Element.Lightning:
                    Flash(new Color(1f, 0.96f, 0.45f, 0.35f));
                    Burst(center, 16, 0f, 180f, 520f, 850f, 0f, 0.24f, 18f,
                          new Color(1f, 1f, 0.65f), new Color(1f, 0.85f, 0.20f), 0f);
                    break;
                case Element.Wind:
                    Burst(center, 14, 0f, 180f, 200f, 380f, -40f, 0.5f, 28f,
                          new Color(0.64f, 0.96f, 0.74f), new Color(0.28f, 0.68f, 0.48f), 340f);
                    break;
            }
        }

        private void Burst(Vector2 center, int count, float baseDeg, float spreadDeg,
                           float spMin, float spMax, float gravity, float life, float size,
                           Color cA, Color cB, float spin)
        {
            for (int i = 0; i < count; i++)
            {
                float ang = (baseDeg + Random.Range(-spreadDeg, spreadDeg)) * Mathf.Deg2Rad;
                float sp = Random.Range(spMin, spMax);
                Vector2 vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * sp;
                var img = NewImage("fx", _phaseRoot, cA);
                Vector2 jitter = new Vector2(Random.Range(-14f, 14f), Random.Range(-14f, 14f));
                Anchor(img.rectTransform, new Vector2(0.5f, 0.5f), center + jitter, new Vector2(size, size));
                float s = spin * (Random.value < 0.5f ? -1f : 1f);
                StartCoroutine(Particle(img, vel, gravity, life * Random.Range(0.8f, 1.2f), cA, cB, s));
            }
        }

        private IEnumerator Particle(Image img, Vector2 vel, float gravity, float life, Color cA, Color cB, float spin)
        {
            RectTransform rt = img.rectTransform;
            Vector2 pos = rt.anchoredPosition;
            float t = 0f;
            while (t < life)
            {
                float dt = Time.deltaTime;
                pos += vel * dt;
                vel.y -= gravity * dt;
                rt.anchoredPosition = pos;
                float p = t / life;
                rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.2f, p);
                if (spin != 0f) rt.Rotate(0f, 0f, spin * dt);
                Color c = Color.Lerp(cA, cB, p);
                img.color = new Color(c.r, c.g, c.b, 1f - p);
                t += dt;
                yield return null;
            }
            if (img) Destroy(img.gameObject);
        }

        private void Flash(Color color)
        {
            var img = NewImage("Flash", _phaseRoot, color);
            Stretch(img.rectTransform);
            StartCoroutine(FadeFlash(img, color));
        }

        private IEnumerator FadeFlash(Image img, Color c)
        {
            float t = 0f;
            const float dur = 0.18f;
            while (t < dur)
            {
                t += Time.deltaTime;
                img.color = new Color(c.r, c.g, c.b, Mathf.Lerp(c.a, 0f, t / dur));
                yield return null;
            }
            if (img) Destroy(img.gameObject);
        }

        // -------------------------------------------------- 화면 흔들림 / 사운드

        private void Shake(float magnitude)
        {
            _shakeMag = Mathf.Max(_shakeMag, magnitude);
            _shake = ShakeDur;
        }

        private void Sfx(Element e)
        {
            if (_audio != null && _elemSfx != null)
                _audio.PlayOneShot(_elemSfx[(int)e]);
        }

        private void BuildAudio()
        {
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.volume = 0.6f;

            _elemSfx = new AudioClip[5];
            _elemSfx[(int)Element.Fire]      = Noise(0.40f, 6f, 0.30f, 0.6f);
            _elemSfx[(int)Element.Water]     = Tone(1100f, 360f, 0.22f, 9f, 0.5f);
            _elemSfx[(int)Element.Earth]     = Tone(150f, 60f, 0.28f, 7f, 0.45f);
            _elemSfx[(int)Element.Lightning] = Noise(0.26f, 11f, 0.92f, 0.7f);
            _elemSfx[(int)Element.Wind]      = Noise(0.50f, 3f, 0.14f, 0.45f);

            _sfxHit      = Noise(0.14f, 20f, 0.60f, 0.7f);
            _sfxDeath    = Tone(520f, 80f, 0.50f, 5f, 0f);
            _sfxGameOver = Tone(320f, 60f, 0.90f, 3f, 0f);
        }

        private static AudioClip Noise(float dur, float decay, float lowpass, float gain)
        {
            const int rate = 44100;
            int n = Mathf.Max(1, (int)(rate * dur));
            var data = new float[n];
            float prev = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                float w = Random.Range(-1f, 1f);
                prev = Mathf.Lerp(prev, w, lowpass);
                data[i] = prev * Mathf.Exp(-decay * t) * gain;
            }
            var c = AudioClip.Create("noise", n, 1, rate, false);
            c.SetData(data, 0);
            return c;
        }

        private static AudioClip Tone(float f0, float f1, float dur, float decay, float wob)
        {
            const int rate = 44100;
            int n = Mathf.Max(1, (int)(rate * dur));
            var data = new float[n];
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                float f = Mathf.Lerp(f0, f1, t / dur) + (wob > 0f ? Mathf.Sin(t * 40f) * 30f * wob : 0f);
                phase += 6.283185307 * f / rate;
                data[i] = (float)System.Math.Sin(phase) * Mathf.Exp(-decay * t) * 0.6f;
            }
            var c = AudioClip.Create("tone", n, 1, rate, false);
            c.SetData(data, 0);
            return c;
        }

        // -------------------------------------------------------------- UI 기초

        private void BuildRoot()
        {
            var canvasGo = new GameObject("GameCanvas", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0f;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(canvasGo.transform, false);
            _canvasRt = content.GetComponent<RectTransform>();
            Stretch(_canvasRt);

            var bg = NewImage("BG", _canvasRt, new Color(0.10f, 0.11f, 0.18f));
            Stretch(bg.rectTransform);

            var phase = new GameObject("Phase", typeof(RectTransform));
            phase.transform.SetParent(content.transform, false);
            _phaseRoot = phase.GetComponent<RectTransform>();
            Stretch(_phaseRoot);
        }

        private void UpdateHpText()
        {
            if (_hpText != null) _hpText.text = $"{_enemyHp} / {_enemyMaxHp}";
        }

        private void UpdatePlayerHpText()
        {
            if (_playerHpText != null) _playerHpText.text = $"YOU   {_playerHp} / {PlayerMaxHp}";
        }

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

        private void AddTap(RectTransform rt, System.Action onTap)
        {
            _taps.Add((rt, onTap));
        }

        private void AddFace(RectTransform parent, float s)
        {
            var eye = new Color(0.12f, 0.12f, 0.16f);
            Anchor(NewImage("eyeL", parent, eye).rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-70 * s, 40 * s), new Vector2(60 * s, 84 * s));
            Anchor(NewImage("eyeR", parent, eye).rectTransform, new Vector2(0.5f, 0.5f), new Vector2(70 * s, 40 * s), new Vector2(60 * s, 84 * s));
            Anchor(NewImage("mouth", parent, eye).rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -64 * s), new Vector2(170 * s, 38 * s));
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
