using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Tobii.StreamEngine;

namespace TobiiAdvanced.Tobii;

public class Device : IDisposable
{
    private readonly nint _device;
    private readonly ITobiiEyeData? _wearable;

    public event Action<EyeData>? OnData;

    public Device(ILogger logger, nint api, string deviceUrl, string license = "")
    {
        if (license != "")
        {
            logger.LogInformation("Creating device with license.");

            var licenseResults = new List<tobii_license_validation_result_t>();

            var res = Interop.tobii_device_create_ex(api, deviceUrl,
                Interop.tobii_field_of_use_t.TOBII_FIELD_OF_USE_INTERACTIVE,
                new[] { license }, licenseResults,
                out _device);
            if (res != tobii_error_t.TOBII_ERROR_NO_ERROR || _device == nint.Zero)
            {
                throw new Exception("Failed to create tobii device: " + res);
            }

            if (licenseResults.Count > 0 &&
                licenseResults[0] == tobii_license_validation_result_t.TOBII_LICENSE_VALIDATION_RESULT_OK)
            {
                logger.LogInformation("Subscribed to advanced data.");

                _wearable = new WearableAdvanced(_device);
                _wearable.OnData += data => OnData?.Invoke(data);
                _wearable.Subscribe();
                return;
            }

            logger.LogWarning("License validation failed: " + licenseResults[0]);
        }
        else
        {
            var res = Interop.tobii_device_create(api, deviceUrl,
                Interop.tobii_field_of_use_t.TOBII_FIELD_OF_USE_INTERACTIVE, out _device);
            if (res != tobii_error_t.TOBII_ERROR_NO_ERROR || _device == nint.Zero)
            {
                throw new Exception("Failed to create tobii device: " + res);
            }
        }

        logger.LogInformation("Subscribed to consumer data.");

        _wearable = new WearableConsumer(_device);
        _wearable.Subscribe();
    }

    public void Update()
    {
        _wearable?.Update();
    }

    public void Dispose()
    {
        _wearable?.Dispose();

        tobii_error_t res = Interop.tobii_device_destroy(_device);
        if (res != tobii_error_t.TOBII_ERROR_NO_ERROR)
        {
            throw new Exception("Failed to destroyed tobii device: " + res);
        }
    }
}
