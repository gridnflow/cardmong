using Cardmong.Data;

namespace Cardmong.Data
{
    public class SessionData
    {
        public static SessionData Instance { get; } = new SessionData();

        public string AccessToken  { get; private set; }
        public string RefreshToken { get; private set; }
        public long   UserId       { get; private set; }
        public string Nickname     { get; private set; }

        private SessionData()
        {
            AccessToken  = LocalStorage.Load("access_token");
            RefreshToken = LocalStorage.Load("refresh_token");
        }

        public void SetSession(string accessToken, string refreshToken,
                               long userId, string nickname)
        {
            AccessToken  = accessToken;
            RefreshToken = refreshToken;
            UserId       = userId;
            Nickname     = nickname;

            LocalStorage.Save("access_token",  accessToken);
            LocalStorage.Save("refresh_token", refreshToken);
        }

        public bool IsLoggedIn() => !string.IsNullOrEmpty(AccessToken);

        public void Clear()
        {
            AccessToken  = null;
            RefreshToken = null;
            UserId       = 0;
            Nickname     = null;

            LocalStorage.Delete("access_token");
            LocalStorage.Delete("refresh_token");
        }
    }
}
