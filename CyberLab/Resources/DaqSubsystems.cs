using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Keithley.DAQ6510;

#region System Subsystem

public sealed class SystemSubsystem
{
    private readonly Daq6510Client _client;

    internal SystemSubsystem(Daq6510Client client) => _client = client;

    public async Task<InstrumentIdentity> GetIdentityAsync(CancellationToken ct = default)
    {
        string raw = await _client.QueryRawAsync("*IDN?", ct).ConfigureAwait(false);
        return InstrumentIdentity.Parse(raw);
    }

    public Task ResetAsync(CancellationToken ct = default)
        => _client.SendCommandAsync("*RST", ct);

    public Task ClearStatusAsync(CancellationToken ct = default)
        => _client.SendCommandAsync("*CLS", ct);

    public async Task WaitForOperationCompleteAsync(CancellationToken ct = default)
    {
        _ = await _client.QueryRawAsync("*OPC?", ct).ConfigureAwait(false);
    }

    public async Task SetCommandLanguageAsync(CommandLanguage language, CancellationToken ct = default)
    {
        string langStr = language == CommandLanguage.Scpi ? "SCPI" : "TSP";
        await _client.Transport.WriteAsync($"*LANG {langStr}", ct).ConfigureAwait(false);
    }

    public async Task<CommandLanguage> GetCommandLanguageAsync(CancellationToken ct = default)
    {
        string resp = await _client.Transport.QueryStringAsync("*LANG?", ct).ConfigureAwait(false);
        return resp.Trim().Equals("TSP", StringComparison.OrdinalIgnoreCase)
            ? CommandLanguage.Tsp
            : CommandLanguage.Scpi;
    }

    public async Task<InstrumentError> GetNextErrorAsync(CancellationToken ct = default)
    {
        string raw = await _client.Transport.QueryStringAsync(":SYSTem:ERRor:NEXT?", ct).ConfigureAwait(false);
        int commaIndex = raw.IndexOf(',');
        if (commaIndex < 0)
            return new InstrumentError(-1, raw);

        int.TryParse(raw[..commaIndex].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int code);
        string msg = raw[(commaIndex + 1)..].Trim().Trim('"');
        return new InstrumentError(code, msg);
    }

    public async Task<IReadOnlyList<InstrumentError>> DrainErrorQueueAsync(CancellationToken ct = default)
    {
        var errors = new List<InstrumentError>();
        for (int i = 0; i < 64; i++)
        {
            var err = await GetNextErrorAsync(ct).ConfigureAwait(false);
            if (err.IsNoError)
                break;
            errors.Add(err);
        }
        return errors;
    }

    public Task ClearErrorQueueAsync(CancellationToken ct = default)
        => _client.Transport.WriteAsync(":SYSTem:CLEar", ct);

    public Task SetDataFormatAsync(
        DataFormat format,
        ByteOrder byteOrder = ByteOrder.Swapped,
        CancellationToken ct = default)
    {
        string fmt = format switch
        {
            DataFormat.Ascii => "ASCii",
            DataFormat.Real64 => "REAL",
            DataFormat.SingleReal32 => "SREal",
            _ => "ASCii"
        };
        string order = byteOrder == ByteOrder.Swapped ? "SWAPped" : "NORMal";
        return _client.SendCommandAsync($":FORMat:DATA {fmt};:FORMat:BORDer {order}", ct);
    }

    public Task BeepAsync(double frequencyHz = 1000, double durationSeconds = 0.2, CancellationToken ct = default)
        => _client.SendCommandAsync(
            $":SYSTem:BEEPer {ScpiSyntax.FormatDouble(frequencyHz)}, {ScpiSyntax.FormatDouble(durationSeconds)}", ct);

    public Task SetFrontPanelDisplayEnabledAsync(bool enabled, CancellationToken ct = default)
        => _client.SendCommandAsync($":DISPlay:LIGHT:STATe {(enabled ? "ON50" : "OFF")}", ct);

