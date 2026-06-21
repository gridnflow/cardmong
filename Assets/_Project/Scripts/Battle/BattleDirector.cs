using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cardmong.Network.Dto;

namespace Cardmong.Battle
{
    public class BattleDirector : MonoBehaviour
    {
        [SerializeField] private MonsterSpawner spawner;
        [SerializeField] private BattleField field;

        private Dictionary<long, MonsterEntity> _monsters;
        private float _playbackSpeed = 1f;

        public event Action<string> OnBattleFinished;

        public void StartPlayback(BattleResultDto result)
        {
            // Derive attacker/defender IDs from the battle log.
            // The first DEATH event's side tells us who lost; SourceId is always attacker-side.
            // Simpler: collect all unique IDs in order of first appearance for each slot.
            var attackerIds = new List<long>();
            var defenderIds = new List<long>();
            CollectSides(result.BattleLog, attackerIds, defenderIds);

            _monsters = spawner.SpawnFromLog(result.BattleLog, attackerIds, defenderIds);

            // Derive HP for each monster from first ATTACK event targeting them
            foreach (var (id, entity) in _monsters)
                entity.SetHp(500); // placeholder — server doesn't send max HP in log

            StartCoroutine(PlayLogs(result));
        }

        // Heuristic: first half of unique source IDs = attackers, rest = defenders.
        // Server log always starts with attacker actions at tick 1.
        private static void CollectSides(List<BattleLogDto> logs,
            List<long> attackers, List<long> defenders)
        {
            if (logs == null) return;

            // Collect unique sourceIds in order of first appearance
            var seen      = new HashSet<long>();
            var allActors = new List<long>();
            foreach (var log in logs)
            {
                if (log.SourceId.HasValue && seen.Add(log.SourceId.Value))
                    allActors.Add(log.SourceId.Value);
            }

            // First actor = first attacker. Build two groups by checking who attacks whom first.
            // Simpler: split evenly — first half attackers, second half defenders.
            int half = (allActors.Count + 1) / 2;
            for (int i = 0; i < allActors.Count; i++)
            {
                if (i < half) attackers.Add(allActors[i]);
                else          defenders.Add(allActors[i]);
            }

            // Fallback — if only one actor appears (all HEAL events), put them all as attackers
            if (defenders.Count == 0 && attackers.Count > 0)
                defenders.Add(attackers[0]);
        }

        private IEnumerator PlayLogs(BattleResultDto result)
        {
            int prevTick = 0;
            // 1 tick = 100ms game time at 1x speed
            const float tickDuration = 0.1f;

            foreach (var log in result.BattleLog)
            {
                int tickDelta = log.Tick - prevTick;
                if (tickDelta > 0)
                    yield return new WaitForSeconds(tickDelta * tickDuration / _playbackSpeed);

                PlayEvent(log);
                prevTick = log.Tick;
            }

            yield return new WaitForSeconds(1f / _playbackSpeed);
            OnBattleFinished?.Invoke(result.Result);
        }

        private void PlayEvent(BattleLogDto log)
        {
            MonsterEntity source = null;
            MonsterEntity target = null;

            if (log.SourceId.HasValue) _monsters.TryGetValue(log.SourceId.Value, out source);
            if (log.TargetId.HasValue) _monsters.TryGetValue(log.TargetId.Value, out target);

            switch (log.Type?.ToUpperInvariant())
            {
                case "ATTACK":
                    if (source != null && target != null)
                        source.PlayAttack(target, log.Value);
                    break;

                case "SKILL":
                    if (source != null && target != null)
                        source.PlaySkill(log.Extra ?? "", target, log.Value);
                    break;

                case "HEAL":
                    if (target != null)
                        target.PlayHeal(log.Value);
                    else if (source != null)
                        source.PlayHeal(log.Value);
                    break;

                case "STUN":
                    if (target != null)
                        target.ApplyDebuffVfx("stun");
                    break;

                case "DEATH":
                    if (source != null)
                        source.PlayDeath();
                    break;
            }
        }

        public void SetSpeed(float speed) => _playbackSpeed = speed;
    }
}
