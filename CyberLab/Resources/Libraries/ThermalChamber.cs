using System;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

public enum Mb1OperatingMode
{
    Unknown = 0,
    Basic = 0x1000,
    Manual = 0x0800,
    Auto = 0x0400
}

public class ThermalChamber : IDisposable
{
    private const ushort ADDR_TEMP_PROCESS_VALUE = 0x11A9;
    private const ushort ADDR_HUMID_PROCESS_VALUE = 0x11CD;
    private const ushort ADDR_TEMP_SETPOINT_BASIC = 0x1581;
    private const ushort ADDR_HUMID_SETPOINT_BASIC = 0x1583;
    private const ushort ADDR_MODE_READ_WRITE = 0x1A22;

    private readonly string _host;
    private readonly int _port;
    private readonly byte _slaveId;

    private TcpClient _client;
    private NetworkStream _stream;

    public bool IsConnected = false;
    private bool isStreamReady = true;
    public ThermalChamber(string host, int port = 10001, byte slaveId = 1)
    {
        _host = host;
        _port = port;
        _slaveId = slaveId;
        _client = new TcpClient();
    }
    public bool checkIfConnected()
    {
        if (_client.Connected)
        {
            return true;
        }
        return false;
    }
    public void Connect()
    {
        if (_client.Connected) return;
        try
        {
            _client.Connect(_host, _port);
            _stream = _client.GetStream();
            _stream.ReadTimeout = 2000;
            IsConnected = true;
        }
        catch (Exception ex)
        {
            throw new Exception($"Nie można połączyć się z {_host}:{_port}. Błąd: {ex.Message}", ex);
        }
    }
    public void Disconnect()
    {
        _stream?.Close();
        _client?.Close();
    }
    public double ReadTemperature()
    {
        return Math.Round((double)ReadHoldingRegistersFloat(ADDR_TEMP_PROCESS_VALUE), 2);
    }
    public double ReadSetPoint()
    {
        return (double)ReadHoldingRegistersFloat(ADDR_TEMP_SETPOINT_BASIC);
    }
    public double ReadHumidity()
    {
        return (double)ReadHoldingRegistersFloat(ADDR_HUMID_PROCESS_VALUE);
    }
    public Mb1OperatingMode ReadOperatingMode()
    {
        ushort modeValue = ReadSingleRegister(ADDR_MODE_READ_WRITE);

        if ((modeValue & (ushort)Mb1OperatingMode.Basic) != 0)
            return Mb1OperatingMode.Basic;
        if ((modeValue & (ushort)Mb1OperatingMode.Manual) != 0)
            return Mb1OperatingMode.Manual;
        if ((modeValue & (ushort)Mb1OperatingMode.Auto) != 0)
            return Mb1OperatingMode.Auto;

        return Mb1OperatingMode.Unknown;
    }

