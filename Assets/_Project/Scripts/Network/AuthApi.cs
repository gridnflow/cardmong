using System.Threading.Tasks;
using Cardmong.Network.Dto;

namespace Cardmong.Network
{
    public static class AuthApi
    {
        public static Task<TokenResponse> Register(RegisterRequest request)
            => ApiClient.Instance.PostAsync<TokenResponse>("/auth/register", request);

        public static Task<TokenResponse> Login(LoginRequest request)
            => ApiClient.Instance.PostAsync<TokenResponse>("/auth/login", request);

        public static Task<TokenResponse> Refresh(string refreshToken)
            => ApiClient.Instance.PostAsync<TokenResponse>("/auth/refresh",
                new { RefreshToken = refreshToken });
    }
}
