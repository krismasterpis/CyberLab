using System;
using System.Threading;
using System.Threading.Tasks;

namespace Keithley.DAQ6510;

public sealed class Daq6510Client : IAsyncDisposable, IDisposable
{
    public DaqTcpTransport Transport { get; }

    public SystemSubsystem System { get; }
    public SenseSubsystem Sense { get; }
    public DigitizeSubsystem Digitize { get; }
    public RouteSubsystem Route { get; }
    public ScanSubsystem Scan { get; }
    public TraceSubsystem Trace { get; }
    public TriggerSubsystem Trigger { get; }

    /// <summary>
    /// When enabled, automatically queries :SYSTem:ERRor:NEXT? after every SCPI command write
    /// and throws a <see cref="DaqException"/> if the instrument reports a syntax or settings conflict.
    /// </summary>
    public bool VerifyErrorsAfterEveryCommand { get; set; }

    public Daq6510Client(
        string host,
        int port = DaqTcpTransport.DefaultScpiPort,
        int deadSocketPort = DaqTcpTransport.DefaultDeadSocketPort)
    {
        Transport = new DaqTcpTransport(host, port, deadSocketPort);
        System = new SystemSubsystem(this);
        Sense = new SenseSubsystem(this);
        Digitize = new DigitizeSubsystem(this);
        Route = new RouteSubsystem(this);
        Scan = new ScanSubsystem(this);
        Trace = new TraceSubsystem(this);
        Trigger = new TriggerSubsystem(this);
    }

    /// <summary>
    /// Connects to the DAQ6510, optionally terminates dead sockets on port 5030,
    /// verifies SCPI language mode, and clears the error queue.
    /// </summary>
    public async Task ConnectAndInitializeAsync(
        bool forceResetDeadSockets = false,
        bool resetInstrument = false,
        CancellationToken ct = default)
    {
        await Transport.ConnectAsync(forceResetDeadSockets, ct).ConfigureAwait(false);

        // Ensure instrument is in SCPI command mode
        var lang = await System.GetCommandLanguageAsync(ct).ConfigureAwait(false);
        if (lang != CommandLanguage.Scpi)
        {
            await System.SetCommandLanguageAsync(CommandLanguage.Scpi, ct).ConfigureAwait(false);
            // Reconnect after language switch as recommended by Keithley manual
            await Task.Delay(500, ct).ConfigureAwait(false);
            await Transport.ConnectAsync(forceResetDeadSockets: false, ct).ConfigureAwait(false);
        }

        if (resetInstrument)
        {
            await System.ResetAsync(ct).ConfigureAwait(false);
        }

        await System.ClearStatusAsync(ct).ConfigureAwait(false);
        await System.ClearErrorQueueAsync(ct).ConfigureAwait(false);
    }

    public async Task SendCommandAsync(string command, CancellationToken ct = default)
    {
        await Transport.WriteAsync(command, ct).ConfigureAwait(false);

        if (VerifyErrorsAfterEveryCommand)
        {
            var err = await System.GetNextErrorAsync(ct).ConfigureAwait(false);
            if (!err.IsNoError)
            {
                throw new DaqException(err.Code, $"{err.Message} (Triggered by command: '{command}')");
            }
        }
    }

    public async Task<string> QueryRawAsync(string query, CancellationToken ct = default)
    {
        string response = await Transport.QueryStringAsync(query, ct).ConfigureAwait(false);

        if (VerifyErrorsAfterEveryCommand)
        {
            var err = await System.GetNextErrorAsync(ct).ConfigureAwait(false);
            if (!err.IsNoError)
            {
                throw new DaqException(err.Code, $"{err.Message} (Triggered by query: '{query}')");
            }
        }

        return response;
    }

    public void Dispose() => Transport.Dispose();

    public ValueTask DisposeAsync() => Transport.DisposeAsync();
}