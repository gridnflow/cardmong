using UnityEngine;
using TMPro;
using Cardmong.Battle;

namespace Cardmong.UI.Battle
{
    public class BattleSpeedButton : MonoBehaviour
    {
        [SerializeField] private BattleDirector director;
        [SerializeField] private TextMeshProUGUI speedText;

        private float[] _speeds = { 1f, 2f, 3f };
        private int _currentIndex = 0;

        public void OnClickToggleSpeed()
        {
            _currentIndex = (_currentIndex + 1) % _speeds.Length;
            float speed = _speeds[_currentIndex];
            director.SetSpeed(speed);
            speedText.text = $"{speed}x";
        }
    }
}
