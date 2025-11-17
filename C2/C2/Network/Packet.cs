using System;
using System.Collections.Generic;
using System.Text;

namespace C2.Network
{
    public class HeaderPacket
    {
        public const int HEADER_PACKET_SIZE = 13;

        public string SrcId { get; set; }     // 4 bytes
        public string DestId { get; set; }    // 4 bytes
        public uint Seq { get; set; }         // 4 bytes
        public byte MsgSize { get; set; }     // 1 byte

        public HeaderPacket(string srcId, string destId, uint seq, byte msgSize)
        {
            SrcId = srcId;
            DestId = destId;
            Seq = seq;
            MsgSize = msgSize;
        }

        public byte[] Serialize()
        {
            byte[] buffer = new byte[HEADER_PACKET_SIZE];
            Array.Copy(Encoding.ASCII.GetBytes(SrcId), 0, buffer, 0, 4);
            Array.Copy(Encoding.ASCII.GetBytes(DestId), 0, buffer, 4, 4);

            buffer[8] = (byte)((Seq >> 24) & 0xFF);
            buffer[9] = (byte)((Seq >> 16) & 0xFF);
            buffer[10] = (byte)((Seq >> 8) & 0xFF);
            buffer[11] = (byte)(Seq & 0xFF);

            buffer[12] = MsgSize;
            return buffer;
        }

        public static HeaderPacket Deserialize(byte[] buffer)
        {
            if (buffer.Length < HEADER_PACKET_SIZE)
                throw new ArgumentException("Buffer too small for HeaderPacket");

            string srcId = Encoding.ASCII.GetString(buffer, 0, 4);
            string destId = Encoding.ASCII.GetString(buffer, 4, 4);

            uint seqVal = (uint)((buffer[8] << 24) |
                                 (buffer[9] << 16) |
                                 (buffer[10] << 8) |
                                 buffer[11]);

            byte size = buffer[12];
            return new HeaderPacket(srcId, destId, seqVal, size);
        }

        public override string ToString()
        {
            return $"HeaderPacket(src_id={SrcId}, dest_id={DestId}, seq={Seq}, msg_size={MsgSize})";
        }
    }

    // 추상 베이스 패킷
    public abstract class BasePacket
    {
        public HeaderPacket Header { get; set; }
        public abstract byte[] Serialize();
        public abstract void Deserialize(byte[] buffer);
    }

    public class MslInfoPacket : BasePacket
    {
        public const int MSL_INFO_PACKET_SIZE = 36;

        public int X { get; set; }
        public int Y { get; set; }
        public short Z { get; set; }

        public int Vx { get; set; }
        public int Vy { get; set; }
        public short Vz { get; set; }

        public int PipX { get; set; }
        public int PipY { get; set; }
        public short PipZ { get; set; }

        public uint FlightTime { get; set; }

        public byte FlightStatus { get; set; }    // 문자열(1글자)
        public byte TelemetryStatus { get; set; }   // 그대로 1바이트

        public MslInfoPacket() { }

        public MslInfoPacket(HeaderPacket header,
                             int x, int y, short z,
                             int vx, int vy, short vz,
                             int pipX, int pipY, short pipZ,
                             uint flightTime, byte flightStatus, byte telemetryStatus)
        {
            Header = header;
            X = x; Y = y; Z = z;
            Vx = vx; Vy = vy; Vz = vz;
            PipX = pipX; PipY = pipY; PipZ = pipZ;
            FlightTime = flightTime;
            FlightStatus = flightStatus;
            TelemetryStatus = telemetryStatus;
        }

