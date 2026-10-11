using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace CyberLab.ViewModels
{
    public partial class HomeViewModel : ObservableObject
    {
        private readonly MainViewModel? _mainVm;
        private readonly DispatcherTimer _monitorTimer;

        [ObservableProperty]
        private string _tabTitle = "Home";

        // ==================== GLOBAL MISSION CONTROL ====================
        [ObservableProperty]
        private string _globalAppStatus = "Ready";

        [ObservableProperty]
        private bool _isSessionRunning = false;

        [ObservableProperty]
        private TimeSpan _sessionElapsed = TimeSpan.Zero;

        [ObservableProperty]
        private string _sessionElapsedStr = "00:00:00";

        // ==================== CYBERLAB ROUTINE ENGINE ====================
        [ObservableProperty]
        private string _routineName = "Standard Temperature & Spectral Sweep Routine";

        [ObservableProperty]
        private string _routineStatus = "Configured (Ready to start)";

        [ObservableProperty]
        private int _routineCurrentStep = 1;

        [ObservableProperty]
        private int _routineTotalSteps = 6;

        [ObservableProperty]
        private string _routineStepDescription = "Step 1/6: Set chamber temperature to 25.0 °C and await thermal equilibrium";

        [ObservableProperty]
        private double _routineProgressPercent = 16.6;

        // ==================== SYSTEM & PC TELEMETRY ====================
        [ObservableProperty]
        private double _cpuLoadPercent = 0;

        [ObservableProperty]
        private string _cpuLoadStr = "0%";

        [ObservableProperty]
        private double _ramLoadPercent = 0;

        [ObservableProperty]
        private string _ramUsedStr = "0 GB / 0 GB";

        [ObservableProperty]
        private double _batteryPercent = 100;

        [ObservableProperty]
        private string _batteryStatusStr = "Checking...";

        // ==================== INSTRUMENT TEAMS & TELEMETRY ====================
        // Reference Temperature
        [ObservableProperty]
        private bool _isRefTempConnected = false;

        [ObservableProperty]
        private double _refTemperature = 24.85;

        [ObservableProperty]
        private string _refTempDeviceName = "DAQ PT100 Reference";

        // Lasers
        [ObservableProperty]
        private bool _isLasersConnected = true;

        [ObservableProperty]
        private string _lasersDeviceName = "Santec Tunable Laser (172.16.2.131)";

        [ObservableProperty]
        private string _laserEmissionState = "STANDBY";

        [ObservableProperty]
        private double _laserWavelength = 1550.00;

        [ObservableProperty]
        private double _laserPowerDbm = 10.0;

        // Switches
        [ObservableProperty]
        private bool _isSwitchConnected = false;

        [ObservableProperty]
        private string _switchDeviceName = "Polatis / Agiltron Matrix Switch";

        [ObservableProperty]
        private string _switchCurrentRoute = "IN 1 ➔ OUT 1";

        [ObservableProperty]
        private string _switchMode = "Manual";

        // Interrogator
        [ObservableProperty]
        private bool _isInterrogatorConnected = false;

        [ObservableProperty]
        private string _interrogatorDeviceName = "High-Speed FBG Interrogator";

        [ObservableProperty]
        private string _interrogatorState = "Standby";

        [ObservableProperty]
        private int _interrogatorActiveChannels = 4;

        [ObservableProperty]
        private double _interrogatorFrequencyHz = 1000;

        // Power Meter
        [ObservableProperty]
        private bool _isPowerMeterConnected = false;

        [ObservableProperty]
        private string _powerMeterDeviceName = "Optical Power Meter";

        [ObservableProperty]
        private double _powerMeterDbm = -18.42;

        [ObservableProperty]
        private double _powerMeterWavelengthNm = 1550.0;

        [ObservableProperty]
        private string _powerMeterRange = "Auto (-70 to +10 dBm)";

        // Linked ViewModels
        public ThermalChamberViewModel? ThermalChamber =>
            _mainVm?.DeviceViewModels.OfType<ThermalChamberViewModel>().FirstOrDefault();

        public OsaViewModel? Osa =>
            _mainVm?.DeviceViewModels.OfType<OsaViewModel>().FirstOrDefault();

        public HomeViewModel() : this(null) { }

        public HomeViewModel(MainViewModel? mainVm)
        {
            _mainVm = mainVm;

            _monitorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _monitorTimer.Tick += MonitorTimer_Tick;
            _monitorTimer.Start();

            // Initial immediate sample
            UpdateCpuUsage();
            UpdateRamUsage();
            UpdatePowerStatus();
        }

        private void MonitorTimer_Tick(object? sender, EventArgs e)
        {
            if (IsSessionRunning)
            {
                SessionElapsed = SessionElapsed.Add(TimeSpan.FromSeconds(1));
                SessionElapsedStr = SessionElapsed.ToString(@"hh\:mm\:ss");
            }

            UpdateCpuUsage();
            UpdateRamUsage();
            UpdatePowerStatus();
        }

        // ==================== SESSION COMMANDS ====================
        [RelayCommand]
        private void StartSession()
        {
            IsSessionRunning = true;
            GlobalAppStatus = "Session Running";
        }

        [RelayCommand]
        private void PauseSession()
        {
            IsSessionRunning = false;
            GlobalAppStatus = "Session Paused";
        }

        [RelayCommand]
        private void ResetSession()
        {
            IsSessionRunning = false;
            SessionElapsed = TimeSpan.Zero;
            SessionElapsedStr = "00:00:00";
            GlobalAppStatus = "Ready";
        }

        [RelayCommand]
        private void NavigateToTab(string tabTitle)
        {
            if (_mainVm == null) return;
            var target = _mainVm.DeviceViewModels.FirstOrDefault(vm =>
            {
                var prop = vm.GetType().GetProperty("TabTitle");
                return (prop?.GetValue(vm) as string) == tabTitle;
            });
            if (target != null)
            {
                _mainVm.SelectedDevice = target;
            }
        }

        // ==================== WIN32 P/INVOKE SYSTEM METRICS ====================
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_POWER_STATUS
        {
            public byte ACLineStatus;
            public byte BatteryFlag;
            public byte BatteryLifePercent;
            public byte SystemStatusFlag;
            public uint BatteryLifeTime;
            public uint BatteryFullLifeTime;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS status);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetSystemTimes(
            out System.Runtime.InteropServices.ComTypes.FILETIME lpIdleTime,
            out System.Runtime.InteropServices.ComTypes.FILETIME lpKernelTime,
            out System.Runtime.InteropServices.ComTypes.FILETIME lpUserTime);

        private ulong _prevIdleTime;
        private ulong _prevKernelTime;
        private ulong _prevUserTime;

        private static ulong FileTimeToUInt64(System.Runtime.InteropServices.ComTypes.FILETIME ft)
        {
            return ((ulong)ft.dwHighDateTime << 32) | (uint)ft.dwLowDateTime;
        }

        private void UpdateCpuUsage()
        {
            if (GetSystemTimes(out var idleTime, out var kernelTime, out var userTime))
            {
                ulong currentIdle = FileTimeToUInt64(idleTime);
                ulong currentKernel = FileTimeToUInt64(kernelTime);
                ulong currentUser = FileTimeToUInt64(userTime);

                if (_prevKernelTime != 0 && _prevUserTime != 0)
                {
                    ulong idleDiff = currentIdle - _prevIdleTime;
                    ulong kernelDiff = currentKernel - _prevKernelTime;
                    ulong userDiff = currentUser - _prevUserTime;
                    ulong totalSystem = kernelDiff + userDiff;

                    if (totalSystem > 0 && totalSystem >= idleDiff)
                    {
                        double percent = (1.0 - ((double)idleDiff / totalSystem)) * 100.0;
                        CpuLoadPercent = Math.Clamp(Math.Round(percent, 1), 0.0, 100.0);
                        CpuLoadStr = $"{CpuLoadPercent:0.#}%";
                    }
                }

                _prevIdleTime = currentIdle;
                _prevKernelTime = currentKernel;
                _prevUserTime = currentUser;
            }
        }

        private void UpdateRamUsage()
        {
            var memStatus = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
            if (GlobalMemoryStatusEx(ref memStatus))
            {
                RamLoadPercent = memStatus.dwMemoryLoad;
                double totalGb = memStatus.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
                double availGb = memStatus.ullAvailPhys / (1024.0 * 1024.0 * 1024.0);
                double usedGb = Math.Max(0.0, totalGb - availGb);
                RamUsedStr = $"{usedGb:F1} GB / {totalGb:F1} GB ({RamLoadPercent}%)";
            }
        }

        private void UpdatePowerStatus()
        {
            if (GetSystemPowerStatus(out var powerStatus))
            {
                if (powerStatus.BatteryFlag == 128 || powerStatus.BatteryLifePercent == 255)
                {
                    BatteryPercent = 100;
                    BatteryStatusStr = "AC Powered (Desktop)";
                }
                else
                {
                    BatteryPercent = powerStatus.BatteryLifePercent;
                    bool isCharging = (powerStatus.BatteryFlag & 8) != 0;
                    bool isAc = powerStatus.ACLineStatus == 1;

                    if (isCharging)
                        BatteryStatusStr = $"Charging ({BatteryPercent}%)";
                    else if (isAc)
                        BatteryStatusStr = $"AC Connected ({BatteryPercent}%)";
                    else
                        BatteryStatusStr = $"Battery ({BatteryPercent}%)";
                }
            }
            else
            {
                BatteryPercent = 100;
                BatteryStatusStr = "AC Powered";
            }
        }
    }
}

