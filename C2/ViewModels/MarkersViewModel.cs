using CommunityToolkit.Mvvm.ComponentModel;
namespace C2.ViewModels
{
    public partial class MissileMarkerViewModel : ObservableObject
    {
        [ObservableProperty] private string id = "MSL-001";
        [ObservableProperty] private string targetId = "TARGET-001";
        [ObservableProperty] private double altitude = 0;
        [ObservableProperty] private double yaw = 90;
        [ObservableProperty] private double latitude;
        [ObservableProperty] private double longitude;
    }
    public partial class TargetMarkerViewModel : ObservableObject
    {
        [ObservableProperty] private string targetId;
        [ObservableProperty] private double altitude;
        [ObservableProperty] private double yaw;
        [ObservableProperty] private double latitude;
        [ObservableProperty] private double longitude;
    }
    public partial class PIPMarkerViewModel : ObservableObject
    {
        [ObservableProperty] private double x;
        [ObservableProperty] private double y;
    }
}
