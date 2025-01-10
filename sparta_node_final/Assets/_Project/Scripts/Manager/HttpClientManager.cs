using System;
using System.Threading.Tasks;
using UnityEngine;
using HttpClient.Dtos;

public class HttpClientManager : HttpClientManagerBase<HttpClientManager>
{
    [SerializeField] private string DEFAULT_IP = "127.0.0.1";
    [SerializeField] private int DEFAULT_PORT = 3000;
    [SerializeField] private string DEFAULT_PROTOCOL = "http";

    protected override void Awake()
    {
        base.Awake();
        // 개발단계: 기본 값으로 초기화
        Init($"{DEFAULT_PROTOCOL}://{DEFAULT_IP}:{DEFAULT_PORT}");

        Debug.Log($"HttpClientManager initialized with URL: {baseUrl}");
    }

    private DateTime tokenExpiresAt;

    public async Task<(bool success, string message, RegisterResponseDto response)> Register(string email, string nickname, string password)
    {
        var request = new RegisterRequestDto
        {
            Email = email,
            Nickname = nickname,
            Password = password
        };

        return await Post<RegisterResponseDto>("/api/auth/register", request);
    }

    public async Task<(bool success, string message, LoginResponseDto response)> Login(string email, string password)
    {
        var request = new LoginRequestDto { Email = email, Password = password };
        var result = await Post<LoginResponseDto>("/api/auth/login", request);

        if (result.success && !string.IsNullOrEmpty(result.response?.token))
        {
            SetAuthToken(result.response.token);

            // 토큰 만료 시간 저장
            if (DateTime.TryParse(result.response.expiresAt, out DateTime expiresAt))
            {
                tokenExpiresAt = expiresAt;
            }
        }

        return result;
    }

    public async Task<(bool success, string message)> Logout()
    {
        var result = await Post<object>("/api/auth/logout", new { });
        if (result.success)
        {
            // 토큰 초기화
            SetAuthToken(null);
        }
        return (result.success, result.message);
    }

    public bool IsTokenValid()
    {
        // 토큰 만료 5분 전부터는 만료된 것으로 간주
        return !string.IsNullOrEmpty(authToken) && DateTime.UtcNow.AddMinutes(5) < tokenExpiresAt;
    }
}