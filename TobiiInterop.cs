using Microsoft.Extensions.Logging;
using Tobii.StreamEngine;

namespace VRCFT.Tobii.Advanced;

public class TobiiInterop : IDisposable
{
    private readonly IntPtr _api;

    public TobiiInterop(ILogger logger)
    {
        var tobiiLog = new tobii_custom_log_t
        {
            log_func = delegate(IntPtr context, tobii_log_level_t level, string text)
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

        tobii_error_t res = Interop.tobii_api_create(out _api, tobiiLog);
        if (res != tobii_error_t.TOBII_ERROR_NO_ERROR)
        {
            throw new Exception("Failed to create tobii API with error code " + res.ToString());
        }
    }

    public List<string> EnumerateDevices()
    {
        List<string> urls;
        var res = Interop.tobii_enumerate_local_device_urls(_api, out urls);
        if (res != tobii_error_t.TOBII_ERROR_NO_ERROR)
        {
            throw new Exception("Failed to enumerate devices with error code " + res.ToString());
        }

        return urls;
    }

    public TobiiDevice? CreateDevice(string url)
    {
        return new TobiiDevice(_api, url);
    }

    public TobiiDevice? CreateDevice(string url, string license)
    {
        return new TobiiDevice(_api, url, license);
    }

    public void Dispose()
    {
        tobii_error_t res = Interop.tobii_api_destroy(_api);
        if (res != tobii_error_t.TOBII_ERROR_NO_ERROR)
        {
            throw new Exception("Failed to destroy tobii API with error code " + res.ToString());
        }
    }
}