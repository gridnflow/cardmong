using System.Collections.Generic;
using UnityEngine;
using Cardmong.Network.Dto;

namespace Cardmong.Battle
{
    public class MonsterSpawner : MonoBehaviour
    {
        [SerializeField] private MonsterEntity monsterPrefab;
        [SerializeField] private BattleField field;

        // sourceId → MonsterEntity; side=0 attacker left, side=1 defender right
        public Dictionary<long, MonsterEntity> SpawnFromLog(List<BattleLogDto> logs,
            List<long> attackerIds, List<long> defenderIds)
        {
            var monsters = new Dictionary<long, MonsterEntity>();

            int aIndex = 0;
            foreach (long id in attackerIds)
            {
                var entity = SpawnMonster(id, side: 0, slotIndex: aIndex++, total: attackerIds.Count);
                monsters[id] = entity;
            }

            int dIndex = 0;
            foreach (long id in defenderIds)
            {
                var entity = SpawnMonster(id, side: 1, slotIndex: dIndex++, total: defenderIds.Count);
                monsters[id] = entity;
            }

            return monsters;
        }

        private MonsterEntity SpawnMonster(long userCardId, int side, int slotIndex, int total)
        {
            // Left side x=-3~-1, right side x=1~3
            float xBase  = side == 0 ? -3f : 1f;
            float ySpread = (total > 1) ? (slotIndex - (total - 1) / 2f) * 1.5f : 0f;
            var pos = new Vector3(xBase + (side == 0 ? 0f : 2f), ySpread, 0f);

            var entity = Instantiate(monsterPrefab, pos, Quaternion.identity);
            entity.Init(userCardId, side == 1); // flip sprite for right side
            return entity;
        }
    }
}
