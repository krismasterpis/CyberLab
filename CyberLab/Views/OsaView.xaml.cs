using CyberLab.Resources.Controls;
using CyberLab.Resources.Libraries;
using CyberLab.Resources.Popups;
using CyberLab.Resources.Services;
using CyberLab.ViewModels;
using Microsoft.Win32;
using ScottPlot;
using ScottPlot.Plottables;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CyberLab.Views
{
    /// <summary>
    /// Logika interakcji dla klasy OsaView.xaml
    /// </summary>
    public partial class OsaView : UserControl
    {
        private static readonly Regex rgx = new Regex("[0-9]+");
        private TimerEventService? _timerService;
        private readonly Stopwatch stopWatch = new Stopwatch();
        private readonly OSA osa = new OSA();
        private OsaViewModel OPVM = null!;

        private readonly Dictionary<string, Scatter> scatters = new();
        private readonly Dictionary<string, List<double>> scattersXs = new();
        private readonly Dictionary<string, List<double>> scattersYs = new();
        private readonly Dictionary<string, TraceType> tracesTypes = new();

        private readonly List<System.Drawing.Color> pallette = new()
        {
            System.Drawing.Color.Yellow,
            System.Drawing.Color.Cyan,
            System.Drawing.Color.Red,
            System.Drawing.Color.Lime,
            System.Drawing.Color.Orange,
            System.Drawing.Color.Fuchsia,
            System.Drawing.Color.DarkGreen,
            System.Drawing.Color.DarkViolet,
            System.Drawing.Color.Brown,
            System.Drawing.Color.DarkBlue
        };

        private Measurement currentMeasure = new();
        private Measurement? prevMeasure;
        private ScottPlot.AxisRules.MaximumBoundary? normalRule;
        private readonly Progress<int> progressIndicator;
        private string csvFolderPath;
        private const string defaultOsaFolderPath = "./csv/OSA";

        private CancellationTokenSource? _cts;
        private Task? _workerTask;
        private bool _isInitialized = false;

        public bool IsBGTaskRunning => _workerTask != null && !_workerTask.IsCompleted;

        public OsaView()
        {
            InitializeComponent();
            csvFolderPath = defaultOsaFolderPath;

            progressIndicator = new Progress<int>(value =>
            {
                MeasurementProgressBar.Value = value;
            });

            this.DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is OsaViewModel vm)
            {
                OPVM = vm;
                InitializeOsaView();
            }
        }

        private void InitializeOsaView()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            OPVM.ChamberTemp = App.isThermalChamberActive ? $"{App.globalTemperatureTC:F1} °C" : "Inactive";

            if (Application.Current is App app)
            {
                _timerService = app.TimerService;
                _timerService.TimerTriggered += OnTimerTriggered;
            }

            var transparent = ScottPlot.Color.FromColor(System.Drawing.Color.Transparent);
            OPVM.OSAplot.Plot.FigureBackground.Color = transparent;
            OPVM.OSAplot.Plot.Axes.Color(ScottPlot.Color.FromHex("888888"));
            OPVM.OSAplot.Plot.Grid.MajorLineColor = ScottPlot.Color.FromHex("444444");
            OPVM.OSAplot.Plot.Grid.MinorLineColor = ScottPlot.Color.FromHex("222222");

            PixelPadding padding = new PixelPadding(75, 35, 75, 35);
            OPVM.OSAplot.Plot.Layout.Fixed(padding);
            OPVM.OSAplot.Plot.Axes.Left.Label.Text = "Optical Power (dBm)";
            OPVM.OSAplot.Plot.Axes.Left.Label.FontSize = 18;
            OPVM.OSAplot.Plot.Axes.Left.TickLabelStyle.FontSize = 14;
            OPVM.OSAplot.Plot.Axes.Bottom.Label.Text = "Wavelength (nm)";
            OPVM.OSAplot.Plot.Axes.Bottom.Label.FontSize = 18;
            OPVM.OSAplot.Plot.Axes.Bottom.TickLabelStyle.FontSize = 14;

            normalRule = new ScottPlot.AxisRules.MaximumBoundary(
                xAxis: OPVM.OSAplot.Plot.Axes.Bottom,
                yAxis: OPVM.OSAplot.Plot.Axes.Left,
                new ScottPlot.AxisLimits(
                    OSA.WAVELENGTH_RANGE_MIN_nm,
                    OSA.WAVELENGTH_RANGE_MAX_nm,
                    OSA.RECEIVER_SENSITIVITY_dBm,
                    OSA.MAX_OPTICAL_INPUT_LEVEL_dBm));

            OPVM.OSAplot.Plot.Axes.Rules.Clear();
            OPVM.OSAplot.Plot.Axes.Rules.Add(normalRule);

            currentMeasure = new Measurement();
            scatters.Clear();
            scattersXs.Clear();
            scattersYs.Clear();
            tracesTypes.Clear();

            for (int i = 0; i < 10; i++)
            {
                var name = $"Trace({(char)('A' + i)})";
                tracesTypes.Add(name, TraceType.Blank);
                scattersXs.Add(name, new List<double>());
                scattersYs.Add(name, new List<double>());

                var scatter = OPVM.OSAplot.Plot.Add.Scatter(scattersXs[name], scattersYs[name]);
                scatter.Color = ScottPlot.Color.FromColor(pallette[i]);
                scatter.LegendText = name;
                scatter.IsVisible = false;
                scatters.Add(name, scatter);
            }

            OPVM.OSAplot.Refresh();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!osa.IsConnected)
            {
                ConnectButton.IsEnabled = false;
                string ipTarget = OPVM.IpAddress.Trim();

                await Task.Run(() =>
                {
                    try
                    {
                        osa.Connect($"TCPIP0::{ipTarget}::inst0::INSTR");
                        if (osa.IsConnected)
                        {
                            string queryResult = osa.Query(AnritsuMs9740AScpiCommands.IDENTIFICATION_QUERY);
                            string[] parts = queryResult.Split(',');
                            osa.identity = parts.Length >= 2 ? $"{parts[0]} {parts[1]}" : queryResult;
                        }
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(ex.Message, "Błąd połączenia", MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                    }
                });

                ConnectButton.IsEnabled = true;

                if (osa.IsConnected)
                {
                    ConnectButton.Content = "Disconnect";
                    OPVM.DeviceIdentity = osa.identity;
                    OPVM.IsOSAConnected = true;

                    var limit = AnritsuMs9740ATimeConsts.VBWcoeff.GetValueOrDefault((int)osa.VBW_SETTING, 0.5) *
                                AnritsuMs9740ATimeConsts.SamplingPointsCoeff.GetValueOrDefault(osa.SAMPLING_POINTS_COUNT, 1.0);
                    OPVM.LimitStr = Math.Round(limit, 2).ToString(CultureInfo.InvariantCulture);
                }
            }
            else
            {
                osa.Disconnect();
                if (!osa.IsConnected)
                {
                    osa.identity = "Not connected";
                    ConnectButton.Content = "Connect";
                    OPVM.DeviceIdentity = osa.identity;
                    OPVM.IsOSAConnected = false;
                }
            }
        }

        private void ipAddressTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string ip = ipAddressTextBox.Text;
            bool isValid = IsStrictIPv4(ip);
            ipAddressTextBox.BorderBrush = isValid ? Brushes.Green : Brushes.Red;
            ConnectButton.IsEnabled = isValid;
            if (OPVM != null) OPVM.IsValidIp = isValid;
        }

        public bool IsStrictIPv4(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
                return false;

            string[] parts = ip.Trim().Split('.');
            if (parts.Length != 4)
                return false;

            foreach (var part in parts)
            {
                if (!int.TryParse(part, out int byteVal) || byteVal < 0 || byteVal > 255)
                    return false;
            }

            return true;
        }

        private void UpdateScatterData()
        {
            foreach (var trace in currentMeasure.traces.Values)
            {
                if (scattersXs.TryGetValue(trace.name, out var xs) && scattersYs.TryGetValue(trace.name, out var ys))
                {
                    xs.Clear();
                    xs.AddRange(trace.wavelengths);
                    ys.Clear();
                    ys.AddRange(trace.attenuationsRaw);
                }
            }

            Dispatcher.Invoke(() =>
            {
                OPVM.OSAplot.Refresh();
                OPVM.OSAplot.Plot.Axes.AutoScale();
                UpdateElapsedTimeColor();
            });
        }

        private void UpdateElapsedTimeColor()
        {
            if (OPVM.LocalTimer != null && OPVM.LocalTimer.Interval > 0)
            {
                ElapsedTimeTextBlock.Foreground = (OPVM.ElapsedMs < OPVM.LocalTimer.Interval * 1000)
                    ? Brushes.LightGreen
                    : Brushes.OrangeRed;
            }
            else
            {
                ElapsedTimeTextBlock.Foreground = (Brush)FindResource("LabelBlueBrush");
            }
        }

        private async void SingleButton_Click(object sender, RoutedEventArgs e)
        {
            stopWatch.Restart();
            prevMeasure = new Measurement(currentMeasure);

            await Task.Run(() =>
            {
                osa.ReadSingle(currentMeasure, tracesTypes, progressIndicator);
                currentMeasure.Time = DateTime.Now;
                if (App.isThermalChamberActive)
                {
                    currentMeasure.Temperature = App.globalTemperatureTC;
                }
            });

            stopWatch.Stop();
            OPVM.ElapsedMs = stopWatch.ElapsedMilliseconds;
            OPVM.ChamberTemp = App.isThermalChamberActive ? $"{App.globalTemperatureTC:F1} °C" : "Inactive";
            UpdateScatterData();
        }

        private void OsaTraceControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not OsaTraceControl temp) return;

            char traceChar = temp.Name.Last();
            string traceKey = $"Trace({traceChar})";
            TraceType type = temp.SelectedComboBoxItemString switch
            {
                "Write" => TraceType.Write,
                "Fix" => TraceType.Fix,
                "Calculate" => TraceType.Calculate,
                _ => TraceType.Blank
            };

            osa.SendTraceType(traceChar, type);
            if (scatters.TryGetValue(traceKey, out var scatter))
            {
                scatter.IsVisible = type != TraceType.Blank;
            }

            if (currentMeasure.traces.TryGetValue(traceKey, out var currentTrace))
            {
                currentTrace.type = type;
            }
            tracesTypes[traceKey] = type;

            if (type == TraceType.Calculate)
            {
                OsaCalcPopup popup = new OsaCalcPopup
                {
                    Owner = Window.GetWindow(this)
                };
                bool? result = popup.ShowDialog();
                if (result == true && popup.str.Length >= 3 && currentTrace != null)
                {
                    currentTrace.calculateTrace1 = popup.str[0];
                    currentTrace.calculateSign = popup.str[1];
                    currentTrace.calculateTrace2 = popup.str[2];
                    temp.Text = "Trace " + popup.str;
                }
            }
            else
            {
                temp.Text = $"Trace {traceChar}";
            }

            if (tracesTypes.Values.Contains(TraceType.Calculate))
            {
                OPVM.OSAplot.Plot.Axes.Rules.Clear();
                OPVM.OSAplot.Refresh();
            }
            else if (normalRule != null)
            {
                OPVM.OSAplot.Plot.Axes.Rules.Clear();
                OPVM.OSAplot.Plot.Axes.Rules.Add(normalRule);
                OPVM.OSAplot.Refresh();
                OPVM.OSAplot.Plot.Axes.AutoScale();
            }
        }

        private async void OnTimerTriggered()
        {
            stopWatch.Restart();
            prevMeasure = new Measurement(currentMeasure);

            await Task.Run(() =>
            {
                osa.ReadSingle(currentMeasure, tracesTypes, progressIndicator);
                currentMeasure.Time = DateTime.Now;
                if (App.isThermalChamberActive)
                {
                    currentMeasure.Temperature = App.globalTemperatureTC;
                }
            });

            stopWatch.Stop();
            OPVM.ElapsedMs = stopWatch.ElapsedMilliseconds;
            OPVM.ChamberTemp = App.isThermalChamberActive ? $"{App.globalTemperatureTC:F1} °C" : "Inactive";
            UpdateScatterData();

            SaveToCSV();
        }

        private async void LocalTimerElapsed(object? sender, EventArgs e)
        {
            if (OPVM.LocalTimer == null) return;

            stopWatch.Restart();
            prevMeasure = new Measurement(currentMeasure);

            await Task.Run(() =>
            {
                currentMeasure.Time = DateTime.Now;
                osa.ReadSingle(currentMeasure, tracesTypes, progressIndicator);
                if (App.isThermalChamberActive)
                {
                    currentMeasure.Temperature = App.globalTemperatureTC;
                }
                currentMeasure.Time = DateTime.Now;
            });

            stopWatch.Stop();
            OPVM.ElapsedMs = stopWatch.ElapsedMilliseconds;
            OPVM.ChamberTemp = App.isThermalChamberActive ? $"{App.globalTemperatureTC:F1} °C" : "Inactive";
            UpdateScatterData();

            if (CyberLabEnabledCheckBox.IsChecked == true)
            {
                SaveToCSV();
            }
            OPVM.LocalMeasNum += 1;
        }

        private void IntervalTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !rgx.IsMatch(e.Text);
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveToCSV();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();

            if (OPVM.LocalTimer != null)
            {
                OPVM.LocalTimer.Stop();
                IntervalTextBox.IsEnabled = true;
            }
        }

        private void settingsButton_Click(object sender, RoutedEventArgs e)
        {
            OsaSettingsPopup popup = new OsaSettingsPopup(osa)
            {
                Owner = Window.GetWindow(this)
            };
            popup.ShowDialog();
        }

        private void SaveToCSV()
        {
            try
            {
                string targetFolder = (!string.IsNullOrWhiteSpace(csvFolderPath) && Directory.Exists(csvFolderPath))
                    ? csvFolderPath
                    : defaultOsaFolderPath;

                Directory.CreateDirectory(targetFolder);

                var csv = new StringBuilder();
                csv.AppendLine($"Thermal Chamber Temperature; {currentMeasure.Temperature}");

                var headers = new List<string> { "Wavelength (nm)" };
                foreach (var trace in currentMeasure.traces.Values)
                {
                    if (trace.type != TraceType.Blank)
                    {
                        headers.Add(trace.name);
                    }
                }
                csv.AppendLine(string.Join(';', headers));

                int count = 0;
                List<double> wavelengthsTemp = new List<double>();
                foreach (var trace in currentMeasure.traces.Values)
                {
                    if (trace.wavelengths.Count > count)
                    {
                        count = trace.wavelengths.Count;
                        wavelengthsTemp = new List<double>(trace.wavelengths);
                    }
                }

                var row = new List<string>();
                for (int j = 0; j < count; j++)
                {
                    row.Clear();
                    row.Add(wavelengthsTemp[j].ToString(CultureInfo.InvariantCulture));
                    foreach (var trace in currentMeasure.traces.Values)
                    {
                        if (trace.type != TraceType.Blank)
                        {
                            string val = (j < trace.attenuationsRaw.Count)
                                ? trace.attenuationsRaw[j].ToString(CultureInfo.InvariantCulture)
                                : string.Empty;
                            row.Add(val);
                        }
                    }
                    csv.AppendLine(string.Join(';', row));
                }

                string fileName = Path.Combine(targetFolder, $"OSA-{currentMeasure.Time:yyyy-MM-dd_HH-mm-ss}.csv");
                File.WriteAllText(fileName, csv.ToString());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd podczas zapisu do CSV: {ex.Message}");
            }
        }

        private void saveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new OpenFolderDialog
            {
                Title = "Select Destination Folder",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (folderDialog.ShowDialog() == true)
            {
                csvFolderPath = folderDialog.FolderName;
                csvFolderPathTextBlock.Text = csvFolderPath;
            }
        }

        private void LoopButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IntervalTextBox.IsEnabled)
            {
                OPVM.LocalTimer?.Stop();
                IntervalTextBox.IsEnabled = true;
            }
            else
            {
                if (long.TryParse(IntervalTextBox.Text, out var interv) && interv > 0)
                {
                    var limit = AnritsuMs9740ATimeConsts.VBWcoeff.GetValueOrDefault((int)osa.VBW_SETTING, 0.5) *
                                AnritsuMs9740ATimeConsts.SamplingPointsCoeff.GetValueOrDefault(osa.SAMPLING_POINTS_COUNT, 1.0);

                    if (interv > limit)
                    {
                        OPVM.LocalTimer = new LocalTimer(interv, LocalTimerElapsed);
                        OPVM.LocalTimer.Reset();
                        OPVM.LocalTimer.Start();
                        IntervalTextBox.IsEnabled = false;
                    }
                    else
                    {
                        MessageBox.Show($"Wymagany minimalny interwał wynosi > {limit:F1} s dla obecnych ustawień VBW i punktów próbkowania.",
                                        "Nieprawidłowy interwał", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    IntervalTextBox.Text = "5";
                }
            }
        }

        private void LiveButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsBGTaskRunning) return;

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _workerTask = Task.Run(async () =>
            {
                await BackgroundLoop(token);
            }, token);
        }

        private async Task BackgroundLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    stopWatch.Restart();
                    prevMeasure = new Measurement(currentMeasure);

                    osa.ReadSingle(currentMeasure, tracesTypes, progressIndicator);
                    currentMeasure.Time = DateTime.Now;
                    if (App.isThermalChamberActive)
                    {
                        currentMeasure.Temperature = App.globalTemperatureTC;
                    }

                    stopWatch.Stop();
                    OPVM.ElapsedMs = stopWatch.ElapsedMilliseconds;
                    OPVM.ChamberTemp = App.isThermalChamberActive ? $"{App.globalTemperatureTC:F1} °C" : "Inactive";

                    UpdateScatterData();

                    await Task.Delay(1000, token);
                }
            }
            catch (TaskCanceledException)
            {
                // Normal cancellation
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd w pętli tła OSA: {ex.Message}");
            }
        }
    }
}

