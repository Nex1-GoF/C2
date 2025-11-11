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

    // MslInfoPacket
    public class MslInfoPacket : BasePacket
    {
        public int Latitude { get; set; }
        public int Longitude { get; set; }
        public short Altitude { get; set; }
        public short Yaw { get; set; }
        public short Pitch { get; set; }
        public uint FlightTime { get; set; }
        public char FlightStatus { get; set; }
        public char TelemetryStatus { get; set; }

        public MslInfoPacket() { }

        public MslInfoPacket(HeaderPacket header,
                             int latitude, int longitude, short altitude,
                             short yaw, short pitch,
                             uint flightTime, char flightStatus, char telemetryStatus)
        {
            Header = header;
            Latitude = latitude;
            Longitude = longitude;
            Altitude = altitude;
            Yaw = yaw;
            Pitch = pitch;
            FlightTime = flightTime;
            FlightStatus = flightStatus;
            TelemetryStatus = telemetryStatus;
        }

        public override byte[] Serialize()
        {
            var buffer = new List<byte>(Header.Serialize());

            buffer.AddRange(BitConverter.GetBytes(Latitude));
            buffer.AddRange(BitConverter.GetBytes(Longitude));
            buffer.AddRange(BitConverter.GetBytes(Altitude));
            buffer.AddRange(BitConverter.GetBytes(Yaw));
            buffer.AddRange(BitConverter.GetBytes(Pitch));
            buffer.AddRange(BitConverter.GetBytes(FlightTime));
            buffer.Add((byte)FlightStatus);
            buffer.Add((byte)TelemetryStatus);

            return buffer.ToArray();
        }

        public override void Deserialize(byte[] buffer)
        {
            if (buffer.Length < HeaderPacket.HEADER_PACKET_SIZE + 20)
                throw new ArgumentException("Buffer too small for MslInfoPacket");

            var hdrBuffer = new byte[HeaderPacket.HEADER_PACKET_SIZE];
            Array.Copy(buffer, 0, hdrBuffer, 0, HeaderPacket.HEADER_PACKET_SIZE);
            Header = HeaderPacket.Deserialize(hdrBuffer);

            Latitude = BitConverter.ToInt32(buffer, HeaderPacket.HEADER_PACKET_SIZE);
            Longitude = BitConverter.ToInt32(buffer, HeaderPacket.HEADER_PACKET_SIZE + 4);
            Altitude = BitConverter.ToInt16(buffer, HeaderPacket.HEADER_PACKET_SIZE + 8);
            Yaw = BitConverter.ToInt16(buffer, HeaderPacket.HEADER_PACKET_SIZE + 10);
            Pitch = BitConverter.ToInt16(buffer, HeaderPacket.HEADER_PACKET_SIZE + 12);
            FlightTime = BitConverter.ToUInt32(buffer, HeaderPacket.HEADER_PACKET_SIZE + 14);
            FlightStatus = (char)buffer[HeaderPacket.HEADER_PACKET_SIZE + 18];
            TelemetryStatus = (char)buffer[HeaderPacket.HEADER_PACKET_SIZE + 19];
        }

        public override string ToString()
        {
            return $"[MslInfoPacket]\n{Header}\n" +
                   $"Position(lat={Latitude}, lon={Longitude}, alt={Altitude})\n" +
                   $"Attitude(yaw={Yaw}, pitch={Pitch})\n" +
                   $"FlightTime={FlightTime} ms, Status={FlightStatus}, Telemetry={TelemetryStatus}";
        }
    }

    // MslCmdPacket
    public class MslCmdPacket : BasePacket
    {
        public HeaderPacket Header { get; set; }
        public char CommandType { get; set; }

        public MslCmdPacket(HeaderPacket header, char commandType)
        {
            Header = header;
            CommandType = commandType;
        }

        public override byte[] Serialize()
        {
            var buffer = new List<byte>(Header.Serialize());
            buffer.Add((byte)CommandType);
            return buffer.ToArray();
        }

        public override void Deserialize(byte[] buffer)
        {
            if (buffer.Length < HeaderPacket.HEADER_PACKET_SIZE + 1)
                throw new ArgumentException("Buffer too small for MslCmdPacket");

            var hdrBuffer = new byte[HeaderPacket.HEADER_PACKET_SIZE];
            Array.Copy(buffer, 0, hdrBuffer, 0, HeaderPacket.HEADER_PACKET_SIZE);
            Header = HeaderPacket.Deserialize(hdrBuffer);

            CommandType = (char)buffer[HeaderPacket.HEADER_PACKET_SIZE];
        }

        public override string ToString()
        {
            return $"{Header}, MslCmdPacket(commandType={CommandType})";
        }
    }

    // TgtInfoPacket
    public class TgtInfoPacket : BasePacket
    {
        public string Id { get; set; }        // 4 bytes 문자열
        public char Type { get; set; }        // 1 byte
        public int X { get; set; }            // int32_t (4 bytes)
        public int Y { get; set; }            // int32_t (4 bytes)
        public int Z { get; set; }            // int32_t (4 bytes)
        public short Vx { get; set; }         // int16_t (2 bytes)
        public short Vy { get; set; }         // int16_t (2 bytes)
        public short Vz { get; set; }         // int16_t (2 bytes)
        public uint Timestamp { get; set; }   // uint32_t (4 bytes)

        public TgtInfoPacket() { }

        public TgtInfoPacket(HeaderPacket header, string id,
                             int x, int y, int z,
                             short vx, short vy, short vz,
                             char type, uint timestamp)
        {
            Header = header;
            Id = id.Length == 4 ? id : id.PadRight(4).Substring(0, 4);
            X = x;
            Y = y;
            Z = z;
            Vx = vx;
            Vy = vy;
            Vz = vz;
            Type = type;
            Timestamp = timestamp;
        }

        public override byte[] Serialize()
        {
            var buffer = new List<byte>(Header.Serialize());

            var body = new List<byte>();
            body.AddRange(Encoding.ASCII.GetBytes(Id.Substring(0, 4)));
            body.Add((byte)Type);
            body.AddRange(BitConverter.GetBytes(X));
            body.AddRange(BitConverter.GetBytes(Y));
            body.AddRange(BitConverter.GetBytes(Z));
            body.AddRange(BitConverter.GetBytes(Vx));
            body.AddRange(BitConverter.GetBytes(Vy));
            body.AddRange(BitConverter.GetBytes(Vz));
            body.AddRange(BitConverter.GetBytes(Timestamp));

            buffer.AddRange(body);
            return buffer.ToArray();
        }

        public override void Deserialize(byte[] buffer)
        {
            if (buffer.Length < HeaderPacket.HEADER_PACKET_SIZE + 27)
                throw new ArgumentException("Buffer too small for TgtInfoPacket");

            var hdrBuffer = new byte[HeaderPacket.HEADER_PACKET_SIZE];
            Array.Copy(buffer, 0, hdrBuffer, 0, HeaderPacket.HEADER_PACKET_SIZE);
            Header = HeaderPacket.Deserialize(hdrBuffer);

            Id = Encoding.ASCII.GetString(buffer, HeaderPacket.HEADER_PACKET_SIZE, 4);
            Type = (char)buffer[HeaderPacket.HEADER_PACKET_SIZE + 4];
            X = BitConverter.ToInt32(buffer, HeaderPacket.HEADER_PACKET_SIZE + 5);
            Y = BitConverter.ToInt32(buffer, HeaderPacket.HEADER_PACKET_SIZE + 9);
            Z = BitConverter.ToInt32(buffer, HeaderPacket.HEADER_PACKET_SIZE + 13);
            Vx = BitConverter.ToInt16(buffer, HeaderPacket.HEADER_PACKET_SIZE + 17);
            Vy = BitConverter.ToInt16(buffer, HeaderPacket.HEADER_PACKET_SIZE + 19);
            Vz = BitConverter.ToInt16(buffer, HeaderPacket.HEADER_PACKET_SIZE + 21);
            Timestamp = BitConverter.ToUInt32(buffer, HeaderPacket.HEADER_PACKET_SIZE + 23);
        }

        public override string ToString()
        {
            return $"[TgtInfoPacket]\n{Header}\n" +
                   $"Detection(id={Id}, type={Type}, " +
                   $"x={X}, y={Y}, z={Z}, " +
                   $"vx={Vx}, vy={Vy}, vz={Vz}, " +
                   $"timestamp={Timestamp} ms)";
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