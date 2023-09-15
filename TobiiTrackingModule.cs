using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Tobii.StreamEngine;
using VRCFaceTracking;
using VRCFaceTracking.Core.Types;

namespace VRCFT.Tobii.Advanced;

public class TobiiAdvancedModule : ExtTrackingModule
{
    private TobiiInterop? _tobii;
    private TobiiDevice? _device;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern bool SetDllDirectory(string? lpPathName);

    public override (bool SupportsEye, bool SupportsExpression) Supported => (true, false);

    public override (bool eyeSuccess, bool expressionSuccess) Initialize(bool eyeAvailable,
        bool expressionAvailable)
    {
        LoggerExtensions.LogInformation(Logger, "Initializing...");

        string? assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (assemblyPath == null)
        {
            LoggerExtensions.LogError(Logger, "Failed to get assembly path!");
            return new ValueTuple<bool, bool>(false, false);
        }

        LoggerExtensions.LogInformation(Logger, "Adding assembly path to DLL search path: " + assemblyPath);
        SetDllDirectory(assemblyPath);

        LoggerExtensions.LogInformation(Logger, "Initializing Tobii...");
        bool success = false;
        try
        {
            _tobii = new TobiiInterop(Logger);
            IEnumerable<string> dev = _tobii.EnumerateDevices();
            if (!dev.Any())
            {
                LoggerExtensions.LogError(Logger, "No devices found!");
                return (false, false);
            }

            LoggerExtensions.LogInformation(Logger,
                dev.Aggregate("Found devices: ", (current, d) => current + d + ", "));

            var licensePath = Path.Combine(assemblyPath, "tobii.license");
            bool hasLicense = File.Exists(licensePath);

            if (hasLicense)
            {
                LoggerExtensions.LogInformation(Logger, "Loading license...");

                var license = File.ReadAllText(licensePath);
                _device = _tobii.CreateDevice(dev.First(), license);
            }
            else
            {
                LoggerExtensions.LogWarning(Logger, $"No license found in {licensePath}, using default...");

                _device = _tobii.CreateDevice(dev.First());
            }

            success = true;
        }
        catch (Exception e)
        {
            LoggerExtensions.LogError(Logger, "Error while initializing Tobii: " + e.Message);
        }

        return (success, false);
    }

    public override void Update()
    {
        if (_device == null)
        {
            return;
        }

        _device.Update();

        var v = _device.LatestData.gaze_direction_combined_normalized_xyz;
        // var r = v;
        // var l = v;

        var l = _device.LatestData.left.gaze_direction_normalized_xyz;
        var r = _device.LatestData.right.gaze_direction_normalized_xyz;

        UnifiedTracking.Data.Eye.Left.Gaze = new Vector2(-l.x, l.y);
        UnifiedTracking.Data.Eye.Left.Openness =
            ((_device.LatestData.left.blink == tobii_state_bool_t.TOBII_STATE_BOOL_TRUE) ? 0f : 1f);

        if (_device.LatestData.left.pupil_diameter_mm > 1f)
        {
            UnifiedTracking.Data.Eye.Left.PupilDiameter_MM = _device.LatestData.left.pupil_diameter_mm;
        }

        UnifiedTracking.Data.Eye.Right.Gaze = new Vector2(-r.x, r.y);
        UnifiedTracking.Data.Eye.Right.Openness =
            ((_device.LatestData.right.blink == tobii_state_bool_t.TOBII_STATE_BOOL_TRUE) ? 0f : 1f);
        if (_device.LatestData.right.pupil_diameter_mm > 1f)
        {
            UnifiedTracking.Data.Eye.Right.PupilDiameter_MM = _device.LatestData.right.pupil_diameter_mm;
        }

        if (UnifiedTracking.Data.Eye._minDilation < 1.5f)
        {
            UnifiedTracking.Data.Eye._minDilation = 1.5f;
        }

        // Logger.LogInformation(
        //     $"{UnifiedTracking.Data.Eye._minDilation} {UnifiedTracking.Data.Eye._maxDilation} {UnifiedTracking.Data.Eye.
        //         Combined().PupilDiameter_MM}");
    }

    public override void Teardown()
    {
        _device?.Unsubscribe();
        _device?.Dispose();
        _tobii?.Dispose();
    }
}