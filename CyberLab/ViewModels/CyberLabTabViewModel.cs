using CommunityToolkit.Mvvm.ComponentModel;

namespace CyberLab.ViewModels
{
    public partial class CyberLabTabViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _tabTitle = "CyberLab";
    }
}

