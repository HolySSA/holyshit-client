using Ironcow;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DataManager : DataManagerBase<DataManager>
{
    public List<UserInfo> users = new List<UserInfo>();

    public override void Init()
    {
        base.Init();
        userInfo = UserInfo.CreateRandomUser();
    }

    public async void OnLogout()
    {
        // HTTP 로그아웃 요청
        if (HttpClientManager.instance.IsTokenValid())
        {
            var result = await HttpClientManager.instance.Logout();
            if (!result.success)
            {
                Debug.LogWarning($"HTTP 로그아웃 실패: {result.message}");
            }
        }

        // 로비 서버 연결 해제
        if (SocketManager.instance.isConnected)
        {
            SocketManager.instance.Disconnect();
        }
        else
        {
            if (SceneManager.GetActiveScene().name != "Main")
            {
                await SceneManager.LoadSceneAsync("Main");
            }
            else
            {
                UIManager.Hide<UITopBar>();
                UIManager.Hide<UIGnb>();
                await UIManager.Show<PopupLogin>();
            }
        }
    }
}
