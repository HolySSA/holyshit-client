using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Ironcow
{
    public class StorageManager : MonoSingleton<StorageManager>
    {
        #region keys
        private static UserInfo _userInfo;
        public static UserInfo userInfo
        {
            get
            {
                if (_userInfo == null)
                {
                    var data = PlayerPrefs.GetString("userInfo", JsonUtility.ToJson(new UserInfo()));
                    _userInfo = new UserInfo();// JsonUtility.FromJson<UserInfo>(data);
                }
                return _userInfo;
            }
            set
            {
                PlayerPrefs.SetString("userInfo", JsonUtility.ToJson(_userInfo));
            }
        }

        public static DateTime HEART_TIME { get => DateTime.Parse(PlayerPrefs.GetString("heart_time", DateTime.Now.ToString())); set => PlayerPrefs.SetString("heart_time", value.ToString()); }

        public static string LOGIN_INFO { get => PlayerPrefs.GetString("loginInfo", ""); set => PlayerPrefs.SetString("loginInfo", value); }
        public static string EDITOR_NICKNAME { get => PlayerPrefs.GetString("editorNickname", "뉴토_"); set => PlayerPrefs.SetString("editorNickname", value); }
        public const string EDITOR_NICKNAME_KEY = "editorNickname";
        public static string USER_ID { get => PlayerPrefs.GetString("USER_ID", null); }
        /// <summary>자동 로그인 시 유저의 ID(KEY)</summary>
        public static string USER_ID_AUTO { get => PlayerPrefs.GetString("USER_ID_AUTO", null); set => PlayerPrefs.SetString("USER_ID_AUTO", value); }

        public const string USER_ID_AUTO_KEY = "USER_ID_AUTO";

        /// <summary>로그인 하는 플랫폼(Google/Facebook/Kakao/Apple)</summary>
        public static string LOGIN_PLATFORM { get => PlayerPrefs.GetString("LOGIN_PLATFORM", null); set => PlayerPrefs.SetString("LOGIN_PLATFORM", value); }

        public const string LOGIN_PLATFORM_KEY = "LOGIN_PLATFORM";

        public static int USER_MIDX { get => PlayerPrefs.GetInt("USER_MIDX", 0); }
        public const string USER_MIDX_KEY = "USER_MIDX";

        public static string PROFILE_INTRODUCTION { get => PlayerPrefs.GetString("PROFILE_INTRODUCTION", "저는 호기심이 많아 다양한 취미 활동에 도전하지만\n주로 활동적인 일에 더 적극적입니다!"); set => PlayerPrefs.SetString("PROFILE_INTRODUCTION", value); }

        /// <summary>
        /// 토큰 저장
        /// </summary>
        private static string _jwt;
        public static string JWT
        {
            get
            {
                if (string.IsNullOrEmpty(_jwt))
                {
                    _jwt = PlayerPrefs.GetString("JWT", "");
                }
                return _jwt;
            }
            set
            {
                _jwt = value;
                PlayerPrefs.SetString("JWT", value);
            }
        }

        /// <summary>
        /// 토큰 유효성 검사를 위한 만료 시간 저장
        /// </summary>
        private static long _jwtExpiresAt;
        public static long JWTExpiresAt
        {
            get => PlayerPrefs.GetInt("JWT_EXPIRES_AT", 0);
            set => PlayerPrefs.SetInt("JWT_EXPIRES_AT", (int)value);
        }

        /// <summary>
        /// 토큰 유효성 검사
        /// </summary>
        public static bool IsValidToken()
        {
            if (string.IsNullOrEmpty(JWT))
                return false;

            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return currentTime < JWTExpiresAt;
        }

        /// <summary>
        /// 토큰 초기화 (로그아웃 시 호출)
        /// </summary>
        public static void ClearToken()
        {
            JWT = "";
            JWTExpiresAt = 0;
            PlayerPrefs.DeleteKey("JWT");
            PlayerPrefs.DeleteKey("JWT_EXPIRES_AT");
        }

        public static string INSIDE_VIEW { get => PlayerPrefs.GetString("InsideView", "view1"); set => PlayerPrefs.SetString("InsideView", value); }
        #endregion

        public void SetEditorNickname(string nickname)
        {
            PlayerPrefs.SetString(EDITOR_NICKNAME_KEY, nickname);
        }

        public string GetEditorNickname()
        {
            return EDITOR_NICKNAME;
        }

        public static void OnSaveUserInfo()
        {
            userInfo = userInfo;
        }

        /// <summary>
        /// 캐릭터 정보
        /// </summary>
        private static Dictionary<CharacterType, CharacterInfo> _characterInfos;
        public static Dictionary<CharacterType, CharacterInfo> characterInfos
        {
            get
            {
                if (_characterInfos == null)
                    _characterInfos = new Dictionary<CharacterType, CharacterInfo>();
                return _characterInfos;
            }
        }

        /// <summary>
        /// 캐릭터 정보 저장
        /// </summary>
        public static void SaveCharacterInfos(IList<CharacterInfoData> characters)
        {
            characterInfos.Clear();
            foreach (var charData in characters)
            {
                characterInfos[charData.CharacterType] = new CharacterInfo(charData);
            }
        }
    }
}