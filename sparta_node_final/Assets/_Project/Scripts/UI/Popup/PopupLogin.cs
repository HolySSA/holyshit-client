using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ironcow;
using UnityEngine.UI;
using TMPro;
using Unity.Multiplayer.Playmode;
using System;

public class PopupLogin : UIBase
{
    // UI 컴포넌트들
    [SerializeField] private GameObject touch; // 터치 영역
    [SerializeField] private GameObject buttonSet; // 버튼 셋
    [SerializeField] private GameObject register; // 회원가입 영역
    [SerializeField] private GameObject login; // 로그인 영역
    // 로그인 입력 필드
    [SerializeField] private TMP_InputField loginId;
    [SerializeField] private TMP_InputField loginPassword;
    // 회원가입 입력 필드
    [SerializeField] private TMP_InputField regId;
    [SerializeField] private TMP_InputField regNickname;
    [SerializeField] private TMP_InputField regPassword;
    [SerializeField] private TMP_InputField regPasswordRe;

    /// <summary>
    /// 초기화
    /// </summary>
    public override void Opened(object[] param)
    {
        // 패널 비활성화
        register.SetActive(false);
        login.SetActive(false);

        // 이전에 저장된 로그인 정보 불러오기
        var tags = CurrentPlayer.ReadOnlyTags();
        if (tags.Length == 0)
        {
            tags = new string[1] { "player1" };
        }
        loginId.text = PlayerPrefs.GetString("id" + tags[0], "");
        loginPassword.text = PlayerPrefs.GetString("password" + tags[0], "");
    }

    public override void HideDirect()
    {
        UIManager.Hide<PopupLogin>();
    }

    /// <summary>
    /// 로그인 버튼 클릭
    /// </summary>
    public void OnClickLogin()
    {
        // 버튼 셋 비활성화 및 로그인 영역 활성화
        buttonSet.SetActive(false);
        login.SetActive(true);
    }

    /// <summary>
    /// 회원가입 버튼 클릭
    /// </summary>
    public void OnClickRegister()
    {
        // 버튼 셋 비활성화 및 회원가입 영역 활성화
        buttonSet.SetActive(false);
        register.SetActive(true);
    }

    /// <summary>
    /// 로그인 요청 전송
    /// </summary>
    public async void OnClickSendLogin()
    {
        var (success, message, response) = await HttpClientManager.instance.Login(loginId.text, loginPassword.text);

        if (success && response != null)
        {
            if (response.result == HttpClient.Dtos.LoginResult.DuplicateLogin)
            {
                UIManager.ShowAlert("이미 로그인 중인 계정입니다.");
                return;
            }

            // 로그인 정보 저장
            var tags = CurrentPlayer.ReadOnlyTags();
            if (tags.Length == 0)
            {
                tags = new string[1] { "player1" };
            }
            // 로그인 정보 로컬 저장
            PlayerPrefs.SetString("id" + tags[0], loginId.text);
            PlayerPrefs.SetString("password" + tags[0], loginPassword.text);

            // 토큰 및 만료 시간 저장
            StorageManager.JWT = response.token;
            if (DateTime.TryParse(response.expiresAt, out DateTime expiresAt))
                StorageManager.JWTExpiresAt = ((DateTimeOffset)expiresAt).ToUnixTimeSeconds();

            // 유저 정보 저장
            UserInfo.myInfo = new UserInfo(response.userId, response.nickname);

            // 로비 서버 연결이 이미 되어있다면 연결 해제
            if (SocketManager.instance.isConnected)
                SocketManager.instance.Disconnect();

            // 로비 서버 연결
            if (!string.IsNullOrEmpty(response.lobbyHost))
            {
                SocketManager.instance.Init(response.lobbyHost, response.lobbyPort);
                SocketManager.instance.Connect(() =>
                {
                    // 로비 서버 로그인 요청
                    GamePacket packet = new GamePacket();
                    packet.LoginRequest = new C2SLoginRequest
                    {
                        UserId = response.userId,
                        Token = StorageManager.JWT
                    };
                    SocketManager.instance.Send(packet);
                });
            }
            else
            {
                UIManager.ShowAlert("TCP 서버 정보를 받아오지 못했습니다.");
            }
        }
        else
        {
            UIManager.ShowAlert(message ?? "로그인에 실패했습니다.");
        }
    }