    public Task SetUserSwipeScreenTextAsync(string line1, string line2 = "", CancellationToken ct = default)
        => _client.SendCommandAsync(
            $":DISPlay:CLEar;:DISPlay:USER1:TEXT:DATA \"{line1}\";:DISPlay:USER2:TEXT:DATA \"{line2}\";:DISPlay:SCRReen USER", ct);
}

#endregion

#region Sense (DMM Measurement) Subsystem

public sealed class SenseSubsystem
{
    private readonly Daq6510Client _client;

    internal SenseSubsystem(Daq6510Client client) => _client = client;

    /// <summary>
    /// Sets the active measurement function globally or for specific switch channels.
    /// </summary>
    public Task SetFunctionAsync(
        DmmFunction function,
        ChannelList? channels = null,
        CancellationToken ct = default)
    {
        string chSuffix = channels is not null ? $", {channels}" : string.Empty;
        return _client.SendCommandAsync($":SENSe:FUNCtion {function.ToScpiQuotedFunction()}{chSuffix}", ct);
    }

    /// <summary>
    /// Performs an immediate single measurement and returns the value (:READ?).
    /// </summary>
    public async Task<double> ReadSingleAsync(string bufferName = "defbuffer1", CancellationToken ct = default)
    {
        string raw = await _client.QueryRawAsync($":READ? \"{bufferName}\", READing", ct).ConfigureAwait(false);
        return double.Parse(raw.Trim(), CultureInfo.InvariantCulture);
    }

    public Task SetRangeAsync(
        DmmFunction function,
        double range,
        ChannelList? channels = null,
        CancellationToken ct = default)
    {
        string chSuffix = channels is not null ? $", {channels}" : string.Empty;
        return _client.SendCommandAsync(
            $":SENSe:{function.ToScpiFunctionName()}:RANGe:UPPer {ScpiSyntax.FormatDouble(range)}{chSuffix}", ct);
    }

    public Task SetAutoRangeAsync(
        DmmFunction function,
        bool enabled,
        ChannelList? channels = null,
        CancellationToken ct = default)
    {
        string chSuffix = channels is not null ? $", {channels}" : string.Empty;
        return _client.SendCommandAsync(
            $":SENSe:{function.ToScpiFunctionName()}:RANGe:AUTO {ScpiSyntax.FormatBool(enabled)}{chSuffix}", ct);
    }

    /// <summary>
    /// Sets integration time in Number of Power Line Cycles (0.0005 to 15 PLC on 60Hz, 0.0005 to 12 PLC on 50Hz).
    /// </summary>
    public Task SetNplcAsync(
        DmmFunction function,
        double nplc,
        ChannelList? channels = null,
        CancellationToken ct = default)
    {
        string chSuffix = channels is not null ? $", {channels}" : string.Empty;
        return _client.SendCommandAsync(
            $":SENSe:{function.ToScpiFunctionName()}:NPLCycles {ScpiSyntax.FormatDouble(nplc)}{chSuffix}", ct);
    }

    /// <summary>
    /// Sets integration aperture time in seconds.
    /// </summary>
    public Task SetApertureAsync(
        DmmFunction function,
        double seconds,
        ChannelList? channels = null,
        CancellationToken ct = default)
    {
        string chSuffix = channels is not null ? $", {channels}" : string.Empty;
        return _client.SendCommandAsync(
            $":SENSe:{function.ToScpiFunctionName()}:APERture {ScpiSyntax.FormatDouble(seconds)}{chSuffix}", ct);
    }

    public Task SetAutoZeroAsync(
        DmmFunction function,
        AutoZeroMode mode,
        ChannelList? channels = null,
        CancellationToken ct = default)
    {
        if (mode == AutoZeroMode.Once)
            return _client.SendCommandAsync(":SENSe:AZERo:ONCE", ct);

        string state = mode == AutoZeroMode.On ? "1" : "0";
        string chSuffix = channels is not null ? $", {channels}" : string.Empty;
        return _client.SendCommandAsync(
            $":SENSe:{function.ToScpiFunctionName()}:AZERo:STATe {state}{chSuffix}", ct);
    }

