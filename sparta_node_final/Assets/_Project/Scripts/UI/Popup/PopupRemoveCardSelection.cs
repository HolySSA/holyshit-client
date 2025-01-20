using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ironcow;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using Google.Protobuf.Collections;

public class PopupRemoveCardSelection : UIListBase<Card>
{
    [SerializeField] private UIPagingViewController uiPagingViewController;
    [SerializeField] private Button use;
    [SerializeField] private Transform weaponSlot;
    [SerializeField] private List<Transform> equipSlots;
    [SerializeField] private List<Transform> debuffSlots;
    [SerializeField] private TMP_Text count;

    List<Card> selectCards = new List<Card>(); // 선택된 카드 리스트
    UserInfo targetUserInfo; // 대상 유저 정보
    string targetRcode;

    public override void Opened(object[] param)
    {
        targetUserInfo = UserInfo.myInfo;
        count.text = string.Format("버릴 카드 : {0} 최소 갯수 : {1}", selectCards.Count, Mathf.Max(0, UserInfo.myInfo.handCards.Count - UserInfo.myInfo.hp));
        SetList();
    }

    public override void HideDirect()
    {
        UIGame.instance.SetDeckCount();
        UIManager.Hide<PopupRemoveCardSelection>();
    }

    public void ClearWeapon()
    {
        if (weaponSlot.childCount > 0)
            Destroy(weaponSlot.GetChild(0).gameObject);
    }

    /// <summary>
    /// 무기 추가
    /// </summary>
    public void AddWeapon(CardDataSO data)
    {
        var item = Instantiate(itemPrefab, weaponSlot);
        item.Init(data);
        item.rectTransform.anchoredPosition = Vector2.zero;
        item.rectTransform.sizeDelta = new Vector2(180, 246);
    }

    public void ClearEquips()
    {
        for (int i = 0; i < equipSlots.Count; i++)
        {
            if (equipSlots[i].childCount > 0)
                Destroy(equipSlots[i].GetChild(0).gameObject);
        }
    }

    public void AddEquip(CardDataSO data, int idx)
    {
        var slot = equipSlots[idx];
        var item = Instantiate(itemPrefab, slot);
        item.Init(data);
        item.rectTransform.anchoredPosition = Vector2.zero;
        item.rectTransform.sizeDelta = new Vector2(180, 246);
    }

    public void ClearDebuffs()
    {
        for (int i = 0; i < debuffSlots.Count; i++)
        {
            if (debuffSlots[i].childCount > 0)
                Destroy(debuffSlots[i].GetChild(0).gameObject);
        }
    }

    public void AddDebuff(CardDataSO data, int idx)
    {
        var slot = debuffSlots[idx];
        var item = Instantiate(itemPrefab, slot);
        item.Init(data);
        item.rectTransform.anchoredPosition = Vector2.zero;
        item.rectTransform.sizeDelta = new Vector2(180, 246);
    }

    /// <summary>
    /// 카드 목록 초기화 및 설정
    /// </summary>
    public override void SetList()
    {
        ClearList();
        ClearEquips();
        ClearWeapon();
        ClearDebuffs();

        // 핸드 카드 추가
        for (int i = 0; i < targetUserInfo.handcardCount; i++)
        {
            var item = AddItem();
            item.Init(targetUserInfo.handCards[i], OnClickItem);
        }

        // 무기 카드 추가
        if(targetUserInfo.weapon != null)
        {
            AddWeapon(targetUserInfo.weapon);
        }

        // 장비 카드 추가
        for (int i = 0; i < targetUserInfo.equips.Count; i++)
        {
            AddEquip(targetUserInfo.equips[i], i);
        }

        // 디버프 카드 추가
        for (int i = 0; i < targetUserInfo.debuffs.Count; i++)
        {
            AddDebuff(targetUserInfo.debuffs[i], i);
        }
    }

    /// <summary>
    /// 카드 선택 이벤트
    /// </summary>
    public void OnClickItem(Card card)
    {
        if (selectCards.Contains(card))
        {
            selectCards.Remove(card);
            card.OnSelect(false);
        }
        else
        {
            selectCards.Add(card);
            card.OnSelect(true);
        }

        // 카운트 업데이트
        count.text = string.Format("버릴 카드 : {0} 최소 갯수 : {1}", selectCards.Count, Mathf.Max(0, UserInfo.myInfo.handCards.Count - UserInfo.myInfo.hp));
        
        // 버튼 및 카운트 활성화 설정
        use.gameObject.SetActive(UserInfo.myInfo.handCards.Count - selectCards.Count == UserInfo.myInfo.hp);
        count.gameObject.SetActive(UserInfo.myInfo.handCards.Count - selectCards.Count != UserInfo.myInfo.hp);
    }

    /// <summary>
    /// 카드 데이터 생성
    /// </summary>
    public List<CardData> CreateField()
    {
        var list = new List<CardData>();
        for (int i = 0; i < selectCards.Count; i++)
        {
            var cardData = list.Find(obj => obj.Type == selectCards[i].cardData.cardType);
            if(cardData != null)
            {
                cardData.Count++;
            }
            else
            {
                list.Add(new CardData() { Type = selectCards[i].cardData.cardType, Count = 1 });
            }
        }
        return list;
    }

    /// <summary>
    /// 버리기 요청 이벤트
    /// </summary>
    public void OnClickUse()
    {
        if (SocketManager.instance.isConnected)
        {
            GamePacket packet = new GamePacket();
            packet.DestroyCardRequest = new C2SDestroyCardRequest();
            packet.DestroyCardRequest.DestroyCards.AddRange(CreateField());
            SocketManager.instance.Send(packet);
        }
        else
        {
        }
    }
}