using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ironcow;
using UnityEngine.UI;
using Ironcow.WebSocketPacket;
using Google.Protobuf;
using static GamePacket;
using Unity.VisualScripting;
using System;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using static UnityEngine.UIElements.UxmlAttributeDescription;

public class SocketManager : TCPSocketManagerBase<SocketManager>
{
    public int level = 1;
    public bool isAnimationPlaying = false;

    /// <summary>
    /// 로그인 응답 처리
    /// </summary>
    public void LoginResponse(GamePacket gamePacket)
    {
        var response = gamePacket.LoginResponse;

        // 인증 실패 시 return
        if (response.FailCode == GlobalFailCode.AuthenticationFailed)
            return;

        // 캐릭터 정보 저장
        StorageManager.SaveCharacterInfos(response.Characters);
        // 로그인 처리
        UIManager.Get<PopupLogin>().OnLoginEnd(response.Success, response.LastSelectedCharacter);
    }

    public void SelectCharacterResponse(GamePacket gamePacket)
    {
        var response = gamePacket.SelectCharacterResponse;
        if (response.Success)
        {
            // 현재 선택된 캐릭터 정보 저장
            UserInfo.myInfo.SetCharacterRcode(response.CharacterType);

            // UI 업데이트
            UIManager.Get<UIMain>()?.UpdateCharacterImage(response.CharacterType);
            UIManager.Get<PopupCharacter>().HideDirect();
        }
    }

    /// <summary>
    /// 방 생성 응답 처리
    /// </summary>
    public void CreateRoomResponse(GamePacket gamePacket)
    {
        var response = gamePacket.CreateRoomResponse;
        // 유저 캐릭터 정보
        UserInfo.myInfo.SetUserInfo(response.Room.Users[0]);
        // 방 생성
        UIManager.Get<PopupRoomCreate>().OnRoomCreateResult(response.Success, response.Room);
    }

    /// <summary>
    /// 방 목록 응답 처리
    /// </summary>
    public void GetRoomListResponse(GamePacket gamePacket)
    {
        var response = gamePacket.GetRoomListResponse;
        UIManager.Get<UIMain>().SetRoomList(response.Rooms.ToList());
    }

    /// <summary>
    /// 방 참여 응답 처리
    /// </summary>
    public void JoinRoomResponse(GamePacket gamePacket)
    {
        var response = gamePacket.JoinRoomResponse;
        if (response.Success)
            UIManager.Show<UIRoom>(response.Room);
    }

    /// <summary>
    /// 랜덤 방 참여 응답 처리
    /// </summary>
    public void JoinRandomRoomResponse(GamePacket gamePacket)
    {
        var response = gamePacket.JoinRandomRoomResponse;
        if (response.Success)
            UIManager.Show<UIRoom>(response.Room);
    }

    /// <summary>
    /// 방 참여 알림 처리
    /// </summary>
    public void JoinRoomNotification(GamePacket gamePacket)
    {
        var response = gamePacket.JoinRoomNotification;
        if (response.JoinUser.Id != UserInfo.myInfo.id)
            UIManager.Get<UIRoom>().AddUserInfo(response.JoinUser.ToUserInfo());
    }

    /// <summary>
    /// 방 나가기 응답 처리
    /// </summary>
    public void LeaveRoomResponse(GamePacket gamePacket)
    {
        var response = gamePacket.LeaveRoomResponse;
        if (response.Success)
            UIManager.Hide<UIRoom>();
    }

    /// <summary>
    /// 방 나가기 알림 처리
    /// </summary>
    public void LeaveRoomNotification(GamePacket gamePacket)
    {
        var response = gamePacket.LeaveRoomNotification;
        UIManager.Get<UIRoom>().RemoveUserInfo(response.UserId);

        if (response.OwnerId != 0)
            UIManager.Get<UIRoom>().UpdateRoomOwner(response.OwnerId);
    }

    /// <summary>
    /// 레디 응답 처리
    /// </summary>
    public void RoomReadyResponse(GamePacket gamePacket)
    {
        var response = gamePacket.RoomReadyResponse;
        if (!response.Success)
        {
            UIManager.ShowAlert(response.FailCode.ToString(), "게임 레디 실패");
            Debug.Log("RoomReadyResponse Failcode : " + response.FailCode.ToString());
        }
    }

