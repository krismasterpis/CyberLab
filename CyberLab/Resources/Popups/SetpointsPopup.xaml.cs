using CyberLab.Resources.Libraries;
using CyberLab.ViewModels;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.TickGenerators;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CyberLab.Resources.Popups
{
    public partial class SetpointsPopup : Window
    {
        private static readonly Regex rgx = new Regex("[0-9]+");
        private Scatter MyScatterPlot;

        private List<double> MyPointsX;
        private List<double> MyPointsY;
        private List<int> MyTimes = new List<int>();
        private int tau_cool;
        private int tau_heat;
        private List<Text> MyLabels;
        private List<SetPoint> setPointsLocal;

        private int? draggedPointIndex = null;
        private Marker highlightedMarker;
        private ThermalChamberViewModel TCVM;

        public SetpointsPopup(List<double> inputX_, List<double> inputY_, ThermalChamberViewModel _TCVM, List<SetPoint> setpoints)
        {
            InitializeComponent();
            MyPointsX = inputX_;
            MyPointsY = inputY_;
            TCVM = _TCVM;
            setPointsLocal = setpoints;
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (TCVM.TauCooling != 0 || TCVM.TauHeating != 0)
            {
                TimeConstantsCheckedBox.IsChecked = true;
                UserDefinedCheckedBox.IsChecked = false;
                tau_heat = TCVM.TauHeating;
                tau_cool = TCVM.TauCooling;
                HeatingTextBox.Text = tau_heat.ToString();
                CoolingTextBox.Text = tau_cool.ToString();
            }
            else
            {
                foreach (var setpoint in setPointsLocal)
                {
                    MyTimes.Add(setpoint.timeS);
                }
                if (!MyTimes.Contains(-1))
                {
                    TimeConstantsCheckedBox.IsChecked = false;
                    UserDefinedCheckedBox.IsChecked = true;
                }
                else
                {
                    TimeConstantsCheckedBox.IsChecked = false;
                    UserDefinedCheckedBox.IsChecked = false;
                }
            }
            InitializePlot();
        }

        private void InitializePlot()
        {
            MyLabels = new List<Text>();
            MyScatterPlot = WpfPlot1.Plot.Add.Scatter(MyPointsX, MyPointsY);
            WpfPlot1.Plot.Axes.Bottom.TickGenerator = new NumericFixedInterval(1);
            WpfPlot1.Plot.FigureBackground.Color = ScottPlot.Color.FromColor(System.Drawing.Color.Transparent);
            WpfPlot1.Plot.Axes.Color(ScottPlot.Color.FromHex("888888"));
            WpfPlot1.Plot.Grid.MajorLineColor = ScottPlot.Color.FromHex("444444");
            WpfPlot1.Plot.Grid.MinorLineColor = ScottPlot.Color.FromHex("222222");

            PixelPadding padding = new PixelPadding(75, 35, 75, 35);
            WpfPlot1.Plot.Layout.Fixed(padding);
            WpfPlot1.Plot.XLabel("Checkpoints");
            WpfPlot1.Plot.YLabel("Temperature (°C)");
            WpfPlot1.Plot.Axes.Left.Label.FontSize = 20;
            WpfPlot1.Plot.Axes.Left.TickLabelStyle.FontSize = 16;
            WpfPlot1.Plot.Axes.Bottom.Label.FontSize = 20;
            WpfPlot1.Plot.Axes.Bottom.TickLabelStyle.FontSize = 16;
            WpfPlot1.Plot.Axes.Left.Min = -50;
            WpfPlot1.Plot.Axes.Left.Max = 200;
            WpfPlot1.Plot.Axes.Bottom.Min = 0;
            WpfPlot1.Plot.Axes.Bottom.Max = MyPointsX.Count() + 1;

            MyScatterPlot.LineStyle.Width = 3;
            MyScatterPlot.MarkerStyle.Size = 10;
            MyScatterPlot.MarkerStyle.FillColor = ScottPlot.Color.FromColor(System.Drawing.Color.DodgerBlue);

            for (int i = 0; i < MyPointsX.Count(); i++)
            {
                var label = CreateLabelForPoint(MyPointsX[i], MyPointsY[i], 0);
                MyLabels.Add(label);
                if (MyTimes.Count() != MyPointsX.Count())
                {
                    MyTimes.Add(-1);
                }
                UpdateLabel(i, MyTimes[i]);
                WpfPlot1.Plot.Add.Plottable(label);
            }

            highlightedMarker = WpfPlot1.Plot.Add.Marker(0, 0);
            highlightedMarker.IsVisible = false;
            highlightedMarker.Size = 15;
            highlightedMarker.Color = ScottPlot.Color.FromColor(System.Drawing.Color.Red);
            highlightedMarker.Shape = ScottPlot.MarkerShape.OpenCircle;
            highlightedMarker.LineWidth = 2;

            WpfPlot1.UserInputProcessor.UserActionResponses.Clear();
            WpfPlot1.Refresh();
        }

        private void WpfPlot1_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            Pixel mousePixel = new(e.GetPosition(WpfPlot1).X, e.GetPosition(WpfPlot1).Y);
            Coordinates mouseCoordinates = WpfPlot1.Plot.GetCoordinates(mousePixel);
            DataPoint nearestPoint = MyScatterPlot.Data.GetNearest(mouseCoordinates, WpfPlot1.Plot.LastRender, 10);

            if (nearestPoint.IsReal)
            {
                highlightedMarker.Location = nearestPoint.Coordinates;
                highlightedMarker.IsVisible = true;

                if (e.RightButton == MouseButtonState.Pressed)
                {
                    var (intVal, doubleVal) = TwoNumbersInputDialog.Show(
                        "Parse values",
                        "UserTime (s):",
                        "SetPoint Temperature (°C):",
                        MyTimes[nearestPoint.Index],
                        MyPointsY[nearestPoint.Index]
                    );

                    if (intVal != null && doubleVal != null)
                    {
                        MyPointsY[nearestPoint.Index] = (double)doubleVal;
                        if (intVal <= 0)
                        {
                            MyTimes[nearestPoint.Index] = -1;
                        }
                        else
                        {
                            MyTimes[nearestPoint.Index] = (int)intVal;
                        }
                        UpdateLabel(nearestPoint.Index, MyTimes[nearestPoint.Index]);
                    }
                    highlightedMarker.IsVisible = false;
                }
                else
                {
                    draggedPointIndex = nearestPoint.Index;
                }
                WpfPlot1.Refresh();
            }
        }

        private void WpfPlot1_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (draggedPointIndex.HasValue)
            {
                Pixel mousePixel = new(e.GetPosition(WpfPlot1).X, e.GetPosition(WpfPlot1).Y);
                Coordinates mouseCoordinates = WpfPlot1.Plot.GetCoordinates(mousePixel);
                int index = draggedPointIndex.Value;

                if (mouseCoordinates.Y >= -40 && mouseCoordinates.Y <= 180)
                {
                    MyPointsY[index] = Math.Round(mouseCoordinates.Y, 1);
                    double originalX = MyPointsX[index];
                    highlightedMarker.Location = new Coordinates(originalX, mouseCoordinates.Y);
                    UpdateLabel(index, MyTimes[index]);
                }
                else
                {
                    if (mouseCoordinates.Y < -40)
                    {
                        MyPointsY[index] = -40;
                        double originalX = MyPointsX[index];
                        highlightedMarker.Location = new Coordinates(originalX, -40);
                        UpdateLabel(index, MyTimes[index]);
                    }
                    else
                    {
                        MyPointsY[index] = 180;
                        double originalX = MyPointsX[index];
                        highlightedMarker.Location = new Coordinates(originalX, 180);
                        UpdateLabel(index, MyTimes[index]);
                    }
                }
                WpfPlot1.Refresh();
            }
        }

        private void WpfPlot1_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            draggedPointIndex = null;
            highlightedMarker.IsVisible = false;
            WpfPlot1.Refresh();
        }

        private Text CreateLabelForPoint(double x, double y, int time)
        {
            return new Text()
            {
                LabelText = $"{Math.Round(y, 1)} ({time} s)",
                Location = new Coordinates(x, y),
                LabelFontSize = 12,
                OffsetX = 5,
                OffsetY = -5,
                Alignment = Alignment.LowerLeft,
                Color = ScottPlot.Color.FromColor(System.Drawing.Color.DodgerBlue),
            };
        }

        private void UpdateLabel(int index, int time)
        {
            double newY = MyPointsY[index];
            double originalX = MyPointsX[index];
            var labelToUpdate = MyLabels[index];

            if (time != -1) labelToUpdate.LabelText = $"{Math.Round(newY, 1)} ({time} s)";
            else labelToUpdate.LabelText = $"{Math.Round(newY, 1)} (0 s)";

            labelToUpdate.Location = new Coordinates(originalX, newY);
        }

        private void WpfPlot1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Pixel mousePixel = new(e.GetPosition(WpfPlot1).X, e.GetPosition(WpfPlot1).Y);
                Coordinates mouseCoordinates = WpfPlot1.Plot.GetCoordinates(mousePixel);
                DataPoint nearestPoint = MyScatterPlot.Data.GetNearest(mouseCoordinates, WpfPlot1.Plot.LastRender, 10);

                if (nearestPoint.IsReal)
                {
                    MyPointsY[nearestPoint.Index] = 0;
                    double originalX = MyPointsX[nearestPoint.Index];
                    highlightedMarker.Location = new Coordinates(originalX, mouseCoordinates.Y);
                    UpdateLabel(nearestPoint.Index, MyTimes[nearestPoint.Index]);
                    WpfPlot1.Refresh();
                }
            }
        }

        public List<double> GetPoints()
        {
            return MyPointsY;
        }

        private void checkButt_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void plusButt_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MyPointsY.Add(0);
            MyPointsX.Add(MyPointsY.Count());
            WpfPlot1.Plot.Axes.Bottom.Max = MyPointsX.Count() + 1;

            var label = CreateLabelForPoint(MyPointsX.Last(), MyPointsY.Last(), 0);
            MyLabels.Add(label);
            MyTimes.Add(0);

            WpfPlot1.Plot.Add.Plottable(label);
            WpfPlot1.Refresh();
        }

        private void minusButt_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (MyPointsY.Count() > 2)
            {
                MyPointsY.RemoveAt(MyPointsY.Count() - 1);
                MyPointsX.RemoveAt(MyPointsX.Count() - 1);
                WpfPlot1.Plot.PlottableList.Remove(MyLabels.Last());
                MyLabels.RemoveAt(MyLabels.Count() - 1);
                MyTimes.Remove(MyTimes.Count() - 1);
                WpfPlot1.Plot.Axes.Bottom.Max = MyPointsX.Count() + 1;
                WpfPlot1.Refresh();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (MyPointsY.Count() >= 2)
            {
                TCVM.IsSetpointsSetted = true;
                var i = 0;
                foreach (var point in MyPointsY)
                {
                    var temp = new SetPoint();
                    temp.temperature = point;

                    if (UserDefinedCheckedBox.IsChecked == true)
                    {
                        temp.timeS = MyTimes[i];
                        temp.timeConstS_cool = 0;
                        temp.timeConstS_heat = 0;
                    }
                    if (TimeConstantsCheckedBox.IsChecked == true)
                    {
                        temp.timeConstS_cool = tau_cool;
                        temp.timeConstS_heat = tau_heat;
                        temp.timeS = -1;
                    }

                    temp.Id = i;
                    setPointsLocal.Add(temp);
                    i++;
                }
            }
            else
            {
                TCVM.IsSetpointsSetted = true;
            }

            TCVM.TauCooling = tau_cool;
            TCVM.TauHeating = tau_heat;
            TCVM.LoopMode = LoopCheckedBox.IsChecked ?? false;
            TCVM.ReverseMode = ReverseCheckedBox.IsChecked ?? false;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox temp)
            {
                var result = rgx.IsMatch(temp.Text);
                int.TryParse(temp.Text, out var intResult);

                if (temp.Name == "HeatingTextBox") tau_heat = intResult;
                if (temp.Name == "CoolingTextBox") tau_cool = intResult;
            }
        }
    }

    public class TwoNumbersInputDialog : Window
    {
        private TextBox _intTextBox;
        private TextBox _doubleTextBox;

        public int? IntValue { get; private set; }
        public double? DoubleValue { get; private set; }

        public TwoNumbersInputDialog(string title, string promptInt, string promptDouble, int time, double temperature)
        {
            Title = title;
            Width = 320;
            Height = 220;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#1E1E22"));
            Foreground = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#E0E0E0"));

            var stack = new StackPanel { Margin = new Thickness(15) };

            var str = time == -1 ? "0" : time.ToString();

            stack.Children.Add(new TextBlock
            {
                Text = promptInt,
                Foreground = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#CCCCCC")),
                Margin = new Thickness(0, 0, 0, 5)
            });

            _intTextBox = new TextBox
            {
                Margin = new Thickness(0, 0, 0, 15),
                Text = str,
                Padding = new Thickness(5),
                Background = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#1E1E22")),
                Foreground = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#E0E0E0")),
                BorderBrush = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#555555"))
            };
            stack.Children.Add(_intTextBox);

            stack.Children.Add(new TextBlock
            {
                Text = promptDouble,
                Foreground = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#CCCCCC")),
                Margin = new Thickness(0, 0, 0, 5)
            });

            _doubleTextBox = new TextBox
            {
                Margin = new Thickness(0, 0, 0, 15),
                Text = temperature.ToString(),
                Padding = new Thickness(5),
                Background = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#1E1E22")),
                Foreground = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#E0E0E0")),
                BorderBrush = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#555555"))
            };
            stack.Children.Add(_doubleTextBox);

            var okButton = new Button
            {
                Content = "OK",
                Width = 80,
                Padding = new Thickness(5),
                Background = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#007ACC")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            okButton.Click += OkButton_Click;
            stack.Children.Add(okButton);

            Content = stack;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(_intTextBox.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int intVal))
            {
                if (intVal >= 0)
                {
                    IntValue = intVal;
                }
                else
                {
                    MessageBox.Show("Parse correct value greater or equal 0!", "Error",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            if (double.TryParse(_doubleTextBox.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleVal))
            {
                if (doubleVal >= -40 && doubleVal <= 180)
                {
                    DoubleValue = doubleVal;
                }
                else
                {
                    MessageBox.Show("Parse correct value between -40 and 180!", "Error",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            DialogResult = true;
        }

        public static (int? intValue, double? doubleValue) Show(string title, string promptInt, string promptDouble, int time, double temperature)
        {
            var dialog = new TwoNumbersInputDialog(title, promptInt, promptDouble, time, temperature);
            bool? result = dialog.ShowDialog();
            return result == true ? (dialog.IntValue, dialog.DoubleValue) : (null, null);
        }
    }
}