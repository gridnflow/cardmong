using UnityEngine;

namespace Cardmong.Core
{
    /// <summary>
    /// Boot 씬 진입점. 로그인 절차 없이 곧바로 게임(로비)으로 이동한다.
    /// </summary>
    public class AppStateManager : MonoBehaviour
    {
        private void Start()
        {
            SceneLoader.Load(SceneLoader.Lobby);
        }
    }
}
