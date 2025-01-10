using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ironcow;
using UnityEngine.UI;
using TMPro;

public class PopupRoomCreate : UIBase
{
    [SerializeField] private TMP_InputField roomName; // 방 이름 입력 필드
    [SerializeField] private TMP_Dropdown count; // 방 인원 선택 드롭다운

    /// <summary>
    ///  방 생성 팝업 열기
    /// </summary>
    public override void Opened(object[] param)
    {
        // 랜덤 방 이름 생성
        var roomNameSample = new List<string>() { "같이 게임 해요!", "즐거운 게임방", "같이 한판?", "오늘도 즐겁게 한판?", "고고! 시작!" };
        roomName.text = roomNameSample.RandomValue();
    }

    /// <summary>
    /// 방 생성 팝업 닫기
    /// </summary>
    public override void HideDirect()
    {
        UIManager.Hide<PopupRoomCreate>();
    }

    /// <summary>
    /// 방 생성 버튼 클릭
    /// </summary>
    public void OnClickCreate()
    {
        if (SocketManager.instance.isConnected)
        {
            // 방 생성 요청
            GamePacket packet = new GamePacket();
            packet.CreateRoomRequest = new C2SCreateRoomRequest() { MaxUserNum = count.value + 4, Name = roomName.text };
            SocketManager.instance.Send(packet);
        }
        else
        {
            // 오프라인 테스트용 방 생성
            OnRoomCreateResult(true, new RoomData() { Id = 1, MaxUserNum = count.value + 4, Name = roomName.text, OwnerId = UserInfo.myInfo.id, State = 0 });
        }
    }

    /// <summary>
    /// 방 생성 결과 처리
    /// </summary>
    public void OnRoomCreateResult(bool isSuccess, RoomData roomData)
    {
        if(isSuccess)
        {
            // 방 생성 성공 시 방 UI로 전환
            UIManager.Show<UIRoom>(roomData);
            HideDirect();
        }
    }
}