    public Task SetAutoDelayAsync(
        DmmFunction function,
        bool enabled,
        ChannelList? channels = null,
        CancellationToken ct = default)
    {
        string chSuffix = channels is not null ? $", {channels}" : string.Empty;
        return _client.SendCommandAsync(
            $":SENSe:{function.ToScpiFunctionName()}:ADELay:STATe {ScpiSyntax.FormatBool(enabled)}{chSuffix}", ct);
    }

    public Task SetLineSyncAsync(
        DmmFunction function,
        bool enabled,
        ChannelList? channels = null,
        CancellationToken ct = default)
    {
        string chSuffix = channels is not null ? $", {channels}" : string.Empty;
        return _client.SendCommandAsync(
            $":SENSe:{function.ToScpiFunctionName()}:LSYNc:STATe {ScpiSyntax.FormatBool(enabled)}{chSuffix}", ct);
    }

    public Task SetOffsetCompensatedOhmsAsync(
        DmmFunction resistanceFunction,
        bool enabled,
        ChannelList? channels = null,
        CancellationToken ct = default)
    {
        if (resistanceFunction is not (DmmFunction.Resistance2Wire or DmmFunction.Resistance4Wire))
            throw new ArgumentException("Offset compensation applies only to 2-wire or 4-wire resistance.");

        string chSuffix = channels is not null ? $", {channels}" : string.Empty;
        return _client.SendCommandAsync(
            $":SENSe:{resistanceFunction.ToScpiFunctionName()}:OCOMpensated {ScpiSyntax.FormatBool(enabled)}{chSuffix}", ct);
    }

    public Task ConfigureAveragingFilterAsync(
        DmmFunction function,
        bool enabled,
        int count = 10,
        FilterType filterType = FilterType.Moving,
        ChannelList? channels = null,
        CancellationToken ct = default)
    {
        string fn = function.ToScpiFunctionName();
        string tcon = filterType == FilterType.Moving ? "MOVing" : "REPeat";
        string chSuffix = channels is not null ? $", {channels}" : string.Empty;

        string cmd =
            $":SENSe:{fn}:AVERage:TCONtrol {tcon}{chSuffix};" +
            $":SENSe:{fn}:AVERage:COUNt {count}{chSuffix};" +
            $":SENSe:{fn}:AVERage:STATe {ScpiSyntax.FormatBool(enabled)}{chSuffix}";

        return _client.SendCommandAsync(cmd, ct);
    }

    public Task ConfigureThermocoupleAsync(
        ThermocoupleType tcType,
        ReferenceJunctionType refJunction = ReferenceJunctionType.Internal,
        double simulatedRefTempCelsius = 23.0,
        ChannelList? channels = null,
        CancellationToken ct = default)
    {
        string chSuffix = channels is not null ? $", {channels}" : string.Empty;
        string rjun = refJunction switch
        {
            ReferenceJunctionType.Simulated => "SIMulated",
            ReferenceJunctionType.Internal => "INTernal",
            ReferenceJunctionType.External => "EXTernal",
            _ => "INTernal"
        };

        var sb = new StringBuilder();
        sb.Append($":SENSe:FUNCtion \"TEMPerature\"{chSuffix};");
        sb.Append($":SENSe:TEMPerature:TRANsducer TC{chSuffix};");
        sb.Append($":SENSe:TEMPerature:TCouple:TYPE {tcType}{chSuffix};");
        sb.Append($":SENSe:TEMPerature:TCouple:RJUNction:RSELect {rjun}{chSuffix}");

        if (refJunction == ReferenceJunctionType.Simulated)
        {
            sb.Append($";:SENSe:TEMPerature:TCouple:RJUNction:SIMulated {ScpiSyntax.FormatDouble(simulatedRefTempCelsius)}{chSuffix}");
        }

        return _client.SendCommandAsync(sb.ToString(), ct);
    }

