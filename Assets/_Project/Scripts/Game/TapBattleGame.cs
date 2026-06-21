using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace Cardmong.Game
{
    /// <summary>
    /// 백엔드 없이 오프라인으로 도는 세로 모바일 탭 배틀 슬라이스.
    /// 모든 UI를 코드로 생성하므로(흰색 스프라이트 사용) 렌더링이 보장되고,
    /// 손으로 만든 씬 목업의 불완전한 컴포넌트 문제를 우회한다.
    /// 입력은 New Input System(Pointer.current)으로 직접 처리한다.
    /// </summary>
    public class TapBattleGame : MonoBehaviour
    {
        private static readonly Color[] Palette =
        {
            new Color(0.45f, 0.78f, 0.40f),
            new Color(0.40f, 0.62f, 0.92f),
            new Color(0.88f, 0.45f, 0.40f),
            new Color(0.80f, 0.55f, 0.92f),
            new Color(0.92f, 0.74f, 0.36f),
        };

        private Sprite _box;
        private RectTransform _canvasRt;
        private Image _monster;
        private RectTransform _monsterRt;
        private Image _hpFill;
        private TMP_Text _hpText;
        private TMP_Text _killText;
        private TMP_Text _monsterLabel;
        private RectTransform _attackBtnRt;

        private int _kills;
        private int _maxHp;
        private int _hp;
        private bool _busy;
        private float _hitTimer;
        private float _btnTimer;
        private Color _monsterColor;

        private void Start()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;

            var tex = Texture2D.whiteTexture;
            _box = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                 new Vector2(0.5f, 0.5f), 100f);

            BuildUI();
            NewMonster(instant: true);
        }

        private void Update()
        {
            var p = Pointer.current;
            if (!_busy && p != null && p.press.wasPressedThisFrame)
                Attack();

            // 몬스터 타격 반동
            if (_hitTimer > 0f)
            {
                _hitTimer -= Time.deltaTime;
                float k = Mathf.Clamp01(_hitTimer / 0.12f);
                _monsterRt.localScale = Vector3.one * (1f + 0.18f * k);
            }

            // 버튼 눌림 반동
            if (_btnTimer > 0f)
            {
                _btnTimer -= Time.deltaTime;
                float k = Mathf.Clamp01(_btnTimer / 0.10f);
                _attackBtnRt.localScale = Vector3.one * (1f - 0.08f * k);
            }

            // HP 바 부드럽게 감소
            float target = _maxHp > 0 ? (float)_hp / _maxHp : 0f;
            _hpFill.fillAmount = Mathf.MoveTowards(_hpFill.fillAmount, target, 2.5f * Time.deltaTime);
        }

        // ----------------------------------------------------------- gameplay

        private void Attack()
        {
            bool crit = Random.value < 0.15f;
            int dmg = Random.Range(8, 15) * (crit ? 2 : 1);

            _hp = Mathf.Max(0, _hp - dmg);
            UpdateHpText();
            _hitTimer = 0.12f;
            _btnTimer = 0.10f;
            SpawnDamage(dmg, crit);

            if (_hp <= 0)
                StartCoroutine(KillAndRespawn());
        }

        private void NewMonster(bool instant)
        {
            _maxHp = 100 + _kills * 40;
            _hp = _maxHp;
            _monsterColor = Palette[_kills % Palette.Length];
            _monster.color = _monsterColor;
            _monsterLabel.text = $"SLIME  Lv.{_kills + 1}";
            _hpFill.fillAmount = 1f;
            UpdateHpText();

            if (instant)
            {
                _monsterRt.localScale = Vector3.one;
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
                _monsterRt.localScale = Vector3.one * EaseOutBack(t / 0.35f);
                yield return null;
            }
            _monsterRt.localScale = Vector3.one;
            _busy = false;
        }

        private IEnumerator KillAndRespawn()
        {
            _busy = true;
            _kills++;
            _killText.text = $"KILLS  {_kills}";

            float t = 0f;
            Vector3 s0 = _monsterRt.localScale;
            while (t < 0.22f)
            {
                t += Time.deltaTime;
                float p = t / 0.22f;
                _monsterRt.localScale = Vector3.LerpUnclamped(s0, Vector3.zero, p);
                _monster.color = Color.Lerp(_monsterColor, new Color(1f, 1f, 1f, 0f), p);
                yield return null;
            }
            _monsterRt.localScale = Vector3.zero;
            yield return new WaitForSeconds(0.12f);
            NewMonster(instant: false);
        }

        private void UpdateHpText() => _hpText.text = $"{_hp} / {_maxHp}";

        private void SpawnDamage(int dmg, bool crit)
        {
            var t = NewText("Dmg", _canvasRt, (crit ? "CRIT " : "") + dmg,
                            crit ? 80 : 56, crit ? new Color(1f, 0.55f, 0.15f) : Color.white);
            t.fontStyle = FontStyles.Bold;
            var jitter = new Vector2(Random.Range(-120f, 120f), Random.Range(-20f, 40f));
            Anchor(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 120) + jitter, new Vector2(500, 110));
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
                rt.anchoredPosition = start + new Vector2(0, 160f * p);
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
            scaler.matchWidthOrHeight = 0f; // 세로: 너비 기준
            _canvasRt = canvasGo.GetComponent<RectTransform>();

            var bg = NewImage("BG", _canvasRt, new Color(0.10f, 0.11f, 0.18f));
            Stretch(bg.rectTransform);

            var title = NewText("Title", _canvasRt, "CARDMONG  TAP BATTLE", 46, new Color(1f, 0.85f, 0.2f));
            title.fontStyle = FontStyles.Bold;
            Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -110), new Vector2(960, 70));

            _killText = NewText("Kills", _canvasRt, "KILLS  0", 40, Color.white);
            Anchor(_killText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -190), new Vector2(960, 60));

            var hpBg = NewImage("HpBg", _canvasRt, new Color(0f, 0f, 0f, 0.55f));
            Anchor(hpBg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 430), new Vector2(680, 54));

            _hpFill = NewImage("HpFill", hpBg.rectTransform, new Color(0.30f, 0.85f, 0.35f));
            Stretch(_hpFill.rectTransform, 6f);
            _hpFill.type = Image.Type.Filled;
            _hpFill.fillMethod = Image.FillMethod.Horizontal;
            _hpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _hpFill.fillAmount = 1f;

            _hpText = NewText("HpText", hpBg.rectTransform, "", 30, Color.white);
            Stretch(_hpText.rectTransform);

            _monster = NewImage("Monster", _canvasRt, Palette[0]);
            _monsterRt = _monster.rectTransform;
            Anchor(_monsterRt, new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(380, 380));
            AddFace(_monsterRt);

            _monsterLabel = NewText("MLabel", _canvasRt, "", 34, Color.white);
            Anchor(_monsterLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -180), new Vector2(700, 60));

            var atk = NewImage("AttackBtn", _canvasRt, new Color(0.95f, 0.35f, 0.30f));
            _attackBtnRt = atk.rectTransform;
            Anchor(_attackBtnRt, new Vector2(0.5f, 0f), new Vector2(0, 300), new Vector2(760, 170));
            var atkLabel = NewText("AtkLabel", _attackBtnRt, "TAP  TO  ATTACK", 48, Color.white);
            atkLabel.fontStyle = FontStyles.Bold;
            Stretch(atkLabel.rectTransform);

            var hint = NewText("Hint", _canvasRt, "tap anywhere on the screen", 28, new Color(1f, 1f, 1f, 0.5f));
            Anchor(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 210), new Vector2(800, 50));
        }

        private void AddFace(RectTransform parent)
        {
            var eyeColor = new Color(0.12f, 0.12f, 0.16f);
            Anchor(NewImage("eyeL", parent, eyeColor).rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-70, 40), new Vector2(60, 84));
            Anchor(NewImage("eyeR", parent, eyeColor).rectTransform, new Vector2(0.5f, 0.5f), new Vector2(70, 40), new Vector2(60, 84));
            Anchor(NewImage("mouth", parent, eyeColor).rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -64), new Vector2(170, 38));
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
    }
}
