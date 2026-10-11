using System;
using System.Collections.Generic;

namespace CyberLab.Resources.Libraries
{
    public class Measurement
    {
        public int Id { get; set; }
        public DateTime Time { get; set; }
        public double Temperature { get; set; }
        public Dictionary<string, Trace> traces { get; set; } = new();

        public Measurement()
        {
            for (int i = 0; i < 10; i++)
            {
                var name = $"Trace({(char)('A' + i)})";
                traces.Add(name, new Trace(name));
            }
        }

        public Measurement(Measurement other)
        {
            if (other == null) return;

            Id = other.Id;
            Time = other.Time;
            Temperature = other.Temperature;
            foreach (var kvp in other.traces)
            {
                var trace = new Trace(kvp.Value.name)
                {
                    type = kvp.Value.type,
                    calculateTrace1 = kvp.Value.calculateTrace1,
                    calculateTrace2 = kvp.Value.calculateTrace2,
                    calculateSign = kvp.Value.calculateSign
                };
                trace.SetData(kvp.Value.wavelengths, kvp.Value.attenuationsRaw);
                traces[kvp.Key] = trace;
            }
        }
    }
}

