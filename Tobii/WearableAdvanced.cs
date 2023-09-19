using System.Runtime.InteropServices;
using Tobii.StreamEngine;
using VRCFaceTracking.Core.Types;

namespace VRCFT_Tobii_Advanced.Tobii;

public class WearableAdvanced : IWearable
{
    private readonly nint _device;
    private bool _isSubscribed;
    private EyeData _eyeData;

    public WearableAdvanced(nint device)
    {
        _device = device;
    }

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

    public EyeData GetEyeData()
    {
        return _eyeData;
    }

    private static void UpdateData(ref tobii_wearable_advanced_data_t data, nint userData)
    {
        var dataLeft = data.left;
        var left = new EyeData.Eye
        {
            GlazeDirectionIsValid = dataLeft.gaze_direction_validity == tobii_validity_t.TOBII_VALIDITY_VALID,
            GlazeDirection = new Vector2(-dataLeft.gaze_direction_normalized_xyz.x,
                dataLeft.gaze_direction_normalized_xyz.y),
            PupilDiameterIsValid = dataLeft.pupil_diameter_validity == tobii_validity_t.TOBII_VALIDITY_VALID,
            PupilDiameterMm = dataLeft.pupil_diameter_mm,
            IsBlinkingIsValid = dataLeft.blink_validity == tobii_validity_t.TOBII_VALIDITY_VALID,
            IsBlink = dataLeft.blink == tobii_state_bool_t.TOBII_STATE_BOOL_TRUE,
        };

        var dataRight = data.right;
        var right = new EyeData.Eye
        {
            GlazeDirectionIsValid = dataRight.gaze_direction_validity == tobii_validity_t.TOBII_VALIDITY_VALID,
            GlazeDirection = new Vector2(-dataRight.gaze_direction_normalized_xyz.x,
                dataRight.gaze_direction_normalized_xyz.y),
            PupilDiameterIsValid = dataRight.pupil_diameter_validity == tobii_validity_t.TOBII_VALIDITY_VALID,
            PupilDiameterMm = dataRight.pupil_diameter_mm,
            IsBlinkingIsValid = dataRight.blink_validity == tobii_validity_t.TOBII_VALIDITY_VALID,
            IsBlink = dataRight.blink == tobii_state_bool_t.TOBII_STATE_BOOL_TRUE,
        };

        var target = GCHandle.FromIntPtr(userData).Target;
        if (target is WearableAdvanced dev)
        {
            dev._eyeData = new EyeData
            {
                Left = left,
                Right = right,
            };
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