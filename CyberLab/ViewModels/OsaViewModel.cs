using CommunityToolkit.Mvvm.ComponentModel;
using CyberLab.Resources.Libraries;
using ScottPlot.WPF;
using System;
using System.Net;
using System.Windows;

namespace CyberLab.ViewModels
{
    public partial class OsaViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _tabTitle = "OSA Measurement";

        [ObservableProperty]
        private string _ipAddress = "172.16.2.80";

        [ObservableProperty]
        private bool _isValidIp = true;

        [ObservableProperty]
        private bool _isOSAConnected = false;

        [ObservableProperty]
        private string _deviceIdentity = "Not connected";

        [ObservableProperty]
        private string _limitStr = "0";

        [ObservableProperty]
        private int _localMeasNum = 0;

        [ObservableProperty]
        private long _elapsedMs = 0;

        [ObservableProperty]
        private LocalTimer? _localTimer;

        [ObservableProperty]
        private TimeSpan _localEstTime = TimeSpan.Zero;

        [ObservableProperty]
        private Visibility _isLocalTimerEnabled = Visibility.Hidden;

        [ObservableProperty]
        private bool _isRepeatEnabled = false;

        [ObservableProperty]
        private string _chamberTemp = "--";

        public TimeSpan LocalTime => LocalTimer?.Time ?? TimeSpan.Zero;

        public WpfPlot OSAplot { get; } = new WpfPlot();

        public OsaViewModel()
        {
            IsValidIp = IPAddress.TryParse(IpAddress?.Trim(), out _);
        }

        partial void OnIpAddressChanged(string value)
        {
            IsValidIp = IPAddress.TryParse(value?.Trim(), out _);
        }
    }
}