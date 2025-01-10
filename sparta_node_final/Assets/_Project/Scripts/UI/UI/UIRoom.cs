using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ironcow;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using TMPro;
using Google.Protobuf.Collections;
using System.Linq;

public class UIRoom : UIBase
{
    [SerializeField] private List<ItemRoomSlot> slots;
    [SerializeField] private Button buttonExit;
    [SerializeField] private Button buttonStart;
    [SerializeField] private Button buttonReady;
    [SerializeField] private TMP_Text roomNo;
    [SerializeField] private TMP_Text roomName;
    [SerializeField] private TMP_Text roomCount;

    private List<UserInfo> users = new List<UserInfo>();
    private List<int> readyUserIds = new List<int>();  // 레디한 유저 ID

    private bool isReady = false;  // 현재 유저의 레디 상태
    private int maxUserCount;
    RoomData roomData;

    /// <summary>
    /// UI가 열릴 때 호출되는 초기화 메서드
    /// </summary>
    public override void Opened(object[] param)
    {
        UIManager.Hide<UIGnb>();
        roomData = (RoomData)param[0];

        // 기존 상태 초기화
        readyUserIds.Clear();
        users.Clear();

        SetRoomInfo(roomData);

        // 입장 후 레디 상태 요청
        if (SocketManager.instance.isConnected)
        {
            GamePacket packet = new GamePacket();
            packet.GetRoomReadyStateRequest = new C2SGetRoomReadyStateRequest
            {
                RoomId = roomData.Id
            };
            SocketManager.instance.Send(packet);
        }
    }

    /// <summary>
    /// 방 정보 설정 및 UI 업데이트
    /// </summary>
    public void SetRoomInfo(RoomData roomData)
    {
        // 방 정보 설정
        roomNo.text = roomData.Id.ToString();
        roomName.text = roomData.Name;
        maxUserCount = roomData.MaxUserNum;
        roomCount.text = string.Format("{0}/{1}", roomData.Users.Count, roomData.MaxUserNum);

        // users 리스트 초기화
        users.Clear();

        // 유저 정보 설정
        foreach (var userData in roomData.Users)
        {
            var userInfo = userData.Id == UserInfo.myInfo.id ? userData.ToUserInfo() : new UserInfo(userData);
            users.Add(userInfo);
        }

        // 빈 방일 경우
        if (roomData.Users.Count == 0)
        {
            // 내 정보 추가
            users.Add(UserInfo.myInfo);
        }

        // 슬롯 UI 업데이트
        for (int i = 0; i < slots.Count; i++)
        {
            var userInfo = users.Count > i ? users[i] : null;
            slots[i].SetItem(userInfo, userInfo != null ? null : (!SocketManager.instance.isConnected ? OnClickTestAddUser : null));

            // 기존 레디 상태 복원
            if (userInfo != null && readyUserIds.Contains(userInfo.id))
                slots[i].SetReady(true);
        }

        // 버튼 상태 초기화
        buttonStart.gameObject.SetActive(false);
        buttonReady.interactable = roomData.State == 0;
        buttonExit.interactable = roomData.State == 0;

        UpdateStartButton();
    }

    /// <summary>
    /// 새로운 유저 추가 및 UI 업데이트
    /// </summary>
    public void AddUserInfo(UserInfo userinfo)
    {
        users.Add(userinfo);
        // 슬롯 UI 업데이트
        for (int i = 0; i < slots.Count; i++)
        {
            var userInfo = users.Count > i ? users[i] : null;
            slots[i].SetItem(userInfo, userInfo != null ? null : (!SocketManager.instance.isConnected ? OnClickTestAddUser : null));

            // 기존 레디 상태 복원
            if (userInfo != null && readyUserIds.Contains(userInfo.id))
                slots[i].SetReady(true);
        }

        // 인원 표시, 시작 버튼(방장) 업데이트
        roomCount.text = string.Format("{0}/{1}", users.Count, maxUserCount);
        UpdateStartButton();
    }