    /// <summary>
    /// 레디 알림 처리
    /// </summary>
    public void RoomReadyNotification(GamePacket gamePacket)
    {
        var notification = gamePacket.RoomReadyNotification;
        var readyStates = notification.UserReady;
        UIManager.Get<UIRoom>().UpdateUserReady(readyStates.UserId, readyStates.IsReady);
    }

    /// <summary>
    /// 방 레디 상태 조회 응답 처리
    /// </summary>
    public void GetRoomReadyStateResponse(GamePacket gamePacket)
    {
        var response = gamePacket.GetRoomReadyStateResponse;
        if (response.Success)
        {
            // 모든 유저의 레디 상태 동기화
            foreach (var userReady in response.ReadyStates)
            {
                UIManager.Get<UIRoom>().UpdateUserReady(userReady.UserId, userReady.IsReady);
            }
        }
    }

    /// <summary>
    /// 게임 준비 응답 처리
    /// </summary>
    public void GamePrepareResponse(GamePacket gamePacket)
    {
        var response = gamePacket.GamePrepareResponse;
        if (!response.Success)
        {
            UIManager.ShowAlert(response.FailCode.ToString(), "게임 준비 실패");
            Debug.Log("GamePrepareResponse Failcode : " + response.FailCode.ToString());
        }
    }

    /// <summary>
    /// 게임 준비 알림 처리
    /// </summary>
    public void GamePrepareNotification(GamePacket gamePacket)
    {
        var response = gamePacket.GamePrepareNotification;

        if (response.Room != null)
            UIManager.Get<UIRoom>().SetRoomInfo(response.Room);
        if (response.Room.Users != null)
            UIManager.Get<UIRoom>().OnPrepare(response.Room.Users);
    }

    /// <summary>
    /// 게임 시작 응답 처리
    /// </summary>
    public void GameStartResponse(GamePacket gamePacket)
    {
        var response = gamePacket.GameStartResponse;
        if (response.FailCode != 0)
        {
            UIManager.ShowAlert(response.FailCode.ToString(), "게임 시작 실패");
            Debug.Log("GameStartResponse Failcode : " + response.FailCode.ToString());
        }
    }

    /// <summary>
    /// 게임 시작 알림 처리
    /// </summary>
    public void GameStartNotification(GamePacket gamePacket)
    {
        var response = gamePacket.GameStartNotification;
        var serverInfo = response.ServerInfo;
        var roomData = UIManager.Get<UIRoom>().GetRoomData();

        Disconnect(false, false); // 로비 서버 연결 해제
        Init(serverInfo.Host, serverInfo.Port); // 게임 서버 연결
        Connect(() =>
        {
            // 게임 서버 초기화 패킷 전송
            GamePacket initPacket = new GamePacket();
            initPacket.GameServerInitRequest = new C2SGameServerInitRequest
            {
                UserId = UserInfo.myInfo.id,
                Token = serverInfo.Token,
                RoomData = roomData
            };
            Send(initPacket);
        });
    }

    public async void GameServerInitResponse(GamePacket gamePacket)
    {
        var response = gamePacket.GameServerInitResponse;
        if (response.Success)
        {
            // 게임 씬으로 전환
            await SceneManager.LoadSceneAsync("Game");

            // 게임 준비 화면 표시???
        }
    }

