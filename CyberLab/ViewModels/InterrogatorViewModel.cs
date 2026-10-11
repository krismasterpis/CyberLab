using CommunityToolkit.Mvvm.ComponentModel;

namespace CyberLab.ViewModels
{
    public partial class InterrogatorViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _tabTitle = "Interrogator";
    }
}