    /// <summary>
    /// 유저 제거 및 UI 업데이트
    /// </summary>
    public void RemoveUserInfo(int userId)
    {
        var userToRemove = users.Find(obj => obj.id == userId);
        if (userToRemove != null)
        {
            users.Remove(userToRemove);
            readyUserIds.Remove(userId);

            // 슬롯 UI 업데이트
            for (int i = 0; i < slots.Count; i++)
            {
                var userInfo = users.Count > i ? users[i] : null;
                slots[i].SetItem(userInfo, userInfo != null ? null : (!SocketManager.instance.isConnected ? OnClickTestAddUser : null));

                // 기존 레디 상태 복원
                if (userInfo != null && readyUserIds.Contains(userInfo.id))
                    slots[i].SetReady(true);
            }

            // 인원 표시, 시작 버튼(방장) 업데이트
            roomCount.text = string.Format("{0}/{1}", users.Count, maxUserCount);
            UpdateStartButton();
        }
        else
        {
            Debug.LogError($"Failed to find user with ID {userId} to remove");
            Debug.Log($"Current users: {string.Join(", ", users.Select(u => $"ID: {u.id}"))}");
        }
    }

    public void OnClickTestAddUser(int slot)
    {
        var newUser = UserInfo.CreateRandomUser();
        users.Add(newUser);
        slots[slot].SetItem(newUser, null);
        roomCount.text = string.Format("{0}/{1}", users.Count, maxUserCount);
        buttonStart.gameObject.SetActive(true);
        buttonStart.interactable = true;
    }

    /// <summary>
    /// 레디 버튼 클릭 메서드
    /// </summary>
    public void OnClickReadyButton()
    {
        // 현재 레디 상태 전송
        GamePacket packet = new GamePacket();
        packet.RoomReadyRequest = new C2SRoomReadyRequest();
        packet.RoomReadyRequest.IsReady = !isReady; // 현재 레디 상태 반대로 송신
        SocketManager.instance.Send(packet);

        UpdateUserReady(UserInfo.myInfo.id, !isReady);
    }

    // 레디 상태 업데이트
    public void UpdateUserReady(int userId, bool isReady)
    {
        Debug.Log($"UpdateUserReady - UserID: {userId}, IsReady: {isReady}");

        // 자신의 레디 상태 업데이트
        if (userId == UserInfo.myInfo.id)
            this.isReady = isReady;

        if (isReady && !readyUserIds.Contains(userId))
            readyUserIds.Add(userId);
        else
            readyUserIds.Remove(userId);

        // 레디 UI 업데이트
        var userIndex = users.FindIndex(u => u.id == userId);
        if (userIndex >= 0 && userIndex < slots.Count)
        {
            slots[userIndex].SetReady(isReady);
            Debug.Log($"Setting ready state for user {userId} at index {userIndex} to {isReady}");
        }

        UpdateStartButton();
    }

    /// <summary>
    /// 게임 시작 버튼 클릭 메서드
    /// </summary>
    public void OnClickGameStart()
    {
        //if (users.Count < 4) return;
        if (SocketManager.instance.isConnected)
        {
            GamePacket packet = new GamePacket();
            packet.GamePrepareRequest = new C2SGamePrepareRequest();
            SocketManager.instance.Send(packet);
        }
        else
        {
            var roles = new Dictionary<int, List<eRoleType>>() {
                { 4, new List<eRoleType>() { eRoleType.target, eRoleType.psychopass, eRoleType.hitman, eRoleType.hitman } },
                { 5, new List<eRoleType>() { eRoleType.target, eRoleType.psychopass, eRoleType.hitman, eRoleType.hitman, eRoleType.bodyguard } },
                { 6, new List<eRoleType>() { eRoleType.target, eRoleType.psychopass, eRoleType.hitman, eRoleType.hitman, eRoleType.hitman, eRoleType.bodyguard } },
                { 7, new List<eRoleType>() { eRoleType.target, eRoleType.psychopass, eRoleType.hitman, eRoleType.hitman, eRoleType.hitman, eRoleType.bodyguard, eRoleType.bodyguard } }
            };

            var role = roles[users.Count];

            users.ForEach(obj =>
            {
                var rand = Random.Range(0, role.Count);
                obj.roleType = role[rand];
                role.RemoveAt(rand);
            });

            var characters = new List<CharacterDataSO>(DataManager.instance.GetDatas<CharacterDataSO>());
            users.ForEach(obj =>
            {
                var rand = Random.Range(0, characters.Count);
                obj.selectedCharacterRcode = characters[rand].rcode;
                characters.RemoveAt(rand);
            });

            OnPrepare(users);
        }
    }

