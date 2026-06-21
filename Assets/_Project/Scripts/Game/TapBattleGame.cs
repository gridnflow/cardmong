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
    /// 하단의 카드를 누르면 그 카드 안의 몬스터가 튀어나와 적에게 날아가 공격하고
    /// 다시 카드로 돌아가며, 카드는 쿨다운 후 재사용된다. 적을 처치하면 더 강한
    /// 적이 등장한다. 모든 UI는 코드로 생성하고 입력은 New Input System으로 처리한다.
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
        private const float MonsterBase = 360f; // 얼굴 비율 기준 크기

        private Sprite _box;
        private RectTransform _canvasRt;
        private Image _enemy;
        private RectTransform _enemyRt;
        private TMP_Text _enemyLabel;
        private Image _hpFill;
        private TMP_Text _hpText;
        private TMP_Text _killText;
        private readonly List<Card> _cards = new();

        private int _kills;
        private int _maxHp;
        private int _hp;
        private bool _busy;
        private float _enemyHit;
        private Color _enemyColor;

        private void Start()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;

            var tex = Texture2D.whiteTexture;
            _box = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                 new Vector2(0.5f, 0.5f), 100f);

            BuildUI();
            NewEnemy(instant: true);
        }

        private void Update()
        {
            // 카드 쿨다운
            foreach (var c in _cards)
            {
                if (c.Cd > 0f)
                {
                    c.Cd = Mathf.Max(0f, c.Cd - Time.deltaTime);
                    c.CdOverlay.fillAmount = c.Cd / c.CdTotal;
                }
            }

            // 적 타격 반동
            if (_enemyHit > 0f)
            {
                _enemyHit -= Time.deltaTime;
                float k = Mathf.Clamp01(_enemyHit / 0.14f);
                _enemyRt.localScale = Vector3.one * (1f + 0.16f * k);
            }

            // HP 바 부드럽게
            float target = _maxHp > 0 ? (float)_hp / _maxHp : 0f;
            _hpFill.fillAmount = Mathf.MoveTowards(_hpFill.fillAmount, target, 2.5f * Time.deltaTime);

            // 입력: 카드 탭
            var p = Pointer.current;
            if (!_busy && p != null && p.press.wasPressedThisFrame)
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

        // ------------------------------------------------------------- gameplay

        private IEnumerator PlayCard(Card c)
        {
            c.Cd = c.CdTotal;
            c.CdOverlay.fillAmount = 1f;

            // 카드에서 몬스터가 튀어나옴
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

            DealDamage(c.Atk);

            // 카드로 복귀
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

        private void DealDamage(int atk)
        {
            if (_busy) return;

            bool crit = Random.value < 0.12f;
            int dmg = Mathf.Max(1, atk + Random.Range(-2, 3)) * (crit ? 2 : 1);

            _hp = Mathf.Max(0, _hp - dmg);
            UpdateHpText();
            _enemyHit = 0.14f;
            SpawnDamage(dmg, crit, CanvasPoint(_enemyRt) + new Vector2(0, 130));

            if (_hp <= 0)
                StartCoroutine(KillAndRespawn());
        }

        private void NewEnemy(bool instant)
        {
            _maxHp = 120 + _kills * 60;
            _hp = _maxHp;
            _enemyColor = EnemyPalette[_kills % EnemyPalette.Length];
            _enemy.color = _enemyColor;
            _enemyLabel.text = $"ENEMY  Lv.{_kills + 1}";
            _hpFill.fillAmount = 1f;
            UpdateHpText();

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

        private void UpdateHpText() => _hpText.text = $"{_hp} / {_maxHp}";

        private void SpawnDamage(int dmg, bool crit, Vector2 pos)
        {
            var t = NewText("Dmg", _canvasRt, (crit ? "CRIT " : "") + dmg,
                            crit ? 80 : 56, crit ? new Color(1f, 0.55f, 0.15f) : Color.white);
            t.fontStyle = FontStyles.Bold;
            var jitter = new Vector2(Random.Range(-90f, 90f), Random.Range(-20f, 30f));
            Anchor(t.rectTransform, new Vector2(0.5f, 0.5f), pos + jitter, new Vector2(500, 110));
            StartCoroutine(FloatDamage(t));
        }

        private IEnumerator FloatDamage(TMP_Text t)
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
            Anchor(_enemyRt, new Vector2(0.5f, 0.5f), new Vector2(0, 250), new Vector2(MonsterBase, MonsterBase));
            AddFace(_enemyRt, 1f);

            _enemyLabel = NewText("EnemyLabel", _canvasRt, "", 34, Color.white);
            Anchor(_enemyLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(700, 56));

            var hint = NewText("Hint", _canvasRt, "tap a card to send your monster", 28, new Color(1f, 1f, 1f, 0.55f));
            Anchor(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 660), new Vector2(900, 48));

            BuildHand();
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

            var nameText = NewText("Name", card.Rt, a.Name, 30, Color.white);
            nameText.fontStyle = FontStyles.Bold;
            Anchor(nameText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -34), new Vector2(210, 40));

            var swatch = NewImage("Swatch", card.Rt, a.Color);
            Anchor(swatch.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 16), new Vector2(150, 150));
            AddFace(swatch.rectTransform, 150f / MonsterBase);

            var atkText = NewText("Atk", card.Rt, $"ATK {a.Atk}", 30, new Color(1f, 0.85f, 0.3f));
            atkText.fontStyle = FontStyles.Bold;
            Anchor(atkText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(210, 40));

            var cd = NewImage("Cd", card.Rt, new Color(0f, 0f, 0f, 0.62f));
            Stretch(cd.rectTransform);
            cd.type = Image.Type.Filled;
            cd.fillMethod = Image.FillMethod.Vertical;
            cd.fillOrigin = (int)Image.OriginVertical.Top;
            cd.fillAmount = 0f;
            card.CdOverlay = cd;

            return card;
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
