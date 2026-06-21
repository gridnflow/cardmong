using System.Collections.Generic;
using UnityEngine;
using Cardmong.Network.Dto;

namespace Cardmong.Battle
{
    public class MonsterSpawner : MonoBehaviour
    {
        [SerializeField] private MonsterEntity monsterPrefab;
        [SerializeField] private BattleField field;

        public Dictionary<int, MonsterEntity> SpawnAll(List<BattleLogDto> logs)
        {
            var monsters = new Dictionary<int, MonsterEntity>();

            foreach (var log in logs)
            {
                if (!monsters.ContainsKey(log.ActorCardId))
                    monsters[log.ActorCardId] = SpawnMonster(log.ActorCardId);

                if (log.TargetCardId.HasValue && !monsters.ContainsKey(log.TargetCardId.Value))
                    monsters[log.TargetCardId.Value] = SpawnMonster(log.TargetCardId.Value);
            }

            return monsters;
        }

        private MonsterEntity SpawnMonster(int cardId)
        {
            var entity = Instantiate(monsterPrefab, field.GetPosition(0, 0), Quaternion.identity);
            entity.Init(cardId);
            return entity;
        }
    }
}
