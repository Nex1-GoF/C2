using C2.Services;
using C2.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;


namespace C2
{

    public partial class App : Application
    {
        public App()
        {
            // DI 컨테이너로 전역 싱글톤 객체들을 관리함
            // 원래는 직접 구현하려고 했지만, 기본적으로 제공하는 기능이었음

            Ioc.Default.ConfigureServices(
                new ServiceCollection() 
                    .AddSingleton<MissileService>() // 서비스 등록
                    .AddTransient<MissilePanelViewModel>() // 뷰모델 등록
                    .BuildServiceProvider()
            );
        }
    }



}
