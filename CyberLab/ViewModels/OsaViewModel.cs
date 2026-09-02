using CommunityToolkit.Mvvm.ComponentModel;

namespace CyberLab.ViewModels
{
    public partial class OsaViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _tabTitle = "OSA Measurement";
    }
}