        public override byte[] Serialize()
        {
            var buffer = new List<byte>(Header.Serialize());

            buffer.AddRange(BitConverter.GetBytes(X));
            buffer.AddRange(BitConverter.GetBytes(Y));
            buffer.AddRange(BitConverter.GetBytes(Z));

            buffer.AddRange(BitConverter.GetBytes(Vx));
            buffer.AddRange(BitConverter.GetBytes(Vy));
            buffer.AddRange(BitConverter.GetBytes(Vz));

            buffer.AddRange(BitConverter.GetBytes(PipX));
            buffer.AddRange(BitConverter.GetBytes(PipY));
            buffer.AddRange(BitConverter.GetBytes(PipZ));

            buffer.AddRange(BitConverter.GetBytes(FlightTime));
            buffer.Add(FlightStatus);

            // TelemetryStatus: 그대로 1바이트
            buffer.Add(TelemetryStatus);

            return buffer.ToArray();
        }

        public override void Deserialize(byte[] buffer)
        {
            if (buffer.Length < HeaderPacket.HEADER_PACKET_SIZE + MSL_INFO_PACKET_SIZE)
                throw new ArgumentException("Buffer too small for MslInfoPacket");

            var hdrBuffer = new byte[HeaderPacket.HEADER_PACKET_SIZE];
            Array.Copy(buffer, 0, hdrBuffer, 0, HeaderPacket.HEADER_PACKET_SIZE);
            Header = HeaderPacket.Deserialize(hdrBuffer);

            int offset = HeaderPacket.HEADER_PACKET_SIZE;

            X = BitConverter.ToInt32(buffer, offset); offset += 4;
            Y = BitConverter.ToInt32(buffer, offset); offset += 4;
            Z = BitConverter.ToInt16(buffer, offset); offset += 2;

            Vx = BitConverter.ToInt32(buffer, offset); offset += 4;
            Vy = BitConverter.ToInt32(buffer, offset); offset += 4;
            Vz = BitConverter.ToInt16(buffer, offset); offset += 2;

            PipX = BitConverter.ToInt32(buffer, offset); offset += 4;
            PipY = BitConverter.ToInt32(buffer, offset); offset += 4;
            PipZ = BitConverter.ToInt16(buffer, offset); offset += 2;

            FlightTime = BitConverter.ToUInt32(buffer, offset); offset += 4;

            // FlightStatus: 1바이트 → string 변환
            FlightStatus = buffer[offset]; offset += 1;

            // TelemetryStatus: 1바이트 그대로
            TelemetryStatus = buffer[offset];
        }
        public override string ToString()
        {
            return $"[MslInfoPacket]\n{Header}\n" +
                   $"Position(X={X}, Y={Y}, Z={Z})\n" +
                   $"Velocity(Vx={Vx}, Vy={Vy}, Vz={Vz})\n" +
                   $"PIP(X={PipX}, Y={PipY}, Z={PipZ})\n" +
                   $"FlightTime={FlightTime} ms, " +
                   $"Status={FlightStatus}, Telemetry={TelemetryStatus}";
        }
    }

    public class MslCmdPacket : BasePacket
    {
        public override void Deserialize(byte[] buffer)
        {
            if (buffer.Length < HeaderPacket.HEADER_PACKET_SIZE)
                throw new ArgumentException("Buffer too small for MslCmdPacket");

            var hdrBuffer = new byte[HeaderPacket.HEADER_PACKET_SIZE];
            Array.Copy(buffer, 0, hdrBuffer, 0, HeaderPacket.HEADER_PACKET_SIZE);
            Header = HeaderPacket.Deserialize(hdrBuffer);
        }

        public override byte[] Serialize()
        {
            var buffer = new List<byte>(Header.Serialize());
            return buffer.ToArray();
        }
            
        public override string ToString()
        {
            return $"[MslCmdPacket]\n{Header}";
        }
    }

