using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace VRCFT_Tobii_Advanced.Tobii;

public class Client : IDisposable
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern bool SetDllDirectory(string? lpPathName);

    private readonly ILogger _logger;
    private Api? _tobii;
    private Device? _device;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public event Action<EyeData>? OnData;

    public Client(ILogger logger)
    {
        _logger = logger;
    }

    public bool Connect()
    {
        var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (assemblyPath == null)
        {
            _logger.LogError("Failed to get assembly path!");
            return false;
        }

        _logger.LogInformation($"Adding assembly path to DLL search path: {assemblyPath}");
        SetDllDirectory(assemblyPath);

        _logger.LogInformation("Initializing Tobii...");

        try
        {
            _tobii = new Api(_logger);
            IEnumerable<string> dev = _tobii.EnumerateDevices();
            if (!dev.Any())
            {
                _logger.LogError("No devices found!");
                return false;
            }

            _logger.LogInformation(dev.Aggregate("Found devices: ", (current, d) => current + d + ", "));

            var licensePath = Path.Combine(assemblyPath, "license.json");
            bool hasLicense = File.Exists(licensePath);

            var firstUrl = dev.First();
            _logger.LogInformation($"Connecting to first device: {firstUrl}...");

            if (hasLicense)
            {
                _logger.LogInformation("Loading license...");

                var license = File.ReadAllText(licensePath);
                _device = _tobii.CreateDevice(firstUrl, license);
            }
            else
            {
                _logger.LogInformation($"No license found in {licensePath}, using default license...");

                _device = _tobii.CreateDevice(firstUrl);

            }

            Task.Run(HandleAsyncData, _cancellationTokenSource.Token);

            _device.OnData += data =>
            {
                OnData?.Invoke(data);
            };

            _logger.LogInformation("Done.");

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError($"Failed to initialize Tobii: {e.Message}");
        }

        return false;
    }

    private async void HandleAsyncData()
    {
        while (true)
        {
            if (_device == null)
            {
                continue;
            }

            try
            {
                _device.Update();
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed to get eye data: {e.Message}");
                return;
            }

            const int delay = 1000 / 120; // 120hz update rate
            await Task.Delay(TimeSpan.FromMicroseconds(delay));
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _device?.Dispose();
        _tobii?.Dispose();
    }
}