using C2.Models;
using C2.Services;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace C2.Network
{
    public class TargetReceiver
    {
        private readonly TargetService _service;
        private readonly UdpClient _udp;
        private bool _running = true;
        private readonly int _listenPort;


        public TargetReceiver(int port = 50000)
        {
            _listenPort = port;
            _service = TargetService.Instance;
            _udp = new UdpClient(_listenPort);
        }

        public void Start()
        {
            _running = true;
            Console.WriteLine($"[TargetReceiver] Listening (event-based) on port {_listenPort}");
            BeginReceiveLoop();
        }

        public void Stop()
        {
            _running = false;
            _udp.Close();
        }

        /// <summary>
        /// 비동기 수신 루프 시작
        /// </summary>
        private async void BeginReceiveLoop()
        {
            while (_running)
            {
                try
                {
                    // 데이터가 수신될 때까지 비동기 대기 (polling 아님)
                    UdpReceiveResult result = await _udp.ReceiveAsync();

                    if (!_running)
                        break;

                    byte[] data = result.Buffer;

                    // 변환 함수 호출
                    Target ReceiveTarget = Parse(data);
                    if (ReceiveTarget != null)
                    {
                        _service.ReceiveTargetData(ReceiveTarget);
                        SendToRadar(ReceiveTarget);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // 소켓이 닫힐 때 발생하는 정상 종료 예외
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TargetReceiver] Error: {ex.Message}");
                    await Task.Delay(50);
                }
            }
        }

        /// <summary>
        /// 표적 정보를 레이더로 재전송 (선택)
        /// </summary>
        private void SendToRadar(Target target)
        {
            /*
                작성해주세요


            */
            //참고용
            //try
            //{
            //    using var radarClient = new UdpClient();
            //    string msg = $"ID={target.Id}, Lat={target.CurLoc.Lat:F5}, Lon={target.CurLoc.Lon:F5}, Alt={target.Altitude}";
            //    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(msg);
            //    radarClient.Send(bytes, bytes.Length, "127.0.0.1", 51000);
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"[RadarSend] {ex.Message}");
            //}
        }

        private Target Parse(byte[] data)
        {
            Target target = null;
            /*
                작성해주세요


            */
            return target;
        }

    }
}