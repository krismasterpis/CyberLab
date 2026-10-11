using Ivi.Visa;
using NationalInstruments.Visa;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace CyberLab.Resources.Libraries
{
    public class OSA : IDisposable
    {
        const int BUFFER_LEN = 1024;
        const int OBE_BYTE = 1;
        const char BLOCK_CHAR = '#';

        private MessageBasedSession? mbSession;

        public bool IsConnected { get; private set; }
        private int timeout = 500;
        private string? ip;
        private string? ip2;
        public string identity = "Not Connected";

        private Dictionary<string, Trace> tracesLocal = new Dictionary<string, Trace>();

        public const string MODEL_NAME = "MS9740A";
        public const string MANUFACTURER = "ANRITSU CORPORATION";

        #region Enums for Settings

        public enum ResolutionSetting
        {
            Res_0_03_nm,
            Res_0_05_nm,
            Res_0_07_nm,
            Res_0_10_nm,
            Res_0_20_nm,
            Res_0_50_nm,
            Res_1_00_nm
        }

        public enum VbwSetting
        {
            VBW_10_Hz = 10,
            VBW_100_Hz = 100,
            VBW_200_Hz = 200,
            VBW_1_kHz = 1000,
            VBW_2_kHz = 2000,
            VBW_10_kHz = 10000,
            VBW_100_kHz = 100000,
            VBW_1_MHz = 1000000
        }

        public enum WavelengthDisplayUnit
        {
            InAir,
            InVacuum
        }

        public enum StorageMode
        {
            Off,
            SweepAverage,
            MaxHold,
            MinHold,
            Overlap
        }

        public enum WdmDetectionType
        {
            Peak,
            Threshold
        }

        #endregion

        #region Wavelength Parameters

        public const double WAVELENGTH_RANGE_MIN_nm = 600;
        public const double WAVELENGTH_RANGE_MAX_nm = 1750;

        public double SPAN_WAVELENGTH_nm;
        public double CENTER_WAVELENGTH_nm;
        public double START_WAVELENGTH_nm;
        public double STOP_WAVELENGTH_nm;

        public const double WAVELENGTH_ACCURACY_WITH_CAL_OPTION_pm = 20.0;
        public const double WAVELENGTH_STABILITY_pm = 5.0;

        public ResolutionSetting RESOLUTION_SETTING;

        #endregion

        #region Level Parameters

        public double REFERENCE_LEVEL_dBm;
        public double LOG_SCALE_PER_DIVISION_dB;

        public const double LEVEL_ACCURACY_dB = 0.4;
        public const double LEVEL_STABILITY_dB = 0.02;
        public const double LEVEL_LINEARITY_dB = 0.05;

        public const double MAX_OPTICAL_INPUT_LEVEL_dBm = 23;
        public const double RECEIVER_SENSITIVITY_dBm = -90;

        public bool INTERNAL_OPTICAL_ATTENUATOR_ENABLED;

        #endregion

        #region Sweep and Data Capture Parameters

        public const double SWEEP_SPEED_s_per_500nm = 0.3;

        public int SAMPLING_POINTS_COUNT;
        public int POINT_AVERAGE_COUNT;
        public int SWEEP_AVERAGE_COUNT;
        public string SMOOTHING_ENABLED = "OFF";

        public VbwSetting VBW_SETTING;
        public StorageMode TRACE_STORAGE_MODE;

        #endregion

        #region Dynamic Range Parameters

        public const int DYNAMIC_RANGE_HIGH_MODE_at_1nm_dB = 70;
        public const int DYNAMIC_RANGE_HIGH_MODE_at_0_4nm_dB = 60;
        public const int DYNAMIC_RANGE_HIGH_MODE_at_0_2nm_dB = 42;
        public const int DYNAMIC_RANGE_NORMAL_MODE_at_1nm_dB = 62;
        public const int DYNAMIC_RANGE_NORMAL_MODE_at_0_4nm_dB = 58;

        #endregion

        #region Trigger Parameters

        public bool EXTERNAL_TRIGGER_ENABLED;
        public double EXTERNAL_TRIGGER_DELAY_s;
        public int MEASUREMENT_INTERVAL_TIME_s;

        #endregion

        public OSA()
        {
            IsConnected = false;
            mbSession = null;
            for (int i = 0; i < 10; i++)
            {
                var name = $"Trace({(char)('A' + i)})";
                tracesLocal.Add(name, new Trace(name));
            }
        }

        public static bool PingHost(string nameOrAddress)
        {
            bool pingable = false;
            Ping pinger = null;

            try
            {
                pinger = new Ping();
                PingReply reply = pinger.Send(nameOrAddress);
                pingable = reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {
                // Return false on exception
            }
            finally
            {
                pinger?.Dispose();
            }

            return pingable;
        }

        public void Connect(string _ip, int _timeout = -1)
        {
            try
            {
                if (string.IsNullOrEmpty(_ip))
                {
                    throw new Exception("Adres urządzenia nie może być pusty!");
                }
                ip = _ip;
                var parts = ip.Split("::");
                if (parts.Length < 2)
                {
                    throw new Exception("Nieprawidłowy format adresu zasobu VISA!");
                }
                ip2 = parts[1];
                if (_ip.Contains("::SOCKET"))
                {
                    throw new Exception("Adres urządzenia nie może odnosić się do połączenia typu Socket!");
                }
                else if (!_ip.Contains("::INSTR"))
                {
                    throw new Exception("Adres urządzenia musi odnosić się do połączenia typu INSTR!");
                }
                if (PingHost(ip2))
                {
                    ResourceManager rManager = new ResourceManager();
                    mbSession = (MessageBasedSession)rManager.Open(ip);
                    if (_timeout != -1)
                    {
                        mbSession.TimeoutMilliseconds = _timeout;
                    }
                    if (mbSession.HardwareInterfaceType == HardwareInterfaceType.Tcp)
                    {
                        mbSession.TerminationCharacterEnabled = true;
                    }
                    IsConnected = true;
                }
                else
                {
                    throw new Exception("Brak odpowiedzi ping na podany adres hosta.");
                }
                if (IsConnected) ReadParameters();
            }
            catch (Exception e)
            {
                throw new Exception($"Wykryto niespodziewany błąd: {e.Message} podczas łączenia z {ip2}");
            }
        }

        public void Disconnect()
        {
            if (!IsConnected || mbSession == null)
            {
                return;
            }

            try
            {
                mbSession.Dispose();
                Debug.WriteLine("Rozłączono z OSA.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Błąd podczas rozłączania: {ex.Message}");
            }
            finally
            {
                IsConnected = false;
                mbSession = null;
            }
        }

        public string Query(string qCommand)
        {
            if (mbSession == null) return string.Empty;

            try
            {
                mbSession.FormattedIO.WriteLine(qCommand);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Wykryto błąd podczas wysyłania zapytania {qCommand}: {e.Message}");
            }
            try
            {
                return Read();
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Wykryto błąd podczas odczytu odpowiedzi na {qCommand}: {e.Message}");
            }
            return string.Empty;
        }

        public void SetTimeout(int _timeout)
        {
            timeout = _timeout;
            if (mbSession != null)
            {
                mbSession.TimeoutMilliseconds = timeout;
            }
        }

        private string Read()
        {
            if (mbSession == null) return string.Empty;

            string data = null;
            try
            {
                data += mbSession.FormattedIO.ReadChar();

                if (data != BLOCK_CHAR.ToString())
                {
                    data += mbSession.FormattedIO.ReadLine();
                }
                else
                {
                    string sizeBuffer1 = Convert.ToString(mbSession.FormattedIO.ReadChar());
                    data += sizeBuffer1;

                    string sizeBuffer2 = "";
                    for (int i = 0; i < Convert.ToInt32(sizeBuffer1); i++)
                    {
                        sizeBuffer2 += mbSession.FormattedIO.ReadChar();
                    }
                    data += sizeBuffer2;

                    for (int i = 0; i <= Convert.ToInt32(sizeBuffer2); i++)
                    {
                        data += mbSession.FormattedIO.ReadChar();
                    }

                    mbSession.FormattedIO.DiscardBuffers();
                }
                data = data?.TrimEnd('\n', '\r');
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Wykryto błąd podczas odczytu: {e.Message}");
            }
            return data ?? string.Empty;
        }

        public void Write(string wCommand)
        {
            if (mbSession == null) return;

            try
            {
                mbSession.FormattedIO.WriteLine(wCommand);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Wykryto błąd podczas zapisu {wCommand}: {e.Message}");
            }
        }

        public void Dispose()
        {
            Disconnect();
        }

        public void ReadParameters()
        {
            string temp = Query(AnritsuMs9740AScpiCommands.SPAN_WAVELENGTH_QUERY);
            if (double.TryParse(temp, CultureInfo.InvariantCulture, out double resultD)) SPAN_WAVELENGTH_nm = resultD;

            temp = Query(AnritsuMs9740AScpiCommands.CENTER_WAVELENGTH_QUERY);
            if (double.TryParse(temp, CultureInfo.InvariantCulture, out resultD)) CENTER_WAVELENGTH_nm = resultD;

            temp = Query(AnritsuMs9740AScpiCommands.START_WAVELENGTH_QUERY);
            if (double.TryParse(temp, CultureInfo.InvariantCulture, out resultD)) START_WAVELENGTH_nm = resultD;

            temp = Query(AnritsuMs9740AScpiCommands.STOP_WAVELENGTH_QUERY);
            if (double.TryParse(temp, CultureInfo.InvariantCulture, out resultD)) STOP_WAVELENGTH_nm = resultD;

            temp = Query(AnritsuMs9740AScpiCommands.RESOLUTION_QUERY);
            RESOLUTION_SETTING = temp switch
            {
                "1.0" => ResolutionSetting.Res_1_00_nm,
                "0.50" => ResolutionSetting.Res_0_50_nm,
                "0.20" => ResolutionSetting.Res_0_20_nm,
                "0.10" => ResolutionSetting.Res_0_10_nm,
                "0.07" => ResolutionSetting.Res_0_07_nm,
                "0.05" => ResolutionSetting.Res_0_05_nm,
                "0.03" => ResolutionSetting.Res_0_03_nm,
                _ => RESOLUTION_SETTING
            };

            temp = Query(AnritsuMs9740AScpiCommands.SAMPLING_POINTS_QUERY);
            if (int.TryParse(temp, out int resultI)) SAMPLING_POINTS_COUNT = resultI;

            temp = Query(AnritsuMs9740AScpiCommands.POINT_AVERAGE_QUERY);
            if (int.TryParse(temp, out resultI)) POINT_AVERAGE_COUNT = resultI;

            temp = Query(AnritsuMs9740AScpiCommands.SWEEP_AVERAGE_QUERY);
            if (int.TryParse(temp, out resultI)) SWEEP_AVERAGE_COUNT = resultI;

            temp = Query(AnritsuMs9740AScpiCommands.SMOOTHING_QUERY);
            SMOOTHING_ENABLED = temp;

            temp = Query(AnritsuMs9740AScpiCommands.VIDEO_BANDWIDTH_QUERY);
            VBW_SETTING = temp switch
            {
                "10HZ" => VbwSetting.VBW_10_Hz,
                "100HZ" => VbwSetting.VBW_100_Hz,
                "200HZ" => VbwSetting.VBW_200_Hz,
                "1KHZ" => VbwSetting.VBW_1_kHz,
                "2KHZ" => VbwSetting.VBW_2_kHz,
                "10KHZ" => VbwSetting.VBW_10_kHz,
                "100KHZ" => VbwSetting.VBW_100_kHz,
                "1MHZ" => VbwSetting.VBW_1_MHz,
                _ => VBW_SETTING
            };
        }

        private List<double> generateXS(double _center, double _span, int _points)
        {
            string startStr = Query("STA?");
            string stopStr = Query("STO?");

            if (!double.TryParse(startStr, CultureInfo.InvariantCulture, out double min))
                min = double.TryParse(startStr.Replace(',', '.'), CultureInfo.InvariantCulture, out min) ? min : (_center - _span / 2);

            if (!double.TryParse(stopStr, CultureInfo.InvariantCulture, out double max))
                max = double.TryParse(stopStr.Replace(',', '.'), CultureInfo.InvariantCulture, out max) ? max : (_center + _span / 2);

            if (_points <= 1) return new List<double> { min };

            double step = (max - min) / (_points - 1);
            List<double> xs = new List<double>(_points);

            for (int j = 0; j < _points; j++)
            {
                if (j == 0)
                    xs.Add(Math.Round(min, 5));
                else if (j == _points - 1)
                    xs.Add(Math.Round(max, 5));
                else
                    xs.Add(Math.Round(min + j * step, 5));
            }
            return xs;
        }

        public void ReadSingle(Measurement measurement, Dictionary<string, TraceType> types, IProgress<int> progress)
        {
            try
            {
                Write(AnritsuMs9740AScpiCommands.SINGLE_SWEEP);
                Write("*CLS");
                progress?.Report(10);
                WaitForEndOfOperation();
                progress?.Report(50);

                List<double> Xs = generateXS(this.CENTER_WAVELENGTH_nm, this.SPAN_WAVELENGTH_nm, this.SAMPLING_POINTS_COUNT);
                progress?.Report(60);

                var queryCommands = new Dictionary<string, string>
                {
                    ["Trace(A)"] = AnritsuMs9740AScpiCommands.MEMORY_DATA_A_CSV_QUERY,
                    ["Trace(B)"] = AnritsuMs9740AScpiCommands.MEMORY_DATA_B_CSV_QUERY,
                    ["Trace(C)"] = AnritsuMs9740AScpiCommands.MEMORY_DATA_C_CSV_QUERY,
                    ["Trace(D)"] = AnritsuMs9740AScpiCommands.MEMORY_DATA_D_CSV_QUERY,
                    ["Trace(E)"] = AnritsuMs9740AScpiCommands.MEMORY_DATA_E_CSV_QUERY,
                    ["Trace(F)"] = AnritsuMs9740AScpiCommands.MEMORY_DATA_F_CSV_QUERY,
                    ["Trace(G)"] = AnritsuMs9740AScpiCommands.MEMORY_DATA_G_CSV_QUERY,
                    ["Trace(H)"] = AnritsuMs9740AScpiCommands.MEMORY_DATA_H_CSV_QUERY,
                    ["Trace(I)"] = AnritsuMs9740AScpiCommands.MEMORY_DATA_I_CSV_QUERY,
                    ["Trace(J)"] = AnritsuMs9740AScpiCommands.MEMORY_DATA_J_CSV_QUERY,
                };

                foreach (var trace in measurement.traces.Values)
                {
                    if (trace.name == null) continue;

                    TraceType currentType = types.ContainsKey(trace.name) ? types[trace.name] : trace.type;

                    if (currentType == TraceType.Write && queryCommands.TryGetValue(trace.name, out var queryCmd))
                    {
                        string result = Query(queryCmd);
                        string[] temp = result.Split(',');
                        List<double> Ys = new List<double>();
                        for (int i = 0; i < temp.Length - 1; i++)
                        {
                            if (double.TryParse(temp[i], CultureInfo.InvariantCulture, out var value))
                            {
                                Ys.Add(value);
                            }
                        }
                        trace.SetData(Xs, Ys);
                    }
                    else if (currentType == TraceType.Blank)
                    {
                        trace.Clear();
                    }
                    else if (currentType == TraceType.Calculate)
                    {
                        var char1 = trace.calculateTrace1;
                        var char2 = trace.calculateTrace2;
                        string key1 = $"Trace({char1})";
                        string key2 = $"Trace({char2})";

                        if (measurement.traces.ContainsKey(key1) && measurement.traces.ContainsKey(key2))
                        {
                            var t1 = measurement.traces[key1];
                            var t2 = measurement.traces[key2];
                            List<double> x = new List<double>();
                            List<double> y = new List<double>();
                            int count = Math.Min(t1.wavelengths.Count, t2.attenuationsRaw.Count);
                            for (int i = 0; i < count; i++)
                            {
                                x.Add(t1.wavelengths[i]);
                                y.Add(t1.attenuationsRaw[i] - t2.attenuationsRaw[i]);
                            }
                            trace.SetData(x, y);
                        }
                    }
                }
                progress?.Report(100);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Wykryto błąd podczas odczytu SINGLE: {e.Message}");
            }
        }

        public void SendTraceType(char traceChar, TraceType type)
        {
            string modeStr = type switch
            {
                TraceType.Write => "WRITE",
                _ => "BLANK"
            };
            Write($"TTP {traceChar},{modeStr}");
        }

        public bool WaitForEndOfOperation()
        {
            while (true)
            {
                string status = Query("ESR2?");
                if (!string.IsNullOrEmpty(status) && status != "0")
                {
                    break;
                }
            }
            return true;
        }
    }
}
