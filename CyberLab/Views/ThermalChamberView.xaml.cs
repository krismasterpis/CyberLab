using CyberLab;
using CyberLab.ViewModels;
using CyberLab.Resources.Services;
using CyberLab.Resources.Libraries;
using CyberLab.Resources.Popups;
using Microsoft.Win32;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.TickGenerators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CyberLab.Views
{
    public enum SequenceMode
    {
        Loop,       // 1 -> 2 -> 3 -> 1 -> 2 -> 3
        Reverse     // 1 -> 2 -> 3 -> 2 -> 1 -> 2
    }

    public partial class ThermalChamberView : UserControl
    {
        private static readonly Regex rgx = new Regex("[0-9]+");
        private TimerEventService _timerService;
        private readonly Stopwatch sw = new Stopwatch();
        private int? _pendingInterval = null;

        private ThermalChamberViewModel TCVM;
        private ThermalChamber TC;

        private readonly List<double> temperatureSetpointsY = new() { 0, 0, 0, 0, 0 };
        private readonly List<double> temperatureSetpointsX = new() { 1, 2, 3, 4, 5 };
        private readonly List<SetPoint> temperatureSetpoints = new();
        private readonly List<double> temperatureX = new();
        private readonly List<double> temperatureY = new();
        private readonly List<DateTime> timestampList = new();
        private readonly List<double> errorX = new();
        private readonly List<double> errorY = new();
        private readonly List<int> elapsedMSList = new();

        private Scatter setpointsScatter;
        private Scatter temperatureScatter;
        private Scatter errorScatter;
        private HorizontalLine setPointLine;
        private HorizontalLine tauLine;

        private readonly SemaphoreSlim _sem1 = new(1, 1);
        private readonly SemaphoreSlim _sem2 = new(1, 1);

        private int currentSetPointIndex = 0;
        private int localSetPoint = -100;
        private int intResult;
        private int direction = 1;

        private string tempType = "Idle";
        private bool measurementFailed = false;
        private bool isCycleStarted = false;

        private readonly string defaultAutosaveFolderPath = "./temperature_autosave";

        public SequenceMode Mode { get; set; }

        public ThermalChamberView()
        {
            InitializeComponent();
            this.DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is ThermalChamberViewModel vm)
            {
                TCVM = vm;

                if (Application.Current is App app)
                    _timerService = app.TimerService;

                InitializePlots();

                TCVM.LocalTimer = new LocalTimer(TCVM.Interval, LocalTimerElapsed);
                TCVM.LocalTimer2 = new LocalTimer(0, LocalTimerElapsed2);
                TCVM.LocalTimer3 = new LocalTimer(300, LocalTimer3Elapsed);
            }
        }

        private void InitializePlots()
        {
            var transparent = ScottPlot.Color.FromColor(System.Drawing.Color.Transparent);
            TCVM.MainPlot.Plot.FigureBackground.Color = transparent;
            TCVM.MainPlot.Plot.Axes.Color(ScottPlot.Color.FromHex("888888"));
            TCVM.MainPlot.Plot.Grid.MajorLineColor = ScottPlot.Color.FromHex("444444");
            TCVM.MainPlot.Plot.Grid.MinorLineColor = ScottPlot.Color.FromHex("222222");
            TCVM.SetPointsPlot.Plot.FigureBackground.Color = transparent;
            TCVM.SetPointsPlot.Plot.Axes.Color(ScottPlot.Color.FromHex("888888"));
            TCVM.SetPointsPlot.Plot.Grid.MajorLineColor = ScottPlot.Color.FromHex("444444");
            TCVM.SetPointsPlot.Plot.Grid.MinorLineColor = ScottPlot.Color.FromHex("222222");

            TCVM.MainPlot.Plot.Axes.Left.Label.Text = "Temperature (°C)";
            TCVM.SetPointsPlot.Plot.Axes.Left.Label.Text = "Temperature (°C)";
            TCVM.MainPlot.Plot.Axes.Left.Label.FontSize = 20;
            TCVM.SetPointsPlot.Plot.Axes.Left.Label.FontSize = 20;
            TCVM.MainPlot.Plot.Axes.Left.TickLabelStyle.FontSize = 16;
            TCVM.SetPointsPlot.Plot.Axes.Left.TickLabelStyle.FontSize = 16;

            TCVM.MainPlot.Plot.Axes.Bottom.Label.Text = "Time (s)";
            TCVM.SetPointsPlot.Plot.Axes.Bottom.Label.Text = "Setpoints (-)";
            TCVM.MainPlot.Plot.Axes.Bottom.Label.FontSize = 20;
            TCVM.SetPointsPlot.Plot.Axes.Bottom.Label.FontSize = 20;
            TCVM.MainPlot.Plot.Axes.Bottom.TickLabelStyle.FontSize = 16;
            TCVM.SetPointsPlot.Plot.Axes.Bottom.TickLabelStyle.FontSize = 16;

            TCVM.SetPointsPlot.UserInputProcessor.UserActionResponses.Clear();
            TCVM.SetPointsPlot.Plot.Axes.Bottom.TickGenerator = new NumericFixedInterval(1);

            setpointsScatter = TCVM.SetPointsPlot.Plot.Add.Scatter(temperatureSetpointsX, temperatureSetpointsY);
            setpointsScatter.LineWidth = 3;
            setpointsScatter.MarkerStyle.Size = 10;
            setpointsScatter.MarkerStyle.FillColor = ScottPlot.Color.FromColor(System.Drawing.Color.DodgerBlue);

            temperatureScatter = TCVM.MainPlot.Plot.Add.Scatter(temperatureX, temperatureY);
            temperatureScatter.MarkerStyle.Size = 10;

            errorScatter = TCVM.MainPlot.Plot.Add.Scatter(errorX, errorY);
            errorScatter.Color = ScottPlot.Color.FromColor(System.Drawing.Color.Red);
            errorScatter.LineWidth = 0;
            errorScatter.MarkerStyle.Size = 15;
            errorScatter.MarkerStyle.Shape = MarkerShape.Cross;

            TCVM.MainPlot.Refresh();
            TCVM.SetPointsPlot.Refresh();
        }

        private bool IsStrictIPv4(string ip) => IPAddress.TryParse(ip, out _);

        private async void ConnectButt_Click(object sender, RoutedEventArgs e)
        {
            if (TC == null || !TC.checkIfConnected())
            {
                string currentIp = TCVM.IpAddress;

                if (IsStrictIPv4(currentIp))
                {
                    TC = new ThermalChamber(currentIp);

                    bool isSuccess = false;
                    double fetchedSetPoint = 0;
                    string fetchedStatus = "Idle";

                    await Task.Run(async () =>
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            try
                            {
                                TC.Connect();
                                TC.SetOperatingMode(Mb1OperatingMode.Manual);

                                fetchedSetPoint = TC.ReadSetPoint();
                                fetchedStatus = Enum.GetName(TC.ReadOperatingMode());

                                isSuccess = TC.IsConnected;
                                break;
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Błąd połączenia (próba {i + 1}): {ex.Message}");
                                await Task.Delay(200);
                            }
                        }
                    });

                    if (isSuccess)
                    {
                        TCVM.CurrSetPoint = fetchedSetPoint;
                        TCVM.ThermalChamberStatus = fetchedStatus;
                        TCVM.IsConnected = true;

                        if (setPointLine == null)
                        {
                            setPointLine = TCVM.MainPlot.Plot.Add.HorizontalLine(TCVM.CurrSetPoint);
                            setPointLine.LineStyle.Color = ScottPlot.Color.FromHex("#ffa500");
                            setPointLine.LineStyle.Pattern = LinePattern.Dashed;
                        }
                        else
                        {
                            setPointLine.Y = TCVM.CurrSetPoint;
                        }

                        TCVM.MainPlot.Refresh();

                        TCVM.LocalTimer.Start();
                    }
                }
            }
        }

        private void ReadTemperature()
        {
            bool status = true;
            while (status)
            {
                if (!TC.checkIfConnected())
                {
                    TC.Connect();
                }

                var temp = TC.ReadTemperature();

                if (temperatureX.Count == 0)
                    temperatureX.Add(0);
                else
                    temperatureX.Add(temperatureX.Last() + TCVM.Interval);

                temperatureY.Add(temp);
                timestampList.Add(DateTime.Now);

                TCVM.CurrTemperature = temp;
                App.isThermalChamberActive = true;
                App.globalTemperatureTC = temp;

                status = false;
            }
        }

        private void DefineSetpointButt_Click(object sender, RoutedEventArgs e)
        {
            SetpointsPopup popup = new SetpointsPopup(temperatureSetpointsX, temperatureSetpointsY, TCVM, temperatureSetpoints);
            popup.ShowDialog();

            TCVM.SetPointsPlot.Plot.Axes.AutoScale();
            TCVM.SetPointsPlot.Refresh();
        }

        private async void LocalTimerElapsed(object? sender, EventArgs e)
        {
            if (TCVM.LocalTimer == null) return;

            if (_pendingInterval.HasValue)
            {
                int newInterval = _pendingInterval.Value;
                _pendingInterval = null;

                Dispatcher.Invoke(() =>
                {
                    TCVM.LocalTimer.Stop();
                    TCVM.LocalTimer = new LocalTimer(newInterval, LocalTimerElapsed);
                    TCVM.LocalTimer.Start();
                });
            }

            await RunMeasurementCycleAsync();
        }

        private async Task RunMeasurementCycleAsync()
        {
            if (!await _sem1.WaitAsync(0))
            {
                Debug.WriteLine("Poprzedni pomiar trwa zbyt długo. Pomijam ten cykl.");
                return;
            }

            try
            {
                await Task.Run(async () =>
                {
                    var localSw = Stopwatch.StartNew();

                    try
                    {
                        ReadTemperature();
                        if (!TC.checkIfConnected()) TC.Connect();
                        measurementFailed = false;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        measurementFailed = true;
                    }

                    int count = 0;
                    while (measurementFailed && count < 3)
                    {
                        try
                        {
                            if (!TC.checkIfConnected()) TC.Connect();
                            else ReadTemperature();

                            measurementFailed = false;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"{ex.Message} Count:{count}");
                            measurementFailed = true;
                        }

                        count++;
                        await Task.Delay(100);
                    }

                    if (measurementFailed)
                    {
                        if (temperatureX.Any())
                        {
                            errorX.Add(temperatureX.Last());
                            errorY.Add(temperatureY.Last());
                        }
                        TCVM.MeasurementFails++;
                        measurementFailed = false;
                    }

                    localSw.Stop();

                    if (temperatureSetpoints.Any() && currentSetPointIndex < temperatureSetpoints.Count)
                    {
                        bool coolingOk = tempType == "Cooling" && TCVM.CurrTemperature <= tauLine?.Y;
                        bool heatingOk = tempType == "Heating" && TCVM.CurrTemperature >= tauLine?.Y;
                        bool rangeOk = Math.Abs(TCVM.CurrTemperature - TCVM.CurrSetPoint) < 0.1;

                        if (coolingOk || heatingOk || rangeOk)
                        {
                            TCVM.LocalTimer2.Start();
                        }
                    }

                    elapsedMSList.Add((int)localSw.ElapsedMilliseconds);
                    TCVM.ElapsedMsAvg = elapsedMSList.Average();
                    TCVM.ElapsedMs = localSw.ElapsedMilliseconds;
                });

                Dispatcher.Invoke(() =>
                {
                    TCVM.MainPlot.Plot.Axes.AutoScale();
                    TCVM.MainPlot.Refresh();
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Krytyczny błąd w pętli pomiarowej: {ex.Message}");
            }
            finally
            {
                _sem1.Release();
            }
        }

        private async Task SetSetpointAsync()
        {
            await _sem2.WaitAsync(0);

            try
            {
                double prevSetPointTemp = TCVM.CurrSetPoint;
                double currSetPointTemp = temperatureSetpoints[currentSetPointIndex].temperature;

                await Task.Run(async () =>
                {
                    if (currentSetPointIndex < temperatureSetpoints.Count)
                    {
                        TCVM.CurrSetPoint = temperatureSetpoints[currentSetPointIndex].temperature;
                        await _sem1.WaitAsync();
                        try
                        {
                            bool status = true;
                            while (status)
                            {
                                try
                                {
                                    TC.SetTemperature(TCVM.CurrSetPoint);
                                    if (setPointLine != null)
                                    {
                                        setPointLine.Y = TCVM.CurrSetPoint;
                                    }
                                    status = false;
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine(ex.Message);
                                    await Task.Delay(100);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                        finally
                        {
                            _sem1.Release();
                        }

                        if (currentSetPointIndex > 0)
                        {
                            prevSetPointTemp = temperatureSetpoints[currentSetPointIndex - 1].temperature;
                            currSetPointTemp = temperatureSetpoints[currentSetPointIndex].temperature;
                        }

                        GetNext();

                        var tau = prevSetPointTemp + (currSetPointTemp - prevSetPointTemp) * 0.63;

                        if (temperatureSetpoints[currentSetPointIndex].timeS > 0)
                        {
                            TCVM.LocalTimer2.Stop();
                            int timeValue = temperatureSetpoints[currentSetPointIndex].timeS == -1 ? 0 : temperatureSetpoints[currentSetPointIndex].timeS;
                            TCVM.LocalTimer2 = new LocalTimer(timeValue, LocalTimerElapsed2);

                            tempType = prevSetPointTemp <= currSetPointTemp ? "Heating" : "Cooling";
                            TCVM.LocalTimer2.Reset();
                        }
                        else if (temperatureSetpoints[currentSetPointIndex].timeConstS_cool > 0 || temperatureSetpoints[currentSetPointIndex].timeConstS_heat > 0)
                        {
                            TCVM.LocalTimer2.Stop();
                            if (prevSetPointTemp <= currSetPointTemp)
                            {
                                TCVM.LocalTimer2 = new LocalTimer(temperatureSetpoints[currentSetPointIndex].timeConstS_heat, LocalTimerElapsed2);
                                tempType = "Heating";
                            }
                            else
                            {
                                TCVM.LocalTimer2 = new LocalTimer(temperatureSetpoints[currentSetPointIndex].timeConstS_cool, LocalTimerElapsed2);
                                tempType = "Cooling";
                            }
                            TCVM.LocalTimer2.Reset();
                        }

                        if (tauLine == null || !TCVM.MainPlot.Plot.PlottableList.Contains(tauLine))
                        {
                            tauLine = TCVM.MainPlot.Plot.Add.HorizontalLine(tau);
                            tauLine.LineStyle.Color = ScottPlot.Color.FromHex("#008000");
                            tauLine.LineStyle.Pattern = LinePattern.Dashed;
                        }
                        else
                        {
                            tauLine.Y = tau;
                        }
                    }
                    else
                    {
                        TCVM.LocalTimer2.Stop();
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Krytyczny błąd: {ex.Message}");
            }
            finally
            {
                _sem2.Release();
            }
        }

        public SetPoint GetNext()
        {
            SetPoint currentSetpoint = temperatureSetpoints[currentSetPointIndex];

            if (temperatureSetpoints.Count == 1) return currentSetpoint;

            if (Mode == SequenceMode.Loop)
            {
                currentSetPointIndex = (currentSetPointIndex + 1) % temperatureSetpoints.Count;
            }
            else if (Mode == SequenceMode.Reverse)
            {
                currentSetPointIndex += direction;

                if (currentSetPointIndex >= temperatureSetpoints.Count - 1)
                {
                    currentSetPointIndex = temperatureSetpoints.Count - 1;
                    direction = -1;
                }
                else if (currentSetPointIndex <= 0)
                {
                    currentSetPointIndex = 0;
                    direction = 1;
                }
            }
            else
            {
                currentSetPointIndex++;
            }

            return currentSetpoint;
        }

        private void LocalTimerElapsed2(object? sender, EventArgs e)
        {
            if (TCVM.LocalTimer2 == null) return;
            _timerService?.RaiseTimerEvent();
            _ = SetSetpointAsync();
        }

        private void IntervalTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox temp && int.TryParse(temp.Text, out intResult))
            {
                TCVM.IsIntervalSetted = intResult >= 1;
            }
            else
            {
                TCVM.IsIntervalSetted = false;
            }
        }

        private async void SetButt_Click(object sender, RoutedEventArgs e)
        {
            if (localSetPoint >= -40 && localSetPoint <= 180)
            {
                await Task.Run(() =>
                {
                    if (!TC.checkIfConnected()) TC.Connect();

                    TC.SetTemperature(localSetPoint);
                    TCVM.CurrSetPoint = localSetPoint;

                    if (setPointLine != null)
                    {
                        setPointLine.Y = TCVM.CurrSetPoint;
                    }
                });
            }
        }

        private void SetPointTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox temp && int.TryParse(temp.Text, out var parsedResult))
            {
                localSetPoint = parsedResult;
            }
        }

        private void StartButt_Click(object sender, RoutedEventArgs e)
        {
            if (isCycleStarted)
            {
                TCVM.LocalTimer2?.Stop();

                if (tauLine != null)
                {
                    TCVM.MainPlot.Plot.Remove(tauLine);
                }

                currentSetPointIndex = 0;
                isCycleStarted = false;
            }
            else
            {
                _ = SetSetpointAsync();
                isCycleStarted = true;
            }
        }

        private void IntervalChangeButt_Click(object sender, RoutedEventArgs e)
        {
            TCVM.Interval = intResult;
            if (TCVM.IsConnected)
            {
                _pendingInterval = intResult;
            }
            else
            {
                TCVM.LocalTimer?.Stop();
                TCVM.LocalTimer = new LocalTimer(intResult, LocalTimerElapsed);
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            temperatureX.Clear();
            temperatureY.Clear();
            errorX.Clear();
            errorY.Clear();
            TCVM.MainPlot.Refresh();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog { Title = "Choose destination folder" };

            var tempX = new List<double>(temperatureX);
            var tempY = new List<double>(temperatureY);
            var timeStamps = new List<DateTime>(timestampList);

            if (dialog.ShowDialog() == true && !string.IsNullOrEmpty(dialog.FolderName))
            {
                var dateTime = DateTime.Now;
                var fileName = $"{dialog.FolderName}/Temperature-{dateTime:dd-MM-yyyy_HH-mm-ss}.csv";

                var csv = new StringBuilder();
                csv.AppendLine("Global DateTime; Time (s); Temperature (°C)");

                for (int i = 0; i < tempX.Count; i++)
                {
                    csv.AppendLine($"{timeStamps[i]:yyyy-MM-dd HH:mm:ss.fff};{tempX[i]};{tempY[i]}");
                }

                await File.WriteAllTextAsync(fileName, csv.ToString());
            }
        }

        private async void LocalTimer3Elapsed(object? sender, EventArgs e)
        {
            if (TCVM.LocalTimer3 != null)
            {
                var tempX = new List<double>(temperatureX);
                var tempY = new List<double>(temperatureY);
                var timeStamps = new List<DateTime>(timestampList);

                await Task.Run(async () =>
                {
                    var dateTime = DateTime.Now;
                    var csv = new StringBuilder();
                    csv.AppendLine("Global DateTime; Time (s); Temperature (°C)");

                    for (int i = 0; i < tempX.Count; i++)
                    {
                        csv.AppendLine($"{timeStamps[i]:yyyy-MM-dd HH:mm:ss.fff};{tempX[i]};{tempY[i]}");
                    }

                    if (!Directory.Exists(defaultAutosaveFolderPath))
                    {
                        Directory.CreateDirectory(defaultAutosaveFolderPath);
                    }

                    var fileName = $"{defaultAutosaveFolderPath}/Temperature-{dateTime:dd-MM-yyyy_HH-mm-ss}.csv";

                    await File.WriteAllTextAsync(fileName, csv.ToString());
                });
            }
        }

        private void AutosaveCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (TCVM.IsAutosaveEnabled)
            {
                TCVM.LocalTimer3?.Start();
            }
            else
            {
                TCVM.LocalTimer3?.Stop();
            }
        }
    }
}