    public Task ConfigureRtdAsync(
        TemperatureTransducer wireMode,
        RtdType rtdType = RtdType.Pt100,
        ChannelList? channels = null,
        CancellationToken ct = default)
    {
        string trans = wireMode switch
        {
            TemperatureTransducer.Rtd2Wire => "RTD",
            TemperatureTransducer.Rtd3Wire => "TRTD",
            TemperatureTransducer.Rtd4Wire => "FRTD",
            _ => throw new ArgumentException("Wire mode must be Rtd2Wire, Rtd3Wire, or Rtd4Wire.", nameof(wireMode))
        };

        string typeProp = wireMode switch
        {
            TemperatureTransducer.Rtd2Wire => "TWO",
            TemperatureTransducer.Rtd3Wire => "THRee",
            TemperatureTransducer.Rtd4Wire => "FOUR",
            _ => "FOUR"
        };

        string rtdStr = rtdType switch
        {
            RtdType.Pt100 => "PT100",
            RtdType.Pt385 => "PT385",
            RtdType.Pt3916 => "PT3916",
            RtdType.D100 => "D100",
            RtdType.F100 => "F100",
            RtdType.User => "USER",
            _ => "PT100"
        };

        string chSuffix = channels is not null ? $", {channels}" : string.Empty;
        string cmd =
            $":SENSe:FUNCtion \"TEMPerature\"{chSuffix};" +
            $":SENSe:TEMPerature:TRANsducer {trans}{chSuffix};" +
            $":SENSe:TEMPerature:RTD:{typeProp} {rtdStr}{chSuffix}";

        return _client.SendCommandAsync(cmd, ct);
    }
}

#endregion

#region Digitize Subsystem (1 MS/s High-Speed Capture)

public sealed class DigitizeSubsystem
{
    private readonly Daq6510Client _client;

    internal DigitizeSubsystem(Daq6510Client client) => _client = client;

    public Task ConfigureDigitizeAsync(
        DigitizeFunction function,
        double range,
        int sampleRateSaPerSec = 1_000_000,
        int sampleCount = 1000,
        ChannelList? channels = null,
        CancellationToken ct = default)
    {
        if (function == DigitizeFunction.None)
            return _client.SendCommandAsync(":SENSe:DIGitize:FUNCtion:ON \"NONE\"", ct);

        if (sampleRateSaPerSec is < 1000 or > 1_000_000)
            throw new ArgumentOutOfRangeException(nameof(sampleRateSaPerSec), "Sample rate must be between 1,000 and 1,000,000 Sa/s.");

        string fn = function.ToScpiDigitizeName();
        string chSuffix = channels is not null ? $", {channels}" : string.Empty;

        string cmd =
            $":SENSe:DIGitize:FUNCtion:ON \"{fn}\"{chSuffix};" +
            $":SENSe:DIGitize:{fn}:RANGe:UPPer {ScpiSyntax.FormatDouble(range)}{chSuffix};" +
            $":SENSe:DIGitize:{fn}:SRATe {sampleRateSaPerSec}{chSuffix};" +
            $":SENSe:DIGitize:COUNt {sampleCount}";

        return _client.SendCommandAsync(cmd, ct);
    }

    public async Task<double> ReadSingleDigitizeAsync(string bufferName = "defbuffer1", CancellationToken ct = default)
    {
        string raw = await _client.QueryRawAsync($":READ:DIGitize? \"{bufferName}\", READing", ct).ConfigureAwait(false);
        return double.Parse(raw.Trim(), CultureInfo.InvariantCulture);
    }
}

#endregion

#region Route / Switching Subsystem

public sealed class RouteSubsystem
{
    private readonly Daq6510Client _client;

    internal RouteSubsystem(Daq6510Client client) => _client = client;

    public Task CloseChannelsAsync(ChannelList channels, CancellationToken ct = default)
        => _client.SendCommandAsync($":ROUTe:CLOSe {channels}", ct);

    public Task OpenChannelsAsync(ChannelList channels, CancellationToken ct = default)
        => _client.SendCommandAsync($":ROUTe:OPEN {channels}", ct);

    public Task OpenAllChannelsAsync(CancellationToken ct = default)
        => _client.SendCommandAsync(":ROUTe:OPEN:ALL", ct);

