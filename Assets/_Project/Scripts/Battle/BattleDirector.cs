using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cardmong.Network.Dto;

namespace Cardmong.Battle
{
    public class BattleDirector : MonoBehaviour
    {
        [SerializeField] private MonsterSpawner spawner;
        [SerializeField] private BattleField field;

        private Dictionary<int, MonsterEntity> _monsters;
        private float _playbackSpeed = 1f;

        public event Action<string> OnBattleFinished;

        public void StartPlayback(BattleResultDto result)
        {
            _monsters = spawner.SpawnAll(result.Logs);
            StartCoroutine(PlayLogs(result));
        }

        private IEnumerator PlayLogs(BattleResultDto result)
        {
            int prevTimeMs = 0;

            foreach (var log in result.Logs)
            {
                int waitMs = log.TimeMs - prevTimeMs;
                if (waitMs > 0)
                    yield return new WaitForSeconds(waitMs / 1000f / _playbackSpeed);

                PlayEvent(log);
                prevTimeMs = log.TimeMs;
            }

            OnBattleFinished?.Invoke(result.Result);
        }

        private void PlayEvent(BattleLogDto log)
        {
            if (!_monsters.TryGetValue(log.ActorCardId, out var actor)) return;

            switch (log.EventType)
            {
                case "ATTACK":
                    if (log.TargetCardId.HasValue &&
                        _monsters.TryGetValue(log.TargetCardId.Value, out var attackTarget))
                        actor.PlayAttack(attackTarget, log.Value ?? 0);
                    break;

                case "SKILL":
                    if (log.TargetCardId.HasValue &&
                        _monsters.TryGetValue(log.TargetCardId.Value, out var skillTarget))
                    {
                        string skillName = log.ExtraData?["skillName"]?.ToString() ?? "";
                        actor.PlaySkill(skillName, skillTarget, log.Value ?? 0);
                    }
                    break;

                case "MOVE":
                    if (log.ExtraData != null)
                    {
                        int x = Convert.ToInt32(log.ExtraData["x"]);
                        int y = Convert.ToInt32(log.ExtraData["y"]);
                        actor.MoveTo(field.GetPosition(x, y));
                    }
                    break;

                case "HEAL":
                    if (log.TargetCardId.HasValue &&
                        _monsters.TryGetValue(log.TargetCardId.Value, out var healTarget))
                        healTarget.PlayHeal(log.Value ?? 0);
                    break;

                case "DEATH":
                    actor.PlayDeath();
                    break;

                case "DEBUFF":
                    string effect = log.ExtraData?["effect"]?.ToString() ?? "";
                    actor.ApplyDebuffVfx(effect);
                    break;
            }
        }

        public void SetSpeed(float speed) => _playbackSpeed = speed;
    }
}
