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

    private float _minValidPupilDiameterMm;

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

        // Update min dilation because it can be invalid.
        const float minPupilDiameterThreshold = 1f;
        if (data.Left.PupilDiameterIsValid
            && data.Right.PupilDiameterIsValid
            && data.Left.PupilDiameterMm > minPupilDiameterThreshold
            && data.Right.PupilDiameterMm > minPupilDiameterThreshold
           )
        {
            _minValidPupilDiameterMm = Math.Min(_minValidPupilDiameterMm,
                Math.Min(data.Left.PupilDiameterMm, data.Right.PupilDiameterMm));
        }

        if (UnifiedTracking.Data.Eye._minDilation < _minValidPupilDiameterMm)
        {
            if (Math.Abs(UnifiedTracking.Data.Eye._minDilation - _minValidPupilDiameterMm) > 0.01f)
            {
                Logger.LogInformation(
                    $"Min dilation changed from {UnifiedTracking.Data.Eye._minDilation} to {_minValidPupilDiameterMm}!");
            }

            UnifiedTracking.Data.Eye._minDilation = _minValidPupilDiameterMm;
        }

#if DEBUG
        Logger.LogDebug($"Left: {UnifiedTracking.Data.Eye.Left.Gaze} {UnifiedTracking.Data.Eye.Left.Openness} {UnifiedTracking.Data.Eye.Left.PupilDiameter_MM}");
        Logger.LogDebug($"Right: {UnifiedTracking.Data.Eye.Right.Gaze} {UnifiedTracking.Data.Eye.Right.Openness} {UnifiedTracking.Data.Eye.Right.PupilDiameter_MM}");
        Logger.LogDebug($"Min dilation: {UnifiedTracking.Data.Eye._minDilation}");
#endif
    }

    public override void Teardown()
    {
        _device?.Dispose();
        _tobii?.Dispose();
    }
}