    public async Task<bool[]> GetClosedStateAsync(ChannelList channels, CancellationToken ct = default)
    {
        string raw = await _client.QueryRawAsync($":ROUTe:CLOSe:STATe? {channels}", ct).ConfigureAwait(false);
        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                  .Select(s => s.Trim() == "1")
                  .ToArray();
    }

    public async Task<string> GetSlotCardIdentityAsync(int slot, CancellationToken ct = default)
    {
        if (slot is < 1 or > 2)
            throw new ArgumentOutOfRangeException(nameof(slot), "Slot must be 1 or 2.");

        return await _client.QueryRawAsync($":SYSTem:CARD{slot}:IDN?", ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Assigns a simulated pseudocard (e.g., "7700", "7701", "7702", "7706", "7708", "7710")
    /// to an empty slot so scan and routing routines can be tested without physical cards installed.
    /// </summary>
    public Task SetPseudoCardAsync(int slot, string cardModelNumber, CancellationToken ct = default)
    {
        if (slot is < 1 or > 2)
            throw new ArgumentOutOfRangeException(nameof(slot), "Slot must be 1 or 2.");

        return _client.SendCommandAsync($":SYSTem:PCARd{slot} {cardModelNumber}", ct);
    }
}

#endregion

#region Scan Engine Subsystem

public sealed class ScanSubsystem
{
    private readonly Daq6510Client _client;

    internal ScanSubsystem(Daq6510Client client) => _client = client;

    public Task CreateScanListAsync(ChannelList channels, CancellationToken ct = default)
        => _client.SendCommandAsync($":ROUTe:SCAN:CREate {channels}", ct);

    public Task AddToScanListAsync(ChannelList channels, CancellationToken ct = default)
        => _client.SendCommandAsync($":ROUTe:SCAN:ADD {channels}", ct);

    public Task SetScanCountAsync(int scanCount, CancellationToken ct = default)
        => _client.SendCommandAsync($":ROUTe:SCAN:COUNt:SCAN {scanCount}", ct);

    public async Task<int> GetScanStepCountAsync(CancellationToken ct = default)
    {
        string raw = await _client.QueryRawAsync(":ROUTe:SCAN:COUNt:STEP?", ct).ConfigureAwait(false);
        return int.Parse(raw.Trim(), CultureInfo.InvariantCulture);
    }

    public Task SetScanIntervalAsync(double intervalSeconds, CancellationToken ct = default)
        => _client.SendCommandAsync($":ROUTe:SCAN:INTerval {ScpiSyntax.FormatDouble(intervalSeconds)}", ct);

    public Task SetScanBufferAsync(string bufferName = "defbuffer1", CancellationToken ct = default)
        => _client.SendCommandAsync($":ROUTe:SCAN:BUFFer \"{bufferName}\"", ct);

    public Task StartScanAsync(CancellationToken ct = default)
        => _client.SendCommandAsync(":INITiate", ct);

    public Task AbortScanAsync(CancellationToken ct = default)
        => _client.SendCommandAsync(":ABORt", ct);

    /// <summary>
    /// Queries :ROUTe:SCAN:STATe? and parses state, scan count, and step count.
    /// </summary>
    public async Task<ScanStatusInfo> GetScanStatusAsync(CancellationToken ct = default)
    {
        string raw = await _client.QueryRawAsync(":ROUTe:SCAN:STATe?", ct).ConfigureAwait(false);
        var parts = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        ScanState state = ScanState.Unknown;
        if (parts.Length > 0)
        {
            string s = parts[0].ToUpperInvariant();
            if (s.Contains("RUN")) state = ScanState.Running;
            else if (s.Contains("SUC")) state = ScanState.Success;
            else if (s.Contains("ABO")) state = ScanState.Aborted;
            else if (s.Contains("BUI")) state = ScanState.Building;
            else if (s.Contains("EMP")) state = ScanState.Empty;
            else if (s.Contains("FAI")) state = ScanState.Failed;
        }

        int scans = parts.Length > 1 && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int sc) ? sc : 0;
        int steps = parts.Length > 2 && int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int st) ? st : 0;

        return new ScanStatusInfo(state, scans, steps);
    }

