using UnityEngine;
using Cardmong.Data;

namespace Cardmong.Core
{
    public class AppStateManager : MonoBehaviour
    {
        private void Start()
        {
            string token = LocalStorage.Load("access_token");

            if (string.IsNullOrEmpty(token))
                SceneLoader.Load(SceneLoader.Login);
            else
                SceneLoader.Load(SceneLoader.Lobby);
        }
    }
}
