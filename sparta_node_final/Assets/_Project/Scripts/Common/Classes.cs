using Google.Protobuf.Collections;
using Ironcow;
using System.Collections.Generic;
using UnityEngine;

public partial class UserInfo
{
    public int id; // 유저 ID
    public string nickname; // 유저 닉네임
    public int coin; // 코인
    // 캐릭터 코드
    private string _selectedCharacterRcode;
    public string selectedCharacterRcode
    {
        get => _selectedCharacterRcode;
        set => _selectedCharacterRcode = value;
    }
    // 역할 타입
    public eRoleType roleType { get => (eRoleType)characterData.RoleType; set => characterData.RoleType = (RoleType)value; }
    public CharacterData characterData = new CharacterData(); // 캐릭터 데이터
    public List<CardDataSO> handCards = new List<CardDataSO>(); // 손에 들고 있는 카드
    public CardDataSO weapon; // 무기
    public List<CardDataSO> equips = new List<CardDataSO>(); // 장비
    public List<CardDataSO> debuffs = new List<CardDataSO>(); // 디버프
    public int handcardCount { get => characterData.HandCardsCount; } // 보유 카드 수
    public int hp { get => characterData.Hp; set => characterData.Hp = value; } // 체력
    public int maxHp; // 최대 체력

    public bool isStelth { get => equips.Find(obj => obj.rcode == "CAD00020") != null; } // 스텔스 상태 여부 (CAD00020 장비 착용 시)
    public bool isRaider { get => equips.Find(obj => obj.rcode == "CAD00018") != null; } // 레이더 상태 여부 (CAD00018 장비 착용 시)
    public int slotRange { get => 1 + (isRaider ? 1 : 0) + (selectedCharacterRcode == "CHA00009" ? 1 : 0); } // 사정거리 계산 (기본 1 + 레이더 보유 시 +1 + 특정 캐릭터(CHA00009) 선택 시 +1)
    public int slotFar { get => (isStelth ? 1 : 0) + (selectedCharacterRcode == "CHA00012" ? 1 : 0); } // 원거리 슬롯 수 (스텔스 시 +1 + 특정 캐릭터(CHA00012) 선택 시 +1)

    // 멀티샷 캐릭터 여부 (CHA00003)
    public bool isMultiShotCharacter { get => selectedCharacterRcode == "CHA00003"; }
    // 필요한 방어 카드 수 계산
    public int needShieldCount { get => (equips.Find(obj => obj.rcode == "CAD00017") == null ? 1 : 2) + (isMultiShotCharacter ? 1 : 0); }
    // 스나이퍼 무기 장착 여부
    public bool isSniper { get => weapon != null && weapon.rcode == "CAD00013"; }
    // 발사 가능 횟수 계산
    public int bbangCount { get => (weapon != null && weapon.rcode == "CAD00016") || characterData.CharacterType == CharacterType.Red ? 99 : ((weapon != null && weapon.rcode == "CAD00014") ? 2 : 1); }
    // 파워샷 무기 장착 여부
    public bool isPowerShot { get => weapon != null && weapon.rcode == "CAD00015"; }
    // 현재 발사 횟수
    public int shotCount { get => characterData.BbangCount; }
    // 추가 발사 가능 여부
    public bool isShotPossible { get => shotCount < bbangCount; }
    // 유저 인덱스
    public int index { get => DataManager.instance.users.FindIndex(obj => obj.id == id); }

    // 랜덤 이름 생성용 리스트
    static List<string> firstName = new List<string>() { "똑똑한", "멍청한", "신나는", "멋있는", "귀여운", "착한", "예쁜", "아름다운" };
    static List<string> lastName = new List<string>() { "테스트1", "테스트2", "테스트3", "테스트4", "테스트5", "테스트6", "테스트7", "테스트8" };

    public UserInfo()
    {
        // 기본 생성자
    }

    /// <summary>
    /// 유저 정보 생성
    /// </summary>
    public UserInfo(int id, string nickname)
    {
        this.id = id;
        this.nickname = nickname;
    }

    /// <summary>
    /// 현재 캐릭터 코드 설정
    /// </summary>
    public void SetCharacterRcode(CharacterType characterType)
    {
        selectedCharacterRcode = $"CHA{(int)characterType:00000}";
        //Debug.Log("selectedCharacterRcode : " + selectedCharacterRcode);
    }

    /// UserData로부터 UserInfo 생성
    /// </summary>
    public UserInfo(UserData userData)
    {
        this.id = userData.Id;
        this.nickname = userData.Nickname;
        characterData = userData.Character;
        if (characterData != null)
        {
            this.maxHp = userData.Character.Hp;
            SetCharacterRcode(userData.Character.CharacterType);
            foreach (var card in userData.Character.HandCards)
            {
                for (int i = 0; i < card.Count; i++)
                {
                    if (card.Type != CardType.None)
                        handCards.Add(card.GetCardData());
                }
            }
            if (userData.Character.Weapon > 0)
            {
                weapon = DataManager.instance.GetData<CardDataSO>(string.Format("CAD{0:00000}", userData.Character.Weapon));
            }
            foreach (var card in userData.Character.Equips)
            {
                equips.Add(DataManager.instance.GetData<CardDataSO>(string.Format("CAD{0:00000}", card)));
            }
            foreach (var card in userData.Character.Debuffs)
            {
                debuffs.Add(DataManager.instance.GetData<CardDataSO>(string.Format("CAD{0:00000}", card)));
            }
        }
    }

