using System;
using System.Text;
using System.Threading.Tasks;
using Ironcow;
using UnityEngine;
using UnityEngine.Networking;
using HttpClient.Dtos;
using Newtonsoft.Json;
using System.Linq;

public abstract class HttpClientManagerBase<T> : MonoSingleton<T> where T : HttpClientManagerBase<T>
{
    protected string baseUrl;
    protected string authToken;

    public void Init(string url)
    {
        baseUrl = url;
    }

    protected async Task<(bool success, string message, TResponse response)> Post<TResponse>(string path, object data)
    {
        var url = baseUrl + path;
        var json = JsonUtility.ToJson(data);

        using (var request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            if (!string.IsNullOrEmpty(authToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {authToken}");
            }

            try
            {
                await request.SendWebRequest();
                return HandleResponse<TResponse>(request);
            }
            catch (Exception e)
            {
                Debug.LogError($"Network error: {e.Message}");
                return (false, "네트워크 오류가 발생했습니다.", default);
            }
        }
    }

    private (bool success, string message, T response) HandleResponse<T>(UnityWebRequest request)
    {
        Debug.Log($"Response Code: {request.responseCode}");
        Debug.Log($"Response Text: {request.downloadHandler.text}");

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonUtility.FromJson<T>(request.downloadHandler.text);
            return (true, "Success", response);
        }

        // 서버 에러 응답 처리 - 회원가입/로그인
        var errorMessage = ParseErrorMessage(request.downloadHandler.text);
        return (false, errorMessage ?? "서버 오류가 발생했습니다.", default);
    }

    private string ParseErrorMessage(string errorJson)
    {
        try
        {
            // Unity의 JsonUtility는 Dictionary를 지원하지 않아서 Newtonsoft.Json 사용
            var error = JsonConvert.DeserializeObject<ErrorResponseDto>(errorJson);
            if (!string.IsNullOrEmpty(error?.message))
            {
                return error.message;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing validation error: {e.Message}");
        }

        return null;
    }

    protected void SetAuthToken(string token)
    {
        authToken = token;
    }

    private void OnApplicationQuit()
    {
        LogoutOnQuit();
    }

    private void OnDisable()
    {
        // 에디터에서 Play 모드 종료 시 로그아웃 요청
        #if UNITY_EDITOR
        LogoutOnQuit();
        #endif
    }

    /// <summary>
    /// 게임 종료 시 로그아웃 요청
    /// </summary>
    private async void LogoutOnQuit()
    {
        if (string.IsNullOrEmpty(authToken)) return;

        try
        {
            var result = await Post<object>("/api/auth/logout", new { });
            if (result.success)
                authToken = null;
        }
        catch (Exception e)
        {
            Debug.LogError($"Logout failed: {e.Message}");
        }
    }
}