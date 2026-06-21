using UnityEngine;

namespace Cardmong.Battle
{
    public class BattleField : MonoBehaviour
    {
        [SerializeField] private float cellSize = 1.5f;
        [SerializeField] private Vector2 origin = new Vector2(-3f, -2f);

        public Vector3 GetPosition(int x, int y)
        {
            return new Vector3(
                origin.x + x * cellSize,
                origin.y + y * cellSize,
                0f
            );
        }
    }
}
