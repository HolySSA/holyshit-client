using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ironcow;
using UnityEngine.UI;
using System;
using TMPro;

public class PopupChat : UIBase
{
    [SerializeField] private ScrollRect chatScrollRect;        // 채팅 스크롤뷰
    [SerializeField] private RectTransform contentRect;       // 스크롤뷰의 컨텐츠
    [SerializeField] private TMP_InputField chatInputField;       // 채팅 입력 필드
    [SerializeField] private GameObject chatItemPrefab;       // 채팅 메시지 프리팹
    private List<GameObject> chatItems = new List<GameObject>();  // 채팅 메시지 오브젝트 관리용

    private bool isScrolledToBottom = true; // 스크롤이 맨 아래에 있는지 확인

    public override void Opened(object[] param)
    {
        chatInputField.onEndEdit.AddListener(OnInputFieldEndEdit);
        chatScrollRect.onValueChanged.AddListener(OnScrollValueChanged);
    }

    private void OnScrollValueChanged(Vector2 value)
    {
        // 스크롤 위치가 거의 바닥에 가까우면 true
        isScrolledToBottom = chatScrollRect.verticalNormalizedPosition <= 0.01f;
    }

    public override void HideDirect()
    {
        UIManager.Hide<PopupChat>();
    }

    private void OnInputFieldEndEdit(string text)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (!string.IsNullOrEmpty(text))
                SendMessage();
        }
    }

    public void OnSendButtonClicked()
    {
        if (!string.IsNullOrEmpty(chatInputField.text))
            SendMessage();
    }

    private void SendMessage()
    {
        string message = chatInputField.text;
        CreateChatMessage("사용자", message, ChatMessageType.User);
        chatInputField.text = "";
        chatInputField.ActivateInputField();
    }

    private void CreateChatMessage(string userName, string message, ChatMessageType messageType)
    {
        // 채팅 메시지 생성
        GameObject chatItem = Instantiate(chatItemPrefab, contentRect);
        ItemChat messageItem = chatItem.GetComponent<ItemChat>();

        if (messageItem != null)
        {
            ChatMessageInfo info = new ChatMessageInfo(userName, message, DateTime.Now, messageType);
            messageItem.SetItem(info);
            chatItems.Add(chatItem);
        }

        Canvas.ForceUpdateCanvases();
        // 스크롤이 맨 아래에 있을 때만 자동 스크롤
        if (isScrolledToBottom)
            StartCoroutine(ScrollToBottom());
    }

    private IEnumerator ScrollToBottom()
    {
        // 다음 프레임에서 스크롤 조정 (UI 업데이트 후)
        yield return new WaitForEndOfFrame();

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        chatScrollRect.verticalNormalizedPosition = 0f;
    }
}