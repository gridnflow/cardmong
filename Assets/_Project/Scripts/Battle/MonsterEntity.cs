using System;
using System.Collections;
using UnityEngine;

namespace Cardmong.Battle
{
    public class MonsterEntity : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private MonsterHpBar hpBar;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private int _maxHp;
        private int _currentHp;
        private int _cardId;

        public void Init(int cardId)
        {
            _cardId = cardId;
        }

        public void SetHp(int maxHp)
        {
            _maxHp     = maxHp;
            _currentHp = maxHp;
            hpBar?.Init(maxHp);
        }

        public void PlayAttack(MonsterEntity target, int damage)
        {
            animator?.SetTrigger("Attack");
            StartCoroutine(DelayedDamage(target, damage, 0.3f));
        }

        public void PlaySkill(string skillName, MonsterEntity target, int damage)
        {
            animator?.SetTrigger("Skill");
            if (damage > 0)
                target.TakeDamage(damage);
        }

        public void MoveTo(Vector3 targetPos)
        {
            StartCoroutine(MoveCoroutine(targetPos));
        }

        public void PlayHeal(int amount)
        {
            _currentHp = Math.Min(_maxHp, _currentHp + amount);
            hpBar?.UpdateHp(_currentHp);
        }

        public void PlayDeath()
        {
            animator?.SetTrigger("Death");
            StartCoroutine(DisableAfterDelay(0.8f));
        }

        public void TakeDamage(int damage)
        {
            _currentHp = Math.Max(0, _currentHp - damage);
            hpBar?.UpdateHp(_currentHp);
            animator?.SetTrigger("Hit");
        }

        public void ApplyDebuffVfx(string effectType)
        {
            // 상태이상 VFX — 추후 파티클 연결
            Debug.Log($"[Debuff] {effectType} applied to card {_cardId}");
        }

        private IEnumerator DelayedDamage(MonsterEntity target, int damage, float delay)
        {
            yield return new WaitForSeconds(delay);
            target.TakeDamage(damage);
        }

        private IEnumerator MoveCoroutine(Vector3 targetPos)
        {
            animator?.SetBool("IsMoving", true);
            float duration = 0.3f;
            float elapsed  = 0f;
            Vector3 startPos = transform.position;

            while (elapsed < duration)
            {
                transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = targetPos;
            animator?.SetBool("IsMoving", false);
        }

        private IEnumerator DisableAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            gameObject.SetActive(false);
        }
    }
}