    /// <summary>
    /// Starts a scan and yields new readings in real time as they accumulate in the buffer.
    /// </summary>
    public async IAsyncEnumerable<DaqReading> StreamScanReadingsAsync(
        string bufferName = "defbuffer1",
        TimeSpan? pollInterval = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        TimeSpan interval = pollInterval ?? TimeSpan.FromMilliseconds(100);
        await _client.Trace.ClearBufferAsync(bufferName, ct).ConfigureAwait(false);
        await StartScanAsync(ct).ConfigureAwait(false);

        int lastReadIndex = 0;

        while (!ct.IsCancellationRequested)
        {
            var status = await GetScanStatusAsync(ct).ConfigureAwait(false);
            int actualCount = await _client.Trace.GetActualCountAsync(bufferName, ct).ConfigureAwait(false);

            if (actualCount > lastReadIndex)
            {
                var chunk = await _client.Trace.GetReadingsAsync(
                    lastReadIndex + 1,
                    actualCount,
                    bufferName,
                    ReadingElements.StandardScan,
                    ct).ConfigureAwait(false);

                foreach (var reading in chunk)
                {
                    yield return reading;
                }

                lastReadIndex = actualCount;
            }

            if (status.State is ScanState.Success or ScanState.Aborted or ScanState.Failed)
            {
                // Final check for any remaining points written right as the scan finished
                int finalCount = await _client.Trace.GetActualCountAsync(bufferName, ct).ConfigureAwait(false);
                if (finalCount > lastReadIndex)
                {
                    var remaining = await _client.Trace.GetReadingsAsync(
                        lastReadIndex + 1,
                        finalCount,
                        bufferName,
                        ReadingElements.StandardScan,
                        ct).ConfigureAwait(false);

                    foreach (var reading in remaining)
                    {
                        yield return reading;
                    }
                }
                yield break;
            }

            await Task.Delay(interval, ct).ConfigureAwait(false);
        }
    }
}

#endregion

#region Trace (Reading Buffer) Subsystem

public sealed class TraceSubsystem
{
    private readonly Daq6510Client _client;

    internal TraceSubsystem(Daq6510Client client) => _client = client;

    public Task CreateBufferAsync(
        string bufferName,
        int capacity,
        BufferStyle style = BufferStyle.Standard,
        CancellationToken ct = default)
    {
        string styleStr = style switch
        {
            BufferStyle.Compact => "COMPact",
            BufferStyle.Standard => "STANdard",
            BufferStyle.Full => "FULL",
            BufferStyle.Writable => "WRITable",
            BufferStyle.FullWritable => "FULLWRITable",
            _ => "STANdard"
        };
        return _client.SendCommandAsync($":TRACe:MAKE \"{bufferName}\", {capacity}, {styleStr}", ct);
    }

    public Task ClearBufferAsync(string bufferName = "defbuffer1", CancellationToken ct = default)
        => _client.SendCommandAsync($":TRACe:CLEar \"{bufferName}\"", ct);

    public Task DeleteBufferAsync(string bufferName, CancellationToken ct = default)
        => _client.SendCommandAsync($":TRACe:DELete \"{bufferName}\"", ct);

    public Task SetFillModeAsync(
        BufferFillMode mode,
        string bufferName = "defbuffer1",
        CancellationToken ct = default)
    {
        string modeStr = mode == BufferFillMode.Continuous ? "CONTinuous" : "ONCE";
        return _client.SendCommandAsync($":TRACe:FILL:MODE {modeStr}, \"{bufferName}\"", ct);
    }

    public async Task<int> GetActualCountAsync(string bufferName = "defbuffer1", CancellationToken ct = default)
    {
        string raw = await _client.QueryRawAsync($":TRACe:ACTual? \"{bufferName}\"", ct).ConfigureAwait(false);
        return int.Parse(raw.Trim(), CultureInfo.InvariantCulture);
    }

