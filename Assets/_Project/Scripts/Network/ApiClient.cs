using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Cardmong.Network.Dto;

namespace Cardmong.Network
{
    public class ApiClient
    {
        public static ApiClient Instance { get; } = new ApiClient();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private const string BaseUrl = "http://localhost:8080/v1";
#else
        private const string BaseUrl = "https://api.cardmong.com/v1";
#endif

        private ApiClient() { }

        public async Task<T> GetAsync<T>(string path)
        {
            using var request = UnityWebRequest.Get(BaseUrl + path);
            await SendRequest(request);
            return ParseResponse<T>(request);
        }

        public async Task<T> PostAsync<T>(string path, object body)
        {
            string json = JsonConvert.SerializeObject(body);
            using var request = new UnityWebRequest(BaseUrl + path, "POST");
            request.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            await SendRequest(request);
            return ParseResponse<T>(request);
        }

        public async Task<T> PutAsync<T>(string path, object body)
        {
            string json = JsonConvert.SerializeObject(body);
            using var request = new UnityWebRequest(BaseUrl + path, "PUT");
            request.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            await SendRequest(request);
            return ParseResponse<T>(request);
        }

        public async Task DeleteAsync(string path)
        {
            using var request = UnityWebRequest.Delete(BaseUrl + path);
            await SendRequest(request);
        }

        private Task SendRequest(UnityWebRequest request)
        {
            var tcs = new TaskCompletionSource<bool>();
            var op  = request.SendWebRequest();
            op.completed += _ => tcs.SetResult(true);
            return tcs.Task;
        }

        private T ParseResponse<T>(UnityWebRequest request)
        {
            if (request.result != UnityWebRequest.Result.Success)
                throw new Exception($"HTTP Error {request.responseCode}: {request.error}");

            var response = JsonConvert.DeserializeObject<ApiResponse<T>>(
                request.downloadHandler.text);

            if (!response.Success)
                throw new Exception($"API Error [{response.Error.Code}]: {response.Error.Message}");

            return response.Data;
        }
    }
}
