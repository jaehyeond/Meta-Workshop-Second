using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;
// using Unity.Assets.Scripts.Utils;

namespace Unity.Assets.Scripts.Data
{
    public class RemoteDataRepository : IDataRepository
    {
        private const string API_BASE_URL = "https://your-backend-url.com/api";
        private string _authToken;

        public void SetAuthToken(string token)
        {
            _authToken = token;
        }

        public async Task<UserGameData> LoadAllUserData()
        {
            return await SendRequest<UserGameData>($"{API_BASE_URL}/user/gamedata", "GET");
        }

        public async Task SaveAllUserData(UserGameData userData)
        {
            await SendRequest($"{API_BASE_URL}/user/gamedata", "POST", userData);
        }

        public async Task SaveUserInfo(UserData userData)
        {
            await SendRequest($"{API_BASE_URL}/user/info", "POST", userData);
        }

        public async Task SaveCurrencies(Dictionary<string, long> currencies)
        {
            await SendRequest($"{API_BASE_URL}/user/currencies", "POST", currencies);
        }

        public async Task SaveGameProgress(GameProgressData progress)
        {
            await SendRequest($"{API_BASE_URL}/user/progress", "POST", progress);
        }

        public async Task SaveLimitations(LimitationData limitations)
        {
            await SendRequest($"{API_BASE_URL}/user/limitations", "POST", limitations);
        }

        private async Task<T> SendRequest<T>(string url, string method, object body = null)
        {
            using (UnityWebRequest request = new UnityWebRequest(url, method))
            {
                if (body != null)
                {
                    string jsonBody = JsonConvert.SerializeObject(body);
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                }

                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {_authToken}");

                try
                {
                    await request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string jsonResponse = request.downloadHandler.text;
                        return JsonConvert.DeserializeObject<T>(jsonResponse);
                    }
                    else
                    {
                        Debug.LogError($"API 요청 실패: {request.error}");
                        throw new Exception($"API 요청 실패: {request.error}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"API 요청 중 오류 발생: {e.Message}");
                    throw;
                }
            }
        }

        private async Task SendRequest(string url, string method, object body)
        {
            await SendRequest<object>(url, method, body);
        }
    }
} 