    public async void GameServerInitNotification(GamePacket gamePacket)
    {
        // 이미 처리 중인지 확인
        if (isProcessingInit) return;
        isProcessingInit = true;

        try
        {
            var response = gamePacket.GameServerInitNotification;

            // UI가 완전히 로드될 때까지 대기
            while (!UIManager.IsOpened<UIGame>())
            {
                await Task.Yield();
            }

            // 게임 준비 화면 끄기


            // 유저 정보 초기화
            DataManager.instance.users.Clear();
            // response로 받은 유저 정보 처리
            foreach (var user in response.Users)
            {
                //Debug.Log($"캐릭터 초기화: ID={user.Id}, CharacterType={user.Character?.CharacterType}");

                var userinfo = user.ToUserInfo();
                if (UserInfo.myInfo.id == user.Id)
                {
                    userinfo = UserInfo.myInfo;
                    UserInfo.myInfo.UpdateUserInfo(user);
                    DataManager.instance.users.Add(UserInfo.myInfo); // 내 정보 추가
                }
                else
                {
                    DataManager.instance.users.Add(userinfo); // 다른 유저 정보 추가
                }

                var character = await GameManager.instance.OnCreateCharacter(userinfo, DataManager.instance.users.Count - 1);
                if (character == null)
                {
                    Debug.LogError($"캐릭터 생성 실패: userId={user.Id}");
                    continue;
                }

                var positionData = response.CharacterPositions.FirstOrDefault(pos => pos.Id == user.Id);
                if (positionData != null)
                {
                    // 캐릭터 생성 및 초기 위치 설정
                    var position = positionData.PositionToVector();
                    character.SetPosition(position);
                }
            }

            // 게임 시작 및 상태 설정
            GameManager.instance.isInit = true;
            GameManager.instance.OnGameStart();
            GameManager.instance.SetGameState(response.GameState);
        }
        finally
        {
            isProcessingInit = false;
        }
    }

    /// <summary>
    /// 위치 업데이트 알림 처리
    /// </summary>
    public void PositionUpdateNotification(GamePacket gamePacket)
    {
        var response = gamePacket.PositionUpdateNotification;
        for (int i = 0; i < response.CharacterPositions.Count; i++)
        {
            if (GameManager.instance.characters != null && GameManager.instance.characters.ContainsKey(response.CharacterPositions[i].Id))
                GameManager.instance.characters[response.CharacterPositions[i].Id].SetMovePosition(response.CharacterPositions[i].ToVector3());
        }
    }

    /// <summary>
    /// 카드 사용 응답 처리
    /// </summary>
    public void UseCardResponse(GamePacket gamePacket)
    {
        var response = gamePacket.UseCardResponse;
        if (response.Success)
        {
            if (UIManager.IsOpened<PopupDeck>())
                UIManager.Hide<PopupDeck>();
            if (UIManager.IsOpened<PopupBattle>())
                UIManager.Hide<PopupBattle>();
            UIGame.instance.SetSelectCard(null);
            GameManager.instance.targetCharacter.OnSelect();
            GameManager.instance.targetCharacter = null;
        }
    }

    /// <summary>
    /// 카드 사용 알림 처리
    /// </summary>
    public async void UseCardNotification(GamePacket gamePacket)
    {
        var response = gamePacket.UseCardNotification;
        var card = response.CardType.GetCardData();
        if (card.isTargetCardSelection && response.UserId == UserInfo.myInfo.id)
        {
            await UIManager.Show<PopupCardSelection>(response.TargetUserId, card.rcode);
        }
        var use = DataManager.instance.users.Find(obj => obj.id == response.UserId);
        var target = DataManager.instance.users.Find(obj => obj.id == response.TargetUserId);
        var text = string.Format(response.TargetUserId != 0 ? "{0}������ {1}ī�带 ����߽��ϴ�." : "{0}������ {1}ī�带 {2}�������� ����߽��ϴ�.",
            use.nickname, response.CardType.GetCardData().displayName, target.nickname);
        UIGame.instance.SetNotice(text);
        if (response.UserId == UserInfo.myInfo.id && card.cardType == CardType.Bbang)
        {
            //UserInfo.myInfo.shotCount++;
            UIGame.instance.SetSelectCard(null);
        }

    }

    public void EquipCardNotification(GamePacket gamePacket)
    {
        var response = gamePacket.UseCardNotification;
        var userinfo = DataManager.instance.users.Find(obj => obj.id == response.UserId);
        userinfo.OnUseCard(response.CardType.GetCardRcode());
    }

    public void CardEffectNotification(GamePacket gamePacket)
    {
        var response = gamePacket.UseCardNotification;
        var use = DataManager.instance.users.Find(obj => obj.id == response.UserId);
        var target = DataManager.instance.users.Find(obj => obj.id == response.TargetUserId);
        var text = string.Format(response.TargetUserId != 0 ? "{0}������ {1}ī�带 ����߽��ϴ�." : "{0}������ {1}ī�带 {2}�������� ����߽��ϴ�.",
            use.nickname, response.CardType.GetCardData().displayName, target.nickname);
        UIGame.instance.SetNotice(text);
    }

    public void FleaMarketPickResponse(GamePacket gamePacket)
    {
        var response = gamePacket.FleaMarketPickResponse;

    }

