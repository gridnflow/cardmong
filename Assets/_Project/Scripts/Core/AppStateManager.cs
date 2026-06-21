using UnityEngine;

namespace Cardmong.Core
{
    /// <summary>
    /// Boot 씬 진입점. 로그인 없이 곧바로 플레이 가능한 게임 씬으로 이동한다.
    /// </summary>
    public class AppStateManager : MonoBehaviour
    {
        private void Start()
        {
            SceneLoader.Load(SceneLoader.Game);
        }
    }
}
