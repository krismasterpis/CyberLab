using System;
using System.Buffers.Binary;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Keithley.DAQ6510;

public sealed class DaqTcpTransport : IAsyncDisposable, IDisposable
{
    public const int DefaultScpiPort = 5025;
    public const int DefaultDeadSocketPort = 5030;

    private readonly SemaphoreSlim _ioLock = new(1, 1);
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private bool _disposed;

    public string Host { get; }
    public int Port { get; }
    public int DeadSocketPort { get; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Optional hook for logging every raw TX and RX command.
    /// </summary>
    public Action<string>? DiagnosticLogger { get; set; }

    public bool IsConnected => _tcpClient?.Connected == true && _stream != null;

    public DaqTcpTransport(
        string host,
        int port = DefaultScpiPort,
        int deadSocketPort = DefaultDeadSocketPort)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("Host IP or hostname is required.", nameof(host));

        Host = host;
        Port = port;
        DeadSocketPort = deadSocketPort;
    }

    /// <summary>
    /// Connects to the DAQ6510 over TCP/IP port 5025.
    /// If forceResetDeadSockets is true, triggers port 5030 first to drop any orphaned sessions.
    /// </summary>
    public async Task ConnectAsync(
        bool forceResetDeadSockets = false,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _ioLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (forceResetDeadSockets)
            {
                await TerminateDeadSocketsInternalAsync(cancellationToken).ConfigureAwait(false);
            }

            CloseInternal();

            _tcpClient = new TcpClient
            {
                NoDelay = true,
                ReceiveBufferSize = 65536,
                SendBufferSize = 16384
            };

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(Timeout);

            await _tcpClient.ConnectAsync(Host, Port, cts.Token).ConfigureAwait(false);
            _stream = _tcpClient.GetStream();
            DiagnosticLogger?.Invoke($"[CONNECTED] {Host}:{Port}");
        }
        finally
        {
            _ioLock.Release();
        }
    }

    /// <summary>
    /// Opens and immediately closes Keithley's Dead Socket Termination (DST) port (default 5030),
    /// forcing the DAQ6510 to release all hung TCP connections on port 5025.
    /// </summary>
    public async Task TerminateDeadSocketsAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _ioLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await TerminateDeadSocketsInternalAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _ioLock.Release();
        }
    }

    private async Task TerminateDeadSocketsInternalAsync(CancellationToken cancellationToken)
    {
        DiagnosticLogger?.Invoke($"[DST] Triggering Dead Socket Termination on {Host}:{DeadSocketPort}");
        using var dstClient = new TcpClient();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(3));

        await dstClient.ConnectAsync(Host, DeadSocketPort, cts.Token).ConfigureAwait(false);
        dstClient.Close();
        // Give the DAQ6510 network stack a brief moment to tear down active sockets
        await Task.Delay(250, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a newline-terminated SCPI/TSP command without waiting for a response.
    /// </summary>
    public async Task WriteAsync(string command, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _ioLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await WriteUnlockedAsync(command, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _ioLock.Release();
        }
    }

    /// <summary>
    /// Sends a command and reads a newline-terminated ASCII response string.
    /// </summary>
    public async Task<string> QueryStringAsync(
        string command,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _ioLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await WriteUnlockedAsync(command, cancellationToken).ConfigureAwait(false);
            var response = await ReadLineUnlockedAsync(cancellationToken).ConfigureAwait(false);
            DiagnosticLogger?.Invoke($"[RX] {response}");
            return response;
        }
        finally
        {
            _ioLock.Release();
        }
    }

    /// <summary>
    /// Sends a query and reads an IEEE 488.2 Definite-Length Arbitrary Binary Block (#n...).
    /// Used when FORMat:DATA is set to REAL (64-bit double) or SREAL (32-bit float).
    /// </summary>
    public async Task<byte[]> QueryBinaryBlockAsync(
        string command,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _ioLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await WriteUnlockedAsync(command, cancellationToken).ConfigureAwait(false);
            return await ReadBinaryBlockUnlockedAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _ioLock.Release();
        }
    }

    /// <summary>
    /// Queries an IEEE 488.2 binary block and decodes it directly into an array of doubles.
    /// </summary>
    public async Task<double[]> QueryBinaryDoublesAsync(
        string command,
        bool isSinglePrecision = false,
        bool isBigEndian = false,
        CancellationToken cancellationToken = default)
    {
        byte[] raw = await QueryBinaryBlockAsync(command, cancellationToken).ConfigureAwait(false);

        if (isSinglePrecision)
        {
            int count = raw.Length / sizeof(float);
            var result = new double[count];
            for (int i = 0; i < count; i++)
            {
                ReadOnlySpan<byte> slice = raw.AsSpan(i * sizeof(float), sizeof(float));
                float val = isBigEndian
                    ? BinaryPrimitives.ReadSingleBigEndian(slice)
                    : BinaryPrimitives.ReadSingleLittleEndian(slice);
                result[i] = val;
            }
            return result;
        }
        else
        {
            int count = raw.Length / sizeof(double);
            var result = new double[count];
            for (int i = 0; i < count; i++)
            {
                ReadOnlySpan<byte> slice = raw.AsSpan(i * sizeof(double), sizeof(double));
                result[i] = isBigEndian
                    ? BinaryPrimitives.ReadDoubleBigEndian(slice)
                    : BinaryPrimitives.ReadDoubleLittleEndian(slice);
            }
            return result;
        }
    }

    private async Task WriteUnlockedAsync(string command, CancellationToken cancellationToken)
    {
        EnsureConnected();
        DiagnosticLogger?.Invoke($"[TX] {command}");

        string payload = command.EndsWith('\n') ? command : command + "\n";
        byte[] bytes = Encoding.ASCII.GetBytes(payload);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(Timeout);

        await _stream!.WriteAsync(bytes, cts.Token).ConfigureAwait(false);
        await _stream.FlushAsync(cts.Token).ConfigureAwait(false);
    }

    private async Task<string> ReadLineUnlockedAsync(CancellationToken cancellationToken)
    {
        EnsureConnected();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(Timeout);

        using var ms = new MemoryStream(256);
        byte[] singleByte = new byte[1];

        while (true)
        {
            int read = await _stream!.ReadAsync(singleByte.AsMemory(0, 1), cts.Token).ConfigureAwait(false);
            if (read == 0)
                throw new IOException("Remote instrument closed the TCP connection unexpectedly.");

            byte b = singleByte[0];
            if (b == (byte)'\n')
                break;

            if (b != (byte)'\r')
                ms.WriteByte(b);
        }

        return Encoding.ASCII.GetString(ms.GetBuffer(), 0, (int)ms.Length);
    }

    private async Task<byte[]> ReadBinaryBlockUnlockedAsync(CancellationToken cancellationToken)
    {
        EnsureConnected();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(Timeout);

        // 1. Read until '#' header
        byte[] headerByte = new byte[1];
        await ReadExactBytesAsync(_stream!, headerByte, cts.Token).ConfigureAwait(false);
        if (headerByte[0] != (byte)'#')
            throw new DaqException($"Expected IEEE 488.2 binary block header '#', but received '{(char)headerByte[0]}'.");

        // 2. Read number of length digits
        await ReadExactBytesAsync(_stream!, headerByte, cts.Token).ConfigureAwait(false);
        int numDigits = headerByte[0] - '0';
        if (numDigits is < 1 or > 9)
            throw new DaqException($"Invalid IEEE 488.2 block length digit count: {numDigits}.");

        // 3. Read byte count ASCII string
        byte[] lengthBytes = new byte[numDigits];
        await ReadExactBytesAsync(_stream!, lengthBytes, cts.Token).ConfigureAwait(false);
        string lengthStr = Encoding.ASCII.GetString(lengthBytes);
        if (!int.TryParse(lengthStr, out int payloadLength) || payloadLength < 0)
            throw new DaqException($"Invalid IEEE 488.2 block byte count: '{lengthStr}'.");

        // 4. Read exact binary payload
        byte[] payload = new byte[payloadLength];
        if (payloadLength > 0)
        {
            await ReadExactBytesAsync(_stream!, payload, cts.Token).ConfigureAwait(false);
        }

        // 5. Consume trailing termination character (\n or \r\n)
        await ReadExactBytesAsync(_stream!, headerByte, cts.Token).ConfigureAwait(false);
        if (headerByte[0] == (byte)'\r')
        {
            await ReadExactBytesAsync(_stream!, headerByte, cts.Token).ConfigureAwait(false);
        }

        DiagnosticLogger?.Invoke($"[RX BINARY] {payloadLength} bytes");
        return payload;
    }

    private static async Task ReadExactBytesAsync(
        Stream stream,
        Memory<byte> buffer,
        CancellationToken cancellationToken)
    {
        int totalRead = 0;
        while (totalRead < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer[totalRead..], cancellationToken).ConfigureAwait(false);
            if (read == 0)
                throw new IOException("Connection closed prematurely while reading binary block.");
            totalRead += read;
        }
    }

    private void EnsureConnected()
    {
        if (!IsConnected)
            throw new InvalidOperationException("DAQ6510 TCP transport is not connected.");
    }

    private void CloseInternal()
    {
        try { _stream?.Dispose(); } catch { /* ignore */ }
        try { _tcpClient?.Close(); } catch { /* ignore */ }
        _stream = null;
        _tcpClient = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        CloseInternal();
        _ioLock.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await _ioLock.WaitAsync().ConfigureAwait(false);
        try
        {
            CloseInternal();
        }
        finally
        {
            _ioLock.Release();
            _ioLock.Dispose();
        }
    }
}