using Google.Protobuf;
using Google.Protobuf.Reflection;
using System;
using System.IO;
using UnityEngine;
using static GamePacket;

namespace Ironcow.WebSocketPacket
{
    public class Packet
    {
        public PayloadOneofCase type;
        public string version;
        public int sequence;
        public byte[] payloadBytes;

        public GamePacket gamePacket
        {
            get
            {
                GamePacket gamePacket = new GamePacket();
                gamePacket.MergeFrom(payloadBytes);
                return gamePacket;
            }
        }

        public Packet(PayloadOneofCase type, string version, int sequence, byte[] payload)
        {
            this.type = type;
            this.version = version;
            this.sequence = sequence;
            this.payloadBytes = payload;
        }

        public ArraySegment<byte> ToByteArray()
        {
            using (var stream = new MemoryStream())
            {
                // 먼저 전체 패킷 크기를 계산
                var versionBytes = System.Text.Encoding.UTF8.GetBytes(version);
                var headerSize = 2 + 1 + versionBytes.Length + 4 + 4; // type(2) + versionLength(1) + version(N) + sequence(4) + payloadLength(4)
                var totalSize = headerSize + payloadBytes.Length;

                using (var writer = new BinaryWriter(stream))
                {
                    // 타입 (2바이트)
                    var typeBytes = BitConverter.GetBytes((short)type);
                    Array.Reverse(typeBytes);
                    writer.Write(typeBytes);

                    // 버전 문자열 (1바이트 길이 + 문자열)
                    writer.Write((byte)versionBytes.Length);
                    writer.Write(versionBytes);

                    // 시퀀스 (4바이트)
                    var sequenceBytes = BitConverter.GetBytes(sequence);
                    Array.Reverse(sequenceBytes);
                    writer.Write(sequenceBytes);

                    // 페이로드 길이 (4바이트)
                    var lengthBytes = BitConverter.GetBytes(payloadBytes.Length);
                    Array.Reverse(lengthBytes);
                    writer.Write(lengthBytes);

                    // 페이로드
                    if (payloadBytes != null && payloadBytes.Length > 0)
                    {
                        writer.Write(payloadBytes);
                    }

                    var result = stream.ToArray();
                    Debug.Log($"Full Packet: {BitConverter.ToString(result)}");
                    return new ArraySegment<byte>(result);
                }
            }
        }
    }
}