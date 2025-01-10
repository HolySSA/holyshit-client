using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ironcow;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 캐릭터 선택 팝업
/// </summary>
public class PopupCharacter : UIListBase<ItemCharacter>
{
    [Header("Character Info UI")]
    [SerializeField] private GameObject infoPanel; // 캐릭터 정보 패널
    [SerializeField] private Image characterImage; // 캐릭터 이미지
    [SerializeField] private TextMeshProUGUI nameText; // 캐릭터 이름
    [SerializeField] private TextMeshProUGUI descriptionText; // 캐릭터 설명
    [SerializeField] private TextMeshProUGUI statsText; // 캐릭터 스탯

    private ItemCharacter selectedCharacter; // 현재 선택한 캐릭터

    public override void Opened(object[] param)
    {
        base.Opened(param);
        infoPanel.SetActive(false);
        SetList(); // 캐릭터 목록 초기화
    }

    /// <summary>
    /// 캐릭터 선택 팝업 닫기
    /// </summary>
    public override void HideDirect()
    {
        UIManager.Hide<PopupCharacter>();
    }

    /// <summary>
    /// 캐릭터 목록 생성/초기화
    /// </summary>
    public override void SetList()
    {
        ClearList(); // 기존 목록 제거

        // StorageManager에 저장된 캐릭터 정보로 목록 생성
        foreach (var characterInfo in StorageManager.characterInfos.Values)
        {
            var item = AddItem();
            item.SetItem(characterInfo, OnSelectCharacter);
        }
    }

    /// <summary>
    /// 캐릭터 선택 시 호출되는 메서드
    /// </summary>
    private void OnSelectCharacter(ItemCharacter item)
    {
        if (selectedCharacter != null)
        {
            selectedCharacter.OnSelect(false);
        }

        selectedCharacter = item;
        selectedCharacter.OnSelect(true);

        // 캐릭터 정보 표시
        var info = item.GetCharacterInfo();
        infoPanel.SetActive(true);

        // UI 업데이트
        characterImage.sprite = DataManager.instance.GetData<CharacterDataSO>($"CHA{(int)info.characterType:00000}").thumbnail;
        nameText.text = info.name;
        descriptionText.text = info.description;
        statsText.text = $"보유 HP : {info.hp}\n플레이 : {info.playCount}회\n승리 : {info.winCount}회";
    }

    /// <summary>
    /// 선택 버튼 클릭 시 호출되는 메서드
    /// </summary>
    public void OnClickSelect()
    {
        if (selectedCharacter == null) return;

        // 캐릭터 선택 요청
        var characterInfo = selectedCharacter.GetCharacterInfo();
        RequestSelectCharacter(characterInfo.characterType);
    }

    private void RequestSelectCharacter(CharacterType characterType)
    {
        GamePacket packet = new GamePacket();
        packet.SelectCharacterRequest = new C2SSelectCharacterRequest
        {
            CharacterType = characterType
        };
        SocketManager.instance.Send(packet);
    }

    /// <summary>
    /// 닫기 버튼 클릭 시 호출되는 메서드
    /// </summary>
    public void OnClickClose()
    {
        if (selectedCharacter != null)
        {
            selectedCharacter.OnSelect(false);
            selectedCharacter = null;
        }
        infoPanel.SetActive(false);
    }
}