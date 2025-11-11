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
        public Int16 Yaw { get; set; }             // 요 (2 bytes)
        public UInt64 DetectedTime { get; set; }   // 탐지 시간 (8 bytes)
        public UInt16 Speed { get; set; }          // 속도 (2 bytes)
        public char DetectedType { get; set; }     // 탐지체 구분 (1 byte)

        public TgtInfoInputPacket() { }

        public TgtInfoInputPacket(HeaderPacket header,
                                  char detectedId, Int32 latitude, Int32 longtitude,
                                  Int16 altitude, Int16 yaw,
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
            Yaw = BitConverter.ToInt16(buffer, offset); offset += 2;
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

}
