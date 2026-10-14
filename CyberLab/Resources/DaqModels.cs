using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Keithley.DAQ6510;

#region Exceptions

public sealed class DaqException : Exception
{
    public int ErrorCode { get; }
    public string ScpiMessage { get; }

    public DaqException(string message) : base(message)
    {
        ScpiMessage = message;
    }

    public DaqException(int errorCode, string scpiMessage)
        : base($"DAQ6510 Error [{errorCode}]: {scpiMessage}")
    {
        ErrorCode = errorCode;
        ScpiMessage = scpiMessage;
    }
}

#endregion

#region Enums

public enum CommandLanguage
{
    Scpi,
    Tsp
}

public enum DmmFunction
{
    VoltageDc,
    VoltageAc,
    CurrentDc,
    CurrentAc,
    Resistance2Wire,
    Resistance4Wire,
    Temperature,
    Frequency,
    Period,
    Capacitance,
    Diode,
    Continuity
}

public enum DigitizeFunction
{
    None,
    Voltage,
    Current
}

public enum DataFormat
{
    Ascii,
    Real64,
    SingleReal32
}

public enum ByteOrder
{
    Normal,
    Swapped
}

public enum AutoZeroMode
{
    Off,
    On,
    Once
}

public enum FilterType
{
    Moving,
    Repeat
}

public enum TemperatureTransducer
{
    Thermocouple,
    Thermistor,
    Rtd2Wire,
    Rtd3Wire,
    Rtd4Wire
}

public enum ThermocoupleType
{
    B, E, J, K, N, R, S, T
}

public enum ReferenceJunctionType
{
    Simulated,
    Internal,
    External
}

public enum RtdType
{
    Pt100,
    Pt385,
    Pt3916,
    D100,
    F100,
    User
}

public enum ThermistorType
{
    Ohm2252 = 2252,
    Ohm5000 = 5000,
    Ohm10000 = 10000
}

public enum BufferStyle
{
    Compact,
    Standard,
    Full,
    Writable,
    FullWritable
}

public enum BufferFillMode
{
    Continuous,
    Once
}

[Flags]
public enum ReadingElements
{
    None = 0,
    Reading = 1 << 0,
    Channel = 1 << 1,
    RelativeTime = 1 << 2,
    Timestamp = 1 << 3,
    Unit = 1 << 4,
    Status = 1 << 5,
    StandardScan = Reading | Channel | RelativeTime | Unit | Status
}

[Flags]
public enum ReadingStatusFlags
{
    None = 0,
    Questionable = 1,
    Origin = 6,
    FrontTerminals = 8,
    Limit2Low = 16,
    Limit2High = 32,
    Limit1Low = 64,
    Limit1High = 128,
    FirstReadingInGroup = 256
}

public enum ScanState
{
    Unknown,
    Empty,
    Building,
    Running,
    Aborted,
    Failed,
    Success
}

public enum TriggerEvent
{
    None,
    Display,
    Notify1,
    Notify2,
    Notify3,
    Notify4,
    Notify5,
    Notify6,
    Notify7,
    Notify8,
    Command,
    External,
    Timer1,
    Timer2,
    Timer3,
    Timer4,
    Lan1,
    Lan2,
    Lan3,
    Lan4,
    Blender1,
    Blender2,
    DigitalIo1,
    DigitalIo2,
    DigitalIo3,
    DigitalIo4,
    DigitalIo5,
    DigitalIo6,
    AnalogTrigger
}

public enum TriggerClearOption
{
    Enter,
    Never
}

#endregion

#region Data Records

public sealed record InstrumentIdentity(
    string Manufacturer,
    string Model,
    string SerialNumber,
    string FirmwareVersion)
{
    public static InstrumentIdentity Parse(string idnResponse)
    {
        var parts = idnResponse.Trim().Split(',');
        return new InstrumentIdentity(
            parts.Length > 0 ? parts[0].Trim() : string.Empty,
            parts.Length > 1 ? parts[1].Trim() : string.Empty,
            parts.Length > 2 ? parts[2].Trim() : string.Empty,
            parts.Length > 3 ? parts[3].Trim() : string.Empty);
    }
}

public sealed record InstrumentError(int Code, string Message)
{
    public bool IsNoError => Code == 0;
}

public sealed record ScanStatusInfo(
    ScanState State,
    int CompletedScans,
    int CompletedSteps);

public sealed record BufferStatistics(
    double Average,
    double Minimum,
    double Maximum,
    double PeakToPeak,
    double StdDev);

public sealed record DaqReading(
    double Value,
    string? Channel = null,
    double? RelativeTimeSeconds = null,
    string? Timestamp = null,
    string? Unit = null,
    ReadingStatusFlags Status = ReadingStatusFlags.None);

#endregion

#region Strongly-Typed Channel List Builder

/// <summary>
/// Builds and validates Keithley SCPI channel lists such as (@101:120, 201, 205:210).
/// Slot 1 channels are 101..199; Slot 2 channels are 201..299.
/// </summary>
public sealed class ChannelList
{
    private readonly List<string> _segments = new();

    public ChannelList() { }

    public ChannelList(string rawChannelSpec)
    {
        if (string.IsNullOrWhiteSpace(rawChannelSpec))
            throw new ArgumentException("Channel specification cannot be empty.", nameof(rawChannelSpec));

        var cleaned = rawChannelSpec.Trim();
        if (cleaned.StartsWith("(@", StringComparison.Ordinal) && cleaned.EndsWith(')'))
        {
            cleaned = cleaned[2..^1];
        }
        _segments.Add(cleaned);
    }

    public static ChannelList Single(int slot, int channel)
        => new ChannelList().AddChannel(slot, channel);