    /// <summary>
    /// 회원가입 요청 전송
    /// </summary>
    public async void OnClickSendRegister()
    {
        // 비밀번호 확인
        if (regPassword.text != regPasswordRe.text)
        {
            UIManager.ShowAlert("비밀번호 확인이 일치하지 않습니다.");
            return;
        }

        var (success, message, response) = await HttpClientManager.instance.Register(regId.text, regNickname.text, regPassword.text);
        if (success)
        {
            // 회원가입 정보 저장
            var tags = CurrentPlayer.ReadOnlyTags();
            if (tags.Length == 0)
            {
                tags = new string[1] { "player1" };
            }
            // 회원가입 정보 로컬 저장
            PlayerPrefs.SetString("id" + tags[0], regId.text);
            PlayerPrefs.SetString("password" + tags[0], regPassword.text);

            OnRegisterEnd(response.success, response.message);
        }
        else
        {
            OnRegisterEnd(false, message ?? "회원가입에 실패했습니다.");
        }
    }

    /// <summary>
    /// 회원가입 취소 버튼 클릭
    /// </summary>
    public void OnClickCancelRegister()
    {
        buttonSet.SetActive(true);
        register.SetActive(false);
    }

    /// <summary>
    /// 로그인 취소 버튼 클릭
    /// </summary>
    public void OnClickCancelLogin()
    {
        buttonSet.SetActive(true);
        login.SetActive(false);
    }

    /// <summary>
    /// 터치 영역 클릭
    /// </summary>
    public void OnTouchScreen()
    {
        touch.SetActive(false);
        buttonSet.SetActive(false);
    }

    /// <summary>
    /// 로그인 처리
    /// </summary>
    public async void OnLoginEnd(bool isSuccess, CharacterType characterType)
    {
        if (isSuccess)
        {
            // 메인 화면 표시
            await UIManager.Show<UIMain>();
            UIManager.Get<UIMain>().OnRefreshRoomList();
            HideDirect();
            await UIManager.Show<UITopBar>();
            await UIManager.Show<UIGnb>();

            // 메인 화면 이미지 업데이트
            UIManager.Get<UIMain>()?.UpdateCharacterImage(characterType);
        }
        else
        {
            UIManager.ShowAlert("아이디 또는 비밀번호가 일치하지 않습니다.");
        }
    }

    /// <summary>
    /// 회원가입 처리
    /// </summary>
    public void OnRegisterEnd(bool isSuccess, string message)
    {
        if (isSuccess)
        {
            UIManager.ShowAlert(message, okCallback: OnRegisterAlertClosed);
        }
        else
        {
            UIManager.ShowAlert(message);
        }

    }

    /// <summary>
    /// 회원가입 성공 알림창이 닫힐 때 호출되는 콜백
    /// </summary>
    private void OnRegisterAlertClosed()
    {
        // 회원가입 영역 비활성화 및 로그인 영역 활성화
        register.SetActive(false);
        login.SetActive(true);

        // 이전에 저장된 로그인 정보 불러오기
        var tags = CurrentPlayer.ReadOnlyTags();
        if (tags.Length == 0)
        {
            tags = new string[1] { "player1" };
        }
        loginId.text = PlayerPrefs.GetString("id" + tags[0]);
        loginPassword.text = PlayerPrefs.GetString("password" + tags[0]);
    }

    /*
    /// <summary>
    /// 서버 변경 버튼 클릭
    /// </summary>
    public void OnClickChangeServer()
    {
        UIManager.Show<PopupConnection>();
    }
    */
}