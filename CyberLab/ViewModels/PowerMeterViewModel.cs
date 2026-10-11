using CommunityToolkit.Mvvm.ComponentModel;

namespace CyberLab.ViewModels
{
    public partial class PowerMeterViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _tabTitle = "Power Meter";
    }
}