    public class TgtInfoInputPacket : BasePacket
    {
        public const int TGT_INFO_INPUT_PACKET_SIZE = 24;
        public char DetectedId { get; set; }       // 탐지 아이디 (1 byte)
        public Int32 Latitude { get; set; }        // 위도 (4 bytes, ×1e7)
        public Int32 Longtitude { get; set; }      // 경도 (4 bytes, ×1e7)
        public Int16 Altitude { get; set; }        // 고도 (2 bytes)
        public UInt16 Yaw { get; set; }             // 요 (2 bytes)
        public UInt64 DetectedTime { get; set; }   // 탐지 시간 (8 bytes)
        public UInt16 Speed { get; set; }          // 속도 (2 bytes)
        public char DetectedType { get; set; }     // 탐지체 구분 (1 byte)

        public TgtInfoInputPacket() { }

        public TgtInfoInputPacket(HeaderPacket header,
                                  char detectedId, Int32 latitude, Int32 longtitude,
                                  Int16 altitude, UInt16 yaw,
                                  UInt64 detectedTime, UInt16 speed, char detectedType)
        {
            Header = header;
            DetectedId = detectedId;
            Latitude = latitude;
            Longtitude = longtitude;
            Altitude = altitude;
            Yaw = yaw;
            DetectedTime = detectedTime;
            Speed = speed;
            DetectedType = detectedType;
        }

        public override byte[] Serialize()
        {
            var buffer = new List<byte>(Header.Serialize());

            var body = new List<byte>();
            body.Add((byte)DetectedId);
            body.AddRange(BitConverter.GetBytes(Latitude));
            body.AddRange(BitConverter.GetBytes(Longtitude));
            body.AddRange(BitConverter.GetBytes(Altitude));
            body.AddRange(BitConverter.GetBytes(Yaw));
            body.AddRange(BitConverter.GetBytes(DetectedTime));
            body.AddRange(BitConverter.GetBytes(Speed));
            body.Add((byte)DetectedType);

            buffer.AddRange(body);
            return buffer.ToArray();
        }

        public override void Deserialize(byte[] buffer)
        {
           
            if (buffer.Length < HeaderPacket.HEADER_PACKET_SIZE + TGT_INFO_INPUT_PACKET_SIZE)
                throw new ArgumentException("Buffer too small for TgtInfoInputPacket");

            var hdrBuffer = new byte[HeaderPacket.HEADER_PACKET_SIZE];
            Array.Copy(buffer, 0, hdrBuffer, 0, HeaderPacket.HEADER_PACKET_SIZE);
            Header = HeaderPacket.Deserialize(hdrBuffer);

            int offset = HeaderPacket.HEADER_PACKET_SIZE;
            DetectedId = (char)buffer[offset]; offset += 1;
            Latitude = BitConverter.ToInt32(buffer, offset); offset += 4;
            Longtitude = BitConverter.ToInt32(buffer, offset); offset += 4;
            Altitude = BitConverter.ToInt16(buffer, offset); offset += 2;
            Yaw = BitConverter.ToUInt16(buffer, offset); offset += 2;
            DetectedTime = BitConverter.ToUInt64(buffer, offset); offset += 8;
            Speed = BitConverter.ToUInt16(buffer, offset); offset += 2;
            DetectedType = (char)buffer[offset]; offset += 1;
        }

        public override string ToString()
        {
            return $"[TgtInfoInputPacket]\n{Header}\n" +
                   $"Detection(id={DetectedId}, type={DetectedType}, " +
                   $"lat={Latitude}, lon={Longtitude}, alt={Altitude}, yaw={Yaw}, " +
                   $"time={DetectedTime}, speed={Speed})";
        }
    }

    // TgtInfoOutPutPacket
    public class TgtInfoOutputPacket : BasePacket
    {
        public const int TGT_INFO_OUTPUT_PACKET_SIZE = 24;

        public int X { get; set; }            // int32_t (4 bytes)
        public int Y { get; set; }            // int32_t (4 bytes)
        public short Z { get; set; }            // int32_t (4 bytes)
        public int Vx { get; set; }         // int16_t (2 bytes)
        public int Vy { get; set; }         // int16_t (2 bytes)
        public short Vz { get; set; }         // int16_t (2 bytes)
        public uint DetectedMslTime { get; set; }   // uint32_t (4 bytes)

