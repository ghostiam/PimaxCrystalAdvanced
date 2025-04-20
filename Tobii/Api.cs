using Microsoft.Extensions.Logging;
using Tobii.StreamEngine;

namespace TobiiAdvanced.Tobii;

public class Api : IDisposable
{
    private readonly ILogger _logger;
    private readonly nint _api;

    public Api(ILogger logger)
    {
        _logger = logger;

        var tobiiLog = new tobii_custom_log_t
        {
            log_func = delegate(nint _, tobii_log_level_t level, string text)
            {
                var logLevel = level switch
                {
                    tobii_log_level_t.TOBII_LOG_LEVEL_ERROR => LogLevel.Error,
                    tobii_log_level_t.TOBII_LOG_LEVEL_WARN => LogLevel.Warning,
                    tobii_log_level_t.TOBII_LOG_LEVEL_INFO => LogLevel.Information,
                    tobii_log_level_t.TOBII_LOG_LEVEL_DEBUG => LogLevel.Debug,
                    tobii_log_level_t.TOBII_LOG_LEVEL_TRACE => LogLevel.Trace,
                    _ => LogLevel.Debug
                };

                logger.Log(logLevel, text);
            }
        };

        var res = Interop.tobii_api_create(out _api, tobiiLog);
        if (res != tobii_error_t.TOBII_ERROR_NO_ERROR)
        {
            throw new Exception("Create API: " + res);
        }
    }

    public List<string> EnumerateDevices()
    {
        var res = Interop.tobii_enumerate_local_device_urls(_api, out var urls);
        if (res != tobii_error_t.TOBII_ERROR_NO_ERROR)
        {
            throw new Exception("Enumerate devices: " + res);
        }

        return urls;
    }

    public Device CreateDevice(string url)
    {
        return new Device(_logger, _api, url);
    }

    public Device CreateDevice(string url, string license)
    {
        return new Device(_logger, _api, url, license);
    }

    public void Dispose()
    {
        var res = Interop.tobii_api_destroy(_api);
        if (res != tobii_error_t.TOBII_ERROR_NO_ERROR)
        {
            throw new Exception("Destroy API: " + res);
        }
    }
}