    public async void FleaMarketNotification(GamePacket gamePacket)
    {
        var response = gamePacket.FleaMarketNotification;
        var ui = UIManager.Get<PopupPleaMarket>();
        if (ui == null)
        {
            ui = await UIManager.Show<PopupPleaMarket>();
        }
        if (!ui.isInitCards)
            ui.SetCards(response.CardTypes);
        if (response.CardTypes.Count > response.PickIndex.Count)
            ui.OnSelectedCard(response.PickIndex);
        else
        {
            UIManager.Hide<PopupPleaMarket>();
            for (int i = 0; i < DataManager.instance.users.Count; i++)
            {
                var targetCharacter = GameManager.instance.characters[DataManager.instance.users[i].id];
                targetCharacter.OnChangeState<CharacterIdleState>();
            }
        }
    }

    public void ReactionResponse(GamePacket gamePacket)
    {
        var response = gamePacket.ReactionResponse;
        if (response.Success)
        {
            if (UIManager.IsOpened<PopupBattle>())
                UIManager.Hide<PopupBattle>();
        }
    }

    // ī�� ��� ������ ���� ���� ���� ������Ʈ
    public async void UserUpdateNotification(GamePacket gamePacket)
    {
        while (isAnimationPlaying)
        {
            await Task.Delay(100);
        }
        var response = gamePacket.UserUpdateNotification;
        var users = DataManager.instance.users.UpdateUserData(response.User);
        if (!GameManager.isInstance || GameManager.instance.characters == null || GameManager.instance.characters.Count == 0) return;
        var myIndex = users.FindIndex(obj => obj.id == UserInfo.myInfo.id);
        for (int i = 0; i < users.Count; i++)
        {
            var targetCharacter = GameManager.instance.characters[users[i].id];
            if (users[i].hp == 0)
            {
                targetCharacter.SetDeath();
                UIGame.instance.SetDeath(users[i].id);
            }
            targetCharacter.OnVisibleMinimapIcon(Util.GetDistance(myIndex, i, DataManager.instance.users.Count) + users[i].slotFar <= UserInfo.myInfo.slotRange && myIndex != i); // ������ �Ÿ��� �ִ� ���� �����ܸ� ǥ��

            GamePacket packet = new GamePacket();
            Action<int, int> callback = (type, userId) =>
            {
                if (type == 0 || userId == 0)
                {
                    packet.ReactionRequest = new C2SReactionRequest();
                    packet.ReactionRequest.ReactionType = (ReactionType)type;
                }
                else
                {
                    packet.UseCardRequest = new C2SUseCardRequest();
                    packet.UseCardRequest.CardType = (CardType)type;
                    packet.UseCardRequest.TargetUserId = userId;
                }
                Send(packet);
            };
            if (users[i].id == UserInfo.myInfo.id)
            {
                var user = users[i];
                var targetId = user.characterData.StateInfo.StateTargetUserId;
                var targetInfo = DataManager.instance.users.Find(obj => obj.id == targetId);
                if (user.debuffs.Find(obj => obj.rcode == "CAD00023"))
                {
                    UIGame.instance.SetBombButton(true);
                }
                else
                {
                    UIGame.instance.SetBombButton(false);
                }
                switch ((eCharacterState)users[i].characterData.StateInfo.State)
                {
                    case eCharacterState.BBANG_SHOOTER: // �� ��� �� ���
                        {
                            targetCharacter.OnChangeState<CharacterStopState>();
                        }
                        break;
                    case eCharacterState.BBANG_TARGET: // �� Ÿ��
                        {
                            var card = DataManager.instance.GetData<CardDataSO>("CAD00001");
                            if (user.handCards.FindAll(obj => obj.rcode == card.defCard).Count >= targetInfo.needShieldCount)
                            {
                                targetCharacter.OnChangeState<CharacterStopState>();
                                UIManager.Show<PopupBattle>(card.rcode, users[i].characterData.StateInfo.StateTargetUserId, callback);
                            }
                            else
                            {
                                callback.Invoke(0, 0);
                            }
                        }
                        break;
                    case eCharacterState.DEATH_MATCH: // ���� ���
                        {
                            var card = DataManager.instance.GetData<CardDataSO>("CAD00006");
                            if (user.handCards.Find(obj => obj.rcode == card.defCard))
                            {
                                targetCharacter.OnChangeState<CharacterStopState>();
                                var ui = await UIManager.Show<PopupBattle>(card.rcode, targetId, callback);
                                ui.SetActiveControl(false);
                            }
                        }
                        break;
                    case eCharacterState.DEATH_MATCH_TURN: // ���� ����
                        {
                            var card = DataManager.instance.GetData<CardDataSO>("CAD00006");
                            if (user.handCards.Find(obj => obj.rcode == card.defCard))
                            {
                                targetCharacter.OnChangeState<CharacterStopState>();
                                var ui = await UIManager.Show<PopupBattle>(card.rcode, targetId, callback);
                                ui.SetActiveControl(true);
                            }
                            else
                            {
                                UIManager.Hide<PopupBattle>();
                                callback.Invoke(0, 0);
                            }
                        }
                        break;
                    case eCharacterState.FLEA_MARKET_TURN: // �ø����� �� ��
                        {
                            targetCharacter.OnChangeState<CharacterStopState>();
                            var ui = UIManager.Get<PopupPleaMarket>();
                            if (ui == null)
                            {
                                ui = await UIManager.Show<PopupPleaMarket>();
                            }
                            var dt = DateTimeOffset.FromUnixTimeMilliseconds(user.characterData.StateInfo.NextStateAt) - DateTime.UtcNow;
                            ui.SetUserSelectTurn((int)dt.TotalSeconds);
                        }
                        break;
                    case eCharacterState.FLEA_MARKET_WAIT: // �ö��� ���
                        {
                            targetCharacter.OnChangeState<CharacterStopState>();
                            var ui = UIManager.Get<PopupPleaMarket>();
                            if (ui == null)
                            {
                                ui = await UIManager.Show<PopupPleaMarket>();
                            }
                        }
                        break;
                    case eCharacterState.GUERRILLA_SHOOTER:
                        {
                            targetCharacter.OnChangeState<CharacterStopState>();
                        }
                        break;
                    case eCharacterState.GUERRILLA_TARGET:
                        {
                            var card = DataManager.instance.GetData<CardDataSO>("CAD00007");
                            if (user.handCards.Find(obj => obj.rcode == card.defCard))
                            {
                                targetCharacter.OnChangeState<CharacterStopState>();
                                var ui = await UIManager.Show<PopupBattle>(card.rcode, targetId, callback);
                                ui.SetActiveControl(true);
                            }
                            else
                            {
                                UIManager.Hide<PopupBattle>();
                                callback.Invoke(0, 0);
                            }
                        }
                        break;
                    case eCharacterState.BIG_BBANG_SHOOTER:
                        {
                            targetCharacter.OnChangeState<CharacterStopState>();
                        }
                        break;
                    case eCharacterState.BIG_BBANG_TARGET:
                        {
                            var card = DataManager.instance.GetData<CardDataSO>("CAD00002");
                            if (user.handCards.Find(obj => obj.rcode == card.defCard))
                            {
                                targetCharacter.OnChangeState<CharacterStopState>();
                                var ui = await UIManager.Show<PopupBattle>(card.rcode, targetId, callback);
                                ui.SetActiveControl(true);
                            }
                            else
                            {
                                UIManager.Hide<PopupBattle>();
                                callback.Invoke(0, 0);
                            }
                        }
                        break;
                    case eCharacterState.ABSORBING:
                        {
                            targetCharacter.OnChangeState<CharacterStopState>();
                        }
                        break;
                    case eCharacterState.ABSORB_TARGET:
                        {
                            targetCharacter.OnChangeState<CharacterStopState>();
                        }
                        break;
                    case eCharacterState.HALLUCINATING:
                        {
                            targetCharacter.OnChangeState<CharacterStopState>();
                        }
                        break;
                    case eCharacterState.HALLUCINATION_TARGET:
                        {
                            targetCharacter.OnChangeState<CharacterStopState>();
                        }
                        break;
                    case eCharacterState.NONE:
                        {
                            if (!targetCharacter.IsState<CharacterDeathState>())
                            {
                                targetCharacter.OnChangeState<CharacterIdleState>();
                            }
                            if (UIManager.IsOpened<PopupPleaMarket>())
                                UIManager.Hide<PopupPleaMarket>();
                            if (UIManager.IsOpened<PopupBattle>())
                                UIManager.Hide<PopupBattle>();
                        }
                        break;
                    case eCharacterState.CONTAINED:
                        {
                            Debug.Log(user.id + " is prison");
                            GameManager.instance.userCharacter.OnChangeState<CharacterPrisonState>();
                        }
                        break;
                    default:
                        targetCharacter.OnChangeState<CharacterStopState>();
                        break;
                }
            }
            else
            {
                if (!targetCharacter.IsState<CharacterDeathState>())
                {
                    if ((eCharacterState)users[i].characterData.StateInfo.State == eCharacterState.NONE)
                    {
                        targetCharacter.OnChangeState<CharacterIdleState>();
                    }
                    else
                    {
                        targetCharacter.OnChangeState<CharacterStopState>();
                    }
                }
            }
        }
        if (UIGame.instance != null)
            UIGame.instance.UpdateUserSlot(users);
    }