        public TgtInfoOutputPacket() { }

        public TgtInfoOutputPacket(HeaderPacket header,
                                   int x, int y, short z,
                                   int vx, int vy, short vz,
                                   uint detectedMslTime)
        {
            Header = header;
            X = x;
            Y = y;
            Z = z;
            Vx = vx;
            Vy = vy;
            Vz = vz;
            DetectedMslTime = detectedMslTime;
        }

        public override byte[] Serialize()
        {
            var buffer = new List<byte>(Header.Serialize());

            var body = new List<byte>();
            body.AddRange(BitConverter.GetBytes(X));
            body.AddRange(BitConverter.GetBytes(Y));
            body.AddRange(BitConverter.GetBytes(Z));
            body.AddRange(BitConverter.GetBytes(Vx));
            body.AddRange(BitConverter.GetBytes(Vy));
            body.AddRange(BitConverter.GetBytes(Vz));
            body.AddRange(BitConverter.GetBytes(DetectedMslTime));

            buffer.AddRange(body);
            return buffer.ToArray();
        }

        public override void Deserialize(byte[] buffer)
        {
            if (buffer.Length < HeaderPacket.HEADER_PACKET_SIZE + TGT_INFO_OUTPUT_PACKET_SIZE)
                throw new ArgumentException("Buffer too small for TgtInfoOutputPacket");

            var hdrBuffer = new byte[HeaderPacket.HEADER_PACKET_SIZE];
            Array.Copy(buffer, 0, hdrBuffer, 0, HeaderPacket.HEADER_PACKET_SIZE);
            Header = HeaderPacket.Deserialize(hdrBuffer);

            int offset = HeaderPacket.HEADER_PACKET_SIZE;
            X = BitConverter.ToInt32(buffer, offset); offset += 4;
            Y = BitConverter.ToInt32(buffer, offset); offset += 4;
            Z = BitConverter.ToInt16(buffer, offset); offset += 2;
            Vx = BitConverter.ToInt32(buffer, offset); offset += 4;
            Vy = BitConverter.ToInt32(buffer, offset); offset += 4;
            Vz = BitConverter.ToInt16(buffer, offset); offset += 2;
            DetectedMslTime = BitConverter.ToUInt32(buffer, offset); offset += 4;
        }

        public override string ToString()
        {
            return $"[TgtInfoOutputPacket]\n{Header}\n" +
                   $"Position(x={X}, y={Y}, z={Z}, " +
                   $"velocity(vx={Vx}, vy={Vy}, vz={Vz}), " +
                   $"DetectedMslTime={DetectedMslTime} ms)";
        }
    }

    public class TgtFinPacket : BasePacket
    {
        public const int TGT_FIN_PACKET_SIZE = 14;
        public char DetectedId { get; set; }// 탐지 아이디 (1 byte)

        public override void Deserialize(byte[] buffer)
        {
            if (buffer.Length < HeaderPacket.HEADER_PACKET_SIZE + TGT_FIN_PACKET_SIZE)
                throw new ArgumentException("Buffer too small for TgtInfoOutputPacket");

            var hdrBuffer = new byte[HeaderPacket.HEADER_PACKET_SIZE];
            Array.Copy(buffer, 0, hdrBuffer, 0, HeaderPacket.HEADER_PACKET_SIZE);
            Header = HeaderPacket.Deserialize(hdrBuffer);

            DetectedId = (char)buffer[HeaderPacket.HEADER_PACKET_SIZE + 1];
        }

        public override byte[] Serialize()
        {
            var buffer = new List<byte>(Header.Serialize());

            var body = new List<byte>();
            body.AddRange(BitConverter.GetBytes(DetectedId));

            buffer.AddRange(body);
            return buffer.ToArray();
        }

