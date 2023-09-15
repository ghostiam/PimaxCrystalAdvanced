using System.Runtime.InteropServices;
using Tobii.StreamEngine;

namespace VRCFT.Tobii.Advanced;

public class TobiiDevice : IDisposable
{
    private readonly IntPtr _device;
    private bool _subscribed;
    public tobii_wearable_advanced_data_t LatestData { get; private set; }

    public TobiiDevice(IntPtr api, string deviceUrl)
    {
        tobii_error_t res = Interop.tobii_device_create(api, deviceUrl,
            Interop.tobii_field_of_use_t.TOBII_FIELD_OF_USE_INTERACTIVE, out _device);
        if (res != tobii_error_t.TOBII_ERROR_NO_ERROR || _device == IntPtr.Zero)
        {
            throw new Exception("Failed to create tobii device with error code " + res.ToString());
        }

        Subscribe();
    }

    public TobiiDevice(IntPtr api, string deviceUrl, string license)
    {
        var licenseResults = new List<tobii_license_validation_result_t>();

        tobii_error_t res = Interop.tobii_device_create_ex(api, deviceUrl,
            Interop.tobii_field_of_use_t.TOBII_FIELD_OF_USE_INTERACTIVE,
            new[] { license }, licenseResults,
            out _device);
        if (res != tobii_error_t.TOBII_ERROR_NO_ERROR || _device == IntPtr.Zero)
        {
            throw new Exception("Failed to create tobii device with error code " + res.ToString());
        }

        if (licenseResults[0] == tobii_license_validation_result_t.TOBII_LICENSE_VALIDATION_RESULT_OK)
        {
            throw new Exception("Failed to validate license " + licenseResults[0].ToString());
        }

        Subscribe();
    }

    private static void UpdateAdvanced(ref tobii_wearable_advanced_data_t data, IntPtr userData)
    {
        var target = GCHandle.FromIntPtr(userData).Target;
        if (target is TobiiDevice dev)
        {
            dev.LatestData = data;
        }
    }

    public void Subscribe()
    {
        GCHandle ptr = GCHandle.Alloc(this);
        IntPtr device = _device;
        tobii_error_t res =
            Interop.tobii_wearable_advanced_data_subscribe(device, UpdateAdvanced, GCHandle.ToIntPtr(ptr));
        if (res != tobii_error_t.TOBII_ERROR_NO_ERROR)
        {
            throw new Exception("Failed to subscribe to tobii device with error code " + res.ToString());
        }

        _subscribed = true;
    }

    public void Unsubscribe()
    {
        _subscribed = false;
        tobii_error_t res = Interop.tobii_wearable_advanced_data_unsubscribe(_device);
        if (res != tobii_error_t.TOBII_ERROR_NO_ERROR)
        {
            throw new Exception("Failed to unsubscribe from tobii device with error code " + res.ToString());
        }
    }

    public void Update()
    {
        tobii_error_t resp = Interop.tobii_wait_for_callbacks(new[] { _device });
        if (resp == tobii_error_t.TOBII_ERROR_TIMED_OUT)
        {
            return;
        }

        if (resp != tobii_error_t.TOBII_ERROR_NO_ERROR)
        {
            throw new Exception("Failed to wait for callbacks with error code " + resp.ToString());
        }

        resp = Interop.tobii_device_process_callbacks(_device);
        if (resp != tobii_error_t.TOBII_ERROR_NO_ERROR)
        {
            throw new Exception("Failed " + resp.ToString());
        }
    }

    public void Dispose()
    {
        if (_subscribed)
        {
            Unsubscribe();
        }

        tobii_error_t res = Interop.tobii_device_destroy(_device);
        if (res != tobii_error_t.TOBII_ERROR_NO_ERROR)
        {
            throw new Exception("Failed to destroy tobii device with error code " + res.ToString());
        }
    }
}