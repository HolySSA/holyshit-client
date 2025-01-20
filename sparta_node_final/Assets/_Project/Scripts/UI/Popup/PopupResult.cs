using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ironcow;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using Unity.VisualScripting;
using Google.Protobuf.Collections;
using System;

[Serializable]
public class ResultUserSlot
{
    public GameObject root;
    public TMP_Text nickname;
    public Image character;
}

public class PopupResult : UIBase
{
    [SerializeField] private List<ResultUserSlot> resultUserSlots; // 결과 유저 슬롯
    [SerializeField] private Image role; // 승리한 팀의 역할 이미지
    [SerializeField] private TMP_Text roleText; // 승리한 팀의 역할 텍스트
    [SerializeField] private GameObject exit; // 나가기 버튼

    private string[] roleTexts = new string[3] { "타겟 + 보디가드", "히트맨", "사이코패스" };

    public override async void Opened(object[] param)
    {
        var winners = (RepeatedField<int>)param[0];
        var winnerType = (WinType)param[1];
        // 승리 팀 역할 정보 표시
        role.gameObject.SetActive(true);
        roleText.gameObject.SetActive(true);
        role.sprite = await ResourceManager.instance.LoadAsset<Sprite>("Role_" + winnerType.ToString(), eAddressableType.Thumbnail);
        roleText.text = roleTexts[(int)winnerType];
        await Task.Delay(500);
        // 승리한 플레이어들의 정보 표시
        for (int i = 0; i < resultUserSlots.Count; i++)
        {
            if (winners.Count > i)
            {
                resultUserSlots[i].root.SetActive(true);
                var userInfo = DataManager.instance.users.Find(obj => obj.id == winners[i]);
                resultUserSlots[i].nickname.text = userInfo.nickname;
                resultUserSlots[i].character.sprite = await ResourceManager.instance.LoadAsset<Sprite>(userInfo.selectedCharacterRcode, eAddressableType.Thumbnail);
            }
            else
            {
                resultUserSlots[i].root.SetActive(false);
            }
        }
        // 2초 후 나가기 버튼 활성화
        await Task.Delay(2000);
        exit.gameObject.SetActive(true);
    }

    public override void HideDirect()
    {
        UIManager.Hide<PopupResult>();
    }

    /// <summary>
    /// 나가기 버튼 클릭 시 호출
    /// </summary>
    public async void OnClickExit()
    {
        // 유저 정보 초기화
        UserInfo.myInfo.Clear();
        DataManager.instance.users.Clear();
        // 메인 씬 로드
        await SceneManager.LoadSceneAsync("Main");
    }
}