    public static ChannelList Single(int threeDigitChannel)
        => new ChannelList().AddChannel(threeDigitChannel);

    public static ChannelList Range(int slot, int startChannel, int endChannel)
        => new ChannelList().AddRange(slot, startChannel, endChannel);

    public static ChannelList Range(int startThreeDigit, int endThreeDigit)
        => new ChannelList().AddRange(startThreeDigit, endThreeDigit);

    public ChannelList AddChannel(int slot, int channel)
    {
        ValidateSlotAndChannel(slot, channel);
        _segments.Add($"{slot}{channel:D2}");
        return this;
    }

    public ChannelList AddChannel(int threeDigitChannel)
    {
        int slot = threeDigitChannel / 100;
        int ch = threeDigitChannel % 100;
        return AddChannel(slot, ch);
    }

    public ChannelList AddRange(int slot, int startChannel, int endChannel)
    {
        ValidateSlotAndChannel(slot, startChannel);
        ValidateSlotAndChannel(slot, endChannel);
        if (endChannel < startChannel)
            throw new ArgumentOutOfRangeException(nameof(endChannel), "End channel must be >= start channel.");

        _segments.Add($"{slot}{startChannel:D2}:{slot}{endChannel:D2}");
        return this;
    }

    public ChannelList AddRange(int startThreeDigit, int endThreeDigit)
    {
        int startSlot = startThreeDigit / 100;
        int endSlot = endThreeDigit / 100;
        if (startSlot != endSlot)
            throw new ArgumentException("Channel range must reside within the same slot.");

        return AddRange(startSlot, startThreeDigit % 100, endThreeDigit % 100);
    }

    private static void ValidateSlotAndChannel(int slot, int channel)
    {
        if (slot is < 1 or > 2)
            throw new ArgumentOutOfRangeException(nameof(slot), "DAQ6510 only has Slot 1 and Slot 2.");
        if (channel is < 1 or > 99)
            throw new ArgumentOutOfRangeException(nameof(channel), "Channel number must be between 1 and 99.");
    }

    /// <summary>
    /// Formats as SCPI channel list: (@101:110,201)
    /// </summary>
    public override string ToString()
    {
        if (_segments.Count == 0)
            throw new InvalidOperationException("ChannelList is empty.");

        return $"(@{string.Join(",", _segments)})";
    }
}

#endregion

#region Internal SCPI Formatting Helpers

internal static class ScpiSyntax
{
    public static string FormatDouble(double value)
        => value.ToString("G15", CultureInfo.InvariantCulture);

    public static string FormatBool(bool state)
        => state ? "1" : "0";

    public static string ToScpiFunctionName(this DmmFunction func) => func switch
    {
        DmmFunction.VoltageDc => "VOLTage:DC",
        DmmFunction.VoltageAc => "VOLTage:AC",
        DmmFunction.CurrentDc => "CURRent:DC",
        DmmFunction.CurrentAc => "CURRent:AC",
        DmmFunction.Resistance2Wire => "RESistance",
        DmmFunction.Resistance4Wire => "FRESistance",
        DmmFunction.Temperature => "TEMPerature",
        DmmFunction.Frequency => "FREQuency",
        DmmFunction.Period => "PERiod",
        DmmFunction.Capacitance => "CAPacitance",
        DmmFunction.Diode => "DIODe",
        DmmFunction.Continuity => "CONTinuity",
        _ => throw new ArgumentOutOfRangeException(nameof(func))
    };

    public static string ToScpiQuotedFunction(this DmmFunction func)
        => $"\"{func.ToScpiFunctionName()}\"";

    public static string ToScpiDigitizeName(this DigitizeFunction func) => func switch
    {
        DigitizeFunction.None => "NONE",
        DigitizeFunction.Voltage => "VOLTage",
        DigitizeFunction.Current => "CURRent",
        _ => throw new ArgumentOutOfRangeException(nameof(func))
    };

    public static string ToScpiEventName(this TriggerEvent evt) => evt switch
    {
        TriggerEvent.None => "NONE",
        TriggerEvent.Display => "DISPlay",
        TriggerEvent.Notify1 => "NOTify1",
        TriggerEvent.Notify2 => "NOTify2",
        TriggerEvent.Notify3 => "NOTify3",
        TriggerEvent.Notify4 => "NOTify4",
        TriggerEvent.Notify5 => "NOTify5",
        TriggerEvent.Notify6 => "NOTify6",
        TriggerEvent.Notify7 => "NOTify7",
        TriggerEvent.Notify8 => "NOTify8",
        TriggerEvent.Command => "COMMand",
        TriggerEvent.External => "EXTernal",
        TriggerEvent.Timer1 => "TIMer1",
        TriggerEvent.Timer2 => "TIMer2",
        TriggerEvent.Timer3 => "TIMer3",
        TriggerEvent.Timer4 => "TIMer4",
        TriggerEvent.Lan1 => "LAN1",
        TriggerEvent.Lan2 => "LAN2",
        TriggerEvent.Lan3 => "LAN3",
        TriggerEvent.Lan4 => "LAN4",
        TriggerEvent.Blender1 => "BLENder1",
        TriggerEvent.Blender2 => "BLENder2",
        TriggerEvent.DigitalIo1 => "DIGio1",
        TriggerEvent.DigitalIo2 => "DIGio2",
        TriggerEvent.DigitalIo3 => "DIGio3",
        TriggerEvent.DigitalIo4 => "DIGio4",
        TriggerEvent.DigitalIo5 => "DIGio5",
        TriggerEvent.DigitalIo6 => "DIGio6",
        TriggerEvent.AnalogTrigger => "ATRig",
        _ => throw new ArgumentOutOfRangeException(nameof(evt))
    };
}

#endregion