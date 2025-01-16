using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ironcow;
using System.Net.Sockets;
using System.Net;
using System;
using Google.Protobuf;
using static GamePacket;
using Ironcow.WebSocketPacket;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.IO;

public abstract class TCPSocketManagerBase<T> : MonoSingleton<T> where T : TCPSocketManagerBase<T>
{
    public bool useDNS = false; // DNS 사용 여부
    public Dictionary<PayloadOneofCase, Action<GamePacket>> _onRecv = new Dictionary<PayloadOneofCase, Action<GamePacket>>();

    public Queue<Packet> sendQueue = new Queue<Packet>(); // 전송 패킷 큐
    public Queue<Packet> receiveQueue = new Queue<Packet>(); // 수신 패킷 큐

    public string ip = "127.0.0.1"; // 서버 IP
    public int port = 3000; // 서버 포트

    public Socket socket; // TCP 소켓
    public string version = "1.0.0"; // 버전
    public int sequenceNumber = 1; // 패킷 시퀀스 번호

    byte[] recvBuff = new byte[1024]; // 수신 버퍼
    private byte[] remainBuffer = Array.Empty<byte>(); // 남은 버퍼

    public bool isConnected; // 연결 여부
    public bool isProcessingInit = false; // 중복 여부
    bool isInit = false; // 초기화 여부

    /// <summary>
    /// 리플렉션을 사용하여 PayloadOneofCase에 해당하는 메서드들을 자동으로 등록
    /// </summary>
    protected void InitPackets()
    {
        if (isInit) return;
        var payloads = Enum.GetNames(typeof(PayloadOneofCase));
        var methods = GetType().GetMethods();
        foreach (var payload in payloads)
        {
            var val = (PayloadOneofCase)Enum.Parse(typeof(PayloadOneofCase), payload);
            var method = GetType().GetMethod(payload);
            if (method != null)
            {
                var action = (Action<GamePacket>)Delegate.CreateDelegate(typeof(Action<GamePacket>), this, method);
                _onRecv.Add(val, action);
            }
        }
        isInit = true;
    }

    /// <summary>
    /// 서버 연결에 필요한 IP와 포트를 초기화하고 패킷 처리 메서드를 등록
    /// </summary>
    /// <param name="ip"></param>
    /// <param name="port"></param>
    /// <returns></returns>
    public TCPSocketManagerBase<T> Init(string ip, int port)
    {
        this.ip = ip;
        this.port = port;
        InitPackets();
        return this;
    }

