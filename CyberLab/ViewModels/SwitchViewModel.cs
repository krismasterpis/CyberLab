using CommunityToolkit.Mvvm.ComponentModel;

namespace CyberLab.ViewModels
{
    public partial class SwitchViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _tabTitle = "Switch";
    }
}

