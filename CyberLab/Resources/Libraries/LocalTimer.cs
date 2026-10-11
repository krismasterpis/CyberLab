using CommunityToolkit.Mvvm.Input;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace CyberLab.Resources.Libraries
{
    public class LocalTimer : INotifyPropertyChanged
    {
        private readonly System.Timers.Timer Timer;
        public TimeSpan _time;
        public TimeSpan estTime = TimeSpan.Zero;
        private Visibility isTimerEnabled;
        public long Interval { get; private set; }
        public ICommand StartTimerCommand { get; }
        public ICommand PauseTimerCommand { get; }
        public ICommand RestartTimerCommand { get; }
        private event EventHandler TimerElapsed;
        public LocalTimer(long intervalSeconds, EventHandler TimerElapsedHandler)
        {
            Interval = intervalSeconds;
            Time = TimeSpan.FromSeconds(Interval);
            Timer = new System.Timers.Timer
            {
                Interval = 1000
            };
            Timer.Elapsed += (s, e) =>
            {
                if (Time.TotalSeconds > 0)
                {
                    Time = Time.Subtract(TimeSpan.FromSeconds(1));
                }
                if (Time.TotalSeconds <= 0)
                {
                    TimerElapsed?.Invoke(this, EventArgs.Empty);
                    Time = TimeSpan.FromSeconds(Interval);
                }
            };
            this.TimerElapsed += TimerElapsedHandler;
            StartTimerCommand = new RelayCommand(Start);
            PauseTimerCommand = new RelayCommand(Stop);
            RestartTimerCommand = new RelayCommand(Reset);
            isTimerEnabled = Visibility.Hidden;
        }

        public TimeSpan Time
        {
            get => _time;
            set
            {
                if (_time != value)
                {
                    _time = value;
                    OnPropertyChanged(nameof(Time));
                }
            }
        }
        public void Start()
        {
            if (isTimerEnabled == Visibility.Hidden)
            {
                Timer.Start();
                isTimerEnabled = Visibility.Visible;
            }
        }
        public void Stop()
        {
            if (isTimerEnabled == Visibility.Visible)
            {
                Timer.Stop();
                isTimerEnabled = Visibility.Hidden;
            }
        }
        public void Reset()
        {
            if (isTimerEnabled == Visibility.Visible)
            {
                Timer.Stop();
                Time = TimeSpan.Zero;
                isTimerEnabled = Visibility.Hidden;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