    // �� ����� (phaseType 3) ī�� ������
    public void DestroyCardResponse(GamePacket gamePacket)
    {
        var response = gamePacket.DestroyCardResponse;
        UIManager.Hide<PopupRemoveCardSelection>();
        UserInfo.myInfo.UpdateHandCard(response.HandCards);
        UIGame.instance.SetSelectCard();
        UIGame.instance.SetDeckCount();
    }

    // ������ ������Ʈ
    public void PhaseUpdateNotification(GamePacket gamePacket)
    {
        var response = gamePacket.PhaseUpdateNotification;
        if (UIGame.instance != null)
            GameManager.instance.SetGameState(response.PhaseType, response.NextPhaseAt);
        for (int i = 0; i < response.CharacterPositions.Count; i++)
        {
            GameManager.instance.characters[DataManager.instance.users[i].id].SetPosition(response.CharacterPositions[i].ToVector3());
        }
    }

    // ���� ����
    public void GameEndNotification(GamePacket gamePacket)
    {
        var response = gamePacket.GameEndNotification;
        GameManager.instance.OnGameEnd();

        UIManager.Show<PopupResult>(response.Winners, response.WinType);
    }

    public void CardSelectResponse(GamePacket gamePacket)
    {
        var response = gamePacket.CardSelectResponse;
        if (response.Success)
        {
            UIManager.Hide<PopupCardSelection>();
        }
        else
        {
            Debug.Log("CardSelectResponse is failed");
        }
    }

