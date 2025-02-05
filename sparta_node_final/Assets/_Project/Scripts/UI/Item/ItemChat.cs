using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ironcow;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class ItemChat : ObjectPoolBase
{
  [Header("UI Components")]
  [SerializeField] private TextMeshProUGUI userNameText; // 유저 이름
  [SerializeField] private TextMeshProUGUI messageText; // 메시지 내용
  [SerializeField] private TextMeshProUGUI timeText; // 시간

  private ChatMessageInfo messageInfo;

  public override void Init(params object[] param)
  {
    if (param.Length >= 1 && param[0] is ChatMessageInfo info)
      SetItem(info);
  }

  /// <summary>
  /// 채팅 메시지 아이템 초기화
  /// </summary>
  public void SetItem(ChatMessageInfo info)
  {
    messageInfo = info;

    // UI 설정
    userNameText.text = info.userName;
    messageText.text = info.message;
    timeText.text = info.time.ToString("HH:mm");

    // 메시지 타입에 따른 UI 설정 (시스템 메시지, 일반 메시지 등)
    SetMessageStyle(info.messageType);
  }

  private void SetMessageStyle(ChatMessageType messageType)
  {
    switch (messageType)
    {
      case ChatMessageType.SystemChat:
        userNameText.gameObject.SetActive(true);
        userNameText.text = "System";
        userNameText.color = Color.yellow;
        messageText.color = Color.yellow;
        break;
      case ChatMessageType.UserChat:
        userNameText.gameObject.SetActive(true);
        userNameText.color = Color.white;
        messageText.color = Color.white;
        break;
    }
  }

  public ChatMessageInfo GetMessageInfo()
  {
    return messageInfo;
  }
}

// 채팅 메시지 정보를 담는 구조체
public struct ChatMessageInfo
{
  public string userName;
  public string message;
  public DateTime time;
  public ChatMessageType messageType;

  public ChatMessageInfo(string userName, string message, DateTime time, ChatMessageType type = ChatMessageType.UserChat)
  {
    this.userName = userName;
    this.message = message;
    this.time = time;
    this.messageType = type;
  }
}