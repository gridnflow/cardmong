using System;
using System.Collections;
using UnityEngine;

namespace Cardmong.Battle
{
    public class MonsterEntity : MonoBehaviour
    {
        [SerializeField] private MonsterHpBar hpBar;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private int _maxHp;
        private int _currentHp;
        private long _userCardId;

        private static readonly Color[] RoleColors =
        {
            new Color(0.9f, 0.3f, 0.3f), // red   — WARRIOR
            new Color(0.3f, 0.5f, 0.9f), // blue  — MAGE
            new Color(0.3f, 0.8f, 0.4f), // green — SUPPORT
            new Color(0.8f, 0.7f, 0.2f), // gold  — TANK
            new Color(0.7f, 0.3f, 0.9f), // purple— ASSASSIN
        };

        public void Init(long userCardId, bool flipX = false)
        {
            _userCardId = userCardId;

            if (spriteRenderer != null)
            {
                // Generate a colored placeholder sprite if no real sprite is assigned
                if (spriteRenderer.sprite == null)
                    spriteRenderer.sprite = PlaceholderSprite.Get((int)(userCardId % RoleColors.Length));

                spriteRenderer.color   = RoleColors[(int)(userCardId % RoleColors.Length)];
                spriteRenderer.flipX   = flipX;
            }
        }

        public void SetHp(int maxHp)
        {
            _maxHp     = maxHp;
            _currentHp = maxHp;
            hpBar?.Init(maxHp);
        }

        public void PlayAttack(MonsterEntity target, int damage)
        {
            StartCoroutine(AttackBounce(target, damage));
        }

        public void PlaySkill(string skillName, MonsterEntity target, int damage)
        {
            StartCoroutine(SkillFlash(target, damage));
        }

        public void PlayHeal(int amount)
        {
            _currentHp = Math.Min(_maxHp, _currentHp + amount);
            hpBar?.UpdateHp(_currentHp);
            StartCoroutine(FlashColor(new Color(0.3f, 1f, 0.5f), 0.2f));
        }

        public void PlayDeath()
        {
            StartCoroutine(DeathFade());
        }

        public void TakeDamage(int damage)
        {
            _currentHp = Math.Max(0, _currentHp - damage);
            hpBar?.UpdateHp(_currentHp);
            StartCoroutine(FlashColor(Color.white, 0.1f));
        }

        public void ApplyDebuffVfx(string effectType)
        {
            StartCoroutine(FlashColor(new Color(0.8f, 0.8f, 0.2f), 0.3f)); // yellow stun flash
        }

        // Quick forward lunge toward target, then return
        private IEnumerator AttackBounce(MonsterEntity target, int damage)
        {
            Vector3 origin = transform.position;
            Vector3 toward = Vector3.Lerp(origin, target.transform.position, 0.35f);

            yield return MoveToSmooth(toward, 0.12f);
            target.TakeDamage(damage);
            yield return MoveToSmooth(origin, 0.12f);
        }

        private IEnumerator SkillFlash(MonsterEntity target, int damage)
        {
            yield return FlashColor(new Color(1f, 0.9f, 0.2f), 0.15f);
            if (damage > 0) target.TakeDamage(damage);
        }

        private IEnumerator MoveToSmooth(Vector3 dest, float duration)
        {
            Vector3 start   = transform.position;
            float   elapsed = 0f;
            while (elapsed < duration)
            {
                transform.position = Vector3.Lerp(start, dest, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = dest;
        }

        private IEnumerator FlashColor(Color flashColor, float duration)
        {
            if (spriteRenderer == null) yield break;
            Color original = spriteRenderer.color;
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(duration);
            spriteRenderer.color = original;
        }

        private IEnumerator DeathFade()
        {
            if (spriteRenderer == null) { gameObject.SetActive(false); yield break; }

            float elapsed  = 0f;
            float duration = 0.6f;
            Color start    = spriteRenderer.color;

            while (elapsed < duration)
            {
                float a = Mathf.Lerp(1f, 0f, elapsed / duration);
                spriteRenderer.color = new Color(start.r, start.g, start.b, a);
                elapsed += Time.deltaTime;
                yield return null;
            }

            gameObject.SetActive(false);
        }
    }
}