    public void SetOperatingMode(Mb1OperatingMode mode)
    {
        if (mode == Mb1OperatingMode.Unknown)
            throw new ArgumentException("Nie można ustawić nieznanego trybu pracy.");

        WriteSingleRegister(ADDR_MODE_READ_WRITE, (ushort)mode);
    }
    public void SetTemperature(double temperature)
    {
        WriteMultipleRegistersFloat(ADDR_TEMP_SETPOINT_BASIC, (float)temperature);
    }
    public void SetHumidity(double humidity)
    {
        WriteMultipleRegistersFloat(ADDR_HUMID_SETPOINT_BASIC, (float)humidity);
    }
    private ushort ReadSingleRegister(ushort address)
    {
        byte[] request = BuildRequestFrame(_slaveId, 0x03, address, 1);
        byte[] response = ExecuteRequest(request);

        if (response.Length != 7 || response[0] != _slaveId || response[1] != 0x03 || response[2] != 2)
        {
            throw new Exception("Otrzymano nieprawidłową odpowiedź od urządzenia przy odczycie rejestru.");
        }

        return (ushort)((response[3] << 8) | response[4]);
    }
    private float ReadHoldingRegistersFloat(ushort startAddress)
    {
        byte[] request = BuildRequestFrame(_slaveId, 0x03, startAddress, 2);
        byte[] response = ExecuteRequest(request);

        if (response.Length != 9 || response[0] != _slaveId || response[1] != 0x03 || response[2] != 4)
        {
            throw new Exception("Otrzymano nieprawidłową odpowiedź od urządzenia.");
        }

        byte[] dataBytes = new byte[4];
        Array.Copy(response, 3, dataBytes, 0, 4);

        return ToCSharpFloat(dataBytes);
    }
    private void WriteMultipleRegistersFloat(ushort startAddress, float value)
    {
        byte[] floatBytes = ToModbusFloat(value);

        byte[] data = new byte[7 + floatBytes.Length];
        data[0] = _slaveId;
        data[1] = 0x10;
        data[2] = (byte)(startAddress >> 8);
        data[3] = (byte)(startAddress & 0xFF);
        data[4] = 0x00;
        data[5] = 0x02;
        data[6] = 0x04;
        Array.Copy(floatBytes, 0, data, 7, floatBytes.Length);

        byte[] request = AddCrc(data);
        byte[] response = ExecuteRequest(request);

        if (response.Length != 8 || response[0] != _slaveId || response[1] != 0x10)
        {
            throw new Exception("Otrzymano nieprawidłową odpowiedź po zapisie danych.");
        }
    }
    private void WriteSingleRegister(ushort address, ushort value)
    {
        byte[] data = new byte[6];
        data[0] = _slaveId;
        data[1] = 0x06;
        data[2] = (byte)(address >> 8);
        data[3] = (byte)(address & 0xFF);
        data[4] = (byte)(value >> 8);
        data[5] = (byte)(value & 0xFF);

        byte[] request = AddCrc(data);
        byte[] response = ExecuteRequest(request);

        if (!request.SequenceEqual(response))
        {
            throw new Exception("Otrzymano nieprawidłową odpowiedź po zapisie rejestru.");
        }
    }
    private byte[] ExecuteRequest(byte[] requestFrame)
    {
        if (!_client.Connected)
        {
            throw new InvalidOperationException("Klient nie jest połączony. Wywołaj Connect() przed wysłaniem zapytania.");
        }

        while (!isStreamReady) ;
        isStreamReady = false;
        byte[] buffer = new byte[256];
        int bytesRead = 0;
        try
        {
            _stream.Write(requestFrame, 0, requestFrame.Length);
            Thread.Sleep(100);

            bytesRead = _stream.Read(buffer, 0, buffer.Length);
        }
        catch (Exception e)
        {
            isStreamReady = true;
            throw new Exception("Error occured in socket.");
        }
        finally
        {
            isStreamReady = true;
        }
        byte[] response = new byte[bytesRead];
        Array.Copy(buffer, 0, response, 0, bytesRead);
        if (response.Length < 5)
        {
            throw new Exception("Odpowiedź od urządzenia jest za krótka.");
        }
        byte[] responseData = new byte[bytesRead - 2];
        Array.Copy(response, 0, responseData, 0, bytesRead - 2);
        byte[] crc = CalculateCrc(responseData);
        if (crc[0] != response[bytesRead - 2] || crc[1] != response[bytesRead - 1])
        {
            throw new Exception("Błąd sumy kontrolnej CRC w odpowiedzi.");
        }

        if ((response[1] & 0x80) > 0)
        {
            throw new Exception($"Urządzenie zgłosiło błąd Modbus. Kod funkcji: 0x{response[1]:X2}, kod błędu: 0x{response[2]:X2}");
        }

        return response;
    }
    private byte[] BuildRequestFrame(byte slaveId, byte functionCode, ushort startAddress, ushort count)
    {
        byte[] frame = new byte[6];
        frame[0] = slaveId;
        frame[1] = functionCode;
        frame[2] = (byte)(startAddress >> 8);
        frame[3] = (byte)(startAddress & 0xFF);
        frame[4] = (byte)(count >> 8);
        frame[5] = (byte)(count & 0xFF);
        return AddCrc(frame);
    }
    private byte[] AddCrc(byte[] data)
    {
        byte[] crc = CalculateCrc(data);
        byte[] result = new byte[data.Length + 2];
        Array.Copy(data, 0, result, 0, data.Length);
        result[data.Length] = crc[0];
        result[data.Length + 1] = crc[1];
        return result;
    }
    private static byte[] CalculateCrc(byte[] data)
    {
        ushort crc = 0xFFFF;
        foreach (byte b in data)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++)
            {
                bool isLsbSet = (crc & 0x0001) != 0;
                crc >>= 1;
                if (isLsbSet)
                {
                    crc ^= 0xA001;
                }
            }
        }
        return new byte[] { (byte)(crc & 0xFF), (byte)(crc >> 8) };
    }
    private float ToCSharpFloat(byte[] modbusBytes)
    {
        if (modbusBytes.Length != 4) throw new ArgumentException("Tablica musi mieć 4 bajty.");
        byte[] csharpBytes = { modbusBytes[2], modbusBytes[3], modbusBytes[0], modbusBytes[1] };
        Array.Reverse(csharpBytes);
        return BitConverter.ToSingle(csharpBytes, 0);
    }
    private byte[] ToModbusFloat(float value)
    {
        byte[] csharpBytes = BitConverter.GetBytes(value);
        Array.Reverse(csharpBytes);
        if (csharpBytes.Length != 4) throw new ArgumentException("Błąd konwersji float na bajty.");
        byte[] modbusBytes = { csharpBytes[2], csharpBytes[3], csharpBytes[0], csharpBytes[1] };
        return modbusBytes;
    }
    public void Dispose()
    {
        Disconnect();
        _client?.Dispose();
    }
}