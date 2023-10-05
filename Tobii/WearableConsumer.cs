using System.Runtime.InteropServices;
using Tobii.StreamEngine;

namespace VRCFT_Tobii_Advanced.Tobii.Wearable;

public class WearableConsumer : IWearable
{
    private readonly nint _device;
    private bool _isSubscribed;

    public WearableConsumer(nint device)
    {
        _device = device;
    }

    public Action<EyeData>? OnData { get; set; }

    public void Subscribe()
    {
        var ptr = GCHandle.Alloc(this);

        var res =
            Interop.tobii_wearable_consumer_data_subscribe(_device, UpdateData, GCHandle.ToIntPtr(ptr));
        if (res != tobii_error_t.TOBII_ERROR_NO_ERROR)
        {
            throw new Exception("Subscribe to Tobii device: " + res);
        }

        _isSubscribed = true;
    }

    public void Unsubscribe()
    {
        _isSubscribed = false;

        var res = Interop.tobii_wearable_consumer_data_unsubscribe(_device);
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

    private static void UpdateData(ref tobii_wearable_consumer_data_t data, nint userData)
    {
        var left = new EyeData.Eye
        {
            GazeDirectionIsValid = data.gaze_direction_combined_validity == tobii_validity_t.TOBII_VALIDITY_VALID,
            GazeDirection = new EyeData.Vector2(
                -data.gaze_direction_combined_normalized_xyz.x,
                data.gaze_direction_combined_normalized_xyz.y
            ),
            OpennessIsValid = data.left.blink_validity == tobii_validity_t.TOBII_VALIDITY_VALID,
            Openness = data.left.blink == tobii_state_bool_t.TOBII_STATE_BOOL_TRUE ? 0f : 1f,
        };

        var right = new EyeData.Eye
        {
            GazeDirectionIsValid = data.gaze_direction_combined_validity == tobii_validity_t.TOBII_VALIDITY_VALID,
            GazeDirection = new EyeData.Vector2(
                -data.gaze_direction_combined_normalized_xyz.x,
                data.gaze_direction_combined_normalized_xyz.y
            ),
            OpennessIsValid = data.right.blink_validity == tobii_validity_t.TOBII_VALIDITY_VALID,
            Openness = data.right.blink == tobii_state_bool_t.TOBII_STATE_BOOL_TRUE ? 0f : 1f,
        };

        var target = GCHandle.FromIntPtr(userData).Target;
        if (target is WearableConsumer dev)
        {
            {
                dev.OnData?.Invoke(new EyeData(left, right));
            }
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