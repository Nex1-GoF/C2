using C2.Models;

namespace C2.Network
{
    internal interface IInitialGuidanceManager
    {
        Task StartAsync(Missile missile);   // 절차 시작
        void Abort();                       // 절차 중단
    }
}