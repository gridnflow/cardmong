using UnityEngine;
using UnityEngine.UI;

namespace Cardmong.Battle
{
    public class MonsterHpBar : MonoBehaviour
    {
        [SerializeField] private Slider slider;

        private int _maxHp;

        public void Init(int maxHp)
        {
            _maxHp        = maxHp;
            slider.minValue = 0;
            slider.maxValue = maxHp;
            slider.value    = maxHp;
        }

        public void UpdateHp(int currentHp)
        {
            slider.value = currentHp;
        }
    }
}