    /// <summary>
    /// 서버에 연결을 시도하고 성공 시 각종 이벤트 처리 시작
    /// </summary>
    /// <param name="callback"></param>
    public async void Connect(UnityAction callback = null)
    {
        // IP 주소 설정
        IPEndPoint endPoint;
        if (IPAddress.TryParse(ip, out IPAddress ipAddress))
        {
            endPoint = new IPEndPoint(ipAddress, port);
        }
        else
        {
            endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
        }

        // DNS 사용 시 호스트 이름으로 IP 주소 찾기
        if (useDNS)
        {
            IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
            ipAddress = ipHost.AddressList[0];

            endPoint = new IPEndPoint(ipAddress, port);
        }

        Debug.Log("Tcp Ip : " + ipAddress.MapToIPv4().ToString() + ", Port : " + port);
        socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            await socket.ConnectAsync(endPoint);
            isConnected = socket.Connected;
            OnReceive();
            StartCoroutine(OnSendQueue());
            StartCoroutine(OnReceiveQueue());
            StartCoroutine(Ping());
            callback?.Invoke();
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    /// <summary>
    /// 서버로부터 데이터 수신하고 패킷으로 파싱하여 receiveQueue에 추가
    /// </summary>
    private async void OnReceive()
    {
        if (socket != null)
        {
            while (socket.Connected && isConnected)
            {
                try
                {
                    var recvByteLength = await socket.ReceiveAsync(recvBuff, SocketFlags.None); //socket.ReceiveAsync�� await�� ��� �� ���ο� �����͸� �ޱ� ������ ����Ѵ�.
                    if (!isConnected)
                    {
                        Debug.Log("Socket is disconnect");
                        break;
                    }
                    if (recvByteLength <= 0)
                    {
                        continue;
                    }

                    var newBuffer = new byte[remainBuffer.Length + recvByteLength];
                    Array.Copy(remainBuffer, 0, newBuffer, 0, remainBuffer.Length);
                    Array.Copy(recvBuff, 0, newBuffer, remainBuffer.Length, recvByteLength);

                    var processedLength = 0;
                    while (processedLength < newBuffer.Length)
                    {
                        if (newBuffer.Length - processedLength < 11)
                        {
                            break;
                        }

                        using var stream = new MemoryStream(newBuffer, processedLength, newBuffer.Length - processedLength);
                        using var reader = new BinaryReader(stream);

                        var typeBytes = reader.ReadBytes(2);
                        Array.Reverse(typeBytes);

                        var type = (PayloadOneofCase)BitConverter.ToInt16(typeBytes);
                        Debug.Log($"PacketType:{type}");

                        var versionLength = reader.ReadByte();
                        if (newBuffer.Length - processedLength < 11 + versionLength)
                        {
                            break;
                        }
                        var versionBytes = reader.ReadBytes(versionLength);
                        var version = System.Text.Encoding.UTF8.GetString(versionBytes);

                        var sequenceBytes = reader.ReadBytes(4);
                        Array.Reverse(sequenceBytes);
                        var sequence = BitConverter.ToInt32(sequenceBytes);

                        var payloadLengthBytes = reader.ReadBytes(4);
                        Array.Reverse(payloadLengthBytes);
                        var payloadLength = BitConverter.ToInt32(payloadLengthBytes);

                        if (newBuffer.Length - processedLength < 11 + versionLength + payloadLength)
                        {
                            break;
                        }
                        var payloadBytes = reader.ReadBytes(payloadLength);

                        var totalLength = 11 + versionLength + payloadLength;
                        var packet = new Packet(type, version, sequence, payloadBytes);
                        receiveQueue.Enqueue(packet);
                        Debug.Log($"Enqueued Type: {type}|{receiveQueue.Count}");

                        processedLength += totalLength;
                    }

                    var remainLength = newBuffer.Length - processedLength;
                    if (remainLength > 0)
                    {
                        remainBuffer = new byte[remainLength];
                        Array.Copy(newBuffer, processedLength, remainBuffer, 0, remainLength);
                        break;
                    }

                    remainBuffer = Array.Empty<byte>();
                }
                catch (Exception e)
                {
                    Debug.LogError($"{e.StackTrace}");
                }
            }
            if (socket != null && socket.Connected)
            {
                Debug.Log("���� ���ú� ���� �ٽ� ����");
                OnReceive();
            }
        }
    }
    /// <summary>
    /// 게임 패킷을 sendQueue에 추가
    /// </summary>
    /// <param name="gamePacket"></param>
    public void Send(GamePacket gamePacket)
    {
        if (socket == null) return;

        Debug.Log($"Sending packet: Type={gamePacket.PayloadCase}, Version={version}, Sequence={sequenceNumber}");
        
        var byteArray = gamePacket.ToByteArray();
        var packet = new Packet(gamePacket.PayloadCase, version, sequenceNumber++, byteArray);
        sendQueue.Enqueue(packet);
    }

    /// <summary>
    /// sendQueue 패킷을 서버로 전송
    /// </summary>
    /// <returns></returns>
    IEnumerator OnSendQueue()
    {
        while (true)
        {
            yield return new WaitUntil(() => sendQueue.Count > 0);
            var packet = sendQueue.Dequeue();

            var bytes = packet.ToByteArray();
            var sent = socket.Send(bytes, SocketFlags.None);

            yield return new WaitForSeconds(0.01f);
        }
    }

    /// <summary>
    /// receiveQueue 패킷을 처리하여 해당하는 이벤트 핸들러 호출
    /// </summary>
    /// <returns></returns>
    IEnumerator OnReceiveQueue()
    {
        while (true)
        {
            yield return new WaitUntil(() => receiveQueue.Count > 0);
            try
            {
                var packet = receiveQueue.Dequeue();
                Debug.Log("Receive Packet : " + packet.type.ToString());
                _onRecv[packet.type].Invoke(packet.gamePacket);
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
            yield return new WaitForSeconds(0.01f);
        }
    }

    /// <summary>
    /// 서버와의 연결 종료 처리
    /// </summary>
    private void OnDestroy()
    {
        Disconnect();
    }

    /// <summary>
    /// 서버와의 연결을 종료하고 필요한 경우 재연결
    /// </summary>
    /// <param name="isReconnect">재연결 여부</param>
    /// <param name="isError">에러 여부</param>
    public async void Disconnect(bool isReconnect = false, bool isError = true)
    {
        StopAllCoroutines();
        if (isConnected)
        {
            this.isConnected = false;
            socket.Disconnect(isReconnect);
            
            if (isReconnect)
            {
                Connect();
            }
            else if (isError)
            {
                if (SceneManager.GetActiveScene().name != "Main")
                {
                    await SceneManager.LoadSceneAsync("Main");
                }
                else
                {
                    UIManager.Hide<UITopBar>();
                    UIManager.Hide<UIGnb>();
                    await UIManager.Show<PopupLogin>();
                }
            }
        }
    }

    /// <summary>
    /// 서버와의 연결 유지를 위한 핑 전송
    /// </summary>
    public IEnumerator Ping()
    {
        while (SocketManager.instance.isConnected)
        {
            yield return new WaitForSeconds(5);
            GamePacket packet = new GamePacket();
            packet.LoginResponse = new S2CLoginResponse();
            //SocketManager.instance.Send(packet);
        }
    }
}
