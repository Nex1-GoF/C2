using C2.Models;
using C2.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace C2.ViewModels
{
    internal class DatalinkService
    {
        // ✅ 싱글톤 인스턴스
        private static DatalinkService? _instance;
        public static DatalinkService Instance => _instance ??= new DatalinkService();

        private readonly TargetService _targetService;
        private readonly MissileService _missileService;

        // ✅ 네트워크 정보
        private readonly (string ip, int port) _controlCenter; // 표적통제부
        private readonly (string ip, int port) _radar;         // 레이더

        // ✅ 미사일별 연결정보 (미사일 객체 - ip - 포트번호 연결)
        private readonly ConcurrentDictionary<string, (string ip, int port, Missile missile)> _missileLinks;

        // ✅ 통신용 소켓
        private readonly UdpClient _udpClient;
        private readonly CancellationTokenSource _cts = new();

        private DatalinkService()
        {
            _targetService = TargetService.Instance;
            _missileService = MissileService.Instance;

            // 기본 네트워크 설정
            // 레이더나 그런쪽의 포트번호 Ip주소 설정
        }

        // ✅ 미사일 등록 (IP, PORT, Missile Reference)
        public void RegisterMissile(string id, string ip, int port)
        {
            // 미사일 서비스에서 id에 해당하는 미사일 가져와서 연동시킴
        }

       
        // ✅ 송수신 태스크 시작 (송수신 태스크 분리 / 병합은 일단 둘 중 하나로 구현)
        public void Start()
        {
            //송수신 쓰레드 생성
        }


        // ✅ 표적통제부 데이터 수신 처리
        private void ReceiveTargetData(string data)
        {
            // 타겟 객체 변환 및 타겟 서비스 불러와서 업데이트
        }

        // 초기유도 절차 수행
        public void InitialGuidence(string missileId, string msg)
        {
            
        }

        public void Stop()
        {
            _cts.Cancel();
            _udpClient.Close();
            Console.WriteLine("[Datalink] 통신 중지");
        }
    }
}
