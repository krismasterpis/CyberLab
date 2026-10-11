using CommunityToolkit.Mvvm.ComponentModel;

namespace CyberLab.ViewModels
{
    public partial class LasersViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _tabTitle = "Lasers";

        [ObservableProperty]
        private string _laserUrl = "http://172.16.2.131/2150";
    }
}