    /// <summary>
    /// UserData 변환
    /// </summary>
    public UserData ToUserData()
    {
        var userData = new UserData();
        userData.Id = this.id;
        userData.Nickname = this.nickname;
        return userData;
    }

    public void OnDayOfAfter()
    {
        //shotCount = 0;
    }

    /// <summary>
    /// 유저 정보 업데이트
    /// </summary>
    public void UpdateUserInfo(UserData userData)
    {
        characterData = userData.Character;
        if (characterData != null)
        {
            handCards.Clear();
            equips.Clear();
            debuffs.Clear();
            weapon = null;
            foreach (var card in userData.Character.HandCards)
            {
                for (int i = 0; i < card.Count; i++)
                {
                    if (card.Type != CardType.None)
                        handCards.Add(card.GetCardData());
                }
            }
            if (userData.Character.Weapon > 0)
            {
                weapon = DataManager.instance.GetData<CardDataSO>(string.Format("CAD{0:00000}", userData.Character.Weapon));
            }
            foreach (var card in userData.Character.Equips)
            {
                equips.Add(DataManager.instance.GetData<CardDataSO>(string.Format("CAD{0:00000}", card)));
            }
            foreach (var card in userData.Character.Debuffs)
            {
                debuffs.Add(DataManager.instance.GetData<CardDataSO>(string.Format("CAD{0:00000}", card)));
            }
        }
    }

    public void SetUserInfo(UserData userData)
    {
        Debug.Log($"[UserInfo] SetUserInfo - ID: {userData.Id}, Nickname: {userData.Nickname}");

        characterData = userData.Character;
        if (characterData != null)
        {
            Debug.Log($"[UserInfo] Character Info - HP: {userData.Character.Hp}, Type: {userData.Character.CharacterType}");
            this.maxHp = userData.Character.Hp;
            foreach (var card in userData.Character.HandCards)
            {
                for (int i = 0; i < card.Count; i++)
                {
                    if (card.Type != CardType.None)
                        handCards.Add(card.GetCardData());
                }
            }
            if (userData.Character.Weapon > 0)
            {
                weapon = DataManager.instance.GetData<CardDataSO>(string.Format("CAD{0:00000}", userData.Character.Weapon));
            }
            foreach (var card in userData.Character.Equips)
            {
                equips.Add(DataManager.instance.GetData<CardDataSO>(string.Format("CAD{0:00000}", card)));
            }
            foreach (var card in userData.Character.Debuffs)
            {
                debuffs.Add(DataManager.instance.GetData<CardDataSO>(string.Format("CAD{0:00000}", card)));
            }
        }
    }

    /// <summary>
    /// 들고 있는 카드 업데이트
    /// </summary>
    public void UpdateHandCard(RepeatedField<CardData> handCards)
    {
        this.handCards.Clear();
        foreach (var card in handCards)
        {
            for (int i = 0; i < card.Count; i++)
            {
                this.handCards.Add(card.GetCardData());
            }
        }
    }

    public void Clear()
    {
        handCards.Clear();
        equips.Clear();
        debuffs.Clear();
        weapon = null;
        characterData = null;
    }

    public static UserInfo CreateRandomUser()
    {
        var userinfo = new UserInfo();
        userinfo.nickname = firstName.RandomValue() + " " + lastName.RandomValue();
        userinfo.id = Util.Random(1000, 9999);

        return userinfo;
    }

    /// <summary>
    /// 손에 카드 추가
    /// </summary>
    public void AddHandCard(CardDataSO card)
    {
        handCards.Add(card);
    }

    /// <summary>
    /// 카드 사용
    /// </summary>
    public CardDataSO OnUseCard(int idx)
    {
        return OnUseCard(handCards[idx]);
    }

    /// <summary>
    /// 카드 사용
    /// </summary>
    public CardDataSO OnUseCard(string rcode)
    {
        return OnUseCard(handCards.Find(obj => obj.rcode == rcode));
    }

    /// <summary>
    /// 카드 사용
    /// </summary>
    public CardDataSO OnUseCard(CardDataSO card)
    {
        switch (card.type)
        {
            case eCardType.active:
                {
                    if (!card.isDirectUse)
                        GameManager.instance.SelectedCard = card;
                    else
                    {
                        handCards.Remove(card);
                        GameManager.instance.TrashCard(card);
                    }
                }
                break;
            case eCardType.weapon:
                {
                    if (weapon != null)
                    {
                        GameManager.instance.TrashCard(weapon);
                    }
                    weapon = card;
                    handCards.Remove(card);
                    UIGame.instance.SetDeckCount();
                }
                break;
            case eCardType.equip:
                {
                    var idx = equips.FindIndex(obj => obj.rcode == card.rcode);
                    if (idx >= 0)
                    {
                        var old = equips[idx];
                        GameManager.instance.TrashCard(old);
                        equips.Remove(old);
                    }
                    equips.Add(card);
                    handCards.Remove(card);
                    UIGame.instance.SetDeckCount();
                }
                break;
            case eCardType.debuff:
                {
                    debuffs.Add(card);
                    if (!card.isDirectUse)
                        GameManager.instance.SelectedCard = card;
                    else
                    {
                        handCards.Remove(card);
                        GameManager.instance.TrashCard(card);
                    }
                }
                break;
        }
        return card;
    }
}