    public async Task<BufferStatistics> GetStatisticsAsync(string bufferName = "defbuffer1", CancellationToken ct = default)
    {
        string avgRaw = await _client.QueryRawAsync($":TRACe:STATistics:AVERage? \"{bufferName}\"", ct).ConfigureAwait(false);
        string minRaw = await _client.QueryRawAsync($":TRACe:STATistics:MINimum? \"{bufferName}\"", ct).ConfigureAwait(false);
        string maxRaw = await _client.QueryRawAsync($":TRACe:STATistics:MAXimum? \"{bufferName}\"", ct).ConfigureAwait(false);
        string pkRaw  = await _client.QueryRawAsync($":TRACe:STATistics:PK2Pk? \"{bufferName}\"", ct).ConfigureAwait(false);
        string stdRaw = await _client.QueryRawAsync($":TRACe:STATistics:STDDev? \"{bufferName}\"", ct).ConfigureAwait(false);

        return new BufferStatistics(
            double.Parse(avgRaw.Trim(), CultureInfo.InvariantCulture),
            double.Parse(minRaw.Trim(), CultureInfo.InvariantCulture),
            double.Parse(maxRaw.Trim(), CultureInfo.InvariantCulture),
            double.Parse(pkRaw.Trim(), CultureInfo.InvariantCulture),
            double.Parse(stdRaw.Trim(), CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Reads structured multi-element data from a trace buffer (1-based indices, inclusive).
    /// </summary>
    public async Task<IReadOnlyList<DaqReading>> GetReadingsAsync(
        int startIndex,
        int endIndex,
        string bufferName = "defbuffer1",
        ReadingElements elements = ReadingElements.StandardScan,
        CancellationToken ct = default)
    {
        if (startIndex < 1 || endIndex < startIndex)
            throw new ArgumentOutOfRangeException(nameof(startIndex), "Indices must be 1-based and endIndex >= startIndex.");

        var elementTokens = new List<string>();
        if (elements.HasFlag(ReadingElements.Reading)) elementTokens.Add("READing");
        if (elements.HasFlag(ReadingElements.Channel)) elementTokens.Add("CHANnel");
        if (elements.HasFlag(ReadingElements.RelativeTime)) elementTokens.Add("RELative");
        if (elements.HasFlag(ReadingElements.Timestamp)) elementTokens.Add("TSTamp");
        if (elements.HasFlag(ReadingElements.Unit)) elementTokens.Add("UNIT");
        if (elements.HasFlag(ReadingElements.Status)) elementTokens.Add("STATus");

        if (elementTokens.Count == 0)
            elementTokens.Add("READing");

        string elementsArg = string.Join(", ", elementTokens);
        string cmd = $":TRACe:DATA? {startIndex}, {endIndex}, \"{bufferName}\", {elementsArg}";
        string raw = await _client.QueryRawAsync(cmd, ct).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(raw))
            return Array.Empty<DaqReading>();

        string[] fields = raw.Split(',', StringSplitOptions.TrimEntries);
        int stride = elementTokens.Count;
        int recordCount = fields.Length / stride;
        var results = new List<DaqReading>(recordCount);

        for (int i = 0; i < recordCount; i++)
        {
            int offset = i * stride;
            double val = double.NaN;
            string? ch = null;
            double? relTime = null;
            string? tstamp = null;
            string? unit = null;
            ReadingStatusFlags status = ReadingStatusFlags.None;

            for (int e = 0; e < stride; e++)
            {
                string tokenType = elementTokens[e];
                string fieldVal = fields[offset + e];

                switch (tokenType)
                {
                    case "READing":
                        double.TryParse(fieldVal, NumberStyles.Float, CultureInfo.InvariantCulture, out val);
                        break;
                    case "CHANnel":
                        ch = fieldVal;
                        break;
                    case "RELative":
                        if (double.TryParse(fieldVal, NumberStyles.Float, CultureInfo.InvariantCulture, out double rt))
                            relTime = rt;
                        break;
                    case "TSTamp":
                        tstamp = fieldVal;
                        break;
                    case "UNIT":
                        unit = fieldVal;
                        break;
                    case "STATus":
                        if (int.TryParse(fieldVal, NumberStyles.Integer, CultureInfo.InvariantCulture, out int st))
                            status = (ReadingStatusFlags)st;
                        break;
                }
            }

            results.Add(new DaqReading(val, ch, relTime, tstamp, unit, status));
        }

        return results;
    }

    /// <summary>
    /// High-speed IEEE 754 binary transfer of reading values from the specified buffer.
    /// Automatically switches FORMat:DATA to REAL (64-bit) or SREAL (32-bit) and restores ASCII afterward.
    /// </summary>
    public async Task<double[]> GetBinaryValuesAsync(
        int startIndex,
        int endIndex,
        string bufferName = "defbuffer1",
        bool useSinglePrecision = false,
        CancellationToken ct = default)
    {
        await _client.System.SetDataFormatAsync(
            useSinglePrecision ? DataFormat.SingleReal32 : DataFormat.Real64,
            ByteOrder.Swapped,
            ct).ConfigureAwait(false);

        try
        {
            string cmd = $":TRACe:DATA? {startIndex}, {endIndex}, \"{bufferName}\", READing";
            return await _client.Transport.QueryBinaryDoublesAsync(
                cmd,
                isSinglePrecision: useSinglePrecision,
                isBigEndian: false,
                cancellationToken: ct).ConfigureAwait(false);
        }
        finally
        {
            await _client.System.SetDataFormatAsync(DataFormat.Ascii, ByteOrder.Swapped, ct).ConfigureAwait(false);
        }
    }
}

#endregion

#region Trigger Flow Block Subsystem

public sealed class TriggerSubsystem
{
    private readonly Daq6510Client _client;

    internal TriggerSubsystem(Daq6510Client client) => _client = client;

    public Task ClearTriggerModelAsync(CancellationToken ct = default)
        => _client.SendCommandAsync(":TRIGger:LOAD \"Empty\"", ct);

    public Task AddBlockBufferClearAsync(int blockNumber, string bufferName = "defbuffer1", CancellationToken ct = default)
        => _client.SendCommandAsync($":TRIGger:BLOCk:BUFFer:CLEar {blockNumber}, \"{bufferName}\"", ct);

    public Task AddBlockMeasureDigitizeAsync(
        int blockNumber,
        string bufferName = "defbuffer1",
        int count = 1,
        CancellationToken ct = default)
        => _client.SendCommandAsync($":TRIGger:BLOCk:MDIGitize {blockNumber}, \"{bufferName}\", {count}", ct);

    public Task AddBlockDelayConstantAsync(int blockNumber, double delaySeconds, CancellationToken ct = default)
        => _client.SendCommandAsync($":TRIGger:BLOCk:DELay:CONStant {blockNumber}, {ScpiSyntax.FormatDouble(delaySeconds)}", ct);

    public Task AddBlockWaitAsync(
        int blockNumber,
        TriggerEvent triggerEvent,
        TriggerClearOption clearOption = TriggerClearOption.Enter,
        CancellationToken ct = default)
    {
        string clr = clearOption == TriggerClearOption.Enter ? "ENTer" : "NEVer";
        return _client.SendCommandAsync($":TRIGger:BLOCk:WAIT {blockNumber}, {triggerEvent.ToScpiEventName()}, {clr}", ct);
    }

    public Task AddBlockBranchCounterAsync(
        int blockNumber,
        int targetCount,
        int branchToBlock,
        CancellationToken ct = default)
        => _client.SendCommandAsync($":TRIGger:BLOCk:BRANch:COUNter {blockNumber}, {targetCount}, {branchToBlock}", ct);

    public Task AddBlockBranchAlwaysAsync(int blockNumber, int branchToBlock, CancellationToken ct = default)
        => _client.SendCommandAsync($":TRIGger:BLOCk:BRANch:ALWays {blockNumber}, {branchToBlock}", ct);

    public Task InitiateTriggerModelAsync(CancellationToken ct = default)
        => _client.SendCommandAsync(":INITiate", ct);

    public Task AbortTriggerModelAsync(CancellationToken ct = default)
        => _client.SendCommandAsync(":ABORt", ct);
}

#endregion