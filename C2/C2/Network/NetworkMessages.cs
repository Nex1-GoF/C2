using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C2.Network
{

    public abstract class MessageHeader
    {
        public byte SenderType { get; set; }          // 송신자 구분
        public string SenderId { get; set; } = "C001";// 4 bytes
        public byte ReceiverType { get; set; }        // 수신자 구분
        public string ReceiverId { get; set; } = "M001";// 4 bytes
        public uint SequenceNumber { get; set; }      // 4 bytes
        public byte BodySize { get; set; }            // 1 byte
        public byte ProcedureStep { get; set; }       // 1 byte

        protected List<byte> SerializeHeader() => new List<byte>(0);
        public abstract byte[] ToBytes();


    }
    public class Msg_II0009 : MessageHeader
    {
        public string TargetId { get; set; } = "E001"; // 4 Byte ASCII

        public override byte[] ToBytes() => new byte[0];
        public static Msg_II0009 FromBytes(byte[] data) => new Msg_II0009();
    }

    public class Msg_II0010 : MessageHeader
    {
        public override byte[] ToBytes() => new byte[0];
        public static Msg_II0010 FromBytes(byte[] data) => new Msg_II0010();

    }

    public class Msg_II0011 : MessageHeader
    {
        public int Latitude { get; set; }   // 4 Byte ×1e7
        public int Longitude { get; set; }  // 4 Byte ×1e7
        public short Altitude { get; set; } // 2 Byte

        public override byte[] ToBytes() => new byte[0];

        public static Msg_II0011 FromBytes(byte[] data) => new Msg_II0011();
    }

    public class Msg_II0012 : MessageHeader
    {
        public string EncryptionKey { get; set; } = string.Empty; // 32 Bytes ASCII

        public override byte[] ToBytes() => new byte[0];

        public static Msg_II0012 FromBytes(byte[] data) => new Msg_II0012();
    }

    public class Msg_II0013 : MessageHeader
    {
        public int PIP_X { get; set; } // 전방 (m)
        public int PIP_Y { get; set; } // 좌방 (m)
        public int PIP_Z { get; set; } // 천방 (m)

        public override byte[] ToBytes() => new byte[0];

        public static Msg_II0013 FromBytes(byte[] data) => new Msg_II0013();
    }
}
