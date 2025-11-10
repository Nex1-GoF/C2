using System;
using System.Collections.Generic;
using System.Text;

namespace C2.Network
{
    // -------------------------------------------------------------
    // 1️⃣ IPacket 인터페이스 — 모든 패킷의 공통 규약
    // -------------------------------------------------------------
    public interface IPacket
    {
        byte[] Serialize();
        void Print();
    }

    // -------------------------------------------------------------
    // 2️⃣ HeaderPacket — 모든 메시지의 기본 헤더
    // -------------------------------------------------------------
    public class HeaderPacket : IPacket
    {
        public const int HEADER_PACKET_SIZE = 13;

        public string SrcId { get; }
        public string DestId { get; }
        public uint Seq { get; }
        public byte MsgSize { get; }

        public HeaderPacket(string srcId, string destId, uint seq, byte msgSize)
        {
            if (srcId.Length != 4 || destId.Length != 4)
                throw new ArgumentException("Source and Destination IDs must be 4 characters long.");

            SrcId = srcId;
            DestId = destId;
            Seq = seq;
            MsgSize = msgSize;
        }

        public byte[] Serialize()
        {
            var buffer = new List<byte>(HEADER_PACKET_SIZE);
            buffer.AddRange(Encoding.ASCII.GetBytes(SrcId));
            buffer.AddRange(Encoding.ASCII.GetBytes(DestId));
            buffer.AddRange(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((int)Seq)));
            buffer.Add(MsgSize);
            return buffer.ToArray();
        }

        public static HeaderPacket Deserialize(byte[] buffer)
        {
            if (buffer.Length < HEADER_PACKET_SIZE)
                throw new ArgumentException("Buffer too small for HeaderPacket");

            string src = Encoding.ASCII.GetString(buffer, 0, 4);
            string dest = Encoding.ASCII.GetString(buffer, 4, 4);
            uint seq = (uint)System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 8));
            byte size = buffer[12];

            return new HeaderPacket(src, dest, seq, size);
        }

        public void Print()
        {
            Console.WriteLine($"HeaderPacket(src_id={SrcId}, dest_id={DestId}, seq={Seq}, msg_size={MsgSize})");
        }
    }

    // -------------------------------------------------------------
    // 3️⃣ UnifiedMessage — HeaderPacket 포함, seq 값에 따라 본문 구성
    // -------------------------------------------------------------
    public class UnifiedMessage : IPacket
    {
        public HeaderPacket Header { get; }

        public int? PIP_X { get; private set; }
        public int? PIP_Y { get; private set; }
        public int? PIP_Z { get; private set; }
        public byte[]? EncryptionKey { get; private set; }

        public void SetEncryptionKey(byte[] keyBytes)
        {
            EncryptionKey = new byte[32];
            Array.Copy(keyBytes, EncryptionKey, Math.Min(32, keyBytes.Length));
        }


        public UnifiedMessage(HeaderPacket header)
        {
            Header = header;
        }

        public void SetPIP(int x, int y, int z)
        {
            PIP_X = x;
            PIP_Y = y;
            PIP_Z = z;
        }


        public byte[] Serialize()
        {
            var headerBytes = Header.Serialize();
            var buffer = new List<byte>(headerBytes);

            switch (Header.Seq)
            {
                case 4 when EncryptionKey != null:
                    // 세션 키 전달 (II-0012)
                    buffer.AddRange(EncryptionKey);
                    break;

                case 6 when PIP_X.HasValue && PIP_Y.HasValue && PIP_Z.HasValue:
                    // PIP 전송 (II-0011)
                    buffer.AddRange(BitConverter.GetBytes(PIP_X.Value));
                    buffer.AddRange(BitConverter.GetBytes(PIP_Y.Value));
                    buffer.AddRange(BitConverter.GetBytes(PIP_Z.Value));
                    break;

                    // 다른 seq (1,2,3,5,7 등)는 Body 없음
            }

            return buffer.ToArray();
        }


        public static UnifiedMessage Deserialize(byte[] buffer)
        {
            HeaderPacket header = HeaderPacket.Deserialize(buffer);
            var msg = new UnifiedMessage(header);

            int offset = HeaderPacket.HEADER_PACKET_SIZE; // 헤더패킷 끝나는곳의 위치?
            int remain = buffer.Length - offset; // 남아있는 버퍼 길이

            // seq == 6 → PIP
            if (header.Seq == 6 && remain >= 12) // 버퍼 길이 12자리 이상, 시퀀스 6
            {
                msg.PIP_X = System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, offset)); // 다음 버퍼
                msg.PIP_Y = System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, offset + 4)); // 그다음 버퍼
                msg.PIP_Z = System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, offset + 8));
            }
            // seq == 4 → EncryptionKey
            else if (header.Seq == 4 && remain >= 32)
            {
                var keyBytes = new byte[32];
                Array.Copy(buffer, offset, keyBytes, 0, 32);
                msg.EncryptionKey = keyBytes;
            }

            return msg;
        }

        public void Print()
        {
            Header.Print();

            if (Header.Seq == 6)
                Console.WriteLine($"PIP = ({PIP_X}, {PIP_Y}, {PIP_Z})");
            else if (Header.Seq == 4)
                Console.WriteLine($"Key = {EncryptionKey}");
        }
    }

    // -------------------------------------------------------------
    // 4️⃣ ResponseMessage — Header만 존재하는 ACK 패킷
    // -------------------------------------------------------------
    public class ResponseMessage : IPacket
    {
        public HeaderPacket Header { get; }

        public ResponseMessage(HeaderPacket header)
        {
            Header = header;
        }

        public byte[] Serialize() => Header.Serialize();

        public static ResponseMessage Deserialize(byte[] buffer)
        {
            var header = HeaderPacket.Deserialize(buffer);
            return new ResponseMessage(header);
        }

        public void Print() => Header.Print();
    }
}
