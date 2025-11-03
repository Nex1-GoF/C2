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
            MissileService _missileService = MissileService.Instance;


        }
    }



}
