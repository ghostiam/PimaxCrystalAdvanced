using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace TobiiAdvanced.BrokenEye;

public class Client : IDisposable
{
    private readonly ILogger _logger;
    private TcpClient? _client;
    private IPAddress? _ipAddress;
    private short _port;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public event Action<EyeData>? OnData;

    public Client(ILogger logger)
    {
        _logger = logger;
    }

    public bool Connect(string ip, int port)
    {
        _client = new TcpClient();
        _ipAddress = IPAddress.Parse(ip);
        _port = Convert.ToInt16(port);

        _logger.LogInformation($"Connecting to {ip}:{port}...");

        try
        {
            var result = _client.ConnectAsync(_ipAddress, _port).Wait(TimeSpan.FromSeconds(3));
            if (!result)
            {
                _logger.LogError("Failed to connect to server");
                return false;
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"Failed to connect to server ({e.Message})");
            return false;
        }

        _logger.LogInformation("Connected to server");

        Task.Run(HandleAsyncData, _cancellationTokenSource.Token);

        return true;
    }

    private async void HandleAsyncData()
    {
        var reconnectAttempts = 0;
        const int maxReconnectAttempts = 50;

        while (true)
        {
            // Try to reconnect every 5 seconds
            try
            {
                if (reconnectAttempts > maxReconnectAttempts)
                {
                    _logger.LogError(
                        $"Failed to reconnect to server after {maxReconnectAttempts} attempts, giving up :(");
                    return;
                }

                if (_client is not { Connected: true })
                {
                    _logger.LogInformation("Reconnecting to server...");

                    _client?.Close();
                    _client = new TcpClient();

                    reconnectAttempts++;

                    _client.ConnectAsync(_ipAddress!, _port).Wait(TimeSpan.FromSeconds(3));

                    _logger.LogInformation("Reconnected to server");

                    reconnectAttempts = 0;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed to reconnect to server, retrying in 5 seconds... ({e.Message})");
                await Task.Delay(TimeSpan.FromSeconds(5));
            }

            if (_client == null)
            {
                continue;
            }

            // Process stream
            try
            {
                var stream = _client.GetStream();
                stream.ReadTimeout = 10000;
                stream.WriteTimeout = 10000;

                // Request advanced data stream
                const byte requestId = 0x00;
                var request = new Memory<byte>(new[] { requestId });
                await stream.WriteAsync(request);

                while (true)
                {
                    await ReadData(stream, requestId);
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed to read data from server ({e.Message})");
            }
        }
    }

    private async Task ReadData(Stream stream, byte requestId)
    {
        // Read id(1 byte) and length (4 bytes big endian)
        var buffer = new byte[5];
        var read = await stream.ReadAsync(buffer);
        if (read != buffer.Length)
        {
            throw new Exception("Failed to read data");
        }

        var id = buffer[0];
        if (id != requestId)
        {
            throw new Exception("Invalid response id");
        }

        var lengthBytes = buffer[1..];
        var length = BitConverter.ToUInt32(lengthBytes);

        var data = new byte[length];
        read = await stream.ReadAsync(data);
        if (read != length)
        {
            throw new Exception("Failed to read data");
        }

        var jsonString = Encoding.UTF8.GetString(data);

        try
        {
            EyeData? eyeDataValue = JsonSerializer.Deserialize<EyeData>(jsonString);
            if (!eyeDataValue.HasValue)
            {
                throw new Exception("Failed to deserialize data");
            }

            var eyeData = eyeDataValue.Value;
            OnData?.Invoke(eyeData);
        }
        catch (Exception e)
        {
            _logger.LogError($"Failed to deserialize data ({e.Message}): {jsonString}");
            throw;
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _client?.Close();
    }

    public bool IsConnected()
    {
        return  _client?.Connected ?? false;
    }
}