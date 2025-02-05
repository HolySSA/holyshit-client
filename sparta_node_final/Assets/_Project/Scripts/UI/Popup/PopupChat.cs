using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ironcow;
using UnityEngine.UI;
using System;
using TMPro;

public class PopupChat : UIBase
{
    private const int MAX_CHAT_MESSAGES = 50;
    private const string CHAT_ITEM_CODE = "ItemChat";

    [SerializeField] private ScrollRect chatScrollRect; // 채팅 스크롤뷰
    [SerializeField] private RectTransform contentRect; // 스크롤뷰의 컨텐츠
    [SerializeField] private TMP_InputField chatInputField; // 채팅 입력 필드
    private Queue<ItemChat> activeChatItems = new Queue<ItemChat>();

    private bool isScrolledToBottom = true; // 스크롤이 맨 아래에 있는지 확인

    public override void Opened(object[] param)
    {
        if (!PoolManager.instance.isInit)
            PoolManager.instance.Init();

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
        // 모든 활성 채팅 아이템을 풀로 반환
        while (activeChatItems.Count > 0)
        {
            var item = activeChatItems.Dequeue();
            item.Release();
        }

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

        // 서버로 채팅 메시지 전송
        GamePacket packet = new GamePacket();
        packet.ChatMessageRequest = new C2SChatMessageRequest
        {
            Message = message,
            MessageType = ChatMessageType.UserChat
        };

        SocketManager.instance.Send(packet);
    }

    public void ClearChatInputField()
    {
        chatInputField.text = "";
        chatInputField.ActivateInputField();
    }

    public void CreateChatMessage(string userName, string message, long timestamp, ChatMessageType messageType)
    {
        // timestamp를 DateTime으로 변환 (Unix timestamp -> DateTime)
        DateTime messageTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime.ToLocalTime();
        ChatMessageInfo info = new ChatMessageInfo(userName, message, messageTime, messageType);
        // 오브젝트 풀에서 채팅 아이템 가져오기
        ItemChat messageItem = PoolManager.instance.Spawn<ItemChat>(CHAT_ITEM_CODE, contentRect, info);
        // 최대 메시지 수를 초과하면 가장 오래된 메시지 제거
        if (activeChatItems.Count >= MAX_CHAT_MESSAGES)
        {
            var oldestItem = activeChatItems.Dequeue();
            oldestItem.Release(); // 오브젝트 풀로 반환
        }
        activeChatItems.Enqueue(messageItem);
        Canvas.ForceUpdateCanvases();
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