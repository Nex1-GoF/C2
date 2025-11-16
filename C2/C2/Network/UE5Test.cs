using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace C2.Network
{
    public class UE5Test
    {
        private UdpClient client;
        private Thread _updateThread;
        private Random rand = new Random();

        // 4대 미사일용 raw pitch/yaw 값
        private short[] pitchRaw = new short[4] { 0, 100, -150, 220 };
        private short[] yawRaw = new short[4] { 0, -200, 300, -500 };

        public void Start()
        {
            client = new UdpClient();
            client.Connect("127.0.0.1", 7777);

            _updateThread = new Thread(SendLoop)
            {
                IsBackground = true
            };
            _updateThread.Start();

            Console.WriteLine("UE5 UDP Test Sender Started (100Hz)");
        }

        private void SendLoop()
        {
            const double dt = 0.01; // 100Hz
            Stopwatch sw = Stopwatch.StartNew();
            double accumulated = 0;

            while (true)
            {
                double elapsed = sw.Elapsed.TotalSeconds;
                sw.Restart();

                accumulated += elapsed;

                while (accumulated >= dt)
                {
                    SendRandomData(dt);
                    accumulated -= dt;
                }

                Thread.Sleep(1); // CPU 점유율 ↓
            }
        }

        private void SendRandomData(double dt)
        {
            for (int i = 0; i < 4; i++)
            {
                byte id = (byte)i;

                // Pitch/Yaw 를 조금씩 랜덤하게 변동
                pitchRaw[i] += (short)rand.Next(-30, 40);
                yawRaw[i] += (short)rand.Next(-40, 50);

                byte[] data = new byte[5];
                data[0] = id;

                // Little endian short → bytes
                byte[] p = BitConverter.GetBytes(pitchRaw[i]);
                byte[] y = BitConverter.GetBytes(yawRaw[i]);

                data[1] = p[0];
                data[2] = p[1];
                data[3] = y[0];
                data[4] = y[1];

                // 전송
                client.Send(data, data.Length);
            }
        }
    }
}
