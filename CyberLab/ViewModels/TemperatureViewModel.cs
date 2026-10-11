using CommunityToolkit.Mvvm.ComponentModel;

namespace CyberLab.ViewModels
{
    public partial class TemperatureViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _tabTitle = "Reference Temp";
    }
}

