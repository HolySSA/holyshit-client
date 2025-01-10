using System;
using System.Collections.Generic;

namespace HttpClient.Dtos
{
  public enum LoginResult
  {
    Success = 0,
    DuplicateLogin = 1,
  }

  [Serializable]
  public class LoginRequestDto
  {
    public string Email;
    public string Password;
  }

  /// <summary>
  /// JSON 직렬화/역직렬화 과정 때문에 ResponseDto는 모두 소문자로 작성해야 함.
  /// </summary>
  [Serializable]
  public class LoginResponseDto
  {
    public int userId;
    public string nickname;
    public string token;
    public string expiresAt;  // DateTime은 JsonUtility에서 직접 지원하지 않아 string으로 받기
    public string lobbyHost;
    public int lobbyPort;
    public LoginResult result;
  }

  [Serializable]
  public class RegisterRequestDto
  {
    public string Email;
    public string Password;
    public string Nickname;
  }

  /// <summary>
  /// JSON 직렬화/역직렬화 과정 때문에 ResponseDto는 모두 소문자로 작성해야 함.
  /// </summary>
  [Serializable]
  public class RegisterResponseDto
  {
    public bool success;
    public string message;
  }

  /// <summary>
  /// JSON 직렬화/역직렬화 과정 때문에 ResponseDto는 모두 소문자로 작성해야 함.
  /// </summary>
  [Serializable]
  public class ErrorResponseDto
  {
    public string message;  // 서버에서 보내는 에러 메시지
  }
}