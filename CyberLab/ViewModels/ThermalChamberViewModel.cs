using CommunityToolkit.Mvvm.ComponentModel;
using CyberLab.Resources.Libraries;
using ScottPlot.WPF;
using System;
using System.Windows;

namespace CyberLab.ViewModels
{
    public partial class ThermalChamberViewModel : ObservableObject
    {
        public WpfPlot MainPlot { get; } = new WpfPlot();
        public WpfPlot SetPointsPlot { get; } = new WpfPlot();

        public int TauHeating { get; set; }
        public int TauCooling { get; set; }

        public TimeSpan LocalTime => LocalTimer?.Time ?? TimeSpan.Zero;

        [ObservableProperty]
        private string _ipAddress = "172.16.2.217";

        [ObservableProperty]
        private string _targetTemperature;

        [ObservableProperty]
        private string _intervalInput = "1";

        [ObservableProperty]
        private string _tabTitle = "Thermal Chamber";

        [ObservableProperty]
        private LocalTimer _localTimer;

        [ObservableProperty]
        private TimeSpan _localEstTime;

        [ObservableProperty]
        private LocalTimer _localTimer2;

        [ObservableProperty]
        private TimeSpan _localEstTime2;

        [ObservableProperty]
        private LocalTimer _localTimer3;

        [ObservableProperty]
        private TimeSpan _localEstTime3;

        [ObservableProperty]
        private long _elapsedMs;

        [ObservableProperty]
        private double _elapsedMsAvg;

        [ObservableProperty]
        private long _measurementFails;

        [ObservableProperty]
        private Visibility _isLocalTimerEnabled;

        [ObservableProperty]
        private bool _isConnected = false;

        [ObservableProperty]
        private bool _isIntervalSetted = false;

        [ObservableProperty]
        private bool _isSetpointsSetted = false;

        [ObservableProperty]
        private string _thermalChamberStatus = "Idle";

        [ObservableProperty]
        private double _currTemperature;

        [ObservableProperty]
        private double _currSetPoint;

        [ObservableProperty]
        private double _nextSetPoint;

        [ObservableProperty]
        private int _interval = 1;

        [ObservableProperty]
        private bool _loopMode = false;

        [ObservableProperty]
        private bool _reverseMode = false;

        [ObservableProperty]
        private bool _isAutosaveEnabled = false;
    }
}