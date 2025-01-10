using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ironcow;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System;

/// <summary>
/// 캐릭터 선택 리스트 아이템
/// </summary>
public class ItemCharacter : UIListItem
{
  [Header("UI Components")]
  [SerializeField] private Image characterImage; // 캐릭터 이미지
  [SerializeField] private Image frameImage;  // 프레임 이미지
  [SerializeField] private TextMeshProUGUI nameText; // 캐릭터 이름
  [SerializeField] private GameObject lockIcon; // 잠금 아이콘
  [SerializeField] private GameObject select; // 선택 표시 오브젝트

  [Header("Colors")]
  [SerializeField] private Color normalColor = Color.white;
  [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);

  private CharacterInfo characterInfo;
  private Action<ItemCharacter> onSelect;

  /// <summary>
  /// 아이템 초기화
  /// </summary>
  public void SetItem(CharacterInfo info, Action<ItemCharacter> callback)
  {
    characterInfo = info;
    onSelect = callback;
    select.SetActive(false);

    // UI 설정
    characterImage.sprite = DataManager.instance.GetData<CharacterDataSO>($"CHA{(int)info.characterType:00000}").thumbnail;
    nameText.text = info.name;
    
    // 잠금 상태에 따른 UI 설정
    bool isLocked = !info.owned;
    lockIcon.SetActive(isLocked);
    frameImage.color = isLocked ? lockedColor : normalColor;
  }

  public void OnClickCharacter()
  {
    // 잠금 상태일 경우 클릭 무시
    if (!characterInfo.owned) return;

    onSelect?.Invoke(this);
  }

  public void OnSelect(bool isSelect)
  {
    select.SetActive(isSelect);
  }

  public CharacterInfo GetCharacterInfo()
  {
    return characterInfo;
  }
}