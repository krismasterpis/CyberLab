using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CyberLab.ViewModels;
using System.Collections.ObjectModel;
using System.Linq;

namespace CyberLab.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<ObservableObject> _deviceViewModels = new();

        [ObservableProperty]
        private ObservableObject _selectedDevice;

        public MainViewModel()
        {
            DeviceViewModels.Add(new ThermalChamberViewModel());
            DeviceViewModels.Add(new OsaViewModel());

            SelectedDevice = DeviceViewModels.First();
        }

        [RelayCommand]
        private void SwitchToThermalChamber()
        {
            var chamberVm = DeviceViewModels.OfType<ThermalChamberViewModel>().FirstOrDefault();
            if (chamberVm != null)
            {
                SelectedDevice = chamberVm;
            }
        }

        [RelayCommand]
        private void SwitchToOsa()
        {
            var osaVm = DeviceViewModels.OfType<OsaViewModel>().FirstOrDefault();
            if (osaVm != null)
            {
                SelectedDevice = osaVm;
            }
        }
    }
}