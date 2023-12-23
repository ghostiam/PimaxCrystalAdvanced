using System.Runtime.InteropServices;
using Tobii.StreamEngine;

namespace VRCFT_Tobii_Advanced.Tobii.Wearable;

public class WearableAdvanced : ITobiiEyeData
{
    private readonly nint _device;
    private bool _isSubscribed;

    public WearableAdvanced(nint device)
    {
        _device = device;
    }

    public Action<EyeData>? OnData { get; set; }

    public void Subscribe()
    {
        var ptr = GCHandle.Alloc(this);

        var res =
            Interop.tobii_wearable_advanced_data_subscribe(_device, UpdateData, GCHandle.ToIntPtr(ptr));
        if (res != tobii_error_t.TOBII_ERROR_NO_ERROR)
        {
            throw new Exception("Subscribe to Tobii device: " + res);
        }

        _isSubscribed = true;
    }

    public void Unsubscribe()
    {
        _isSubscribed = false;

        var res = Interop.tobii_wearable_advanced_data_unsubscribe(_device);
        if (res != tobii_error_t.TOBII_ERROR_NO_ERROR)
        {
            throw new Exception("Unsubscribe from Tobii device: " + res);
        }
    }

    public void Update()
    {
        var res = Interop.tobii_wait_for_callbacks(new[] { _device });
        if (res == tobii_error_t.TOBII_ERROR_TIMED_OUT)
        {
            return;
        }

        if (res != tobii_error_t.TOBII_ERROR_NO_ERROR)
        {
            throw new Exception("Wait for callbacks: " + res);
        }

        res = Interop.tobii_device_process_callbacks(_device);
        if (res != tobii_error_t.TOBII_ERROR_NO_ERROR)
        {
            throw new Exception("Process callbacks: " + res);
        }
    }

    private static void UpdateData(ref tobii_wearable_advanced_data_t data, nint userData)
    {
        var dataLeft = data.left;
        var left = new EyeData.Eye
        {
            GazeDirectionIsValid = dataLeft.gaze_direction_validity == tobii_validity_t.TOBII_VALIDITY_VALID,
            GazeDirection = new EyeData.Vector2(
                -dataLeft.gaze_direction_normalized_xyz.x,
                dataLeft.gaze_direction_normalized_xyz.y
            ),
            PupilDiameterIsValid = dataLeft.pupil_diameter_validity == tobii_validity_t.TOBII_VALIDITY_VALID,
            PupilDiameterMm = dataLeft.pupil_diameter_mm,
            OpennessIsValid = dataLeft.blink_validity == tobii_validity_t.TOBII_VALIDITY_VALID,
            Openness = dataLeft.blink == tobii_state_bool_t.TOBII_STATE_BOOL_TRUE ? 0f : 1f,
        };

        var dataRight = data.right;
        var right = new EyeData.Eye
        {
            GazeDirectionIsValid = dataRight.gaze_direction_validity == tobii_validity_t.TOBII_VALIDITY_VALID,
            GazeDirection = new EyeData.Vector2(
                -dataRight.gaze_direction_normalized_xyz.x,
                dataRight.gaze_direction_normalized_xyz.y
            ),
            PupilDiameterIsValid = dataRight.pupil_diameter_validity == tobii_validity_t.TOBII_VALIDITY_VALID,
            PupilDiameterMm = dataRight.pupil_diameter_mm,
            OpennessIsValid = dataRight.blink_validity == tobii_validity_t.TOBII_VALIDITY_VALID,
            Openness = dataRight.blink == tobii_state_bool_t.TOBII_STATE_BOOL_TRUE ? 0f : 1f,
        };

        var target = GCHandle.FromIntPtr(userData).Target;
        if (target is WearableAdvanced dev)
        {
            dev.OnData?.Invoke(new EyeData(left, right));
        }
    }

    public void Dispose()
    {
        if (_isSubscribed)
        {
            Unsubscribe();
        }
    }
}