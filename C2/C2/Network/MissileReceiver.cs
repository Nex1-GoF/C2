using C2.Models;
using C2.Services;
using System.Net.Sockets;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using C2.Models;
using C2.Services;

namespace C2.Network
{
    public class MissileReceiver
    {
        private readonly MissileService _service;
        private readonly UdpClient _udp;
        private bool _running = true;
        private readonly int _listenPort;
        private readonly int _unrealPort;
        private readonly string _unrealIp;

        // 외부에서 연결할 파서 함수 (byte[] → Missile)
        public Func<byte[], Missile>? ParseMissileData { get; set; }

        public MissileReceiver(int listenPort = 51000, string unrealIp = "192.168.0.50", int unrealPort = 52000)
        {
            _listenPort = listenPort;
            _unrealIp = unrealIp;
            _unrealPort = unrealPort;

            _service = MissileService.Instance;
            _udp = new UdpClient(_listenPort);
        }

        public void Start()
        {
            _running = true;
            Console.WriteLine($"[MissileReceiver] Listening on port {_listenPort}");
            BeginReceiveLoop();
        }

        public void Stop()
        {
            _running = false;
            _udp.Close();
        }

        private async void BeginReceiveLoop()
        {
            while (_running)
            {
                try
                {
                    UdpReceiveResult result = await _udp.ReceiveAsync();
                    
                    if (!_running) break;

                    byte[] data = result.Buffer;

                    Missile ReceiveMissile = Parse(data);
                    // ✅ 외부 파서 연결 (byte[] → Missile)
                    if (ReceiveMissile != null)
                    {       // 업데이트만 수행
                        _service.ReceiveMissileData(ReceiveMissile);
                            // 언리얼 노트북으로 전송
                        SendToUnreal(ReceiveMissile);
                    }
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MissileReceiver] Error: {ex.Message}");
                    await Task.Delay(10);
                }
            }
        }

        private void SendToUnreal(Missile missile)
        {
            try
            {
                using var client = new UdpClient();
                string msg = $"{missile.Id},{missile.Latitude:F6},{missile.Longitude:F6},{missile.Altitude},{missile.Yaw},{missile.Pitch},{missile.Speed},{missile.State}";
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(msg);
                client.Send(bytes, bytes.Length, _unrealIp, _unrealPort);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SendToUnreal] {ex.Message}");
            }
        }

        private Missile Parse(byte[] data)
        {
            Missile missile = null;
            /*
                작성해주세요


            */
            return missile;
        }

    }
}