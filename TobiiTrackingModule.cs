using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using VRCFaceTracking;
using VRCFaceTracking.Core.Types;

namespace VRCFT_Tobii_Advanced;

public class TobiiTrackingModule : ExtTrackingModule
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern bool SetDllDirectory(string? lpPathName);

    private Tobii.Api? _tobii;
    private Tobii.Device? _device;

    public override (bool SupportsEye, bool SupportsExpression) Supported => (true, false);

    public override (bool eyeSuccess, bool expressionSuccess) Initialize(bool eyeAvailable,
        bool expressionAvailable)
    {
        Logger.LogInformation("Initializing module...");

        string? assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (assemblyPath == null)
        {
            Logger.LogError("Failed to get assembly path!");
            return new ValueTuple<bool, bool>(false, false);
        }

        Logger.LogInformation("Adding assembly path to DLL search path: " + assemblyPath);
        SetDllDirectory(assemblyPath);

        Logger.LogInformation("Initializing Tobii...");

        try
        {
            _tobii = new Tobii.Api(Logger);
            IEnumerable<string> dev = _tobii.EnumerateDevices();
            if (!dev.Any())
            {
                Logger.LogError("No devices found!");
                return (false, false);
            }

            Logger.LogInformation(dev.Aggregate("Found devices: ", (current, d) => current + d + ", "));

            var licensePath = Path.Combine(assemblyPath, "license.json");
            bool hasLicense = File.Exists(licensePath);

            if (hasLicense)
            {
                Logger.LogInformation("Loading license...");

                var license = File.ReadAllText(licensePath);
                _device = _tobii.CreateDevice(dev.First(), license);
            }
            else
            {
                Logger.LogInformation($"No license found in {licensePath}, using default license...");

                _device = _tobii.CreateDevice(dev.First());
            }

            Logger.LogInformation("Done.");

            return (true, false);
        }
        catch (Exception e)
        {
            Logger.LogError("Error initializing Tobii: " + e.Message);
        }

        return (false, false);
    }

    private double _minValidPupilDiameterMm = 999f;

    public override void Update()
    {
        if (_device == null)
        {
            return;
        }

        _device.Update();

        var data = _device.GetEyeData();

        UnifiedTracking.Data.Eye.Left.Gaze = data.Left.GlazeDirectionIsValid ? data.Left.GlazeDirection : Vector2.zero;
        UnifiedTracking.Data.Eye.Right.Gaze =
            data.Right.GlazeDirectionIsValid ? data.Right.GlazeDirection : Vector2.zero;

        UnifiedTracking.Data.Eye.Left.Openness = data.Left.IsBlinkingIsValid ? (data.Left.IsBlink ? 0f : 1f) : 1f;
        UnifiedTracking.Data.Eye.Right.Openness = data.Right.IsBlinkingIsValid ? (data.Right.IsBlink ? 0f : 1f) : 1f;

        UnifiedTracking.Data.Eye.Left.PupilDiameter_MM =
            data.Left.PupilDiameterIsValid ? data.Left.PupilDiameterMm : 0f;
        UnifiedTracking.Data.Eye.Right.PupilDiameter_MM =
            data.Right.PupilDiameterIsValid ? data.Right.PupilDiameterMm : 0f;

        // Overwrite the minimum pupil diameter, since if the headset is removed, VRCFT will set it to 0
        // it will no longer be updated, even if the headset is put on again.
        // So I'll overwrite the min diameter it is used independently every update, if it is valid and greater
        // than the minimum threshold.
        const float minPupilDiameterThreshold = 1f;
        if (data.Left.PupilDiameterIsValid
            && data.Right.PupilDiameterIsValid
            && data.Left.PupilDiameterMm > minPupilDiameterThreshold
            && data.Right.PupilDiameterMm > minPupilDiameterThreshold
           )
        {
            _minValidPupilDiameterMm = Math.Min(_minValidPupilDiameterMm, (data.Left.PupilDiameterMm + data.Right.PupilDiameterMm) / 2.0);
        }

        if (data.Left.PupilDiameterIsValid || data.Right.PupilDiameterIsValid)
        {
            UnifiedTracking.Data.Eye._minDilation = (float) _minValidPupilDiameterMm;
        }
    }

    public override void Teardown()
    {
        _device?.Dispose();
        _tobii?.Dispose();
    }
}