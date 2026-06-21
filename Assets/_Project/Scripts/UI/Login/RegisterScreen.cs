using UnityEngine;
using TMPro;
using Cardmong.Core;
using Cardmong.Network;
using Cardmong.Network.Dto;
using Cardmong.Data;
using Cardmong.UI.Common;

namespace Cardmong.UI.Login
{
    public class RegisterScreen : MonoBehaviour
    {
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField nicknameInput;
        [SerializeField] private TMP_InputField passwordInput;

        public async void OnClickRegister()
        {
            string email    = emailInput.text.Trim();
            string nickname = nicknameInput.text.Trim();
            string password = passwordInput.text;

            if (string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(nickname) ||
                string.IsNullOrEmpty(password))
            {
                ToastMessage.Show("모든 항목을 입력해주세요.");
                return;
            }

            LoadingOverlay.Show();
            try
            {
                var result = await AuthApi.Register(new RegisterRequest
                {
                    Email    = email,
                    Nickname = nickname,
                    Password = password
                });

                SessionData.Instance.SetSession(
                    result.AccessToken,
                    result.RefreshToken,
                    result.UserId,
                    result.Nickname
                );

                SceneLoader.Load(SceneLoader.Lobby);
            }
            catch (System.Exception e)
            {
                ToastMessage.Show($"회원가입 실패: {e.Message}");
            }
            finally
            {
                LoadingOverlay.Hide();
            }
        }

        public void OnClickBack() => gameObject.SetActive(false);
    }
}
