using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class UIMain : UIListBase<ItemRoom>
{
    [SerializeField] private Image selectedCharacterImage;
    List<RoomData> rooms;

    public override void Opened(object[] param)
    {
        // 초기 방 목록 갱신
        OnRefreshRoomList();
    }

    public void SetRoomList(List<RoomData> rooms)
    {
        this.rooms = rooms;
        SetList();
    }

    public void OnRefreshRoomList()
    {
        if (SocketManager.instance.isConnected)
        {
            GamePacket packet = new GamePacket();
            packet.GetRoomListRequest = new C2SGetRoomListRequest();
            SocketManager.instance.Send(packet);
        }
    }

    public override void HideDirect()
    {
        UIManager.Hide<UIMain>();
    }

    public override void SetList()
    {
        ClearList();
        for (int i = 0; i < rooms.Count; i++)
        {
            var item = AddItem();
            item.SetItem(rooms[i], OnJoinRoom);
        }
    }

    public void OnClickRandomMatch()
    {
        if (SocketManager.instance.isConnected)
        {
            GamePacket packet = new GamePacket();
            packet.JoinRandomRoomRequest = new C2SJoinRandomRoomRequest();
            SocketManager.instance.Send(packet);
        }
    }

    public void OnClickCreateRoom()
    {
        UIManager.Show<PopupRoomCreate>();
    }

    public void OnJoinRoom(int idx)
    {
        if (SocketManager.instance.isConnected)
        {
            GamePacket packet = new GamePacket();
            packet.JoinRoomRequest = new C2SJoinRoomRequest() { RoomId = idx };
            SocketManager.instance.Send(packet);
        }
    }

    public void UpdateCharacterImage(CharacterType characterType)
    {
        selectedCharacterImage.sprite = DataManager.instance.GetData<CharacterDataSO>($"CHA{(int)characterType:00000}").thumbnail;
    }

    /// <summary>
    /// 캐릭터 선택 버튼 클릭
    /// </summary>
    public void OnClickCharacter()
    {
        UIManager.Show<PopupCharacter>();
    }

    /// <summary>
    /// 채팅 버튼 클릭
    /// </summary>
    public void OnClickChat()
    {
        UIManager.Show<PopupChat>();
    }
}