        public override string ToString()
        {
            return $"[TgtFinPacket]\n{Header}\n" +
                   $"Detection(id={DetectedId})";
        }
    }


    // --------------------------------------------------------
      // 발사 절차 시퀀스
    // --------------------------------------------------------

    public class InitialGuidanceMessage : BasePacket
    {
        public InitialGuidanceMessage(HeaderPacket header)
        {
            Header = header;
        }

        public override byte[] Serialize() => Header.Serialize();

        public override void Deserialize(byte[] buffer)
        {
            Header = HeaderPacket.Deserialize(buffer);
        }
    }
    public class KeyExchangeMessage : BasePacket
    {
        public byte[] EncryptionKey { get; private set; }

        public KeyExchangeMessage(HeaderPacket header)
        {
            Header = header;
            EncryptionKey = Array.Empty<byte>();
        }

        public void SetEncryptionKey(byte[] keyBytes)
        {
            if (keyBytes == null || keyBytes.Length == 0)
                throw new ArgumentException("키 생성 실패");

            EncryptionKey = new byte[32];
            Array.Copy(keyBytes, EncryptionKey, Math.Min(32, keyBytes.Length));
        }

        public override byte[] Serialize()
        {
            var headerBytes = Header.Serialize();
            var buffer = new List<byte>(headerBytes);

            if (EncryptionKey != null && EncryptionKey.Length > 0)
                buffer.AddRange(EncryptionKey);

            return buffer.ToArray();
        }

        public override void Deserialize(byte[] buffer)
        {
            Header = HeaderPacket.Deserialize(buffer);

            int offset = HeaderPacket.HEADER_PACKET_SIZE;
            int remain = buffer.Length - offset;

            if (Header.Seq == 4 && remain >= 32)
            {
                EncryptionKey = new byte[32];
                Array.Copy(buffer, offset, EncryptionKey, 0, 32);
            }
            else
            {
                throw new InvalidOperationException("보낼 패킷과 페이로드가 일치하지 않습니다.");
            }
        }
    }

    public class InitialPipMessage : BasePacket
    {
        public int PIP_X { get; private set; }
        public int PIP_Y { get; private set; }
        public int PIP_Z { get; private set; }

        public InitialPipMessage(HeaderPacket header)
        {
            Header = header;
        }

        public void SetPIP(int x, int y, int z)
        {
            PIP_X = x;
            PIP_Y = y;
            PIP_Z = z;
        }

        public override byte[] Serialize()
        {
            var headerBytes = Header.Serialize();
            var buffer = new List<byte>(headerBytes);

            buffer.AddRange(BitConverter.GetBytes(PIP_X));
            buffer.AddRange(BitConverter.GetBytes(PIP_Y));
            buffer.AddRange(BitConverter.GetBytes(PIP_Z));

            return buffer.ToArray();
        }

        public override void Deserialize(byte[] buffer)
        {
            Header = HeaderPacket.Deserialize(buffer);

            int offset = HeaderPacket.HEADER_PACKET_SIZE;
            int remain = buffer.Length - offset;

            if (Header.Seq == 6 && remain >= 12)
            {
                PIP_X = System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, offset));
                PIP_Y = System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, offset + 4));
                PIP_Z = System.Net.IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, offset + 8));
            }
            else
            {
                throw new InvalidOperationException("보낼 패킷과 페이로드가 일치하지 않습니다.");
            }
        }
    }


    public class ResponseMessage : BasePacket
    {
        public ResponseMessage(HeaderPacket header)
        {
            Header = header;
        }

        public override byte[] Serialize() => Header.Serialize();

        public override void Deserialize(byte[] buffer)
        {
            Header = HeaderPacket.Deserialize(buffer);
        }

        public static ResponseMessage FromBytes(byte[] buffer)
        {
            var header = HeaderPacket.Deserialize(buffer);
            return new ResponseMessage(header);
        }

        public void Print() => Console.WriteLine(Header.ToString());
    }
}