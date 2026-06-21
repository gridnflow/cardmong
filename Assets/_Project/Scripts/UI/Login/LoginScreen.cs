using UnityEngine;
using TMPro;
using Cardmong.Core;
using Cardmong.Network;
using Cardmong.Network.Dto;
using Cardmong.Data;
using Cardmong.UI.Common;

namespace Cardmong.UI.Login
{
    public class LoginScreen : MonoBehaviour
    {
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField passwordInput;

        public async void OnClickLogin()
        {
            string email    = emailInput.text.Trim();
            string password = passwordInput.text;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ToastMessage.Show("이메일과 비밀번호를 입력해주세요.");
                return;
            }

            LoadingOverlay.Show();

            var result = await AuthApi.Login(new LoginRequest
            {
                Email    = email,
                Password = password
            });

            SessionData.Instance.SetSession(
                result.AccessToken,
                result.RefreshToken,
                result.UserId,
                result.Nickname
            );

            LoadingOverlay.Hide();
            SceneLoader.Load(SceneLoader.Lobby);
        }

        public void OnClickRegister()
        {
            // 회원가입 팝업 열기 — 추후 구현
        }
    }
}
