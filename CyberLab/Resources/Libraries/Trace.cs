using System.Collections.Generic;

namespace CyberLab.Resources.Libraries
{
    public class Trace
    {
        public Trace()
        {
            wavelengths = new List<double>();
            attenuationsRaw = new List<double>();
            attenuationsSmooth = new List<double>();
            name = string.Empty;
        }

        public Trace(string _name)
        {
            name = _name;
            wavelengths = new List<double>();
            attenuationsRaw = new List<double>();
            attenuationsSmooth = new List<double>();
        }

        public List<double> wavelengths { get; set; }
        public List<double> attenuationsRaw { get; set; }
        public List<double> attenuationsSmooth { get; set; }
        public string name { get; set; }
        public TraceType type { get; set; } = TraceType.Blank;
        public char calculateTrace1 { get; set; }
        public char calculateTrace2 { get; set; }
        public char calculateSign { get; set; }

        public void SetData(List<double> _wavelengths, List<double> _attenuationRaw)
        {
            wavelengths = new List<double>(_wavelengths);
            attenuationsRaw = new List<double>(_attenuationRaw);
            attenuationsSmooth = new List<double>();
        }

        public void Clear()
        {
            wavelengths.Clear();
            attenuationsRaw.Clear();
            attenuationsSmooth.Clear();
        }
    }

    public enum TraceType
    {
        Blank,
        Write,
        Fix,
        Calculate
    }
}