    // ��ź �ѱ�� 
    public void PassDebuffResponse(GamePacket gamePacket)
    {
        var response = gamePacket.PassDebuffResponse;
        if (response.Success)
        {
            GameManager.instance.targetCharacter.OnSelect();
            GameManager.instance.targetCharacter = null;
            UIGame.instance.SetBombButton(false);
        }
    }

    // ��ź ���� ��
    public void WarningNotification(GamePacket gamePacket)
    {
        var response = gamePacket.WarningNotification;
        UIGame.instance.SetBombAlert(response.WarningType == WarningType.BombWaning);
    }

    // �ִϸ��̼� ��û
    public async void AnimationNotification(GamePacket gamePacket)
    {
        var response = gamePacket.AnimationNotification;
        var target = GameManager.instance.characters[response.UserId].transform;
        isAnimationPlaying = true;
        switch (response.AnimationType)
        {
            case AnimationType.BombAnimation:
                {
                    GameManager.instance.virtualCamera.Target.TrackingTarget = target;
                    var bomb = Instantiate(await ResourceManager.instance.LoadAsset<Transform>("Explosion", eAddressableType.Prefabs));
                    bomb.transform.position = target.position;
                }
                break;
            case AnimationType.SatelliteTargetAnimation:
                {
                    GameManager.instance.virtualCamera.Target.TrackingTarget = target;
                    var beam = Instantiate(await ResourceManager.instance.LoadAsset<Transform>("Beam", eAddressableType.Prefabs));
                    beam.transform.position = target.position;
                }
                break;
            case AnimationType.ShieldAnimation:
                {
                    var shield = Instantiate(await ResourceManager.instance.LoadAsset<Transform>("Shield", eAddressableType.Prefabs));
                    shield.transform.position = target.position;
                }
                break;
        }
    }


}