using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using VRCFaceTracking;
using VRCFaceTracking.Core.Params.Data;
using VRCFaceTracking.Core.Types;

namespace PimaxCrystalAdvanced;

public class PimaxCrystalAdvanced : ExtTrackingModule
{
    private BrokenEye.Client? _beClient;
    private Tobii.Client? _tobiiClient;

    public override (bool SupportsEye, bool SupportsExpression) Supported => (true, false);

    public override (bool eyeSuccess, bool expressionSuccess) Initialize(bool eyeAvailable,
        bool expressionAvailable)
    {
        Logger.LogInformation("Initializing module...");

        Logger.LogInformation("Try use BrokenEye API...");
        _beClient = new BrokenEye.Client(Logger);
        if (_beClient.Connect("127.0.0.1", 5555))
        {
            Logger.LogInformation("Connected to Broken Eye server!");

            _beClient.OnData += UpdateEyeData;

            return (true, false);
        }

        Logger.LogInformation("Failed to connect to Broken Eye server...");

        Logger.LogInformation("Try use Tobii API...");
        _tobiiClient = new Tobii.Client(Logger);
        if (_tobiiClient.Connect())
        {
            Logger.LogInformation("Connected to Tobii API!");

            _tobiiClient.OnData += UpdateEyeData;

            return (true, false);
        }

        Logger.LogInformation("Failed to connect to Tobii...");

        return (false, false);
    }

    private readonly Channel<EyeData> _eyeDataChannel = Channel.CreateUnbounded<EyeData>();

    private void UpdateEyeData(EyeData data)
    {
        _eyeDataChannel.Writer.TryWrite(data);
    }

    private double _minValidPupilDiameterMm = 999f;

    public override void Update()
    {
        var task = _eyeDataChannel.Reader.ReadAsync().AsTask();
        // We block the loop and wait for data, since a wasted spinning loop eats up a lot of CPU.
        task.Wait(TimeSpan.FromMilliseconds(100));

        var data = task.Result;

        if (data.Left.GazeDirectionIsValid)
            UnifiedTracking.Data.Eye.Left.Gaze = data.Left.GazeDirection.ToVRCFT().FlipXCoordinates();

        if (data.Right.GazeDirectionIsValid)
            UnifiedTracking.Data.Eye.Right.Gaze = data.Right.GazeDirection.ToVRCFT().FlipXCoordinates();

        UnifiedTracking.Data.Eye.Left.Openness = data.Left.OpennessIsValid ? data.Left.Openness : 1f;
        UnifiedTracking.Data.Eye.Right.Openness = data.Right.OpennessIsValid ? data.Right.Openness : 1f;

        if (data.Left.PupilDiameterIsValid)
            UnifiedTracking.Data.Eye.Left.PupilDiameter_MM = data.Left.PupilDiameterMm;

        if (data.Right.PupilDiameterIsValid)
            UnifiedTracking.Data.Eye.Right.PupilDiameter_MM = data.Right.PupilDiameterMm;

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
            _minValidPupilDiameterMm = Math.Min(_minValidPupilDiameterMm,
                (data.Left.PupilDiameterMm + data.Right.PupilDiameterMm) / 2.0);
        }

        if (data.Left.PupilDiameterIsValid || data.Right.PupilDiameterIsValid)
        {
            UnifiedTracking.Data.Eye._minDilation = (float)_minValidPupilDiameterMm;
        }

        UpdateEyeExpressions(ref UnifiedTracking.Data.Shapes, data);
    }

    private void UpdateEyeExpressions(ref UnifiedExpressionShape[] data, EyeData external)
    {
        data[3].Weight = external.Left.Wide;
        data[2].Weight = external.Right.Wide;
        data[1].Weight = external.Left.Squeeze;
        data[0].Weight = external.Right.Squeeze;
        data[9].Weight = external.Left.Wide;
        data[11].Weight = external.Left.Wide;
        data[8].Weight = external.Right.Wide;
        data[10].Weight = external.Right.Wide;
        data[5].Weight = external.Left.Squeeze;
        data[7].Weight = external.Left.Squeeze;
        data[4].Weight = external.Right.Squeeze;
        data[6].Weight = external.Right.Squeeze;
    }

    private static Vector2 ToVrcftVector2(EyeData.Vector2 v)
    {
        return new Vector2(v.X, v.Y);
    }

    public override void Teardown()
    {
        _beClient?.Dispose();
        _tobiiClient?.Dispose();
    }
}
