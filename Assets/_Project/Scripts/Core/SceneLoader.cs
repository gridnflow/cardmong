using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cardmong.Core
{
    public static class SceneLoader
    {
        public const string Boot      = "Boot";
        public const string Game      = "Game";
        public const string Lobby     = "Lobby";
        public const string DeckBuild = "DeckBuild";
        public const string Battle    = "Battle";
        public const string Ranking   = "Ranking";

        public static void Load(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}
