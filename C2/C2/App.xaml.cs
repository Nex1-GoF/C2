using C2.Config;
using C2.Network;
using C2.Services;
using System;
using System.Windows;

namespace C2
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // ============================
            // 1. Config 초기화
            // ============================
            try
            {
                var _msl = MissileService.Instance;
                var _tgt = TargetService.Instance;
                var cfg = AppConfig.Network;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[App] Config 초기화 실패: {ex.Message}");
            }
            
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
        }
    }
}
