namespace Cardmong.Network.Dto
{
    [System.Serializable]
    public class RegisterRequest
    {
        public string Email    { get; set; }
        public string Nickname { get; set; }
        public string Password { get; set; }
    }

    [System.Serializable]
    public class LoginRequest
    {
        public string Email    { get; set; }
        public string Password { get; set; }
    }

    [System.Serializable]
    public class TokenResponse
    {
        public long   UserId       { get; set; }
        public string Nickname     { get; set; }
        public string AccessToken  { get; set; }
        public string RefreshToken { get; set; }
        public int    ExpiresIn    { get; set; }
    }
}