    public void OnPrepare(RepeatedField<UserData> users)
    {
        this.users.UpdateUserData(users);

        //this.users.UpdateUserData(users);  // 확장 메서드 방식 - 두 코드는 동일한 동작 수행
        //ExtensionMethods.UpdateUserData(this.users, users);  // 정적 메서드 방식 - 두 코드는 동일한 동작 수행

        OnPrepare(this.users);
    }

    /// <summary>
    /// 게임 준비 처리
    /// </summary>
    public async void OnPrepare(List<UserInfo> userDatas)
    {
        users = userDatas;
        // 타겟 마크 설정
        var idx = users.FindIndex(obj => obj.roleType == eRoleType.target);
        if (idx >= 0)
            slots[idx].OnTargetMark();
        // 내 역할 아이콘 설정
        var myIdx = users.FindIndex(obj => obj.id == UserInfo.myInfo.id);
        slots[myIdx].SetRoleIcon(users[myIdx].roleType);

        await Task.Delay(1000);

        // 캐릭터 설정
        for (int i = 0; i < users.Count; i++)
        {
            slots[i].OnChangeCharacter(users[i].selectedCharacterRcode);
        }

        await Task.Delay(3000);

        DataManager.instance.users = users;

        if (SocketManager.instance.isConnected)
        {
            if (UserInfo.myInfo.id == roomData.OwnerId)
            {
                GamePacket packet = new GamePacket();
                packet.GameStartRequest = new C2SGameStartRequest();
                SocketManager.instance.Send(packet);
            }
        }
        else
        {
            OnGameStart();
        }
    }

    /// <summary>
    /// 게임 시작 처리
    /// </summary>
    public async void OnGameStart()
    {
        await SceneManager.LoadSceneAsync("Game");
    }

    /// <summary>
    /// 시작 버튼 상태 업데이트
    /// </summary>
    private void UpdateStartButton()
    {
        // 방장, 2명 이상, 모든 유저가 레디했을 경우
        bool canStart = roomData.OwnerId == UserInfo.myInfo.id &&
                        users.Count > 1 &&
                        readyUserIds.Count == users.Count;

        // 방장일 경우, 모든 조건 충족 시 시작 버튼 활성화
        if (roomData.OwnerId == UserInfo.myInfo.id)
            buttonStart.gameObject.SetActive(canStart);
        else
            buttonStart.gameObject.SetActive(false);
    }

    /// <summary>
    /// 방 나가기 버튼 클릭 시 호출되는 메서드
    /// </summary>
    public void OnClickExit()
    {
        if (SocketManager.instance.isConnected)
        {
            GamePacket packet = new GamePacket();
            packet.LeaveRoomRequest = new C2SLeaveRoomRequest();
            SocketManager.instance.Send(packet);
        }
        else
        {
            HideDirect();
        }
    }

    /// <summary>
    /// 새로운 방장 업데이트
    /// </summary>
    public void UpdateRoomOwner(int newOwnerId)
    {
        roomData.OwnerId = newOwnerId;
        UpdateStartButton();
    }

    public override void HideDirect()
    {
        UIManager.Hide<UIRoom>();
        UIManager.Show<UIGnb>();
    }
}